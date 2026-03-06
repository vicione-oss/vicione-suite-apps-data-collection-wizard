using DataCollectionWizard.Internal.Extensions;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Comparer;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Internal.Services;

public static class DeviceTreeBuilder
{
    public const string IdSeparatorNameAlias = "__!__";

    private static void AddCloudConfigurations(IDeviceTreeBase deviceTree, IReadOnlyCollection<Connection> cloudConfigurations)
    {
        var nodes = deviceTree.GetNodeAndDescendants().ToArray();
        var dataNodes = nodes.OfType<IDeviceTreeDataNode>();

        foreach (var node in dataNodes)
            node.AddConfigurations(cloudConfigurations);

        var configurableBlobNodes = nodes.OfType<IDeviceTreeConfigurableRawDataNode>();

        foreach (var node in configurableBlobNodes)
        {
            foreach (var cloudConfig in cloudConfigurations)
            {
                if (!node.RawDataConfigurations.ContainsKey(cloudConfig.Id))
                {
                    node.RawDataConfigurations.Add(cloudConfig.Id, new RawDataSettings
                    {
                        Duration = 4000,
                        Frequency = 100000,
                    });
                }
            }
        }
    }

    private static void AddElements(IDeviceTreeBase persistedDeviceTree, IDeviceTreeBase parsedDeviceTree)
    {
        AddNewElements<DeviceTreeIoLinkMaster>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeIoLinkMasterPort>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeDevice>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseDevice>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeStructureNode>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseObject>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseInput>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseAlarm>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseCounter>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseVariants>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeProcessData>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeConstantData>(persistedDeviceTree, parsedDeviceTree);
        AddNewElements<DeviceTreeVseRawData>(persistedDeviceTree, parsedDeviceTree);
    }

    private static void AddNewElements<T>(IDeviceTreeBase persistedDeviceTree, IDeviceTreeBase parsedDeviceTree) where T : class, IDeviceTreeBase
    {
        var currentNodes = persistedDeviceTree.GetNodeAndDescendants().ToDictionary(n => n.Id, n => n);
        var currentMasterDevices = currentNodes.Values
                                               .OfType<DeviceTreeIoLinkMaster>()
                                               .GroupBy(m => m.Url.AbsoluteUri)
                                               .ToDictionary(g => g.Key, g => g.ToArray());

        var parsedNodes = parsedDeviceTree.GetNodeAndDescendants().ToArray();
        var currentElements = currentNodes.Values.OfType<T>().ToDictionary(n => n.Id, n => n);
        var newIoTCoreElements = parsedNodes.OfType<T>()
            .Where(i => i is not DeviceTreeIoLinkMaster m || currentMasterDevices.ContainsKey(m.Url.AbsoluteUri))
            .Where(i => !currentElements.ContainsKey(i.Id))
            .ToArray();

        var parsedChildsParents = new Dictionary<string, string>();

        foreach (var parsedNode in parsedNodes)
        {
            foreach (var child in parsedNode.Children)
                parsedChildsParents.Add(child.Id, parsedNode.Id);
        }

        foreach (var element in newIoTCoreElements)
        {
            var newElement = element.Clone();
            foreach (var ele in newElement.GetNodeAndDescendants())
            {
                ele.IsNew = true;
            }

            var parentId = parsedChildsParents[element.Id];
            var parent = currentNodes[parentId];

            if (!parent.Children.Any(c => c.Id == element.Id))
            {
                parent.Children.Add(newElement);
                currentNodes[element.Id] = newElement;
            }
        }
    }

    private static void AddNewSensors(IDeviceTreeEventTriggerDataNode persistetTriggerNode, IDeviceTreeEventTriggerDataNode parsedTriggerNode)
    {
        foreach (var sensor in parsedTriggerNode.EventTriggerConfigurations)
        {
            if (!persistetTriggerNode.EventTriggerConfigurations.Any(t => t.ReferenceNodeId == sensor.ReferenceNodeId && t.Name == sensor.Name))
            {
                persistetTriggerNode.EventTriggerConfigurations.Add(new EventTriggerConfiguration
                {
                    Name = sensor.Name,
                    ReferenceNodeId = sensor.ReferenceNodeId,
                    IsSensorConfigured = sensor.IsSensorConfigured,
                });
            }
        }
    }

    public static Dictionary<IDeviceTreeBase, IDeviceTreeBase?> CorrelateParsedDevices(IDeviceTreeBase persistedDeviceTree, List<IDeviceTreeBase> parsedDevices)
    {
        // Index der "parsed"-Knoten per Id
        var parsedById = parsedDevices
            .SelectMany(d => d.GetNodeAndDescendants())
            .ToDictionary(n => n.Id, n => n, StringComparer.Ordinal);

        // Alle persistierten Knoten außer Root
        var persisted = persistedDeviceTree
            .GetNodeAndDescendants()
            .Where(n => n is not DeviceTreeRoot);

        // Korrelieren in O(1) pro Knoten
        return persisted.ToDictionary(
            per => per,
            per => parsedById.TryGetValue(per.Id, out var match) ? match : null);
    }

    public static void ExtendCurrentDeviceTree(IDeviceTreeBase persistedDeviceTree, List<IDeviceTreeBase> parsedDevices, IReadOnlyCollection<Connection> cloudConfigurations, bool retainNewFlag = false)
    {
        var parsedDeviceTree = new DeviceTreeRoot
        {
            Children = parsedDevices,
        };

        if (!retainNewFlag)
            SetIsNew(persistedDeviceTree, false);

        AddElements(persistedDeviceTree, parsedDeviceTree);
        RemoveOnlineGenericMasterDevices(persistedDeviceTree);

        var persistentParsedDeviceData = CorrelateParsedDevices(persistedDeviceTree, parsedDevices);

        UpdateVseNames(persistentParsedDeviceData);
        UpdateTriggerNodes(persistentParsedDeviceData);
        UpdateOnlineStatus(persistentParsedDeviceData);
        UpdateUnknownStatus(persistentParsedDeviceData);
        UpdateConstantNodes(persistentParsedDeviceData);
        UpdateAliases(persistentParsedDeviceData);
        UpdatePaths(persistentParsedDeviceData);
        UpdateCloudConfigurations(persistedDeviceTree, cloudConfigurations);
        UpdateStructureUnits(persistentParsedDeviceData);
        UpdateRawDataIdices(persistentParsedDeviceData);
        UpdateApplicationSpecificTag(persistentParsedDeviceData);
        RemoveEmptyStructureNodes(persistentParsedDeviceData, persistedDeviceTree);
    }

    private static void UpdateApplicationSpecificTag(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevicess)
    {
        var relevantNodes = persistedDevicesParsedDevicess.Where(n => n.Key is DeviceTreeDevice)
                                                          .Where(n => n.Value as DeviceTreeDevice is not null);

        foreach (var node in relevantNodes)
        {
            ((DeviceTreeDevice)node.Key).ApplicationSpecificTag = ((DeviceTreeDevice)node.Value!).ApplicationSpecificTag;
        }
    }

    private static void UpdateStructureUnits(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevicess)
    {
        var relevantNodes = persistedDevicesParsedDevicess.Where(n => n.Key is DeviceTreeProcessData)
                                                          .Where(n => n.Value as DeviceTreeProcessData is not null);

        foreach (var node in relevantNodes)
        {
            ((DeviceTreeProcessData)node.Key).StructureUnit = ((DeviceTreeProcessData)node.Value!).StructureUnit;
        }
    }

    private static void UpdateRawDataIdices(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevicess)
    {
        var relevantNodes = persistedDevicesParsedDevicess.Where(n => n.Key is DeviceTreeVseRawData)
                                                          .Where(n => n.Value as DeviceTreeVseRawData is not null);

        foreach (var node in relevantNodes)
        {
            ((DeviceTreeVseRawData)node.Key).Index = ((DeviceTreeVseRawData)node.Value!).Index;
        }
    }

    private static void UpdatePaths(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var relevantNodes = persistedDevicesParsedDevices.Where(n => n.Key is IDeviceTreeVseDataParent)
                                                         .Where(n => n.Value as IDeviceTreeVseDataParent is not null);

        foreach (var node in relevantNodes)
        {
            ((IDeviceTreeVseDataParent)node.Key).Path = ((IDeviceTreeVseDataParent)node.Value!).Path;
        }
    }

    private static Dictionary<string, string> GetNodeNames(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var persistedNodeNames = persistedDevicesParsedDevices.Keys.ToDictionary(n => n.Id, n =>
        {
            if (n is IAliasStructureNode aliasNode) return aliasNode.Alias;
            return n.Name;
        });

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var parsedNodeNames = persistedDevicesParsedDevices.Values
                                                           .Where(n => n is not null)
                                                           .ToDictionary(n => n.Id, n =>
                                                            {
                                                                if (n is IAliasStructureNode aliasNode)
                                                                    return aliasNode.Alias;

                                                                return n.Name;
                                                            });
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        foreach (var parsedNodeName in parsedNodeNames)
        {
            persistedNodeNames[parsedNodeName.Key] = parsedNodeName.Value;
        }

        return persistedNodeNames!;
    }

    private static void RemoveCloudConfigurations(IDeviceTreeBase deviceTree, IReadOnlyCollection<Connection> exisitingCloudConfigurations)
    {
        var nodes = deviceTree.GetNodeAndDescendants().ToArray();
        var dataNodes = nodes.OfType<IDeviceTreeDataNode>().ToArray();
        var cloudIds = exisitingCloudConfigurations.Select(c => c.Id).ToList();

        foreach (var node in dataNodes)
            node.RemoveConfigurations(cloudIds);

        var configurableBlobNodes = nodes.OfType<IDeviceTreeConfigurableRawDataNode>();

        foreach (var node in configurableBlobNodes)
        {
            node.RawDataConfigurations.Clear();
            var newRawDataConfigs = node.RawDataConfigurations.Where(c => exisitingCloudConfigurations.Any(cc => cc.Id == c.Key)).ToDictionary(c => c.Key, c => c.Value);
            newRawDataConfigs.CopyTo(node.RawDataConfigurations);
        }
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
        => RemoveEmptyStructureNodes(deviceTree.GetNodeAndDescendants().ToDictionary(n => n, n => (IDeviceTreeBase?)null), deviceTree);

    public static void RemoveEventTriggers(DeviceTreeRoot tree, IDeviceTreeBase deletingNode)
    {
        var eventTriggerNodes = tree.GetNodeAndDescendants().OfType<IDeviceTreeEventTriggerDataNode>();

        foreach (var triggerNode in eventTriggerNodes)
        {
            triggerNode.EventTriggerConfigurations.RemoveAll(e => e.ReferenceNodeId == deletingNode.Id);
        }
    }

    private static void RemoveOnlineGenericMasterDevices(IDeviceTreeBase deviceTree)
    {
        var currentMasterDeviceList = deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToArray();
        var x = currentMasterDeviceList.Where(c => c.IsOffline);
        var y = x.Where(c => currentMasterDeviceList.Any(cm => !cm.IsOffline && cm.Url == c.Url));
        var mastersToRemove = y.ToArray();

        foreach (var master in mastersToRemove)
        {
            deviceTree.Children.Remove(master);
        }

        var currentMasterDevices = deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToArray();
        foreach (var master in mastersToRemove)
        {
            currentMasterDevices.First(c => c.Url == master.Url).IsNew = master.IsNew;
        }
    }

    private static void SetIsNew(IDeviceTreeBase persistedDeviceTree, bool isNew)
    {
        var nodes = persistedDeviceTree.GetNodeAndDescendants();

        foreach (var node in nodes)
        {
            node.IsNew = isNew;
        }
    }

    private static void SetKnownStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var setKnownElements = persistedDevicesParsedDevices
            .Where(c => ((DeviceTreeDevice)c.Key).IsUnknown)
            .Where(c => c.Value is not null && !((DeviceTreeDevice)c.Value).IsUnknown)
            .ToArray();

        foreach (var element in setKnownElements)
        {
            var device = (DeviceTreeDevice)element.Key;
            device.IsUnknown = false;
            device.Description = element.Value!.Description;
        }
    }

    private static void SetOfflineStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var setOfflineElements = persistedDevicesParsedDevices
            .Where(c => !c.Key.IsOffline)
            .Where(c => c.Value?.IsOffline ?? true)
            .Where(c => c.Key is not DeviceTreeRoot);

        foreach (var element in setOfflineElements)
        {
            element.Key.IsOffline = true;
        }
    }

    private static void SetOnlineStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var setOnlineElements = persistedDevicesParsedDevices
            .Where(c => c.Key.IsOffline)
            .Where(c => !(c.Value?.IsOffline ?? true))
            .Where(c => c.Key is not DeviceTreeRoot);

        foreach (var element in setOnlineElements)
        {
            element.Key.IsOffline = false;
        }
    }

    private static void SetUnknownStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var setUnknownElements = persistedDevicesParsedDevices
            .Where(c => !((DeviceTreeDevice)c.Key).IsUnknown)
            .Where(c => c.Value is not null && ((DeviceTreeDevice)c.Value).IsUnknown);

        foreach (var element in setUnknownElements)
        {
            var device = (DeviceTreeDevice)element.Key;
            device.IsUnknown = true;
            device.Description = element.Value!.Description;
        }
    }

    public static void SortSensors(IDeviceTreeEventTriggerDataNode persistedTriggerNode, Dictionary<string, string> nodeNames)
    {
        var newTriggerConfigurations = persistedTriggerNode.EventTriggerConfigurations
            .OrderBy(t => !t.IsSensorConfigured)
            .ThenBy(t =>
            {
                if (nodeNames.TryGetValue(t.ReferenceNodeId, out var nodeName))
                {
                    return nodeName;
                }

                return t.ReferenceNodeId;
            }, AlphaNumericComparer<string>.Default)
            .ToList();

        persistedTriggerNode.EventTriggerConfigurations.Clear();
        persistedTriggerNode.EventTriggerConfigurations.AddRange(newTriggerConfigurations);
    }

    private static void UpdateAliases(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var relevantNodes = persistedDevicesParsedDevices.Where(n => n.Key is IAliasStructureNode)
                                                         .Where(n => n.Value as IAliasStructureNode is not null);

        foreach (var node in relevantNodes)
        {
            ((IAliasStructureNode)node.Key).Alias = ((IAliasStructureNode)node.Value!).Alias;
        }
    }

    private static void UpdateCloudConfigurations(IDeviceTreeBase deviceTree, IReadOnlyCollection<Connection> connections)
    {
        RemoveCloudConfigurations(deviceTree, connections);
        AddCloudConfigurations(deviceTree, connections);
    }

    private static void UpdateConstantNodes(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var relevantNodes = persistedDevicesParsedDevices.Where(n => n.Key is DeviceTreeConstantData)
                                                         .Where(n => n.Value as DeviceTreeConstantData is not null);

        foreach (var node in relevantNodes)
        {
            ((DeviceTreeConstantData)node.Key).Value = ((DeviceTreeConstantData)node.Value!).Value;
        }
    }

    public static void UpdateOnlineStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        SetOnlineStatus(persistedDevicesParsedDevices);
        SetOfflineStatus(persistedDevicesParsedDevices);
    }

    private static void UpdateSensors(IDeviceTreeEventTriggerDataNode persistedTriggerNode, IDeviceTreeEventTriggerDataNode parsedTriggerNode)
    {
        foreach (var sensor in persistedTriggerNode.EventTriggerConfigurations)
        {
            var parsedSensor = parsedTriggerNode.EventTriggerConfigurations.FirstOrDefault(n => n.ReferenceNodeId == sensor.ReferenceNodeId);

            if (parsedSensor is not null)
            {
                sensor.IsSensorConfigured = parsedSensor.IsSensorConfigured;
            }
        }
    }

    private static void UpdateTriggerNodes(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {

        var relevantNodes = persistedDevicesParsedDevices.Where(n => n.Key is IDeviceTreeEventTriggerDataNode)
                                                         .Where(n => n.Value as IDeviceTreeEventTriggerDataNode is not null);
        var nodeNames = GetNodeNames(persistedDevicesParsedDevices);

        foreach (var node in relevantNodes)
        {
            var persistedTriggerNode = (IDeviceTreeEventTriggerDataNode)node.Key;
            var parsedTriggerNode = (IDeviceTreeEventTriggerDataNode)node.Value!;

            AddNewSensors(persistedTriggerNode, parsedTriggerNode);
            SortSensors(persistedTriggerNode, nodeNames);
            UpdateSensors(persistedTriggerNode, parsedTriggerNode);
        }
    }

    private static void UpdateUnknownStatus(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var deviceTreeDevices = persistedDevicesParsedDevices.Where(d => d.Key is DeviceTreeDevice && d.Value is DeviceTreeDevice)
                                                             .ToDictionary(d => d.Key, d => d.Value);

        SetKnownStatus(deviceTreeDevices);
        SetUnknownStatus(deviceTreeDevices);
    }

    private static void UpdateVseNames(Dictionary<IDeviceTreeBase, IDeviceTreeBase?> persistedDevicesParsedDevices)
    {
        var relevantNodes = persistedDevicesParsedDevices.Where(n => n.Key is DeviceTreeVseDevice)
                                                         .Where(n => n.Value as DeviceTreeVseDevice is not null);

        foreach (var node in relevantNodes)
        {
            node.Key.Name = node.Value!.Name;
        }
    }
}
