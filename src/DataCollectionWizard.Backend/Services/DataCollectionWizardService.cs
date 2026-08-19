using System.Drawing;
using System.Text.Json;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Connections.Contracts;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DataCollectionWizardService(ILogger<DataCollectionWizardService> logger,
    DataCollectionWizardState dataCollectionWizardState,
    ISuiteMediator mediator,
    IClusterService clusterService,
    IDataCollectionWizardDbContext dataCollectionWizardDbContext,
    IInstanceInformationProvider instanceInformationProvider,
    IEnumerable<IDeviceDataflowGenerator> deviceDataflowGenerators,
    IEnumerable<ICloudDataflowGenerator> cloudDataflowGenerators,
    IEnumerable<ICloudFilter> cloudFilters,
    IConnectionService connectionService)
    : IDataCollectionWizardService
{
    private const string EngineHostName = "DCW-Host";
    private const int EngineMinCycleTime = 500;
    private const string EngineNameDeviceScanner = "DCW-Device-Scanner";
    private const string EngineNameKeyIoLink = "IO-Link";
    private const string EngineNameKeyVse = "VSE";
    private const string EngineNamePrefix = "DCW";

    /// <summary>
    /// One kind of device scanner living in the device scanner dataflow. Every scanner works the same way -
    /// write its trigger to start a scan, read its result from the devices connector - so they only differ in
    /// which design they instantiate and which ids their two connectors are pinned to.
    /// </summary>
    /// <param name="Name">The name the scanner's FunctionBlock carries in the dataflow.</param>
    /// <param name="DesignId">The scanner FunctionBlock design to instantiate.</param>
    /// <param name="TriggerDesignId">The design id of the scanner's trigger connector.</param>
    /// <param name="TriggerNodeId">The id the trigger connector is pinned to, so clients can address it.</param>
    /// <param name="DevicesDesignId">The design id of the scanner's result connector.</param>
    /// <param name="DevicesNodeId">The id the result connector is pinned to, so clients can subscribe to it.</param>
    private sealed record DeviceScanner(
        string Name,
        Guid DesignId,
        Guid TriggerDesignId,
        Guid TriggerNodeId,
        Guid DevicesDesignId,
        Guid DevicesNodeId);

    /// <summary>
    /// The scanners the device scanner dataflow holds. A scanner keeps its place in this list, because the
    /// place decides where its FunctionBlock sits in the dataflow - stacked one below the other rather than
    /// all on the same spot, and staying put when another scanner is added later.
    /// </summary>
    private static readonly DeviceScanner[] s_deviceScanners =
    [
        new("IO-Link Scan",
            FunctionBlocks.IoLinkMasterScanner.DesignId,
            FunctionBlocks.IoLinkMasterScanner.Inputs.Trigger,
            FunctionBlocks.IoLinkMasterScanner.Inputs.TriggerNodeId,
            FunctionBlocks.IoLinkMasterScanner.Outputs.Devices,
            FunctionBlocks.IoLinkMasterScanner.Outputs.DevicesNodeId),
        new("VSE Scan",
            FunctionBlocks.VseScanner.DesignId,
            FunctionBlocks.VseScanner.Inputs.Trigger,
            FunctionBlocks.VseScanner.Inputs.TriggerNodeId,
            FunctionBlocks.VseScanner.Outputs.Devices,
            FunctionBlocks.VseScanner.Outputs.DevicesNodeId),
    ];

    public async Task<Cluster?> AddDeviceTreeEnginesAsync(IEnumerable<DeviceEngineInfo> deviceEngineInfos, Guid correlationId, bool allowUseExistingEngine, LogLevel logLevel)
    {
        await LoadLatestClusterAsync();
        var createdEngines = false;

        foreach (var deviceEngineInfo in deviceEngineInfos)
        {
            var type = Type.GetType(deviceEngineInfo.Type);
            if (type is null)
                throw new ArgumentException($"Cannot find type {type}");

            var engineName = GetMasterDeviceEngineName(type, deviceEngineInfo.Address);
            var (engineExists, deviceTreeConnectors) = await DoesEngineAlreadyExistAsync(engineName, deviceEngineInfo.Address);

            if (engineExists && allowUseExistingEngine)
            {
                await mediator.Publish(new DeviceTreeEngineAddedEvent
                {
                    Address = deviceEngineInfo.Address,
                    CorrelationId = correlationId,
                    DeviceTreeConnectors = deviceTreeConnectors
                });

                continue;
            }

            createdEngines = true;
            await AddDeviceRequestEngineAsync(type, dataCollectionWizardState.ClusterBuilder!, deviceEngineInfo.Address, deviceEngineInfo.Username, deviceEngineInfo.Password, correlationId, logLevel);
        }

        return createdEngines ? dataCollectionWizardState.ClusterBuilder!.Cluster : null;
    }

    private async Task AddDeviceRequestEngineAsync(Type type, ClusterBuilder clusterBuilder, Uri address, string? username, string? password, Guid correlationId, LogLevel logLevel)
    {
        IDeviceTreeMasterNode device = type switch
        {
            { } deviceTreeVseDeviceType when deviceTreeVseDeviceType == typeof(DeviceTreeVseDevice) => new DeviceTreeVseDevice
            {
                Alias = $"VSE-{address}",
                Id = "placeholder",
                MacAddress = "ff:ff:ff:ff:ff",
                Name = $"VSE-{address}",
                Url = address,
            },
            { } deviceTreeIoLinkMasterType when deviceTreeIoLinkMasterType == typeof(DeviceTreeIoLinkMaster) => new DeviceTreeIoLinkMaster
            {
                Alias = $"IO-Link-{address}",
                Id = "placeholder",
                MacAddress = "ff:ff:ff:ff:ff",
                Name = $"IO-Link-{address}",
                Url = new UriBuilder(address).Uri,
                // The fetch engine authenticates with these so the tree of an auth-required master can be read.
                Username = username,
                Password = password,
            },
            _ => throw new ArgumentException($"Invalid device type encountered, {type} is not currently supported", type.Name),
        };

        ModifyCluster(clusterBuilder, device, [], out var deviceTreeTrigger, out var deviceTreeOutput, out _, logLevel);

        var deviceConnectorIds = new DeviceConnectorIds
        {
            DeviceAddress = address.ToString(),
            DeviceTreeOutput = deviceTreeOutput,
            TriggerInput = deviceTreeTrigger
        };

        await mediator.Send(new UpsertDeviceConnectorIds([deviceConnectorIds]));
        dataCollectionWizardState.RequestedDevices.Add((address, correlationId));
        dataCollectionWizardState.DeviceTreeConnectors[address] = deviceConnectorIds;
    }

    public async Task<Cluster?> AddDeviceScannerAsync(LogLevel logLevel)
    {
        await LoadLatestClusterAsync();
        var clusterBuilder = dataCollectionWizardState.ClusterBuilder ?? throw new InvalidOperationException("Cluster Builder is not initialized.");

        var engineHost = GetEngineHost(clusterBuilder);

        clusterBuilder.Editors.EngineHost.SetElevatedPrivileges(engineHost, true);
        DataflowGenerator.AddDesigns(clusterBuilder);

        return AddDeviceScannerEngineIfNecessary(clusterBuilder, engineHost, logLevel)
            ? clusterBuilder.Cluster
            : null;
    }

    /// <summary>
    /// Makes sure the cluster holds a device scanner engine that carries every scanner in
    /// <see cref="s_deviceScanners"/>, adding whatever is missing.
    /// </summary>
    /// <returns>True if the cluster was changed and needs deploying; false if it already had everything.</returns>
    private static bool AddDeviceScannerEngineIfNecessary(ClusterBuilder clusterBuilder, EngineHost engineHost, LogLevel? logLevel = null)
    {
        var changed = false;

        var scannerEngine = clusterBuilder.Cluster.GetAllEngines().FirstOrDefault(e => e.Name == EngineNameDeviceScanner);
        var scanDataflow = clusterBuilder.Cluster.Dataflows.FirstOrDefault(d => d.Name == EngineNameDeviceScanner);

        if (scanDataflow is null)
        {
            scanDataflow = clusterBuilder.Editors.Cluster.AddDataflow(EngineNameDeviceScanner, new Version(0, 1));
            changed = true;
        }

        // A scanner added later must end up in the dataflow of a cluster that already carries the earlier ones,
        // so every scanner is checked on its own rather than the dataflow as a whole.
        for (var index = 0; index < s_deviceScanners.Length; index++)
        {
            var scanner = s_deviceScanners[index];

            if (scanDataflow.Root.GetAllNestedFunctionBlocks().Any(f => f.DesignId == scanner.DesignId))
                continue;

            AddDeviceScanner(clusterBuilder, scanDataflow, scanner, index);
            changed = true;
        }

        if (!changed)
            return false;

        if (scannerEngine is null)
        {
            scannerEngine = clusterBuilder.Editors.EngineHost.AddEngine(engineHost, EngineNameDeviceScanner);
            clusterBuilder.Editors.Engine.SetLogLevel(scannerEngine, logLevel ?? LogLevel.Error);
            clusterBuilder.Editors.Engine.SetMinCycleTime(scannerEngine, EngineMinCycleTime);
        }

        clusterBuilder.Editors.FunctionBlock.AssignEngine(scannerEngine, [.. scanDataflow.Root.GetAllNestedFunctionBlocks()]);

        return true;
    }

    /// <summary>
    /// Adds one scanner FunctionBlock to the device scanner dataflow and pins its trigger and result connectors
    /// to the ids clients address them by.
    /// </summary>
    /// <param name="position">
    /// The scanner's place in <see cref="s_deviceScanners"/>, which decides how far down its FunctionBlock
    /// sits. Without a location of their own the blocks all land on the same spot and hide each other.
    /// </param>
    private static void AddDeviceScanner(ClusterBuilder clusterBuilder, Dataflow scanDataflow, DeviceScanner scanner, int position)
    {
        var location = new Point(0, position * FunctionBlocks.DefaultVerticalSeparation);
        var scanFunctionBlock = clusterBuilder.Editors.Container.AddFunctionBlock(scanDataflow.Root, scanner.DesignId, scanner.Name, null, location);

        var devicesConnector = scanFunctionBlock.GetConnectorByDesignId(scanner.DevicesDesignId)!;
        var triggerConnector = scanFunctionBlock.GetConnectorByDesignId(scanner.TriggerDesignId)!;

        clusterBuilder.Editors.Connector.SetId(devicesConnector, scanner.DevicesNodeId);
        clusterBuilder.Editors.Connector.SetId(triggerConnector, scanner.TriggerNodeId);

        clusterBuilder.Editors.Connector.SetEventEnabled(true, devicesConnector, triggerConnector);

        // A scan is started by writing the trigger, not by changing it, so writing the same value again has to
        // count as a change - otherwise only the first scan would ever run.
        clusterBuilder.Editors.Connector.SetMarkAsChangedOnlyIfNotEqual(triggerConnector, false);
    }

    private async Task AddOrUpdateDeviceEngines(ClusterBuilder clusterBuilder, IEnumerable<string> masterNodesToUpdate, IDeviceTreeBase[] deletedNodes,
        IReadOnlyCollection<Connection> publishTargets, IDeviceTreeMasterNode[] allMasters, LogLevel? logLevel)
    {
        var nodesToUpdate = allMasters.IntersectBy(masterNodesToUpdate, n => n.Id);

        if (RemoveLegacyEngines(clusterBuilder))
        {
            nodesToUpdate = allMasters;
        }

        var relevantMasterNodes = nodesToUpdate.Except(deletedNodes)
                                               .Cast<IDeviceTreeMasterNode>()
                                               .ToArray();

        foreach (var deviceTreeMasterDevice in relevantMasterNodes)
        {
            ModifyCluster(clusterBuilder, deviceTreeMasterDevice, publishTargets, out var deviceTreeTrigger, out var deviceTreeOutput, out var outputMapping, logLevel);
            var uri = new UriBuilder(deviceTreeMasterDevice.Url).Uri;

            dataCollectionWizardState.DeviceTreeConnectors[uri] = new DeviceConnectorIds
            {
                DeviceAddress = deviceTreeMasterDevice.Url.ToString(),
                DeviceTreeOutput = deviceTreeOutput,
                TriggerInput = deviceTreeTrigger,
            };

            await mediator.Send(new UpsertDeviceConnectorIds([new DeviceConnectorIds { DeviceAddress = deviceTreeMasterDevice.Url.ToString(),
                DeviceTreeOutput = deviceTreeOutput, TriggerInput = deviceTreeTrigger }]));

            await mediator.Send(new UpsertOutputConnectorMapping(outputMapping));
        }
    }

    public async Task<Cluster> ApplyDeviceTreeAsync(IEnumerable<string> masterNodesToUpdate,
                                           IDeviceTreeBase[] deletedNodes,
                                           DeviceTreeRoot deviceTree, LogLevel? logLevel, CancellationToken cancellationToken)
    {
        var connections = await connectionService.GetConnectionsAsync(cancellationToken);
        var publishTargets = PublishTargetsFilter.GetPublishTargets(connections, cloudFilters);
        await LoadLatestClusterAsync();

        var allMasters = deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToArray();

        RemoveVacantEngines(dataCollectionWizardState, allMasters);

        await RemoveDeletedDeviceEngines(deletedNodes);
        await AddOrUpdateDeviceEngines(dataCollectionWizardState.ClusterBuilder!, masterNodesToUpdate, deletedNodes, publishTargets, allMasters, logLevel);

        return dataCollectionWizardState.ClusterBuilder!.Cluster;
    }

    private async Task<(bool engineExists, DeviceConnectorIds deviceTreeConnectors)> DoesEngineAlreadyExistAsync(string engineName, Uri deviceAddress)
    {
        var clusterBuilder = dataCollectionWizardState.ClusterBuilder ?? throw new InvalidOperationException("Cluster Builder is not initialized.");

        var deviceTreeConnectors = new DeviceConnectorIds
        {
            DeviceAddress = deviceAddress.ToString(),
        };

        if (clusterBuilder.Cluster.Version > dataCollectionWizardState.LatestDeployedClusterVersion)
            return (false, deviceTreeConnectors);

        if (clusterBuilder.Cluster.GetAllEngines().All(e => e.Name != engineName))
            return (false, deviceTreeConnectors);

        var dataflow = clusterBuilder.Cluster.Dataflows.FirstOrDefault(d => d.Name == engineName);
        if (dataflow is null)
            return (false, deviceTreeConnectors);

        var deviceConnectorsResponse = await mediator.Request<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>(new GetDeviceConnectorsRequest(null));
        deviceTreeConnectors = deviceConnectorsResponse.Ids.FirstOrDefault(i => new UriBuilder(i.DeviceAddress).Uri == deviceAddress) ?? deviceTreeConnectors;

        if (dataflow.Root.GetAllNestedFunctionBlocks()
            .SelectMany(fb => fb.Inputs)
            .All(i => i.Id != deviceTreeConnectors.TriggerInput))
        {
            return (false, deviceTreeConnectors);
        }

        if (dataflow.Root.GetAllNestedFunctionBlocks()
            .SelectMany(fb => fb.Outputs)
            .All(i => i.Id != deviceTreeConnectors.DeviceTreeOutput))
        {
            return (false, deviceTreeConnectors);
        }

        return (true, deviceTreeConnectors);
    }

    private static string GetMasterDeviceEngineName(IDeviceTreeMasterNode deviceTreeMaster)
    {
        var key = EngineNameKeyIoLink;
        if (deviceTreeMaster is DeviceTreeVseDevice)
            key = EngineNameKeyVse;

        return $"{EngineNamePrefix}-{key}-{deviceTreeMaster.Url.DnsSafeHost}:{deviceTreeMaster.Url.Port}";
    }

    private static string GetMasterDeviceEngineName(Type deviceType, Uri deviceUrl)
    {
        var key = deviceType switch
        {
            { } deviceTreeVseDeviceType when deviceTreeVseDeviceType == typeof(DeviceTreeVseDevice) => EngineNameKeyVse,
            { } deviceTreeIoLinkMasterType when deviceTreeIoLinkMasterType == typeof(DeviceTreeIoLinkMaster) => EngineNameKeyIoLink,
            _ => throw new InvalidOperationException($"Invalid device type encountered, {deviceType} is not currently supported"),
        };

        return $"{EngineNamePrefix}-{key}-{deviceUrl.DnsSafeHost}";
    }

    private async Task LoadLatestClusterAsync()
    {
        if (dataCollectionWizardState.ClusterBuilder is not null &&
            dataCollectionWizardState.ClusterBuilder.Cluster.Version == dataCollectionWizardState.LatestClusterVersion)
        {
            LogSkipClusterLoading(logger, dataCollectionWizardState.ClusterBuilder.Cluster.Version);
            return;
        }

        dataCollectionWizardState.MachineIdentifier ??= instanceInformationProvider.Local.SerialNumber;
        dataCollectionWizardState.ClusterBuilder = await clusterService.LoadLatestClusterBuilder();

        LogLoadedCluster(logger, nameof(LoadLatestClusterAsync), dataCollectionWizardState.ClusterBuilder.Cluster.Version);
    }

    private void ModifyCluster(ClusterBuilder clusterBuilder, IDeviceTreeMasterNode deviceTreeMasterNode, IReadOnlyCollection<Connection> publishTargets, out Guid deviceTreeTrigger, out Guid deviceTreeOutput, out List<ValueMappingEntry> outputMapping, LogLevel? logLevel)
    {
        deviceTreeTrigger = Guid.Empty;
        deviceTreeOutput = Guid.Empty;

        var engineHost = GetEngineHost(clusterBuilder);
        var engineName = GetMasterDeviceEngineName(deviceTreeMasterNode);
        var dataflow = clusterBuilder.Cluster.Dataflows.FirstOrDefault(d => d.Name == engineName);
        var engine = clusterBuilder.Cluster.GetAllEngines().FirstOrDefault(e => e.Name == engineName) ?? clusterBuilder.Editors.EngineHost.AddEngine(engineHost, engineName);

        clusterBuilder.Editors.Engine.SetMinCycleTime(engine, EngineMinCycleTime);
        clusterBuilder.Editors.EngineHost.SetElevatedPrivileges(engineHost, true);

        if (logLevel is not null)
        {
            clusterBuilder.Editors.Engine.SetLogLevel(engine, logLevel.Value);
            clusterBuilder.Editors.EngineHost.SetLogLevel(engineHost, logLevel.Value);
        }

        if (dataflow is not null)
            clusterBuilder.Editors.Cluster.RemoveDataflow(dataflow);

        dataflow = clusterBuilder.Editors.Cluster.AddDataflow(engineName, new Version(0, 1));

        var dataflowGenerator = new DataflowGenerator(clusterBuilder, logger, dataCollectionWizardState.MachineIdentifier!, [.. deviceDataflowGenerators], [.. cloudDataflowGenerators], [.. cloudFilters]);

        dataflowGenerator.Generate(deviceTreeMasterNode, publishTargets, dataflow, engine, out deviceTreeTrigger, out deviceTreeOutput, out outputMapping);

        AddDeviceScannerEngineIfNecessary(clusterBuilder, engineHost);
    }

    private static EngineHost GetEngineHost(ClusterBuilder clusterBuilder)
    {
        var nodeGroup = clusterBuilder.Cluster.NodeGroups.FirstOrDefault() ?? clusterBuilder.Editors.Cluster.AddNodeGroup();
        var node = nodeGroup.Nodes.FirstOrDefault() ?? clusterBuilder.Editors.NodeGroup.AddNode(nodeGroup);
        var application = node.Applications.FirstOrDefault() ?? clusterBuilder.Editors.Node.AddApplication(node, ClusterApplicationType.CoreOsStandalone);

        var engineHost = clusterBuilder.Cluster.GetAllEngineHosts().FirstOrDefault(e => e.Name == EngineHostName);
        engineHost ??= clusterBuilder.Editors.Application.AddEngineHost(application, EngineHostName);

        return engineHost;
    }

    private async Task RemoveDeletedDeviceEngines(IDeviceTreeBase[] deletedNodes)
    {
        foreach (var deletedNode in deletedNodes)
        {
            if (deletedNode is IDeviceTreeMasterNode deletedMasterNode)
                RemoveDeletedNodeDataflow(deletedMasterNode);

            await mediator.Send(new DeleteOutputConnectorMapping([.. deletedNode.GetNodeAndDescendants().Select(n => n.Id)]));
        }
    }

    private void RemoveDeletedNodeDataflow(IDeviceTreeMasterNode deletedMasterNode)
    {
        var clusterBuilder = dataCollectionWizardState.ClusterBuilder ?? throw new InvalidOperationException("Cluster Builder is not initialized.");

        var nodeGroup = clusterBuilder.Cluster.NodeGroups.FirstOrDefault();
        if (nodeGroup is null)
            return;

        var node = nodeGroup.Nodes.FirstOrDefault();
        if (node is null)
            return;

        var application = node.Applications.FirstOrDefault();
        if (application is null)
            return;

        var engineHost = clusterBuilder.Cluster.GetAllEngineHosts().FirstOrDefault(e => e.Name == EngineHostName);
        if (engineHost is null)
            return;

        var engineName = GetMasterDeviceEngineName(deletedMasterNode);
        var engine = engineHost.Engines.FirstOrDefault(e => e.Name == engineName);
        if (engine is null)
            return;

        clusterBuilder.Editors.EngineHost.RemoveEngine(engine);
        clusterBuilder.Editors.Cluster.RemoveDataflow(clusterBuilder.Cluster.Dataflows.First(d => d.Name == engineName));
    }

    /// <summary>
    /// Removes engines that do not use the seperate enginehost
    /// </summary>
    /// <param name="clusterBuilder"></param>
    /// <returns>true if legacy engines have been removed</returns>
    private static bool RemoveLegacyEngines(ClusterBuilder clusterBuilder)
    {
        var engineHosts = clusterBuilder.Cluster.GetAllEngineHosts().Where(e => e.Name != EngineHostName);
        var legacyEngines = engineHosts.SelectMany(h => h.Engines).Where(e => e.Name.StartsWith(EngineNamePrefix, StringComparison.Ordinal)).ToArray();
        if (legacyEngines.Length == 0)
            return false;

        var enginesDataflows = clusterBuilder.Cluster.Dataflows.Select(d => (d.Root.GetAllNestedFunctionBlocks().Select(f => f.Engine).ToList(), d))
                                                       .SelectMany(d => d.Item1.Select(i => (i, d.d)))
                                                       .GroupBy(i => i.i)
                                                       .Where(i => i.Key is not null)
                                                       .ToDictionary(i => i.Key!, i => i.Select(d => d.d).ToList());

        foreach (var legacyEngine in legacyEngines)
        {
            clusterBuilder.Editors.EngineHost.RemoveEngine(legacyEngine);

            if (enginesDataflows.TryGetValue(legacyEngine, out var dataflows))
            {
                foreach (var dataflow in dataflows)
                {
                    clusterBuilder.Editors.Cluster.RemoveDataflow(dataflow);
                }
            }
        }

        return true;
    }

    private static void RemoveVacantEngines(DataCollectionWizardState dataCollectionWizardState, IDeviceTreeMasterNode[] allMasters)
    {
        var clusterBuilder = dataCollectionWizardState.ClusterBuilder ?? throw new InvalidOperationException("Cluster Builder is not initialized.");

        var existingEngines = clusterBuilder.Cluster.GetAllEngines().Where(n => n.Name.StartsWith(EngineNamePrefix, StringComparison.Ordinal));
        var existingDeviceEngineNames = allMasters.Select(GetMasterDeviceEngineName);

        foreach (var vacantEngine in existingEngines.ExceptBy(existingDeviceEngineNames, e => e.Name).ToArray())
        {
            clusterBuilder.Editors.EngineHost.RemoveEngine(vacantEngine);

            var vacantDataflow = clusterBuilder.Cluster.Dataflows.FirstOrDefault(d => d.Name == vacantEngine.Name);
            if (vacantDataflow is not null)
                clusterBuilder.Editors.Cluster.RemoveDataflow(vacantDataflow);
        }
    }

    public async Task<DeviceTreeRoot> RequestDeviceTreeAsync(CancellationToken cancellationToken)
    {
        var dbItems = await dataCollectionWizardDbContext.Devices.Select(d => d.DeviceTreeJson).ToArrayAsync(cancellationToken);
        var tree = new DeviceTreeRoot
        {
            Children = [.. dbItems.Select(json => JsonSerializer.Deserialize<DeviceTreeStructureNode>(json, SerializerOptions.DeviceTree)).Select(s => s!.Children.First())]
        };

        LogReturnsTreeIdDebug(logger, nameof(RequestDeviceTreeAsync), tree.Id);

        return tree;
    }
}
