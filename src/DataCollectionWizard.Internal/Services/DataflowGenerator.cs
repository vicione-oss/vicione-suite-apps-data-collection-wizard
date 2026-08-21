using System.Drawing;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using DataCollectionWizard.Public.Extensions;
using Microsoft.Extensions.Logging;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace DataCollectionWizard.Internal.Services;

public sealed partial class DataflowGenerator(ClusterBuilder builder, ILogger logger, string machineIdentifier, List<IDeviceDataflowGenerator> deviceDataflowGenerators, List<ICloudDataflowGenerator> cloudDataflowGenerators, List<ICloudFilter> cloudFilters) : IDataflowGenerator
{
    private const string ChildContainerNamePrefixFormatter = "Formatter";
    private const string ContainerNameCompressors = "Compressors";
    private const string ContainerNameMoneoConnect = "moneoConnect";
    private const string ContainerNameSchedulers = "Schedulers";
    private const int ContainerSize = 20;

    // TODO: im Anna dataport yaml und attribut gleich schreiben

    private const string UnexpectedAggregationFunctionErrorMessage = "Encountered unexpected AggregationFunction.";
    private const string UnexpectedCombinationAggregationFunctionAndCloudErrorMessage = "Unexpected combination between AggregationFunction and Cloud connection occurred.";

    private const string WrongDesignIdErrorMessage = "FunctionBlock has wrong DesignId.";

    private static readonly Point s_schedulerContainerLocation = new(FunctionBlocks.DefaultHorizontalSeparation * -1, 0);
    private static readonly Guid s_designIdSystemDataPort = Guid.Parse("c7390e0f-761d-40f6-9112-a31216eac2c7");

    private ChildContainer? _moneoConnectContainer;
    private int _schedulerFbY;

    private static Guid[] GetActiveDataGroupIds(IDeviceTreeBase[] nodeAndDescendants)
        => [.. nodeAndDescendants
            .OfType<IDeviceTreeCompressableDataNode>()
            .SelectMany(d => d.CompressorConfigurations)
            .Where(c => c.Enabled)
            .Select(c => c.DataGroupIdentifier)
            .Union(nodeAndDescendants.OfType<IDeviceTreeSchedulableDataNode>()
                .SelectMany(d => d.SchedulerConfigurations)
                .Where(c => c.Enabled)
                .Select(c => c.DataGroupIdentifier))
            .Union(nodeAndDescendants.OfType<IDeviceTreeEventTriggerDataNode>()
                .SelectMany(d => d.EventTriggerConfigurations)
                .SelectMany(t => t.Triggers)
                .Where(t => t.Enabled && (t.OnWarning || t.OnDamage))
                .Select(t => t.DataGroupIdentifier))
            .Distinct()];

    private static IEnumerable<BlobLoggingConfiguration> GetBlobLoggingConfigurations(IEnumerable<IDeviceTreeBase> nodeAndDescendants, IEnumerable<Guid> enabledConfigs)
    {
        foreach (var schedulableDataNode in nodeAndDescendants.OfType<IDeviceTreeSchedulableDataNode>())
        {
            foreach (var schedulerConfig in schedulableDataNode.SchedulerConfigurations.Where(c => enabledConfigs.Contains(c.DataGroupIdentifier)))
            {
                if (schedulerConfig.Enabled && enabledConfigs.Contains(schedulerConfig.DataGroupIdentifier))
                {
                    yield return new BlobLoggingConfiguration()
                    {
                        DataGroupIdentifier = schedulerConfig.DataGroupIdentifier,
                        NeedsScheduler = true,
                        Node = schedulableDataNode,
                    };
                }
            }
        }

        foreach (var sensor in nodeAndDescendants.OfType<IDeviceTreeEventTriggerDataNode>())
        {
            foreach (var eventTriggerConfiguration in sensor.EventTriggerConfigurations)
            {
                foreach (var eventTrigger in eventTriggerConfiguration.Triggers.Where(t => t.Enabled && (t.OnWarning || t.OnDamage)))
                {
                    if (enabledConfigs.Contains(eventTrigger.DataGroupIdentifier))
                    {
                        yield return new BlobLoggingConfiguration()
                        {
                            DataGroupIdentifier = eventTrigger.DataGroupIdentifier,
                            NeedsEventTrigger = true,
                            Node = sensor,
                        };
                    }
                }
            }
        }
    }

    private static string GetCompressorFbName(IDeviceTreeBase node, CompressorConfiguration configuration)
        => $"{node.Name}-{configuration.Aggregation}-{configuration.CompressionTime}";

    private static Dictionary<ConnectorOutput, IEnumerable<IGrouping<EventTrigger, ErrorStateGuardTuple>>> GetErrorStateGuardGrouping(IEnumerable<ErrorStateGuardTuple> eventTriggerTuples)
        => eventTriggerTuples
            .GroupBy(t => t.ErrorStateOutput)
            .ToDictionary(g => g.Key, g => g.GroupBy(t => t.Configuration, EventTriggerIgnoreDataGroupEqualityComparer.Instance));

    private ConnectorOutput GetFloatConnector(Dataflow dataflow, string converterName, ConnectorOutput subscriberOutput, Dictionary<ConnectorOutput, ConnectorOutput> existingConvertedOutputs)
    {
        if (existingConvertedOutputs.TryGetValue(subscriberOutput, out var existingConvertedOutput))
            return existingConvertedOutput;

        var datatype = builder.DetermineValueType(subscriberOutput);
        var location = new Point
        {
            X = (subscriberOutput.FunctionBlock.X ?? 0) + FunctionBlocks.DefaultHorizontalSeparation,
            Y = subscriberOutput.FunctionBlock.Y ?? 0,
        };

        if (datatype == typeof(bool))
        {
            var boolToDoubleFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.BooleanToDouble.DesignId, converterName, subscriberOutput.FunctionBlock.Container, location);

            builder.Editors.Connector.AddLink(subscriberOutput, boolToDoubleFb.GetInputByDesignId(FunctionBlocks.BooleanToDouble.Inputs.Value));
            var boolConvertedOutput = boolToDoubleFb.GetOutputByDesignId(FunctionBlocks.BooleanToDouble.Outputs.Value);
            existingConvertedOutputs.Add(subscriberOutput, boolConvertedOutput);

            builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(boolConvertedOutput, false);
            builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(boolToDoubleFb.GetInputByDesignId(FunctionBlocks.BooleanToDouble.Inputs.Value), false);

            return boolConvertedOutput;
        }
        else if (datatype == typeof(long))
        {
            var longToDoubleFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.LongToDouble.DesignId, converterName, subscriberOutput.FunctionBlock.Container, location);

            builder.Editors.Connector.AddLink(subscriberOutput, longToDoubleFb.GetInputByDesignId(FunctionBlocks.LongToDouble.Inputs.Value));
            var longConvertedOutput = longToDoubleFb.GetOutputByDesignId(FunctionBlocks.LongToDouble.Outputs.Value);
            existingConvertedOutputs.Add(subscriberOutput, longConvertedOutput);

            builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(longConvertedOutput, false);
            builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(longToDoubleFb.GetInputByDesignId(FunctionBlocks.LongToDouble.Inputs.Value), false);

            return longConvertedOutput;
        }

        return subscriberOutput;
    }

    private static List<IDeviceTreeDataNode> GetLoggedRawDataNodes(IDeviceTreeBase[] allDeviceTreeNodes, Connection cloudConnection)
    {
        var dataNodes = allDeviceTreeNodes.OfType<IDeviceTreeSchedulableDataNode>()
                                              .Where(n => n.SchedulerConfigurations.Any(sc => sc.DataGroupIdentifier == cloudConnection.Id && sc.Enabled))
                                              .Cast<IDeviceTreeDataNode>();

        dataNodes = dataNodes.Union(allDeviceTreeNodes.OfType<IDeviceTreeEventTriggerDataNode>()
                                                      .Where(n => n.EventTriggerConfigurations.Any(etc => etc.IsSensorConfigured
                                                                                                       && etc.Triggers.Any(t => t.DataGroupIdentifier == cloudConnection.Id && t.Enabled))));

        return [.. dataNodes];
    }

    private static List<ProcessDataConfiguration> GetLoggedProcessDataNodes(IDeviceTreeBase[] allDeviceTreeNodes, Connection connection)
        => [.. allDeviceTreeNodes.OfType<IDeviceTreeCompressableDataNode>()
                             .Where(n => n.DataType.SupportsLogging && n.DataType.SupportsCompression)
                             .Select(n => n.CompressorConfigurations.FirstOrDefault(cc => cc.DataGroupIdentifier == connection.Id) is { Enabled: true } configuration
                                              ? new ProcessDataConfiguration(n, configuration)
                                              : null)
                             .OfType<ProcessDataConfiguration>()];

    private static string GetObjectTriggerGuardName(string vseObjectIdentifier, EventTrigger eventTriggerConfiguration)
        => $"Guard-{vseObjectIdentifier}-{(eventTriggerConfiguration.OnDamage ? "Damage" : string.Empty)}{(eventTriggerConfiguration.OnWarning ? "Warning" : string.Empty)}-{eventTriggerConfiguration.Delay}h";

    private static string GetRpmMinMaxTrackerFbName(string suffix, CompressorConfiguration configuration)
        => $"{suffix}-{configuration.Aggregation}-{configuration.CompressionTime}";

    private static string GetSchedulerFbName(SchedulerConfiguration configuration)
        => $"{string.Join(' ', configuration.Times.Keys.Select(k => k.ToString()[..3]))} {configuration.Times.First().Value.Length}x";

    private static void AddErrorStateGuard(ClusterBuilder builder, Dataflow dataflow, ConnectorOutput errorStateOutput,
        ConnectorOutput objectFbErrorStateOut, int guardIndex, EventTrigger eventTriggerConfiguration, IEnumerable<ErrorStateGuardTuple> errorStateTuples)
    {
        var location = new Point((errorStateOutput.FunctionBlock.X ?? 0) + FunctionBlocks.DefaultHorizontalSeparation + (guardIndex * FunctionBlocks.DefaultHorizontalSeparation),
                                 (errorStateOutput.FunctionBlock.Y ?? 0) + (guardIndex * FunctionBlocks.DefaultVerticalSeparation / 3));

        var guard = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.ErrorStateGuard.DesignId,
                                    GetObjectTriggerGuardName(errorStateOutput.FunctionBlock.Name, eventTriggerConfiguration),
                                    errorStateOutput.FunctionBlock.Container,
                                    location);

        builder.Editors.Setting.SetFunctionBlockSetting(guard, FunctionBlocks.ErrorStateGuard.Settings.Delay, (short)eventTriggerConfiguration.Delay);
        builder.Editors.Setting.SetFunctionBlockSetting(guard, FunctionBlocks.ErrorStateGuard.Settings.OnDamage, eventTriggerConfiguration.OnDamage);
        builder.Editors.Setting.SetFunctionBlockSetting(guard, FunctionBlocks.ErrorStateGuard.Settings.OnWarning, eventTriggerConfiguration.OnWarning);

        builder.Editors.Connector.AddLink(objectFbErrorStateOut, guard.GetInputByDesignId(FunctionBlocks.ErrorStateGuard.Inputs.ErrorState));

        foreach (var errorStateTuple in errorStateTuples)
            builder.Editors.Connector.AddLink(guard.GetOutputByDesignId(FunctionBlocks.ErrorStateGuard.Outputs.ErrorState), errorStateTuple.BlobSensorErrorStateTriggerInput, false);
    }

    private void ConnectCompressedSourceToCloud(FunctionBlock compressorFb, AggregationFunctionCloudInputs cloudInput, CompressorConfiguration compressorConfiguration)
    {
        if (compressorFb.DesignId != FunctionBlocks.IntervalStatistic.DesignId && compressorFb.DesignId != FunctionBlocks.RpmAtMinMaxTracker.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(compressorFb));

        var isDataCompressor = compressorFb.DesignId == FunctionBlocks.IntervalStatistic.DesignId;

        var compressorOutputs = FunctionBlocks.IntervalStatistic.Outputs;
        var minMaxTrackerOutputs = FunctionBlocks.RpmAtMinMaxTracker.Outputs;

        Guid[] outputIds = compressorConfiguration.Aggregation switch
        {
            AggregationFunction.Avg => isDataCompressor ? [compressorOutputs.Average] : [minMaxTrackerOutputs.Average],
            AggregationFunction.Last => isDataCompressor ? [compressorOutputs.Last] : throw new NotImplementedException(UnexpectedCombinationAggregationFunctionAndCloudErrorMessage),
            AggregationFunction.Max => isDataCompressor ? [compressorOutputs.Maximum] : [minMaxTrackerOutputs.Maximum],
            AggregationFunction.Min => isDataCompressor ? [compressorOutputs.Minimum] : [minMaxTrackerOutputs.Minimum],
            AggregationFunction.MinMaxAvg => isDataCompressor ? [compressorOutputs.Average, compressorOutputs.Maximum, compressorOutputs.Minimum]
                                                      : [minMaxTrackerOutputs.Average, minMaxTrackerOutputs.Maximum, minMaxTrackerOutputs.Minimum],
            _ => throw new NotImplementedException(UnexpectedAggregationFunctionErrorMessage),
        };

        CloudInput[] inputs = compressorConfiguration.Aggregation switch
        {
            AggregationFunction.Avg => [cloudInput.Avg],
            AggregationFunction.Last => [cloudInput.Last],
            AggregationFunction.Max => [cloudInput.Max],
            AggregationFunction.Min => [cloudInput.Min],
            AggregationFunction.MinMaxAvg => [cloudInput.Avg, cloudInput.Max, cloudInput.Min],
            _ => throw new NotImplementedException(UnexpectedAggregationFunctionErrorMessage),
        };

        for (var i = 0; i < outputIds.Length; i++)
        {
            inputs[i].Connect(compressorFb.GetOutputByDesignId(outputIds[i]), builder);
        }
    }

    private void ConnectCompressorToRpmMinMaxTracker(FunctionBlock compressorFb, FunctionBlock rpmMinMaxTrackerFb)
    {
        if (compressorFb.DesignId != FunctionBlocks.IntervalStatistic.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(compressorFb));

        if (rpmMinMaxTrackerFb.DesignId != FunctionBlocks.RpmAtMinMaxTracker.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(rpmMinMaxTrackerFb));

        var compressorOutputs = FunctionBlocks.IntervalStatistic.Outputs;
        var minMaxTrackerInputs = FunctionBlocks.RpmAtMinMaxTracker.Inputs;

        builder.Editors.Connector.AddLink(compressorFb.GetOutputByDesignId(compressorOutputs.Average), rpmMinMaxTrackerFb.GetInputByDesignId(minMaxTrackerInputs.Average));
        builder.Editors.Connector.AddLink(compressorFb.GetOutputByDesignId(compressorOutputs.Difference), rpmMinMaxTrackerFb.GetInputByDesignId(minMaxTrackerInputs.Difference));
        builder.Editors.Connector.AddLink(compressorFb.GetOutputByDesignId(compressorOutputs.Maximum), rpmMinMaxTrackerFb.GetInputByDesignId(minMaxTrackerInputs.Maximum));
        builder.Editors.Connector.AddLink(compressorFb.GetOutputByDesignId(compressorOutputs.Minimum), rpmMinMaxTrackerFb.GetInputByDesignId(minMaxTrackerInputs.Minimum));
        builder.Editors.Connector.AddLink(compressorFb.GetOutputByDesignId(compressorOutputs.Sum), rpmMinMaxTrackerFb.GetInputByDesignId(minMaxTrackerInputs.Sum));
    }

    private FunctionBlock? ConnectMinMaxTracker(Dataflow dataflow, Dictionary<FunctionBlock, FunctionBlock> minMaxTrackerFbsByCompressor,
        Guid[] activeClouds,
        DeviceDataflowGeneratorResult generateDataflowResult, IDeviceTreeCompressableDataNode compressableDataNode, string suffix,
        CompressorConfiguration configuration, ConnectorOutput subscriberOutput, IDeviceTreeBase parent, FunctionBlock compressorFb)
    {
        FunctionBlock? rpmMinMaxTrackerFb = null;

        if (generateDataflowResult.RotationalFrequencyOutputs.TryGetValue(parent.Id, out var rotationalFrequencyOutputs)
            && compressableDataNode.CompressorConfigurations.Any(c => activeClouds.Contains(c.DataGroupIdentifier)))
        {
            var rpmMinMaxTrackerName = GetRpmMinMaxTrackerFbName(suffix, configuration);

            if (!GetOrAddRpmMinMaxTrackerFb(dataflow, rpmMinMaxTrackerName, compressorFb.Container,
                                           new Point(compressorFb.X ?? 0, compressorFb.Y ?? 0),
                                           minMaxTrackerFbsByCompressor, compressorFb, out rpmMinMaxTrackerFb))
            {
                ConnectSubscriberToRpmMinMaxTracker(subscriberOutput, rotationalFrequencyOutputs.RotSpeed, rotationalFrequencyOutputs.RefValue, rpmMinMaxTrackerFb);
                ConnectCompressorToRpmMinMaxTracker(compressorFb, rpmMinMaxTrackerFb);
            }
        }

        return rpmMinMaxTrackerFb;
    }

    private void ConnectProcessDataToCloud(Dataflow dataflow, IDeviceTreeCompressableDataNode compressableDataNode,
        CompressorConfiguration configuration, DataOutputInfo outputInfo, Dictionary<FunctionBlock, FunctionBlock> minMaxTrackerFbsByCompressor,
        FunctionBlock compressorFb, IDeviceTreeBase parent, DeviceDataflowGeneratorResult generateDataflowResult,
        Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> cloudInputs, Dictionary<Guid, string> connectionNames)
    {
        if (!cloudInputs.TryGetValue(configuration.DataGroupIdentifier, out var inputs) || !inputs.TryGetValue(compressableDataNode.Id, out var aggregationFunctionInput))
        {
            LogMissingCloudInputForDatapoint(logger, compressableDataNode.Id, connectionNames[configuration.DataGroupIdentifier]);
            return;
        }

        FunctionBlock? rpmMinMaxTrackerFb = null;

        if (cloudInputs.Any(cis => cis.Value.Any(ci => ci.Value.RotationalFrequencies.IsConfigured())))
        {
            rpmMinMaxTrackerFb = ConnectMinMaxTracker(dataflow, minMaxTrackerFbsByCompressor, [.. cloudInputs.Keys], generateDataflowResult, compressableDataNode,
            outputInfo.Suffix, configuration, outputInfo.Output, parent, compressorFb);
        }

        if (configuration.CompressionTime == -1)
        {
            aggregationFunctionInput.Value.Connect(outputInfo.Output, builder);
        }                                   // evtl. durch exception ersetzen wenn annaDataPublishers.TryGetValue aktiv ist
        else if (rpmMinMaxTrackerFb is not null)
        {
            ConnectRpmMinMaxTrackerToCloud(rpmMinMaxTrackerFb!, aggregationFunctionInput, configuration);
        }
        else
        {
            ConnectCompressedSourceToCloud(compressorFb, aggregationFunctionInput, configuration);
        }
    }

    private void ConnectRpmMinMaxTrackerToCloud(FunctionBlock rpmMinMaxTrackerFb, AggregationFunctionCloudInputs cloudInput, CompressorConfiguration configuration)
    {
        if (rpmMinMaxTrackerFb.DesignId != FunctionBlocks.RpmAtMinMaxTracker.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(rpmMinMaxTrackerFb));

        var minMaxTrackerOutputs = FunctionBlocks.RpmAtMinMaxTracker.Outputs;

        ConnectCompressedSourceToCloud(rpmMinMaxTrackerFb, cloudInput, configuration);

        cloudInput.RefValueAtMax.Connect(rpmMinMaxTrackerFb.GetOutputByDesignId(minMaxTrackerOutputs.RefValueAtMaximum), builder);
        cloudInput.RotSpeedAtMax.Connect(rpmMinMaxTrackerFb.GetOutputByDesignId(minMaxTrackerOutputs.RotSpeedAtMaximum), builder);
        cloudInput.RefValueAtMin.Connect(rpmMinMaxTrackerFb.GetOutputByDesignId(minMaxTrackerOutputs.RefValueAtMinimum), builder);
        cloudInput.RotSpeedAtMin.Connect(rpmMinMaxTrackerFb.GetOutputByDesignId(minMaxTrackerOutputs.RotSpeedAtMinimum), builder);
    }

    private void ConnectSchedulerToRawDataSubscriber(FunctionBlock scheduler, ConnectorInput triggerInput)
        => builder.Editors.Connector.AddLink(scheduler.GetOutputByDesignId(FunctionBlocks.Scheduler.Outputs.Trigger), triggerInput, false);

    private void ConnectSubscriberToCompressor(Dataflow dataflow, DataOutputInfo outputInfo, FunctionBlock compressor, Dictionary<ConnectorOutput, ConnectorOutput> convertedOutputs)
    {
        if (compressor.DesignId != FunctionBlocks.IntervalStatistic.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(compressor));

        if (outputInfo.ValidOutput is not null)
            builder.Editors.Connector.AddLink(outputInfo.ValidOutput, compressor.GetInputByDesignId(FunctionBlocks.IntervalStatistic.Inputs.Enabled), false);

        var output = outputInfo.Output;
        var datatype = builder.DetermineValueType(output);

        if (datatype == typeof(bool) || datatype == typeof(long))
            output = GetFloatConnector(dataflow, outputInfo.Suffix, output, convertedOutputs);

        builder.Editors.Connector.AddLink(output, compressor.GetInputByDesignId(FunctionBlocks.IntervalStatistic.Inputs.Value), false);
    }

    private void ConnectSubscriberToRpmMinMaxTracker(ConnectorOutput subscriberOutput, ConnectorOutput? rotSpeedOutput,
        ConnectorOutput? refValueOutput, FunctionBlock rpmMinMaxTracker)
    {
        if (rpmMinMaxTracker.DesignId != FunctionBlocks.RpmAtMinMaxTracker.DesignId)
            throw new ArgumentException(WrongDesignIdErrorMessage, nameof(rpmMinMaxTracker));

        builder.Editors.Connector.AddLink(subscriberOutput, rpmMinMaxTracker.GetInputByDesignId(FunctionBlocks.RpmAtMinMaxTracker.Inputs.Value), false);

        if (refValueOutput is not null)
            builder.Editors.Connector.AddLink(refValueOutput, rpmMinMaxTracker.GetInputByDesignId(FunctionBlocks.RpmAtMinMaxTracker.Inputs.RefValue), false);

        if (rotSpeedOutput is not null)
            builder.Editors.Connector.AddLink(rotSpeedOutput, rpmMinMaxTracker.GetInputByDesignId(FunctionBlocks.RpmAtMinMaxTracker.Inputs.RotSpeed), false);
    }

    private void ConnectUncompressedProcessDataToCloud(IDeviceTreeCompressableDataNode compressableDataNode, CompressorConfiguration compressorConfiguration,
        DataOutputInfo dataOutput, Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> cloudInputs, Dictionary<Guid, string> connectionNames)
    {
        if (!cloudInputs.TryGetValue(compressorConfiguration.DataGroupIdentifier, out var inputs) || !inputs.TryGetValue(compressableDataNode.Id, out var aggregationFunctionInput))
        {
            LogMissingCloudInputForDatapoint(logger, compressableDataNode.Id, connectionNames[compressorConfiguration.DataGroupIdentifier]);
            return;
        }

        var output = dataOutput.Output;
        aggregationFunctionInput.Value.Connect(output, builder);
    }

    public void Generate(IDeviceTreeMasterNode master, IReadOnlyCollection<Connection> publishTargets, Dataflow dataflow, Engine engine,
        out Guid deviceTreeTrigger, out Guid deviceTreeOutput, out List<ValueMappingEntry> outputMapping)
    {
        var nodeAndDescendants = master.GetNodeAndDescendants().ToArray();
        var parents = GetParentDictionary(nodeAndDescendants);

        var activeDatagroupIdentifiers = GetActiveDataGroupIds(nodeAndDescendants);

        var activePublishTargets = publishTargets
            .DistinctBy(p => p.Id)
            .Where(t => activeDatagroupIdentifiers.Contains(t.Id))
            .ToArray();

        var compressorFbs = new Dictionary<IDeviceTreeBase, Dictionary<string, FunctionBlock>>();
        var schedulerFbs = new Dictionary<SchedulerConfiguration, FunctionBlock>(SchedulerConfigurationIgnoreDataGroupEqualityComparer.Instance);

        var enabledConfigs = activePublishTargets.Select(c => c.Id).ToArray();

        InitContainerSizeManagers(dataflow, out var dataFormatterContainerManager);

        // A fresh identifier per master, under which the IoTCoreConfiguration function block registers the
        // connection and by which the subscriber function blocks reach that same, shared connection.
        var connectionIdentifier = Guid.NewGuid().ToString();

        var connectionNames = activePublishTargets.ToDictionary(c => c.Id, c => c.Name ?? string.Empty);

        var deviceDataflowGenerator = deviceDataflowGenerators.FirstOrDefault(g => g.DeviceType == master.GetType())
            ?? throw new ArgumentException($"No deviceDataflowGenerator found for {master.GetType()}");

        var deviceTreeFunctionBlockResult = deviceDataflowGenerator.GenerateDeviceTreeSourceFunctionBlock(builder, dataflow, master, connectionIdentifier);
        deviceTreeTrigger = deviceTreeFunctionBlockResult.DeviceTreeTrigger;
        deviceTreeOutput = deviceTreeFunctionBlockResult.DeviceTreeOutput;

        var enabledDataIds = GetEnabledDataIds(master, enabledConfigs);
        var blobLoggingConfigurations = GetBlobLoggingConfigurations(nodeAndDescendants, enabledConfigs).ToArray();
        var generateDataflowResult = deviceDataflowGenerator.GenerateDeviceFunctionBlocks(builder, dataflow, master, enabledDataIds, connectionNames, blobLoggingConfigurations, connectionIdentifier);

        var cloudInputs = GenerateClouds(master, nodeAndDescendants, engine, dataflow, activePublishTargets, generateDataflowResult);

        GenerateProcessDataLogging(dataflow, nodeAndDescendants, master, () => builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameCompressors, null, new Point { X = FunctionBlocks.DefaultHorizontalSeparation }),
            parents, compressorFbs, enabledConfigs, generateDataflowResult, cloudInputs, connectionNames);

        GenerateSchedulableBlobLogging(dataflow, nodeAndDescendants, schedulerFbs, enabledConfigs, cloudInputs, connectionNames, generateDataflowResult);
        GenerateEventTriggerBlobLogging(dataflow, nodeAndDescendants, enabledConfigs, cloudInputs, connectionNames, generateDataflowResult);

        outputMapping = generateDataflowResult.OutputMapping;

        builder.Editors.FunctionBlock.AssignEngine(engine, [.. dataflow.Root.GetAllNestedFunctionBlocks()]);
        builder.Editors.DataPort.AssignEngine(engine, [.. dataflow.DataPorts]);

        // Workaround für lange Ladezeiten wenn sich die Cluster Dependencies ändern
        AddDesigns(builder);
    }

    public static void AddDesigns(ClusterBuilder clusterBuilder)
    {
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.VseObjectSubscriber.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.AnnaObjectData.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.ConstantString.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.IntervalStatistic.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.DataFormatter.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.BooleanToDouble.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.LongToDouble.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(FunctionBlocks.Scheduler.DesignId);
        clusterBuilder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);
        clusterBuilder.AddDataPortDesign(FunctionBlocks.AnnaDataPort.DesignId);
        clusterBuilder.AddDataPortDesign(FunctionBlocks.OpcUaDataPort.DesignId);
        clusterBuilder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(s_designIdSystemDataPort);
    }

    private Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> GenerateClouds(IDeviceTreeMasterNode master, IDeviceTreeBase[] nodeAndDescendants, Engine engine, Dataflow dataflow, Connection[] activePublishTargets, DeviceDataflowGeneratorResult generateDataflowResult)
    {
        var cloudInputs = new Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>>();
        Container? cloudsContainer = null;

        foreach (var cloudDataflowGenerator in cloudDataflowGenerators.DistinctBy(c => c.GetType()))
        {
            var cloudFilter = cloudFilters.FirstOrDefault(f => f.CloudDataflowGeneratorType == cloudDataflowGenerator.GetType())
                ?? throw new InvalidOperationException($"Missing cloud filter for {cloudDataflowGenerator.GetType()}");

            var cloudConnections = cloudFilter.GetCloudConnections(activePublishTargets);

            foreach (var cloudConnection in cloudConnections)
            {
                cloudsContainer ??= builder.Editors.Container.AddSubContainer(dataflow, "Clouds", dataflow.Root, FunctionBlocks.DefaultHorizontalSeparation * 2, 0);
                var container = builder.Editors.Container.AddSubContainer(dataflow, $"{cloudConnection.Name ?? cloudConnection.Id.ToString()}", cloudsContainer, 0, FunctionBlocks.DefaultVerticalSeparation);

                var loggedRawDataNodes = GetLoggedRawDataNodes(nodeAndDescendants, cloudConnection);
                var loggedProcessDataNodes = GetLoggedProcessDataNodes(nodeAndDescendants, cloudConnection);

                cloudInputs[cloudConnection.Id] = cloudDataflowGenerator.GenerateCloudDataflow(cloudConnection, master, builder, dataflow, machineIdentifier, generateDataflowResult.DataOutputs,
                                                                                               engine.MinCycleTime, container, generateDataflowResult.RotationalFrequencyOutputs,
                                                                                               loggedProcessDataNodes, loggedRawDataNodes);
            }
        }

        return cloudInputs;
    }

    private void GenerateEventTriggerBlobLogging(Dataflow dataflow, IDeviceTreeBase[] nodeAndDescendants, Guid[] enabledConfigs,
        Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> cloudInputsByCloud,
        Dictionary<Guid, string> connectionNames, DeviceDataflowGeneratorResult generateDataflowResult)
    {
        var eventTriggerTuples = new List<ErrorStateGuardTuple>();
        var eventTriggerNodes = nodeAndDescendants.OfType<IDeviceTreeEventTriggerDataNode>();

        foreach (var sensor in eventTriggerNodes)
        {
            foreach (var eventTrigger in sensor.EventTriggerConfigurations)
            {
                if (!eventTrigger.Triggers.Any(t => t.Enabled && (t.OnWarning || t.OnDamage)))
                    continue;

                if (!generateDataflowResult.ErrorStateOutputs.TryGetValue(eventTrigger.ReferenceNodeId, out var errorStateOutput))
                {
                    LogNoReferenceNodeId(logger, eventTrigger.ReferenceNodeId);
                    continue;
                }

                if (!generateDataflowResult.RawData.TryGetValue(sensor.Id, out var rawDataInfo))
                {
                    LogNoRawDataInfo(logger, sensor.Id);
                    continue;
                }

                foreach (var eventTriggerConfiguration in eventTrigger.Triggers.Where(t => t.Enabled && (t.OnWarning || t.OnDamage)))
                {
                    if (enabledConfigs.Contains(eventTriggerConfiguration.DataGroupIdentifier))
                    {
                        if (!rawDataInfo.EventTriggerSensors.TryGetValue(eventTriggerConfiguration.DataGroupIdentifier, out var eventTriggerSensor))
                        {
                            LogRawDataInfoMissesEventTriggerSensor(logger, sensor.Id, eventTriggerConfiguration.DataGroupIdentifier);
                            continue;
                        }

                        eventTriggerTuples.Add(new()
                        {
                            BlobSensorDataOutput = eventTriggerSensor.MeasurementOutput,
                            BlobSensorErrorStateTriggerInput = eventTriggerSensor.EventTriggerInput,
                            Configuration = eventTriggerConfiguration,
                            ErrorStateOutput = errorStateOutput,
                            Sensor = sensor,
                        });
                    }
                }
            }
        }

        foreach (var objectGuardKVP in GetErrorStateGuardGrouping(eventTriggerTuples))
        {
            for (var guardIndex = 0; guardIndex < objectGuardKVP.Value.Count(); guardIndex++)
            {
                var guardGrouping = objectGuardKVP.Value.ElementAt(guardIndex);

                AddErrorStateGuard(builder, dataflow, objectGuardKVP.Key, objectGuardKVP.Key, guardIndex, guardGrouping.Key, guardGrouping);

                foreach (var eventTriggerTuple in guardGrouping)
                {
                    if (cloudInputsByCloud.TryGetValue(eventTriggerTuple.Configuration.DataGroupIdentifier, out var cloudInputs))
                    {
                        if (cloudInputs.TryGetValue(eventTriggerTuple.Sensor.Id, out var cloudInput))
                        {
                            cloudInput.RawData.Connect(eventTriggerTuple.BlobSensorDataOutput, builder);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Missing cloud input for cloud {eventTriggerTuple.Configuration.DataGroupIdentifier} ({connectionNames[eventTriggerTuple.Configuration.DataGroupIdentifier]}), node {eventTriggerTuple.Sensor.Id}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Missing cloud inputs for cloud {eventTriggerTuple.Configuration.DataGroupIdentifier} ({connectionNames[eventTriggerTuple.Configuration.DataGroupIdentifier]})");
                    }
                }
            }
        }
    }

    private void GenerateProcessDataLogging(Dataflow dataflow, IDeviceTreeBase[] nodeAndDescendants, IDeviceTreeMasterNode deviceNode, Func<Container> compressorsContainer,
        Dictionary<IDeviceTreeBase, IDeviceTreeBase> parents,
        Dictionary<IDeviceTreeBase, Dictionary<string, FunctionBlock>> compressorFbs, Guid[] enabledConfigs,
        DeviceDataflowGeneratorResult generateDataflowResult,
        Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> cloudInputs,
        Dictionary<Guid, string> connectionNames)
    {
        var compressorContainerManager = new DeviceContainerManager(deviceNode, compressorsContainer, dataflow, builder);

        foreach (var compressableDataNode in nodeAndDescendants.OfType<IDeviceTreeCompressableDataNode>())
        {
            if (!compressableDataNode.DataType.SupportsLogging)
                continue;

            if (!generateDataflowResult.DataOutputs.TryGetValue(compressableDataNode.Id, out var dataOutput))
                continue;

            var activeConfigs = compressableDataNode.CompressorConfigurations.Where(s => s.Enabled && enabledConfigs.Contains(s.DataGroupIdentifier)).ToArray();
            var minMaxTrackerFbsByCompressor = new Dictionary<FunctionBlock, FunctionBlock>();
            var convertedOutputs = new Dictionary<ConnectorOutput, ConnectorOutput>();

            foreach (var configuration in activeConfigs)
            {
                if (compressableDataNode.DataType.SupportsCompression)
                {
                    if (configuration.CompressionTime > 0)
                    {
                        var compressorName = GetCompressorFbName(compressableDataNode, configuration);
                        var parent = parents[compressableDataNode];

                        if (!GetOrAddCompressorFb(dataflow, compressorName, configuration, compressableDataNode, compressorContainerManager, compressorFbs, out var compressorFb))
                            ConnectSubscriberToCompressor(dataflow, dataOutput, compressorFb, convertedOutputs);

                        ConnectProcessDataToCloud(dataflow, compressableDataNode, configuration, dataOutput, minMaxTrackerFbsByCompressor, compressorFb, parent, generateDataflowResult, cloudInputs, connectionNames);
                    }
                    else
                    {
                        ConnectUncompressedProcessDataToCloud(compressableDataNode, configuration, dataOutput, cloudInputs, connectionNames);
                    }
                }
            }
        }
    }

    private void GenerateSchedulableBlobLogging(Dataflow dataflow, IDeviceTreeBase[] nodeAndDescendants,
        Dictionary<SchedulerConfiguration, FunctionBlock> schedulerFbs, Guid[] enabledConfigs,
        Dictionary<Guid, Dictionary<string, AggregationFunctionCloudInputs>> cloudInputs,
        Dictionary<Guid, string> connectionNames, DeviceDataflowGeneratorResult generateDataflowResult)
    {
        Container? schedulerContainer = null;

        foreach (var schedulableDataNode in nodeAndDescendants.OfType<IDeviceTreeSchedulableDataNode>())
        {
            foreach (var schedulerConfig in schedulableDataNode.SchedulerConfigurations.Where(c => enabledConfigs.Contains(c.DataGroupIdentifier)))
            {
                if (schedulerConfig.Enabled && enabledConfigs.Contains(schedulerConfig.DataGroupIdentifier))
                {
                    schedulerContainer ??= builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameSchedulers, null, s_schedulerContainerLocation);

                    if (!generateDataflowResult.RawData.TryGetValue(schedulableDataNode.Id, out var rawDataInfo))
                    {
                        LogNoRawDataInfo(logger, schedulableDataNode.Id);
                        continue;
                    }

                    if (!rawDataInfo.SchedulerSensors.TryGetValue(schedulerConfig.DataGroupIdentifier, out var schedulerRawDataInfo))
                    {
                        LogRawDataInfoMissesScheduledTriggerSensor(logger, schedulableDataNode.Id, schedulerConfig.DataGroupIdentifier);
                        continue;
                    }

                    var schedulerFb = GetOrAddSchedulerFb(dataflow, schedulerConfig, schedulerFbs, schedulerContainer);

                    ConnectSchedulerToRawDataSubscriber(schedulerFb, schedulerRawDataInfo.TriggerInput);

                    if (cloudInputs.TryGetValue(schedulerConfig.DataGroupIdentifier, out var currentCloudInputs))
                    {
                        if (!currentCloudInputs.TryGetValue(schedulableDataNode.Id, out var aggregationFunctionCloudInput))
                        {
                            throw new InvalidOperationException($"Cloud input for cloud {connectionNames[schedulerConfig.DataGroupIdentifier]} ({schedulerConfig.DataGroupIdentifier}) an device {schedulableDataNode.Id} is missing.");
                        }

                        aggregationFunctionCloudInput.RawData.Connect(rawDataInfo.SchedulerSensors[schedulerConfig.DataGroupIdentifier].MeasurementOutput, builder);
                    }
                }
            }
        }
    }

    private static Dictionary<string, bool> GetEnabledDataIds(IDeviceTreeMasterNode master, Guid[] enabledConfigs)
        => master.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>().ToDictionary(n => n.Id, n =>
        {
            var result = n is IDeviceTreeLiveDataNode;

            switch (n)
            {
                case IDeviceTreeCompressableDataNode compressableNode:
                    result |= compressableNode.CompressorConfigurations.Any(c => enabledConfigs.Contains(c.DataGroupIdentifier) && c.Enabled);
                    break;
                case IDeviceTreeConfigurableRawDataNode configurableRawDataNode:
                    result |= configurableRawDataNode.RawDataConfigurations.Any(c => enabledConfigs.Contains(c.Key) && c.Value.Duration > 0);
                    break;
                case IDeviceTreeEventTriggerDataNode eventTriggerNode:
                    result |= eventTriggerNode.EventTriggerConfigurations.Any(c => c.Triggers.Any(t => enabledConfigs.Contains(t.DataGroupIdentifier) && t.Enabled && (t.OnDamage || t.OnWarning)));
                    break;
                case IDeviceTreeSchedulableDataNode schedulableNode:
                    result |= schedulableNode.SchedulerConfigurations.Any(c => enabledConfigs.Contains(c.DataGroupIdentifier) && c is { Enabled: true, Times.Count: > 0 });
                    break;
            }
            return result;
        });

    private bool GetOrAddCompressorFb(Dataflow dataflow, string fbName, CompressorConfiguration configuration, IDeviceTreeBase node,
        DeviceContainerManager compressorContainerManager, Dictionary<IDeviceTreeBase, Dictionary<string, FunctionBlock>> compressorFbs, out FunctionBlock compressorFb)
    {
        var compressorContainer = compressorContainerManager.GetParentContainer(node);

        if (compressorFbs.TryGetValue(node, out var compressorFbsOfNode) && compressorFbsOfNode.TryGetValue(fbName, out compressorFb!))
            return true;

        compressorFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.IntervalStatistic.DesignId, fbName, compressorContainer, 0, FunctionBlocks.DefaultVerticalSeparation + 30);

        builder.Editors.Setting.SetFunctionBlockSetting(compressorFb, FunctionBlocks.IntervalStatistic.Settings.CompressionTime, configuration.CompressionTime);

        foreach (var output in compressorFb.ProcessDataOutputs)
            builder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(output, false);

        if (!compressorFbs.TryGetValue(node, out var compressorFbsOfNewNode))
        {
            compressorFbsOfNewNode = [];
            compressorFbs[node] = compressorFbsOfNewNode;
        }

        compressorFbsOfNewNode.Add(fbName, compressorFb);
        return false;
    }

    private bool GetOrAddRpmMinMaxTrackerFb(Dataflow dataflow, string fbName, Container compressorContainer,
        Point compressorLocation, Dictionary<FunctionBlock, FunctionBlock> minMaxTrackerFbsByCompressor, FunctionBlock compressor, out FunctionBlock minMaxTrackerFb)
    {
        if (minMaxTrackerFbsByCompressor.TryGetValue(compressor, out minMaxTrackerFb!))
            return true;

        minMaxTrackerFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.RpmAtMinMaxTracker.DesignId, fbName, compressorContainer, new Point(compressorLocation.X + FunctionBlocks.DefaultHorizontalSeparation, compressorLocation.Y));
        minMaxTrackerFbsByCompressor.Add(compressor, minMaxTrackerFb);
        return false;
    }

    private FunctionBlock GetOrAddSchedulerFb(Dataflow dataflow, SchedulerConfiguration configuration, Dictionary<SchedulerConfiguration, FunctionBlock> schedulerFbs, Container schedulerContainer)
    {
        if (schedulerFbs.TryGetValue(configuration, out var schedulerFb))
            return schedulerFb;

        var newSchedulerFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.Scheduler.DesignId, GetSchedulerFbName(configuration), schedulerContainer, new Point(0, _schedulerFbY));
        builder.Editors.Setting.SetFunctionBlockSetting(newSchedulerFb, FunctionBlocks.Scheduler.Settings.Times, configuration.ToFbSetting());

        _schedulerFbY += FunctionBlocks.DefaultVerticalSeparation;
        schedulerFbs[configuration] = newSchedulerFb;
        return newSchedulerFb;
    }

    private static Dictionary<IDeviceTreeBase, IDeviceTreeBase> GetParentDictionary(IDeviceTreeBase[] nodeAndDescendants)
    {
        var result = new Dictionary<IDeviceTreeBase, IDeviceTreeBase>();

        foreach (var node in nodeAndDescendants)
        {
            foreach (var child in node.Children)
            {
                result[child] = node;
            }
        }

        return result;
    }

    private void InitContainerSizeManagers(Dataflow dataflow,
        out ContainerSizeManager dataFormatterContainerManager)
    {
        dataFormatterContainerManager = new ContainerSizeManager
        {
            ChildContainerPrefix = ChildContainerNamePrefixFormatter,
            ContainerSize = ContainerSize,
            ItemHeight = FunctionBlocks.DefaultVerticalSeparation + 150,
        };

        dataFormatterContainerManager.CreatingFirstContainer +=
            () => _moneoConnectContainer = builder.Editors.Container.AddContainer(dataflow.Root, ContainerNameMoneoConnect, null, new Point(2 * FunctionBlocks.DefaultHorizontalSeparation, 2 * FunctionBlocks.DefaultVerticalSeparation));

        dataFormatterContainerManager.CreateNewContainer +=
            name => builder.Editors.Container.AddSubContainer(dataflow, name, _moneoConnectContainer, 0, FunctionBlocks.DefaultVerticalSeparation);
    }
}
