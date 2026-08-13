using System.Drawing;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public sealed partial class VseDataflowGenerator(ILogger<VseDataflowGenerator> logger) : IDeviceDataflowGenerator
{
    private const string ContainerNameAlarms = "Alarms";
    private const string ContainerNameCounters = "Counters";
    private const string ContainerNameInputs = "Inputs";
    private const string ContainerNameObjects = "Objects";
    private const string ContainerNameProcessData = "ProcessData";
    private const string ContainerNameRawData = "RawData";
    private const string ContainerNameVariants = "Variants";

    public Type DeviceType => typeof(DeviceTreeVseDevice);

    public DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
                                                                      Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations)
    {
        var result = new DeviceDataflowGeneratorResult();
        var vseDevice = (DeviceTreeVseDevice)device;
        var processDataContainer = builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameProcessData);

        var alarmsNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.Alarms);
        if (alarmsNode is not null)
        {
            var alarms = alarmsNode.Children.OfType<DeviceTreeVseAlarm>();
            AddVseAlarms(builder, dataflow, processDataContainer, vseDevice, alarms.Where(o => o.Children.Any(c => enabledDataIds.TryGetValue(c.Id, out var enabled) && enabled)), device.Url, result);
        }

        var countersNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.Counters);
        if (countersNode is not null)
        {
            var counters = countersNode.Children.OfType<DeviceTreeVseCounter>();
            AddVseCounters(builder, dataflow, processDataContainer, vseDevice, counters.Where(n => n.Children.Any(c => enabledDataIds.TryGetValue(c.Id, out var enabled) && enabled)), device.Url, result);
        }

        var inputsNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.Inputs)?.Children.FirstOrDefault(c => c.Name == NodeNames.External);
        if (inputsNode is not null)
        {
            var inputs = inputsNode.Children.OfType<DeviceTreeVseInput>();
            AddVseInputs(builder, dataflow, processDataContainer, vseDevice, inputs.Where(o => o.Children.Any(c => enabledDataIds.TryGetValue(c.Id, out var enabled) && enabled)), device.Url, result);
        }

        var objectsNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.Objects);
        if (objectsNode is not null)
        {
            var objects = objectsNode.Children.OfType<DeviceTreeVseObject>();
            AddVseObjects(builder, dataflow, processDataContainer, vseDevice, objects.Where(o => o.Children.Any(c => enabledDataIds.TryGetValue(c.Id, out var enabled) && enabled)), device.Url, result);
        }

        var rawDataNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.RawData);
        if (rawDataNode is not null)
        {
            var sensors = rawDataNode.Children.OfType<DeviceTreeVseRawData>();
            AddVseRawDataSensors(builder, dataflow, processDataContainer, device.Url, result, cloudNames, blobLoggingConfigurations);
        }

        var variantsNode = vseDevice.Children.FirstOrDefault(c => c.Name == NodeNames.Variants) as DeviceTreeVseVariants;
        if (variantsNode is not null)
        {
            AddVseVariants(builder, dataflow, processDataContainer, variantsNode, vseDevice, device.Url, result);
        }

        return result;
    }

    public DeviceTreeFunctionblockResult GenerateGetDeviceTreeFunctionblock(ClusterBuilder builder, Dataflow dataflow, string address)
    {
        AddVseDeviceTreeSubscriber(builder, dataflow, address, out var deviceTreeTrigger, out var deviceTreeOutput);

        return new DeviceTreeFunctionblockResult
        {
            DeviceTreeOutput = deviceTreeOutput,
            DeviceTreeTrigger = deviceTreeTrigger,
        };
    }

    private static FunctionBlock AddSensorFb(ClusterBuilder builder, Dataflow dataflow, Uri deviceUri, Container container, DeviceTreeVseRawData sensor, Guid dataGroupIdentifier, string cloudName)
    {
        var sensorFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.VseRawDataSubscriber.DesignId, $"{sensor.Name}-{cloudName}",
            container, 0, FunctionBlocks.DefaultVerticalSeparation);

        var rawDataSettings = sensor.RawDataConfigurations[dataGroupIdentifier];
        var rawDataSettingIds = FunctionBlocks.VseRawDataSubscriber.Settings;

        builder.Editors.Setting.SetFunctionBlockSetting(sensorFb, rawDataSettingIds.Address, deviceUri.GetVseAddress());
        builder.Editors.Setting.SetFunctionBlockSetting(sensorFb, rawDataSettingIds.SensorIndex, sensor.Index);
        builder.Editors.Setting.SetFunctionBlockSetting(sensorFb, rawDataSettingIds.SamplingRate, rawDataSettings.Frequency);
        builder.Editors.Setting.SetFunctionBlockSetting(sensorFb, rawDataSettingIds.Duration, rawDataSettings.Duration);
        builder.Editors.Setting.SetFunctionBlockSetting(sensorFb, rawDataSettingIds.IgnoreTimedValueArray, true);

        return sensorFb;
    }

    private void AddVseAlarms(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, DeviceTreeVseDevice vseDevice, IEnumerable<DeviceTreeVseAlarm> alarms, Uri deviceUri, DeviceDataflowGeneratorResult result)
    {
        var container = builder.Editors.Container.AddContainer(processDataContainer, ContainerNameAlarms, null, new Point { Y = 2 * FunctionBlocks.DefaultVerticalSeparation });
        var alarmFbY = 0;

        foreach (var alarm in alarms)
        {
            var alarmFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.VseAlarmSubscriber.DesignId, $"{alarm.Name}-{alarm.Alias ?? string.Empty}",
                container, new Point { X = 0, Y = alarmFbY, });

            alarmFbY += FunctionBlocks.DefaultVerticalSeparation;

            builder.Editors.Setting.SetFunctionBlockSetting(alarmFb, FunctionBlocks.VseAlarmSubscriber.Settings.Address, deviceUri.GetVseAddress());
            builder.Editors.Setting.SetFunctionBlockSetting(alarmFb, FunctionBlocks.VseAlarmSubscriber.Settings.Path, alarm.Path);

            foreach (var child in alarm.Children.OfType<DeviceTreeProcessData>())
            {
                var output = alarmFb.GetOutputByDesignId(VseAlarmSubscriberOutputs.Instance.NumericValue);
                var uiOutput = alarmFb.GetOutputByDesignId(VseAlarmSubscriberOutputs.Instance.Value);
                var availableOutput = alarmFb.GetOutputByDesignId(VseAlarmSubscriberOutputs.Instance.Available);
                builder.Editors.Connector.SetEventEnabled(false, availableOutput);

                if (output is null)
                {
                    LogNoOutConSubscriber(logger, "alarm", child.Name);
                    continue;
                }

                builder.Editors.Connector.SetEventEnabled(true, uiOutput);
                result.DataOutputs[child.Id] = GetOutputInfo(child, alarm.Name, output, null, availableOutput, (aggregationFunction, compressionGrid) => IdentifierHelper.GetVseAlarmIdentifier(vseDevice, alarm, child, aggregationFunction, compressionGrid));

                result.OutputMapping.Add(new ValueMappingEntry
                {
                    ProcessDataId = child.Id,
                    ValueOutputIdLogging = output.Id,
                    ValueOutputIdUI = uiOutput.Id,
                });
            }
        }
    }

    private void AddVseCounters(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, DeviceTreeVseDevice vseDevice, IEnumerable<DeviceTreeVseCounter> counters, Uri deviceUri, DeviceDataflowGeneratorResult result)
    {
        var container = builder.Editors.Container.AddContainer(processDataContainer, ContainerNameCounters, null, new Point { Y = FunctionBlocks.DefaultVerticalSeparation });
        var counterFbY = 0;

        foreach (var counter in counters)
        {
            var counterFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.VseCounterSubscriber.DesignId, $"{counter.Name}-{counter.Alias ?? string.Empty}",
            container, new Point { X = 0, Y = counterFbY, });
            counterFbY += FunctionBlocks.DefaultVerticalSeparation;

            builder.Editors.Setting.SetFunctionBlockSetting(counterFb, FunctionBlocks.VseCounterSubscriber.Settings.Address, deviceUri.GetVseAddress());
            builder.Editors.Setting.SetFunctionBlockSetting(counterFb, FunctionBlocks.VseCounterSubscriber.Settings.Path, counter.Path);
            builder.Editors.Connector.SetEventEnabled(true, counterFb.ProcessDataOutputs.ToArray());

            var availableOutput = counterFb.GetOutputByDesignId(FunctionBlocks.VseCounterSubscriber.Outputs.Available);
            builder.Editors.Connector.SetEventEnabled(false, availableOutput);

            foreach (var child in counter.Children.OfType<IDeviceTreeCompressableDataNode>())
            {
                // Die Outputs sind genauso benannt wie die Datenpunkte im Baum
                var output = counterFb.GetOutputByName(child.Name);
                if (output is null)
                {
                    LogNoOutConSubscriber(logger, "counter", child.Name);
                    continue;
                }

                result.DataOutputs[child.Id] = GetOutputInfo(child, counter.Name, output, null, availableOutput, (aggregationFunction, compressionGrid) => IdentifierHelper.GetVseCounterIdentifier(vseDevice, counter, child, aggregationFunction, compressionGrid));

                result.OutputMapping.Add(new ValueMappingEntry
                {
                    ProcessDataId = child.Id,
                    UnitOutputId = child.Name == "Value" ? counterFb.GetOutputByDesignId(FunctionBlocks.VseCounterSubscriber.Outputs.Unit).Id : null,
                    ValueOutputIdLogging = output.Id,
                    ValueOutputIdUI = output.Id,
                });
            }
        }
    }

    private static void AddVseDeviceTreeSubscriber(ClusterBuilder builder, Dataflow dataflow, string address, out Guid deviceTreeTrigger, out Guid deviceTreeOutput)
    {
        var uri = new UriBuilder(address).Uri;
        var addressFormatted = uri.GetVseAddress();

        var subscriber = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.VseDeviceTreeSubscriber.DesignId,
            $"TreeSubscriber {addressFormatted}", null, new Point { Y = FunctionBlocks.DefaultVerticalSeparation * -1 });

        builder.Editors.Setting.SetFunctionBlockSetting(subscriber, FunctionBlocks.VseDeviceTreeSubscriber.Settings.Url, addressFormatted);

        var triggerInput = subscriber.GetInputByDesignId(FunctionBlocks.VseDeviceTreeSubscriber.Inputs.Trigger);
        var deviceTreeOuptut = subscriber.GetOutputByDesignId(FunctionBlocks.VseDeviceTreeSubscriber.Outputs.DeviceTree);

        builder.Editors.Connector.SetValue(triggerInput, true);
        builder.Editors.Connector.SetEventEnabled(true, triggerInput);
        builder.Editors.Connector.SetEventEnabled(true, deviceTreeOuptut);
        builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(triggerInput, false);
        deviceTreeTrigger = triggerInput.Id;
        deviceTreeOutput = deviceTreeOuptut.Id;
    }

    private void AddVseInputs(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, DeviceTreeVseDevice vseDevice, IEnumerable<DeviceTreeVseInput> inputs, Uri deviceUri, DeviceDataflowGeneratorResult result)
    {
        var container = builder.Editors.Container.AddContainer(processDataContainer, ContainerNameInputs, null, new Point { Y = FunctionBlocks.DefaultVerticalSeparation * 3 });
        var inputFbY = 0;

        foreach (var input in inputs)
        {
            var inputFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.VseInputSubscriber.DesignId,
                $"{input.Name}-{input.Alias ?? string.Empty}", container, new Point { Y = inputFbY });

            inputFbY += FunctionBlocks.DefaultVerticalSeparation;
            var subscriberSettingIds = FunctionBlocks.VseInputSubscriber.Settings;

            builder.Editors.Setting.SetFunctionBlockSetting(inputFb, subscriberSettingIds.Address, deviceUri.GetVseAddress());
            builder.Editors.Setting.SetFunctionBlockSetting(inputFb, subscriberSettingIds.Path, input.Path);
            builder.Editors.Connector.SetEventEnabled(true, inputFb.ProcessDataOutputs.ToArray());

            var availableOutput = inputFb.GetOutputByDesignId(FunctionBlocks.VseInputSubscriber.Outputs.Available);
            builder.Editors.Connector.SetEventEnabled(false, availableOutput);

            foreach (var child in input.Children.OfType<IDeviceTreeCompressableDataNode>())
            {
                // Die Outputs sind genauso benannt wie die Datenpunkte im Baum
                var output = inputFb.GetOutputByName(child.Name);
                if (output is null)
                {
                    LogNoOutConSubscriber(logger, "input", child.Name);
                    continue;
                }

                if (child.IsWriteable)
                {
                    var inputInput = inputFb.GetInputByName(child.Name);

                    if (inputInput is null)
                        LogNoInConSubscriber(logger, "input", child.Name);
                    else
                        builder.Editors.Connector.SetEventEnabled(true, inputInput);
                }

                result.DataOutputs[child.Id] = GetOutputInfo(child, input.Name, output, null, availableOutput, (aggregationFunction, compressionGrid) => IdentifierHelper.GetVseInputIdentifier(vseDevice, input, child, aggregationFunction, compressionGrid, "External"));

                result.OutputMapping.Add(new ValueMappingEntry
                {
                    ProcessDataId = child.Id,
                    UnitOutputId = child.Name == "Value" ? inputFb.GetOutputByDesignId(FunctionBlocks.VseInputSubscriber.Outputs.Unit).Id : null,
                    ValueOutputIdLogging = output.Id,
                    ValueOutputIdUI = output.Id,
                });
            }
        }
    }

    private void AddVseObjects(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, DeviceTreeVseDevice vseDevice, IEnumerable<DeviceTreeVseObject> objects, Uri deviceUri, DeviceDataflowGeneratorResult result)
    {
        var container = builder.Editors.Container.AddContainer(processDataContainer, ContainerNameObjects);
        var objectFbY = 0;

        foreach (var obj in objects)
        {
            var objectFb = builder.Editors.Container.AddFunctionBlock(dataflow,
                                            FunctionBlocks.VseObjectSubscriber.DesignId,
                                            $"{obj.Name}-{obj.Alias ?? string.Empty}",
                                            container,
                                            new Point
                                            {
                                                X = 0,
                                                Y = objectFbY,
                                            });

            var objectSubscriberOutputIds = FunctionBlocks.VseObjectSubscriber.Outputs;
            var validOutput = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.Valid);
            objectFbY += FunctionBlocks.DefaultVerticalSeparation + 200;

            builder.Editors.Setting.SetFunctionBlockSetting(objectFb, FunctionBlocks.VseObjectSubscriber.Settings.Address, deviceUri.GetVseAddress());
            builder.Editors.Setting.SetFunctionBlockSetting(objectFb, FunctionBlocks.VseObjectSubscriber.Settings.Path, obj.Path);

            builder.Editors.Connector.SetEventEnabled(true, objectFb.ProcessDataOutputs.ToArray());
            builder.Editors.Connector.SetEventEnabled(false,
                [ objectFb.GetOutputByDesignId(objectSubscriberOutputIds.ErrorState),
                  objectFb.GetOutputByDesignId(objectSubscriberOutputIds.RotationalFrequencyTuple),
                  objectFb.GetOutputByDesignId(objectSubscriberOutputIds.Valid),]);

            var rotSpeedOutput = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.RotSpeed);
            var refValueOutput = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.RefValue);
            var rotationalFrequencyTupleOutput = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.RotationalFrequencyTuple);
            var availableOutput = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.Available);
            builder.Editors.Connector.SetEventEnabled(false, availableOutput);

            result.RotationalFrequencyOutputs[obj.Id] = new()
            {
                RefValue = refValueOutput,
                RotationalFrequencyTuple = rotationalFrequencyTupleOutput,
                RotSpeed = rotSpeedOutput,
            };

            foreach (var child in obj.Children.OfType<IDeviceTreeCompressableDataNode>())
            {
                // Die Outputs sind genauso benannt wie die Datenpunkte im Baum
                var output = objectFb.GetOutputByName(child.Name);
                if (output is null)
                {
                    LogNoOutConSubscriber(logger, "object", child.Name);
                    continue;
                }

                Guid? unitOutputId = null;

                if (child.Name is "Average" or "Maximum" or "Minimum" or "Damage" or "Warning")
                    unitOutputId = objectFb.GetOutputByDesignId(FunctionBlocks.VseObjectSubscriber.Outputs.Unit).Id;

                result.OutputMapping.Add(new ValueMappingEntry
                {
                    ProcessDataId = child.Id,
                    UnitOutputId = unitOutputId,
                    ValueOutputIdLogging = output.Id,
                    ValueOutputIdUI = output.Id,
                });

                result.DataOutputs[child.Id] = GetOutputInfo(child,
                                                             obj.Name,
                                                             output,
                                                             validOutput,
                                                             availableOutput,
                                                             (aggregationFunction, compressionGrid) => IdentifierHelper.GetVseObjectIdentifier(vseDevice, obj, child, aggregationFunction, compressionGrid));
            }

            result.ErrorStateOutputs[obj.Id] = objectFb.GetOutputByDesignId(objectSubscriberOutputIds.ErrorState);
        }
    }

    private void AddVseRawDataSensors(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, Uri deviceUri,
                                      DeviceDataflowGeneratorResult result, Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations)
    {
        result.RawDataContainer = builder.Editors.Container.AddSubContainer(dataflow, ContainerNameRawData, processDataContainer, 0, FunctionBlocks.DefaultVerticalSeparation);

        foreach (var nodeBlobLoggingConfigurations in blobLoggingConfigurations.GroupBy(c => c.Node))
        {
            if (nodeBlobLoggingConfigurations.Key is not DeviceTreeVseRawData sensor)
            {
                throw new ArgumentException($"{nodeBlobLoggingConfigurations.Key.Id} is not a {nameof(DeviceTreeVseRawData)}");
            }

            RawDataInfo rawDataInfo = new();

            result.RawData[sensor.Id] = rawDataInfo;

            foreach (var configuration in nodeBlobLoggingConfigurations.GroupBy(c => c.DataGroupIdentifier))
            {
                var sensorFb = AddSensorFb(builder, dataflow, deviceUri, result.RawDataContainer, sensor,
                    configuration.Key, cloudNames[configuration.Key]);

                if (configuration.Any(c => c.NeedsScheduler))
                {
                    rawDataInfo.SchedulerSensors[configuration.Key] = new()
                    {
                        MeasurementOutput = sensorFb.GetOutputByDesignId(FunctionBlocks.VseRawDataSubscriber.Outputs.Data),
                        TriggerInput = sensorFb.GetInputByDesignId(FunctionBlocks.VseRawDataSubscriber.Inputs.Trigger),
                    };
                }

                if (configuration.Any(c => c.NeedsEventTrigger))
                {
                    rawDataInfo.EventTriggerSensors[configuration.Key] = new()
                    {
                        EventTriggerInput = sensorFb.GetInputByDesignId(FunctionBlocks.VseRawDataSubscriber.Inputs.ObjectTrigger),
                        MeasurementOutput = sensorFb.GetOutputByDesignId(FunctionBlocks.VseRawDataSubscriber.Outputs.Data),
                    };
                }
            }
        }
    }

    private void AddVseVariants(ClusterBuilder builder, Dataflow dataflow, Container processDataContainer, DeviceTreeVseVariants variant, DeviceTreeVseDevice vseDevice, Uri deviceUri, DeviceDataflowGeneratorResult result)
    {
        var container = builder.Editors.Container.AddSubContainer(dataflow, ContainerNameVariants, processDataContainer, 0, FunctionBlocks.DefaultVerticalSeparation);
        var variantFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.VseVariantSubscriber.DesignId, variant.Name, container);

        builder.Editors.Setting.SetFunctionBlockSetting(variantFb, FunctionBlocks.VseVariantSubscriber.Settings.Address, deviceUri.GetVseAddress());
        builder.Editors.Setting.SetFunctionBlockSetting(variantFb, FunctionBlocks.VseVariantSubscriber.Settings.Path, variant.Path);
        builder.Editors.Connector.SetEventEnabled(true, variantFb.ProcessDataOutputs.ToArray());
        var availableOutput = variantFb.GetOutputByDesignId(FunctionBlocks.VseVariantSubscriber.Outputs.Available);
        builder.Editors.Connector.SetEventEnabled(false, availableOutput);

        foreach (var child in variant.Children.OfType<IDeviceTreeCompressableDataNode>())
        {
            // Die Outputs sind genauso benannt wie die Datenpunkte im Baum
            var output = variantFb.GetOutputByName(child.Name);
            if (output is null)
            {
                LogNoOutConSubscriber(logger, "variant", child.Name);
                continue;
            }

            if (child.IsWriteable)
            {
                var inputInput = variantFb.GetInputByName(child.Name);

                if (inputInput is null)
                    LogNoInConSubscriber(logger, "variant", child.Name);
                else
                    builder.Editors.Connector.SetEventEnabled(true, inputInput);
            }

            result.DataOutputs[child.Id] = GetOutputInfo(child, "Variant", output, null, availableOutput, (aggregationFunction, compressionGrid) => IdentifierHelper.GetVseVariantIdentifier(vseDevice, aggregationFunction, compressionGrid));

            result.OutputMapping.Add(new ValueMappingEntry
            {
                ProcessDataId = child.Id,
                ValueOutputIdLogging = output.Id,
                ValueOutputIdUI = output.Id,
            });
        }
    }

    private static DataOutputInfo GetOutputInfo(IDeviceTreeCompressableDataNode child, string parentName, ConnectorOutput output, ConnectorOutput? validOutput, ConnectorOutput availableOutput,
                                                Func<AggregationFunction, int, string> getDatpointIdentifier)
    {
        var outputInfo = new DataOutputInfo
        {
            AvailableOutput = availableOutput,
            Output = output,
            Suffix = $"{parentName} {child.Name}",
            ValidOutput = validOutput,
        };

        foreach (var compressorConfig in child.CompressorConfigurations)
        {
            outputInfo.DataPointIdentifiers[compressorConfig.DataGroupIdentifier] = getDatpointIdentifier(compressorConfig.Aggregation, compressorConfig.CompressionTime);
        }

        return outputInfo;
    }
}
