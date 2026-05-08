using System.Drawing;
using ClusterManagement.Public.Iodds;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Public.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class IoLinkDataflowGenerator(IIoddStore ioddStore) : IDeviceDataflowGenerator
{
    private const string ContainerNameProcessData = "ProcessData";

    public Type DeviceType => typeof(DeviceTreeIoLinkMaster);

    private static FunctionBlock AddBlobDataFb(ClusterBuilder builder, Dataflow dataflow, DeviceTreeDevice device, DeviceTreeIoLinkMaster masterDevice, DeviceTreeIoLinkMasterPort port,
        string fbName, Container container)
    {
        var subscriberFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.BlobSubscriber.DesignId, fbName, container, 0, FunctionBlocks.DefaultVerticalSeparation);

        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.Address, $"{masterDevice.Url.DnsSafeHost}:{masterDevice.Url.Port}");
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.ApplicationSpecificTag, device.ApplicationSpecificTag ?? null);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.VendorId, device.VendorId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.DeviceId, device.DeviceId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.ProductName, device.Name);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.PortIndex, (ushort)port.SubIndex);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.IgnoreTimedValueArray, true);

        return subscriberFb;
    }

    private static FunctionBlock AddProcessDataFb(ClusterBuilder builder, Dataflow dataflow, DeviceTreeProcessData processData, DeviceTreeDevice device,
        DeviceTreeIoLinkMaster masterDevice, int port, string fbName, Container parent, out ConnectorOutput unitOutput, out ConnectorOutput valueOutputUi,
        out ConnectorOutput? valueOutputLogging, out ConnectorOutput availableOutput)
    {
        var requiredDesignId = GetRequiredIoTSubscriberDesignIds(processData);

        var subscriberFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, requiredDesignId.FunctionBlock, fbName, parent, 0, FunctionBlocks.DefaultVerticalSeparation);

        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.Address, $"{masterDevice.Url.DnsSafeHost}:{masterDevice.Url.Port}");
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.ApplicationSpecificTag, device.ApplicationSpecificTag);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.VendorId, device.VendorId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.DeviceId, device.DeviceId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.ProductName, device.Name);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.PortIndex, (ushort)port);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.ProcessDataInIndex, (ushort)processData.SubIndex);

        valueOutputUi = subscriberFb.GetOutputByDesignId(requiredDesignId.Outputs.Value);
        valueOutputLogging = null;

        if (requiredDesignId.Outputs.NumericValue is not null)
        {
            valueOutputLogging = subscriberFb.GetOutputByDesignId(requiredDesignId.Outputs.NumericValue.Value);
        }

        unitOutput = subscriberFb.GetOutputByDesignId(requiredDesignId.Outputs.Unit);
        availableOutput = subscriberFb.GetOutputByDesignId(requiredDesignId.Outputs.Available);

        unitOutput.EventEnabled = true;
        valueOutputUi.EventEnabled = true;

        return subscriberFb;
    }

    private void GenerateBlobData(ClusterBuilder builder, Dataflow dataflow, Dictionary<Guid, string> cloudNames, DeviceDataflowGeneratorResult result,
        DeviceTreeIoLinkMaster ioLinkMaster, BlobLoggingConfiguration[] blobLoggingConfigurations, DeviceContainerManager containerManager)
    {
        var allNodes = ioLinkMaster.GetNodeAndDescendants().ToArray();
        var portNodes = allNodes.OfType<DeviceTreeIoLinkMasterPort>().ToArray();

        foreach (var nodeBlobLoggingConfigurations in blobLoggingConfigurations.GroupBy(c => c.Node))
        {
            if (nodeBlobLoggingConfigurations.Key is not DeviceTreeBlobData sensor)
            {
                throw new ArgumentException($"{nodeBlobLoggingConfigurations.Key.Id} is not a {nameof(DeviceTreeVseRawData)}");
            }

            var rawDataInfo = new RawDataInfo
            {
                Unit = string.Empty,
            };

            result.RawData[nodeBlobLoggingConfigurations.Key.Id] = rawDataInfo;

            var device = allNodes.OfType<DeviceTreeDevice>().First(d => d.Children.Contains(nodeBlobLoggingConfigurations.Key));
            var ioLinkPort = portNodes.First(p => p.Children.Contains(device));

            foreach (var configuration in nodeBlobLoggingConfigurations.GroupBy(c => c.DataGroupIdentifier))
            {
                var container = containerManager.GetParentContainer(nodeBlobLoggingConfigurations.Key);
                var fbName = GetBlobDataName(nodeBlobLoggingConfigurations.Key.Name, cloudNames[configuration.Key]);
                var blobFb = AddBlobDataFb(builder, dataflow, device, ioLinkMaster, ioLinkPort, fbName, container);

                if (configuration.Any(c => c.NeedsScheduler))
                {
                    rawDataInfo.SchedulerSensors[configuration.Key] = new ScheduledRawDataInfo
                    {
                        MeasurementOutput = blobFb.GetOutputByDesignId(FunctionBlocks.BlobSubscriber.Outputs.Data),
                        TriggerInput = blobFb.GetInputByDesignId(FunctionBlocks.BlobSubscriber.Inputs.Trigger),
                    };
                }
            }
        }
    }

    public DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
                                                                      Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations)
    {
        var result = new DeviceDataflowGeneratorResult();
        var ioLinkMaster = (DeviceTreeIoLinkMaster)device;
        var processDataContainer = builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameProcessData);
        var deviceContainerManager = new DeviceContainerManager(device, processDataContainer, builder);
        deviceContainerManager.AddNameGeneration<DeviceTreeIoLinkMasterPort>(p => $"Port {p.SubIndex:00}");

        var relevantNodesTuples = GetDataNodesRecursively((DeviceTreeIoLinkMaster)device)
                                    .Where(n => !n.IOLinkDevice?.IsUnknown ?? false)
                                    .Where(n => enabledDataIds[n.Node.Id])
                                    .ToArray();

        // Kinder von Sensoren ohne IODD werden ignoriert, um den IoTCore zu entlasten
        foreach (var (ioLinkDevice, node, ioLinkPort) in relevantNodesTuples)
        {
            if (node is DeviceTreeProcessData processData)
            {
                GenerateProcessData(builder, dataflow, result, ioLinkMaster, ioLinkDevice, node, ioLinkPort, processData, deviceContainerManager);
            }
        }

        GenerateBlobData(builder, dataflow, cloudNames, result, ioLinkMaster, blobLoggingConfigurations, deviceContainerManager);

        return result;
    }

    public DeviceTreeFunctionblockResult GenerateGetDeviceTreeFunctionblock(ClusterBuilder builder, Dataflow dataflow, string address)
    {
        var uri = new UriBuilder(address).Uri;
        var subscriber = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.IoLinkDeviceTreeSubscriber.DesignId, $"TreeSubscriber {uri.DnsSafeHost}:{uri.Port}", null, new Point { Y = FunctionBlocks.DefaultVerticalSeparation * -1 });

        builder.Editors.Setting.SetFunctionBlockSetting(subscriber, FunctionBlocks.IoLinkDeviceTreeSubscriber.Settings.Url, address);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriber, FunctionBlocks.IoLinkDeviceTreeSubscriber.Settings.IoddDirectory, ioddStore.IoddDirectory);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriber, FunctionBlocks.IoLinkDeviceTreeSubscriber.Settings.IoddAutoDownload, ioddStore.GetAutoDownloadIodds());

        var triggerInput = subscriber.GetInputByDesignId(FunctionBlocks.IoLinkDeviceTreeSubscriber.Inputs.Trigger);
        var deviceTreeOutput = subscriber.GetOutputByDesignId(FunctionBlocks.IoLinkDeviceTreeSubscriber.Outputs.DeviceTree);

        builder.Editors.Connector.SetValue(triggerInput, true);
        builder.Editors.Connector.SetEventEnabled(true, triggerInput);
        builder.Editors.Connector.SetEventEnabled(true, deviceTreeOutput);
        builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(triggerInput, false);

        return new DeviceTreeFunctionblockResult
        {
            DeviceTreeOutput = deviceTreeOutput.Id,
            DeviceTreeTrigger = triggerInput.Id,
        };
    }

    private static void GenerateProcessData(ClusterBuilder builder, Dataflow dataflow, DeviceDataflowGeneratorResult result, DeviceTreeIoLinkMaster ioLinkMaster, DeviceTreeDevice device,
        IDeviceTreeDataNode node, DeviceTreeIoLinkMasterPort ioLinkPort, DeviceTreeProcessData processData, DeviceContainerManager containerManager)
    {
        var processDataName = GetSuffixProcessDataName(processData);
        var parentContainer = containerManager.GetParentContainer(processData);
        _ = AddProcessDataFb(builder, dataflow, processData, device, ioLinkMaster, ioLinkPort.SubIndex, processDataName, parentContainer,
            out var unitOutput, out var valueOutputUi, out var valueOutputLogging, out var availableOutput);

        result.OutputMapping.Add(new ValueMappingEntry
        {
            ProcessDataId = processData.Id,
            UnitOutputId = unitOutput.Id,
            ValueOutputIdLogging = valueOutputLogging?.Id ?? Guid.Empty,
            ValueOutputIdUI = valueOutputUi.Id,
        });

        if (valueOutputLogging is null)
        {
            return;
        }

        var outputInfo = new DataOutputInfo { AvailableOutput = availableOutput, Output = valueOutputLogging, Suffix = $"{ioLinkPort.Name} {device.Name} {processData.Name}" };

        if (node is IDeviceTreeCompressableDataNode compressableDataNode)
        {
            foreach (var compressorConfiguration in compressableDataNode.CompressorConfigurations)
            {
                outputInfo.DataPointIdentifiers[compressorConfiguration.DataGroupIdentifier] = IdentifierHelper.GetIoLinkIdentifier(ioLinkMaster, ioLinkPort, device, processData, compressorConfiguration.PoolingMode, compressorConfiguration.CompressionTime);
            }
        }

        result.DataOutputs[processData.Id] = outputInfo;
    }

    private static string GetBlobDataName(string nodeName, string cloud)
        => $"{nodeName}-{cloud}";

    private static IEnumerable<(DeviceTreeDevice IOLinkDevice, IDeviceTreeDataNode Node, DeviceTreeIoLinkMasterPort Port)> GetDataNodesRecursively(
        IDeviceTreeBase deviceTree, DeviceTreeDevice? device = null, DeviceTreeIoLinkMasterPort? port = null)
    {
        foreach (var child in deviceTree.Children)
        {
            if (child is IDeviceTreeDataNode node && device is not null && port is not null)
            {
                if (!node.DataType.SupportedForLiveView() &&
                    !node.DataType.SupportedForLogging())
                {
                    continue;
                }

                yield return (device, node, port);
                continue;
            }

            if (child is DeviceTreeIoLinkMasterPort masterDevicePort)
                port = masterDevicePort;
            else if (child is DeviceTreeDevice ioLinkDevice)
                device = ioLinkDevice;

            foreach (var current in GetDataNodesRecursively(child, device, port))
                yield return current;
        }
    }

    private static IoTSubscriberDesignTuple GetRequiredIoTSubscriberDesignIds(DeviceTreeProcessData processData)
    {
        var booleanSubscriber = new IoTSubscriberDesignTuple
        {
            FunctionBlock = FunctionBlocks.IoLinkBooleanSubscriber.DesignId,
            Outputs = FunctionBlocks.IoLinkBooleanSubscriber.Outputs,
            Settings = FunctionBlocks.IoLinkBooleanSubscriber.Settings,
        };

        var doubleSubscriber = new IoTSubscriberDesignTuple
        {
            FunctionBlock = FunctionBlocks.IoLinkDoubleSubscriber.DesignId,
            Outputs = FunctionBlocks.IoLinkDoubleSubscriber.Outputs,
            Settings = FunctionBlocks.IoLinkDoubleSubscriber.Settings,
        };

        var stringSubscriber = new IoTSubscriberDesignTuple
        {
            FunctionBlock = FunctionBlocks.IoLinkStringSubscriber.DesignId,
            Outputs = FunctionBlocks.IoLinkStringSubscriber.Outputs,
            Settings = FunctionBlocks.IoLinkStringSubscriber.Settings,
        };

        return processData.DataType switch
        {
            DataType.BlobT => throw new ArgumentException($"Datatype {nameof(DataType.BlobT)} is no supported process datatype."),
            DataType.BooleanT => booleanSubscriber,
            DataType.IntegerT => doubleSubscriber,
            DataType.UIntegerT => doubleSubscriber,
            DataType.Float32T => doubleSubscriber,
            DataType.StringT => stringSubscriber,
            DataType.Invalid => throw new ArgumentException($"Datatype {nameof(DataType.Invalid)} is no supported process datatype."),
            DataType.OctetStringT => throw new ArgumentException($"Datatype {nameof(DataType.OctetStringT)} is no supported process datatype."),
            _ => throw new ArgumentException($"Datatype {processData.DataType} is no supported process datatype."),
        };
    }

    private static string GetSuffixProcessDataName(DeviceTreeProcessData processData)
        => processData.Name;
}
