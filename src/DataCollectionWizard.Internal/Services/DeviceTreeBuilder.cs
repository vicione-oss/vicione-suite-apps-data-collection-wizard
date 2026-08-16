using System.Diagnostics;
using DataCollectionWizard.Internal.Extensions;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Comparer;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace DataCollectionWizard.Internal.Services;

public static class DeviceTreeBuilder
{
    public const string IdSeparatorNameAlias = "__!__";

    private static void AddElements(IDeviceTreeBase persistedDeviceTree, IDeviceTreeBase parsedDeviceTree, bool resetIsNew)
    {
        var currentMasterUrls = new HashSet<string>(StringComparer.Ordinal);
        var currentNodes = new Dictionary<string, IDeviceTreeBase>(StringComparer.Ordinal);

        // Single pass over the persisted tree: index every node, collect master URLs
        // and (optionally) reset IsNew, avoiding a separate full traversal.
        foreach (var node in persistedDeviceTree.GetNodeAndDescendants())
        {
            if (resetIsNew)
                node.IsNew = false;

            currentNodes[node.Id] = node;

            if (node is DeviceTreeIoLinkMaster master)
                currentMasterUrls.Add(master.Url.AbsoluteUri);
        }

        var parsedChildsParents = new Dictionary<string, string>(StringComparer.Ordinal);

        // Single pass over the parsed tree: build the child->parent lookup and
        // group nodes by type, avoiding a ToArray() of the whole parsed tree.
        var nodesByType = new Dictionary<Type, List<IDeviceTreeBase>>(13);
        foreach (var parsedNode in parsedDeviceTree.GetNodeAndDescendants())
        {
            foreach (var child in parsedNode.Children)
                parsedChildsParents[child.Id] = parsedNode.Id;

            var type = parsedNode.GetType();
            if (!nodesByType.TryGetValue(type, out var list))
            {
                list = [];
                nodesByType[type] = list;
            }

            list.Add(parsedNode);
        }

        // Process groups in hierarchical order (parents before children)
        ReadOnlySpan<Type> typesToAdd =
        [
            typeof(DeviceTreeIoLinkMaster),
            typeof(DeviceTreeIoLinkMasterPort),
            typeof(DeviceTreeDevice),
            typeof(DeviceTreeVseDevice),
            typeof(DeviceTreeStructureNode),
            typeof(DeviceTreeVseObject),
            typeof(DeviceTreeVseInput),
            typeof(DeviceTreeVseAlarm),
            typeof(DeviceTreeVseCounter),
            typeof(DeviceTreeVseVariants),
            typeof(DeviceTreeProcessData),
            typeof(DeviceTreeAssignedName),
            typeof(DeviceTreeVseRawData),
        ];

        foreach (var type in typesToAdd)
        {
            if (nodesByType.TryGetValue(type, out var group))
                AddNewElementsFromGroup(group, currentNodes, currentMasterUrls, parsedChildsParents);
        }
    }

    private static void AddNewElementsFromGroup(List<IDeviceTreeBase> group, Dictionary<string, IDeviceTreeBase> currentNodes,
        HashSet<string> currentMasterUrls, Dictionary<string, string> parsedChildsParents)
    {
        foreach (var parsedNode in group)
        {
            if (parsedNode is DeviceTreeIoLinkMaster master && !currentMasterUrls.Contains(master.Url.AbsoluteUri))
                continue;

            if (currentNodes.ContainsKey(parsedNode.Id))
                continue;

            if (!parsedChildsParents.TryGetValue(parsedNode.Id, out var parentId))
            {
                Debug.Fail($"Parsed node '{parsedNode.Id}' has no parent in lookup – node will be skipped.");
                continue;
            }

            if (!currentNodes.TryGetValue(parentId, out var parent))
                continue;

            var newElement = parsedNode.Clone();
            foreach (var node in newElement.GetNodeAndDescendants())
            {
                node.IsNew = true;
                currentNodes[node.Id] = node;
            }

            parent.Children.Add(newElement);
        }
    }

    private static void AddNewSensors(IDeviceTreeEventTriggerDataNode persistetTriggerNode, IDeviceTreeEventTriggerDataNode parsedTriggerNode)
    {
        var existingSensors = new HashSet<(string ReferenceNodeId, string Name)>(persistetTriggerNode.EventTriggerConfigurations.Count);
        foreach (var configuration in persistetTriggerNode.EventTriggerConfigurations)
            existingSensors.Add((configuration.ReferenceNodeId, configuration.Name));

        foreach (var sensor in parsedTriggerNode.EventTriggerConfigurations)
        {
            if (existingSensors.Add((sensor.ReferenceNodeId, sensor.Name)))
            {
                persistetTriggerNode.EventTriggerConfigurations.Add(new EventTriggerConfiguration
                {
                    IsSensorConfigured = sensor.IsSensorConfigured,
                    Name = sensor.Name,
                    ReferenceNodeId = sensor.ReferenceNodeId,
                });
            }
        }
    }

    public static Dictionary<IDeviceTreeBase, IDeviceTreeBase?> CorrelateParsedDevices(IDeviceTreeBase persistedDeviceTree, List<IDeviceTreeBase> parsedDevices)
    {
        var parsedById = new Dictionary<string, IDeviceTreeBase>(StringComparer.Ordinal);
        foreach (var device in parsedDevices)
        {
            foreach (var node in device.GetNodeAndDescendants())
                parsedById[node.Id] = node;
        }

        var result = new Dictionary<IDeviceTreeBase, IDeviceTreeBase?>();

        foreach (var persistedNode in persistedDeviceTree.GetNodeAndDescendants())
        {
            if (persistedNode is DeviceTreeRoot)
                continue;

            result[persistedNode] = parsedById.TryGetValue(persistedNode.Id, out var match) ? match : null;
        }

        return result;
    }

    public static void ExtendCurrentDeviceTree(IDeviceTreeBase persistedDeviceTree, List<IDeviceTreeBase> parsedDevices, IReadOnlyCollection<Connection> cloudConfigurations, bool retainNewFlag = false)
    {
        var parsedDeviceTree = new DeviceTreeRoot
        {
            Children = parsedDevices,
        };

        AddElements(persistedDeviceTree, parsedDeviceTree, resetIsNew: !retainNewFlag);
        RemoveOnlineGenericMasterDevices(persistedDeviceTree);

        var persistentParsedDeviceData = CorrelateParsedDevices(persistedDeviceTree, parsedDevices);

        UpdateCorrelatedNodes(persistentParsedDeviceData);
        UpdateCloudConfigurations(persistedDeviceTree, cloudConfigurations);
        RemoveEmptyStructureNodes(persistentParsedDeviceData, persistedDeviceTree);
    }

    /// <summary>
    /// Performs all property updates on correlated nodes in a single pass over the dictionary.
    /// </summary>
    private static void UpdateCorrelatedNodes(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> correlatedNodes)
    {
        List<(DeviceTreeDevice Persisted, DeviceTreeDevice Parsed)>? deviceTreeDevices = null;
        List<KeyValuePair<IDeviceTreeEventTriggerDataNode, IDeviceTreeEventTriggerDataNode>>? triggerNodes = null;

        foreach (var (persisted, parsed) in correlatedNodes)
        {
            // Update online/offline status
            if (persisted is not DeviceTreeRoot)
                ApplyOnlineStatus(persisted, parsed);

            if (parsed is null)
                continue;

            switch (persisted)
            {
                case DeviceTreeVseDevice vseDevice when parsed is DeviceTreeVseDevice parsedVse:
                    vseDevice.Name = parsedVse.Name;
                    break;

                case DeviceTreeDevice device when parsed is DeviceTreeDevice parsedDevice:
                    device.ApplicationSpecificTag = parsedDevice.ApplicationSpecificTag;
                    deviceTreeDevices ??= [];
                    deviceTreeDevices.Add((device, parsedDevice));
                    break;

                case DeviceTreeProcessData processData when parsed is DeviceTreeProcessData parsedProcess:
                    processData.Unit = parsedProcess.Unit;
                    break;

                case DeviceTreeVseRawData rawData when parsed is DeviceTreeVseRawData parsedRaw:
                    rawData.Index = parsedRaw.Index;
                    break;

                case DeviceTreeAssignedName constantData when parsed is DeviceTreeAssignedName parsedConstant:
                    constantData.Value = parsedConstant.Value;
                    break;
            }

            if (persisted is IDeviceTreeDeviceAliasNode aliasNode && parsed is IDeviceTreeDeviceAliasNode parsedAlias)
                aliasNode.Alias = parsedAlias.Alias;

            if (persisted is IDeviceTreeVseDataParent vseParent && parsed is IDeviceTreeVseDataParent parsedVseParent)
                vseParent.Path = parsedVseParent.Path;

            if (persisted is IDeviceTreeEventTriggerDataNode triggerNode && parsed is IDeviceTreeEventTriggerDataNode parsedTrigger)
            {
                triggerNodes ??= [];
                triggerNodes.Add(new(triggerNode, parsedTrigger));
            }
        }

        // Unknown/Known status updates require filtered device pairs
        if (deviceTreeDevices is not null)
            UpdateUnknownStatus(deviceTreeDevices);

        // Trigger nodes need nodeNames which requires the full dictionary
        if (triggerNodes is not null)
        {
            var nodeNames = GetNodeNames(correlatedNodes);

            foreach (var (persistedTrigger, parsedTrigger) in triggerNodes)
            {
                AddNewSensors(persistedTrigger, parsedTrigger);
                SortSensors(persistedTrigger, nodeNames);
                UpdateSensors(persistedTrigger, parsedTrigger);
            }
        }
    }

    private static Dictionary<string, string> GetNodeNames(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var nodeNames = new Dictionary<string, string>(persistedDevicesParsedDevices.Count, StringComparer.Ordinal);

        foreach (var (persisted, parsed) in persistedDevicesParsedDevices)
        {
            // Correlation is by ID, so parsed.Id == persisted.Id when parsed is not null.
            // We prefer the parsed node's Name/Alias as it reflects the current device state.
            var source = parsed ?? persisted;
            nodeNames[source.Id] = source is IDeviceTreeDeviceAliasNode { Alias: not null } alias ? alias.Alias : source.Name;
        }

        return nodeNames!;
    }

    private static void RemoveEmptyStructureNodes(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> nodes, IDeviceTreeBase current)
    {
        for (var i = current.Children.Count - 1; i >= 0; i--)
        {
            var child = current.Children[i];

            RemoveEmptyStructureNodes(nodes, child);

            if (child is DeviceTreeStructureNode && child.Children.Count == 0)
            {
                nodes.Remove(child);
                current.Children.RemoveAt(i);
            }
        }
    }

    public static void RemoveEmptyStructureNodes(IDeviceTreeBase deviceTree)
        => RemoveEmptyStructureNodesCore(deviceTree);

    private static void RemoveEmptyStructureNodesCore(IDeviceTreeBase current)
    {
        for (var i = current.Children.Count - 1; i >= 0; i--)
        {
            var child = current.Children[i];

            RemoveEmptyStructureNodesCore(child);

            if (child is DeviceTreeStructureNode && child.Children.Count == 0)
                current.Children.RemoveAt(i);
        }
    }

    public static void RemoveEventTriggers(DeviceTreeRoot tree, IDeviceTreeBase deletingNode)
    {
        var deletingId = deletingNode.Id;
        foreach (var node in tree.GetNodeAndDescendants())
        {
            if (node is IDeviceTreeEventTriggerDataNode triggerNode)
                triggerNode.EventTriggerConfigurations.RemoveAll(e => string.Equals(e.ReferenceNodeId, deletingId, StringComparison.Ordinal));
        }
    }

    private static void RemoveOnlineGenericMasterDevices(IDeviceTreeBase deviceTree)
    {
        var onlineMasterUrls = new HashSet<Uri>();
        var offlineMasters = new List<IDeviceTreeMasterNode>();

        // Masters are always direct children of deviceTree
        foreach (var child in deviceTree.Children)
        {
            if (child is IDeviceTreeMasterNode master)
            {
                if (master.Status == ConnectionStatus.Online)
                    onlineMasterUrls.Add(master.Url);
                else
                    offlineMasters.Add(master);
            }
        }

        var mastersToRemove = new List<IDeviceTreeMasterNode>();
        foreach (var master in offlineMasters)
        {
            if (onlineMasterUrls.Contains(master.Url))
                mastersToRemove.Add(master);
        }

        if (mastersToRemove.Count == 0)
            return;

        foreach (var master in mastersToRemove)
            deviceTree.Children.Remove(master);

        // Remaining masters are direct children – no full tree traversal needed
        var remainingMasters = new Dictionary<Uri, IDeviceTreeMasterNode>(onlineMasterUrls.Count);
        foreach (var child in deviceTree.Children)
        {
            if (child is IDeviceTreeMasterNode remaining)
                remainingMasters.TryAdd(remaining.Url, remaining);
        }

        foreach (var master in mastersToRemove)
        {
            if (remainingMasters.TryGetValue(master.Url, out var remaining))
                remaining.IsNew = master.IsNew;
        }
    }

    private static void UpdateUnknownStatus(List<(DeviceTreeDevice Persisted, DeviceTreeDevice Parsed)> devices)
    {
        foreach (var (device, parsedDevice) in devices)
        {
            if (device.IsUnknown && !parsedDevice.IsUnknown)
            {
                device.IsUnknown = false;
                device.Description = parsedDevice.Description;
            }
            else if (!device.IsUnknown && parsedDevice.IsUnknown)
            {
                device.IsUnknown = true;
                device.Description = parsedDevice.Description;
            }
        }
    }

    public static void SortSensors(IDeviceTreeEventTriggerDataNode persistedTriggerNode, Dictionary<string, string> nodeNames)
    {
        var comparer = AlphaNumericComparer<string>.Default;

        persistedTriggerNode.EventTriggerConfigurations.Sort((a, b) =>
        {
            // Configured sensors first
            var configCmp = b.IsSensorConfigured.CompareTo(a.IsSensorConfigured);
            if (configCmp != 0)
                return configCmp;

            var nameA = nodeNames.TryGetValue(a.ReferenceNodeId, out var resolvedNameA) ? resolvedNameA : a.ReferenceNodeId;
            var nameB = nodeNames.TryGetValue(b.ReferenceNodeId, out var resolvedNameB) ? resolvedNameB : b.ReferenceNodeId;

            return comparer.Compare(nameA, nameB);
        });
    }

    private static void UpdateCloudConfigurations(IDeviceTreeBase deviceTree, IReadOnlyCollection<Connection> connections)
    {
        var cloudIds = new HashSet<Guid>(connections.Count);
        foreach (var connection in connections)
            cloudIds.Add(connection.Id);

        foreach (var node in deviceTree.GetNodeAndDescendants())
        {
            if (node is IDeviceTreeDataNode dataNode)
            {
                dataNode.RemoveConfigurations(cloudIds);
                dataNode.AddConfigurations(connections);
            }

            if (node is IDeviceTreeConfigurableRawDataNode configurableNode)
            {
                List<Guid>? keysToRemove = null;
                foreach (var key in configurableNode.RawDataConfigurations.Keys)
                {
                    if (!cloudIds.Contains(key))
                        (keysToRemove ??= []).Add(key);
                }

                if (keysToRemove is not null)
                {
                    foreach (var key in keysToRemove)
                        configurableNode.RawDataConfigurations.Remove(key);
                }

                // Add missing cloud configurations
                foreach (var cloudConfig in connections)
                {
                    if (!configurableNode.RawDataConfigurations.ContainsKey(cloudConfig.Id))
                    {
                        configurableNode.RawDataConfigurations.Add(cloudConfig.Id, new RawDataSettings
                        {
                            Duration = 4000,
                            Frequency = 100000,
                        });
                    }
                }
            }
        }
    }

    public static void UpdateOnlineStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        foreach (var (persisted, parsed) in persistedDevicesParsedDevices)
        {
            if (persisted is not DeviceTreeRoot)
                ApplyOnlineStatus(persisted, parsed);
        }
    }

    private static void ApplyOnlineStatus(IDeviceTreeBase persisted, IDeviceTreeBase? parsed)
    {
        var parsedStatus = parsed?.Status ?? ConnectionStatus.Offline;

        if (persisted.Status != parsedStatus)
            persisted.Status = parsedStatus;
    }

    private static void UpdateSensors(IDeviceTreeEventTriggerDataNode persistedTriggerNode, IDeviceTreeEventTriggerDataNode parsedTriggerNode)
    {
        var parsedSensorsByRefId = new Dictionary<string, EventTriggerConfiguration>(
            parsedTriggerNode.EventTriggerConfigurations.Count, StringComparer.Ordinal);
        foreach (var parsed in parsedTriggerNode.EventTriggerConfigurations)
            parsedSensorsByRefId[parsed.ReferenceNodeId] = parsed;

        foreach (var sensor in persistedTriggerNode.EventTriggerConfigurations)
        {
            if (parsedSensorsByRefId.TryGetValue(sensor.ReferenceNodeId, out var parsedSensor))
                sensor.IsSensorConfigured = parsedSensor.IsSensorConfigured;
        }
    }
}
