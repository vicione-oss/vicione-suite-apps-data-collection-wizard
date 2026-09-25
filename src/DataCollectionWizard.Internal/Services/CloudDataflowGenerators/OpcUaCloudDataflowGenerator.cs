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

public class OpcUaCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : CloudDataflowTreeGenerator, ICloudDataflowGenerator
{
    private const string DefaultRootNodeName = "vicione";
    private const int MaxNodeNameLength = 256;

    public string Name => "opcua";

    protected override string PortDesignIdFolder => "Folder";
    protected override string PortDesignIdDataPointDouble => "DataPointFloat";
    protected override string PortDesignIdDataPointString => "DataPointString";

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
        var rootNodeName = GetSafeNodeName(DefaultRootNodeName);
        var rootNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdFolder, dataport, rootNodeName, null, DataPortTransferMode.None);
        var edgeNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdFolder, rootNode, instanceInformationProvider.Local.Name ?? instanceInformationProvider.Local.SerialNumber, null, DataPortTransferMode.None);
        var deviceNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdFolder, edgeNode, GetSafeNodeName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree!.Children, dataport, deviceNode, builder, result);

        return result;
    }

    // The OPC-UA Server DataPort's Folder/DataPoint name validation only rejects control characters and
    // caps length at 256, so unlike MQTT's topic-safe replacement this just strips what's disallowed.
    private protected override string GetSafeNodeName(string name)
    {
        var sanitized = new string([.. name.Where(c => !char.IsControl(c))]);
        return sanitized.Length > MaxNodeNameLength ? sanitized[..MaxNodeNameLength] : sanitized;
    }

    private static DataPort GenerateDataPort(Connection connection, OpcUaServerConnection? opcUaConnection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(opcUaConnection);

        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.OpcUaDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.OpcUaDataPort.Type);


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
