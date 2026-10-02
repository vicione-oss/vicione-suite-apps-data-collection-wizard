using ClusterManagement.Public.Connections.Contracts;
using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.SystemConfiguration.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class OpcUaCloudDataflowGenerator : CloudDataflowTreeGenerator, ICloudDataflowGenerator
{
    // Values of the DataPort's *CertificatesStoreType properties (0 = none, 1 = X509 store, 2 = directory). The
    // connection contract no longer exposes a store type: a configured path means a directory store, none means none.
    private const byte CertificateStoreTypeNone = 0;
    private const byte CertificateStoreTypeDirectory = 2;

    public string Name => "opcua";

    private protected override int? MaxNodeNameLength => 256;

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
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes,
                                                                            IReadOnlyList<NetworkInterface> hostNetworkInterfaces)
    {
        if (!OpcUaCloudFilter.IsOpcUaConnection(connection))
        {
            throw new ArgumentException("Invalid connection type", nameof(connection));
        }

        var result = new Dictionary<string, AggregationFunctionCloudInputs>();
        var loggedNodeIds = loggedProcessDataNodes.Select(n => n.Node.Id).ToHashSet();
        var loggedTree = BuildLoggedTreeRecursively(deviceTreeMaster, loggedNodeIds, loggedProcessDataNodes);

        if (loggedTree is null)
            return result;

        var opcUaConnection = connection.GetOpcUaServerConnection()
            ?? throw new ArgumentException("Connection has no OPC UA server configuration", nameof(connection));

        // The server cannot start without its own application instance certificate, which the DataPort only
        // loads from a configured store (required in its ruleset, but so far only enforced by the editor).
        if (string.IsNullOrWhiteSpace(opcUaConnection.ApplicationCertificatesPath))
            throw new InvalidOperationException($"OPC UA connection '{connection.Name}' has no application certificates path.");

        var serverAddress = GetServerAddress(connection, opcUaConnection, hostNetworkInterfaces);
        var dataport = GenerateDataPort(connection, opcUaConnection, serverAddress, deviceTreeMaster, builder, dataflow);
        var deviceNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdFolder, dataport, GetSafeNodeName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree.Children, dataport, deviceNode, builder, dataOutputs, result);

        return result;
    }

    // The OPC-UA Server DataPort's Folder/DataPoint name validation only rejects control characters and
    // caps length at 256. Dots are allowed there, but the DataPort joins the names of a node's path with dots
    // to build its NodeId, so a data point "Temp.1" and a data point "1" in a folder "Temp" would get the same
    // NodeId and the server would fail to start. Replacing them keeps every path, and so every NodeId, unique.
    private protected override string GetSafeNodeName(string name)
    {
        var sanitized = new string([.. name.Where(c => !char.IsControl(c)).Select(c => c == '.' ? '_' : c)]);
        return sanitized.Length > MaxNodeNameLength ? sanitized[..MaxNodeNameLength.Value] : sanitized;
    }

    // The connection only names the host interface the server binds to, while the DataPort needs an address,
    // so the interface's current IPv4 address is looked up in the host's network configuration.
    private static string GetServerAddress(Connection connection, OpcUaServerConnection opcUaConnection, IReadOnlyList<NetworkInterface> hostNetworkInterfaces)
    {
        if (opcUaConnection.NetworkInterface == OpcUaServerConnection.LocalNetworkInterface)
            return "127.0.0.1";

        var networkInterface = hostNetworkInterfaces.FirstOrDefault(i => string.Equals(i.Name, opcUaConnection.NetworkInterface, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Network interface '{opcUaConnection.NetworkInterface}' of OPC UA connection '{connection.Name}' was not found on the host.");

        return networkInterface.IPv4Address?.ToString()
            ?? throw new InvalidOperationException($"Network interface '{opcUaConnection.NetworkInterface}' of OPC UA connection '{connection.Name}' has no IPv4 address.");
    }

    private static DataPort GenerateDataPort(Connection connection, OpcUaServerConnection opcUaConnection, string serverAddress, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow)
    {
        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.OpcUaDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.OpcUaDataPort.Type);

        // ---- Connection ----
        builder.Editors.DataPort.AddProperty("ApplicationName", dataPort, null, opcUaConnection.ApplicationName);
        builder.Editors.DataPort.AddProperty("ApplicationUri", dataPort, null, opcUaConnection.ApplicationUri);
        builder.Editors.DataPort.AddProperty("Namespace", dataPort, null, opcUaConnection.Namespace);
        builder.Editors.DataPort.AddProperty("Server", dataPort, null, serverAddress);
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
        builder.Editors.DataPort.AddProperty("ApplicationCertificatesStoreType", dataPort, null, CertificateStoreTypeDirectory);
        builder.Editors.DataPort.AddProperty("ApplicationCertificatesStorePath", dataPort, null, opcUaConnection.ApplicationCertificatesPath);
        builder.Editors.DataPort.AddProperty("TrustedCertificatesStoreType", dataPort, null, GetCertificateStoreType(opcUaConnection.TrustedCertificatesPath));
        builder.Editors.DataPort.AddProperty("TrustedCertificatesStorePath", dataPort, null, opcUaConnection.TrustedCertificatesPath);
        builder.Editors.DataPort.AddProperty("TrustedIssuerCertificatesStoreType", dataPort, null, GetCertificateStoreType(opcUaConnection.TrustedIssuerCertificatesPath));
        builder.Editors.DataPort.AddProperty("TrustedIssuerCertificatesStorePath", dataPort, null, opcUaConnection.TrustedIssuerCertificatesPath);
        builder.Editors.DataPort.AddProperty("AutoAcceptUntrustedCertificates", dataPort, null, opcUaConnection.AutoAcceptUntrustedCertificates);

        return dataPort;
    }

    private static byte GetCertificateStoreType(string? path)
        => string.IsNullOrEmpty(path) ? CertificateStoreTypeNone : CertificateStoreTypeDirectory;
}
