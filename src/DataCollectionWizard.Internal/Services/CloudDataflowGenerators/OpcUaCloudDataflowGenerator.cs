using ClusterManagement.Public.Connections.Contracts;
using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class OpcUaCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : ICloudDataflowGenerator
{
    private const string PortDesignIdOpcUaDataPointFloat = "DataPointFloat";
    private const string PortDesignIdOpcUaDataPointString = "DataPointString";
    private const string PortDesignIdOpcUaFolder = "Folder";
    private const string DefaultRootNodeName = "vicione";
    private const int MaxNodeNameLength = 256;

    public string Name => "opcua";

    public Dictionary<string, AggregationFunctionCloudInputs> GenerateCloudDataflow(Connection connection,
                                                                            IDeviceTreeMasterNode deviceTreeMaster,
                                                                            ClusterBuilder builder,
                                                                            Dataflow dataflow,
                                                                            string machineIdentifier,
                                                                            Dictionary<string, DataOutputInfo> dataOutputs,
                                                                            uint engineCycleInterval,
                                                                            ChildContainer cloudContainer,
                                                                            Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                            List<ProcessDataConfiguration> loggedProcessDataNodes,
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes)
    {
        var result = new Dictionary<string, AggregationFunctionCloudInputs>();
        var loggedNodeIds = loggedProcessDataNodes.Select(n => n.Node.Id).ToHashSet();
        var loggedTree = BuildLoggedTreeRecursively(deviceTreeMaster, loggedNodeIds, loggedProcessDataNodes);

        if (loggedTree is null)
            return result;

        var opcUaConnection = connection.GetOpcUaServerConnection();
        var dataport = GenerateDataPort(connection, opcUaConnection, deviceTreeMaster, builder, dataflow);

        // The OPC-UA Server DataPort has no client-side "RootNodeId" setting; the exposed address space is
        // always rooted at a fixed folder named after the suite.
        var rootNodeName = GetOpcUaSafeNodeName(DefaultRootNodeName);
        var rootNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdOpcUaFolder, dataport, rootNodeName, null, DataPortTransferMode.None);
        var edgeNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdOpcUaFolder, rootNode, instanceInformationProvider.Local.Name ?? instanceInformationProvider.Local.SerialNumber, null, DataPortTransferMode.None);
        var deviceNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdOpcUaFolder, edgeNode, GetOpcUaSafeNodeName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree!.Children, dataport, deviceNode, builder, result);

        return result;
    }

    // The OPC-UA Client DataPort's Folder/DataPoint name validation only rejects control characters and
    // caps length at 256, so unlike MQTT's topic-safe replacement this just strips what's disallowed.
    private static string GetOpcUaSafeNodeName(string name)
    {
        var sanitized = new string([.. name.Where(c => !char.IsControl(c))]);
        return sanitized.Length > MaxNodeNameLength ? sanitized[..MaxNodeNameLength] : sanitized;
    }

    internal void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder, Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        foreach (var child in children)
        {
            var dataportNodeDesignId = GetDataPortNodeDesignId(child);
            var dataportNodeTransferMode = GetDataPortNodeTransferMode(child);
            var dataportNodeValueType = GetDataportNodeValueType(child);
            DataPortTreeNode childNode;

            if (parent is null)
            {
                childNode = builder.Editors.DataPort.AddTreeNode(dataportNodeDesignId, dataPort, GetOpcUaSafeNodeName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }
            else
            {
                childNode = builder.Editors.DataPortTreeNode.AddTreeNode(dataportNodeDesignId, parent, GetOpcUaSafeNodeName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }

            if (child.DataConfig is not null)
            {
                result[child.DataConfig.Node.Id] = new AggregationFunctionCloudInputs()
                {
                    Avg = new CloudInput() { InputTreeNode = childNode },
                    Last = new CloudInput() { InputTreeNode = childNode },
                    Max = new CloudInput() { InputTreeNode = childNode },
                    Min = new CloudInput() { InputTreeNode = childNode },
                    Value = new CloudInput() { InputTreeNode = childNode },
                };
            }

            BuildDataportNodesRecursively(child.Children, dataPort, childNode, builder, result);
        }
    }

    // Only Real/Text data points are wired up for now, matching MqttCloudDataflowGenerator's scope. The
    // DataPort itself also defines DataPointBool/Integer/DateTime/Binary node types, so this can be
    // extended once those DataType values are confirmed and their mapping to CLR types is settled.
    private Type? GetDataportNodeValueType(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return null;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.Real:
                return typeof(float);
            case DataType.Text:
                return typeof(string);
            default:
                throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported.");
        }
    }

    private DataPortTransferMode GetDataPortNodeTransferMode(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return DataPortTransferMode.None;
        }

        return DataPortTransferMode.OnChange;
    }

    private string GetDataPortNodeDesignId(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return PortDesignIdOpcUaFolder;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.Real:
                return PortDesignIdOpcUaDataPointFloat;
            case DataType.Text:
                return PortDesignIdOpcUaDataPointString;
            default:
                throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported.");
        }
    }

    internal TreeModel? BuildLoggedTreeRecursively(IDeviceTreeBase node, IEnumerable<string> loggedNodeIds, List<ProcessDataConfiguration> loggedProcessDataNodes)
    {
        var children = new List<TreeModel>();

        foreach (var child in node.Children)
        {
            var loggedChild = BuildLoggedTreeRecursively(child, loggedNodeIds, loggedProcessDataNodes);

            if (loggedChild is not null)
            {
                children.Add(loggedChild);
            }
        }

        if (loggedNodeIds.Contains(node.Id))
        {
            return new TreeModel()
            {
                Children = children,
                DataConfig = loggedProcessDataNodes.FirstOrDefault(n => n.Node.Id == node.Id),
                Id = node.Id,
                Name = node.Name,
            };
        }

        if (children.Any())
        {
            return new TreeModel()
            {
                Children = children,
                Id = node.Id,
                Name = node.Name,
            };
        }

        return null;
    }

    private static DataPort GenerateDataPort(Connection connection, OpcUaServerConnection? opcUaConnection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(opcUaConnection);

        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.OpcUaDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.OpcUaDataPort.Type);

        // Property design ids mirror the OpcUaServerConnection property groups and follow the same naming
        // the OPC-UA Client DataPort mapping uses; the server-only ids (Namespace, SecurityPolicy,
        // TransportQuotas, Min/MaxPublishingInterval) are not confirmed against the actual OPC-UA Server
        // DataPort registration — verify before relying on this in a real deploy.

        // ---- Connection ----
        builder.Editors.DataPort.AddProperty("ApplicationName", dataPort, null, opcUaConnection.ApplicationName);
        builder.Editors.DataPort.AddProperty("ApplicationUri", dataPort, null, opcUaConnection.ApplicationUri);
        builder.Editors.DataPort.AddProperty("Namespace", dataPort, null, opcUaConnection.Namespace);
        builder.Editors.DataPort.AddProperty("Server", dataPort, null, opcUaConnection.Server);
        builder.Editors.DataPort.AddProperty("Port", dataPort, null, opcUaConnection.Port);
        builder.Editors.DataPort.AddProperty("Endpoint", dataPort, null, opcUaConnection.Endpoint);
        builder.Editors.DataPort.AddProperty("SecurityPolicy", dataPort, null, (byte)opcUaConnection.SecurityPolicy);
        builder.Editors.DataPort.AddProperty("TransportQuotas", dataPort, null, opcUaConnection.UseCustomTransportQuotas);
        builder.Editors.DataPort.AddProperty("MinPublishingInterval", dataPort, null, opcUaConnection.MinPublishingIntervalMilliseconds);
        builder.Editors.DataPort.AddProperty("MaxPublishingInterval", dataPort, null, opcUaConnection.MaxPublishingIntervalMilliseconds);

        // ---- Authentication ----
        builder.Editors.DataPort.AddProperty("UserAuthenticationType", dataPort, null, (byte)opcUaConnection.AuthenticationMode);
        builder.Editors.DataPort.AddProperty("User", dataPort, null, opcUaConnection.Username);
        builder.Editors.DataPort.AddProperty("Password", dataPort, null, opcUaConnection.Password);

        // ---- Certificate ----
        builder.Editors.DataPort.AddProperty("ApplicationCertificateSubject", dataPort, null, opcUaConnection.ApplicationCertificateSubject);
        builder.Editors.DataPort.AddProperty("ApplicationCertificatesStoreType", dataPort, null, (byte)opcUaConnection.ApplicationCertificatesStoreType);
        builder.Editors.DataPort.AddProperty("ApplicationCertificatesStorePath", dataPort, null, opcUaConnection.ApplicationCertificatesStorePath);
        builder.Editors.DataPort.AddProperty("TrustedCertificatesStoreType", dataPort, null, (byte)opcUaConnection.TrustedCertificatesStoreType);
        builder.Editors.DataPort.AddProperty("TrustedCertificatesStorePath", dataPort, null, opcUaConnection.TrustedCertificatesStorePath);
        builder.Editors.DataPort.AddProperty("TrustedIssuerCertificatesStoreType", dataPort, null, (byte)opcUaConnection.TrustedIssuerCertificatesStoreType);
        builder.Editors.DataPort.AddProperty("TrustedIssuerCertificatesStorePath", dataPort, null, opcUaConnection.TrustedIssuerCertificatesStorePath);
        builder.Editors.DataPort.AddProperty("AutoAcceptUntrustedCertificates", dataPort, null, opcUaConnection.AutoAcceptUntrustedCertificates);

        return dataPort;
    }
}
