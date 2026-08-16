using System.Drawing;
using ClusterManagement.Public.Iodds;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Public.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class IoLinkDataflowGenerator(IIoddStore ioddStore) : IDeviceDataflowGenerator
{
    private const string ContainerNameProcessData = "ProcessData";

    public Type DeviceType => typeof(DeviceTreeIoLinkMaster);

    private static FunctionBlock AddBlobDataFb(ClusterBuilder builder, Dataflow dataflow, Container container, DeviceTreeDevice device, DeviceTreeIoLinkMasterPort port,
        string fbName, string connectionIdentifier)
    {
        var subscriberFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.BlobSubscriber.DesignId, fbName, container, 0, FunctionBlocks.DefaultVerticalSeparation);

        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.Identifier, connectionIdentifier);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.VendorId, device.VendorId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.DeviceId, device.DeviceId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.ProductName, device.Name);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.PortIndex, (ushort)port.SubIndex);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, FunctionBlocks.BlobSubscriber.Settings.IgnoreTimedValueArray, true);

        return subscriberFb;
    }

    private static FunctionBlock AddProcessDataFb(ClusterBuilder builder, Dataflow dataflow, Container parent, DeviceTreeProcessData processData, DeviceTreeDevice device,
        int port, string fbName, string connectionIdentifier, out ConnectorOutput unitOutput, out ConnectorOutput valueOutputUi,
        out ConnectorOutput? valueOutputLogging, out ConnectorOutput availableOutput)
    {
        var requiredDesignId = GetRequiredIoTSubscriberDesignIds(processData);

        var subscriberFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, requiredDesignId.FunctionBlock, fbName, parent, 0, FunctionBlocks.DefaultVerticalSeparation);

        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.Identifier, connectionIdentifier);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.VendorId, device.VendorId);
        builder.Editors.Setting.SetFunctionBlockSetting(subscriberFb, requiredDesignId.Settings.DeviceId, device.DeviceId);
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

    private void GenerateBlobData(ClusterBuilder builder, Dataflow dataflow, DeviceContainerManager containerManager, Dictionary<Guid, string> cloudNames, DeviceDataflowGeneratorResult result,
        DeviceTreeIoLinkMaster ioLinkMaster, BlobLoggingConfiguration[] blobLoggingConfigurations, string connectionIdentifier)
    {
        var allNodes = ioLinkMaster.GetNodeAndDescendants().ToArray();
        var portNodes = allNodes.OfType<DeviceTreeIoLinkMasterPort>().ToArray();

        foreach (var nodeBlobLoggingConfigurations in blobLoggingConfigurations.GroupBy(c => c.Node))
        {
            if (nodeBlobLoggingConfigurations.Key is not DeviceTreeBlobData sensor)
            {
                throw new ArgumentException($"{nodeBlobLoggingConfigurations.Key.Id} is not a {nameof(DeviceTreeBlobData)}");
            }

            RawDataInfo rawDataInfo = new();

            result.RawData[nodeBlobLoggingConfigurations.Key.Id] = rawDataInfo;

            var device = allNodes.OfType<DeviceTreeDevice>().First(d => d.Children.Contains(nodeBlobLoggingConfigurations.Key));
            var ioLinkPort = portNodes.First(p => p.Children.Contains(device));

            foreach (var configuration in nodeBlobLoggingConfigurations.GroupBy(c => c.DataGroupIdentifier))
            {
                var container = containerManager.GetParentContainer(nodeBlobLoggingConfigurations.Key);
                var fbName = GetBlobDataName(nodeBlobLoggingConfigurations.Key.Name, cloudNames[configuration.Key]);
                var blobFb = AddBlobDataFb(builder, dataflow, container, device, ioLinkPort, fbName, connectionIdentifier);

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
                                                                      Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations, string connectionIdentifier)
    {
        var result = new DeviceDataflowGeneratorResult();
        var ioLinkMaster = (DeviceTreeIoLinkMaster)device;
        var processDataContainer = builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameProcessData);
        var deviceContainerManager = new DeviceContainerManager(device, processDataContainer, builder);

        var relevantNodesTuples = GetDataNodesRecursively((DeviceTreeIoLinkMaster)device)
                                    .Where(n => !n.IOLinkDevice?.IsUnknown ?? false)
                                    .Where(n => enabledDataIds[n.Node.Id])
                                    .ToArray();

        // Kinder von Sensoren ohne IODD werden ignoriert, um den IoTCore zu entlasten
        foreach (var (ioLinkDevice, node, ioLinkPort) in relevantNodesTuples)
        {
            if (node is DeviceTreeProcessData processData)
            {
                GenerateProcessData(builder, dataflow, deviceContainerManager, result, ioLinkMaster, ioLinkDevice, node, ioLinkPort, processData, connectionIdentifier);
            }
        }

        GenerateBlobData(builder, dataflow, deviceContainerManager, cloudNames, result, ioLinkMaster, blobLoggingConfigurations, connectionIdentifier);

        return result;
    }

    public DeviceTreeFunctionBlockResult GenerateDeviceTreeSourceFunctionBlock(ClusterBuilder builder, Dataflow dataflow, string address, string connectionIdentifier)
    {
        var uri = new UriBuilder(address).Uri;
        var configurationFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.IoTCoreConfiguration.DesignId, $"IoTCoreConfiguration {uri.DnsSafeHost}:{uri.Port}", null, new Point { Y = FunctionBlocks.DefaultVerticalSeparation * -1 });

        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.Identifier, connectionIdentifier);
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.Address, address);
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.Username, string.Empty);
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.Password, string.Empty);
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.IoddDirectory, ioddStore.IoddDirectory);
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.IoddAutoDownload, ioddStore.GetAutoDownloadIodds());
        builder.Editors.Setting.SetFunctionBlockSetting(configurationFb, FunctionBlocks.IoTCoreConfiguration.Settings.UseGetDataMulti, true);

        var triggerInput = configurationFb.GetInputByDesignId(FunctionBlocks.IoTCoreConfiguration.Inputs.RebuildDeviceTree);
        var deviceTreeOutput = configurationFb.GetOutputByDesignId(FunctionBlocks.IoTCoreConfiguration.Outputs.DeviceTree);

        builder.Editors.Connector.SetValue(triggerInput, true);
        builder.Editors.Connector.SetEventEnabled(true, triggerInput);
        builder.Editors.Connector.SetEventEnabled(true, deviceTreeOutput);
        builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(triggerInput, false);

        return new DeviceTreeFunctionBlockResult
        {
            DeviceTreeOutput = deviceTreeOutput.Id,
            DeviceTreeTrigger = triggerInput.Id,
        };
    }

    private static void GenerateProcessData(ClusterBuilder builder, Dataflow dataflow, DeviceContainerManager containerManager, DeviceDataflowGeneratorResult result, DeviceTreeIoLinkMaster ioLinkMaster, DeviceTreeDevice device,
        IDeviceTreeDataNode node, DeviceTreeIoLinkMasterPort ioLinkPort, DeviceTreeProcessData processData, string connectionIdentifier)
    {
        var parentContainer = containerManager.GetParentContainer(processData);
        _ = AddProcessDataFb(builder, dataflow, parentContainer, processData, device, ioLinkPort.SubIndex, processData.Name, connectionIdentifier,
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
                outputInfo.DataPointIdentifiers[compressorConfiguration.DataGroupIdentifier] = IdentifierHelper.GetIoLinkIdentifier(ioLinkMaster, ioLinkPort, device, processData, compressorConfiguration.Aggregation, compressorConfiguration.CompressionTime);
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
                if (!node.DataType.SupportsLiveView &&
                    !node.DataType.SupportsLogging)
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
            DataType.Blob => throw new ArgumentException($"Datatype {nameof(DataType.Blob)} is no supported process datatype."),
            DataType.Flag => booleanSubscriber,
            DataType.Whole => doubleSubscriber,
            DataType.UnsignedWhole => doubleSubscriber,
            DataType.Real => doubleSubscriber,
            DataType.Text => stringSubscriber,
            DataType.Unknown => throw new ArgumentException($"Datatype {nameof(DataType.Unknown)} is no supported process datatype."),
            DataType.Octets => throw new ArgumentException($"Datatype {nameof(DataType.Octets)} is no supported process datatype."),
            _ => throw new ArgumentException($"Datatype {processData.DataType} is no supported process datatype."),
        };
    }

}
