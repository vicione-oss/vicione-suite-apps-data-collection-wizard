using System.Net;
using ClusterManagement.Public.Connections.Contracts;
using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using Sdk.Connections.Contracts;
using Sdk.SystemConfiguration.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Tests.CloudDataflowGenerators;

/// <summary>
/// Tests for OpcUaCloudDataflowGenerator. BuildLoggedTreeRecursively and BuildDataportNodesRecursively
/// live on the shared CloudDataflowTreeGenerator base class, so only a representative subset is
/// re-verified here; see MqttCloudDataflowGeneratorTests for the exhaustive coverage of that logic. The
/// remaining tests focus on what differs for OPC UA: node name sanitization rules and the DataPort's
/// property schema.
/// </summary>
public class OpcUaCloudDataflowGeneratorTests
{
    public class BuildLoggedTreeRecursivelyTests
    {
        [Fact]
        public void Returns_null_when_node_is_not_logged_and_has_no_logged_children()
        {
            // Arrange
            var node = new DeviceTreeStructureNode { Id = "node1", Name = "Node 1" };
            var loggedNodeIds = new HashSet<string> { "node2", "node3" };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();

            // Act
            var result = CloudDataflowTreeGenerator.BuildLoggedTreeRecursively(node, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Returns_tree_model_with_data_config_when_node_is_logged_with_process_data()
        {
            // Arrange
            var loggedNodeId = "logged-node";
            var mockDataNode = new DeviceTreeProcessData() { DataType = DataType.Real, Id = loggedNodeId, Name = "Logged Node" };
            var processDataConfig = new ProcessDataConfiguration(mockDataNode, new CompressorConfiguration() { DataGroupIdentifier = Guid.Parse("12300000-0000-0000-1234-000000000000") });
            var loggedNodeIds = new HashSet<string> { loggedNodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration> { processDataConfig };

            // Act
            var result = CloudDataflowTreeGenerator.BuildLoggedTreeRecursively(mockDataNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loggedNodeId, result.Id);
            Assert.NotNull(result.DataConfig);
            Assert.Same(processDataConfig, result.DataConfig);
        }

        [Fact]
        public void Includes_parent_when_child_is_logged()
        {
            // Arrange
            var parentNodeId = "parent";
            var childNodeId = "child";

            var childNode = new DeviceTreeStructureNode { Id = childNodeId, Name = "Child" };
            var parentNode = new DeviceTreeStructureNode { Children = [childNode], Id = parentNodeId, Name = "Parent" };

            var loggedNodeIds = new HashSet<string> { childNodeId }; // Only child is logged
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();

            // Act
            var result = CloudDataflowTreeGenerator.BuildLoggedTreeRecursively(parentNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result); // Parent should be included because child is logged
            Assert.Equal(parentNodeId, result.Id);
            var children = result.Children;
            Assert.Single(children);
            Assert.Equal(childNodeId, children[0].Id);
        }
    }

    /// <summary>
    /// Tests for BuildDataportNodesRecursively, which creates the OPC UA Folder/DataPoint tree
    /// (DataPortTreeNodes) from a filtered TreeModel and populates the cloud input dictionary.
    /// A real ClusterBuilder is used since its editors are sealed classes that cannot be mocked.
    /// </summary>
    public class BuildDataportNodesRecursivelyTests
    {
        private static (ClusterBuilder Builder, DataPort DataPort) CreateDataPort()
        {
            var resolver = Substitute.For<IDependencyResolver>();
            resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
                .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });

            var builder = new ClusterBuilder(resolver);
            builder.AddDataPortDesign(FunctionBlocks.OpcUaDataPort.DesignId);

            var dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));
            var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.OpcUaDataPort.DesignId, "port",
                DataPortDirection.Out, FunctionBlocks.OpcUaDataPort.Type);

            return (builder, dataPort);
        }

        private static OpcUaCloudDataflowGenerator CreateGenerator()
            => new(Substitute.For<ISystemConfigurationService>());

        [Fact]
        public void Creates_folder_node_and_only_strips_control_characters_from_name()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var children = new List<TreeModel> { new() { DataConfig = null, Id = "folder1", Name = "My Folder!" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("Folder", node.DesignId);
            Assert.Equal("My Folder!", node.Name); // unlike MQTT topics, OPC UA node names only forbid control characters
            Assert.Null(node.ValueType);
            Assert.Equal(DataPortTransferMode.None, node.TransferMode);
            Assert.Empty(result); // folders never get an entry in the cloud input dictionary
        }

        [Theory]
        [InlineData(DataType.Flag)]
        [InlineData(DataType.Real)]
        [InlineData(DataType.UnsignedWhole)]
        [InlineData(DataType.Whole)]
        public void Creates_float_data_point_node_for_numeric_data_types(DataType dataType)
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var dataNode = new DeviceTreeProcessData { DataType = dataType, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("DataPointFloat", node.DesignId);
            Assert.Equal(typeof(double), node.ValueType);
            Assert.Equal(DataPortTransferMode.OnChange, node.TransferMode);
        }

        [Fact]
        public void Creates_string_data_point_node_for_text_data_type()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Text, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("DataPointString", node.DesignId);
            Assert.Equal(typeof(string), node.ValueType);
            Assert.Equal(DataPortTransferMode.OnChange, node.TransferMode);
        }

        [Theory]
        [InlineData(DataType.Blob)]
        [InlineData(DataType.Unknown)]
        [InlineData(DataType.Octets)]
        public void Throws_for_unsupported_data_type(DataType dataType)
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var dataNode = new DeviceTreeProcessData { DataType = dataType, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result));
        }

        [Fact]
        public void Populates_result_with_same_input_node_for_all_pooling_modes()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            var input = Assert.Contains("n1", result);
            var createdNode = Assert.Single(dataPort.TreeNodes);
            Assert.Same(createdNode, input.Avg!.InputTreeNode);
            Assert.Same(createdNode, input.Last!.InputTreeNode);
            Assert.Same(createdNode, input.Max!.InputTreeNode);
            Assert.Same(createdNode, input.Min!.InputTreeNode);
            Assert.Same(createdNode, input.Value!.InputTreeNode);
        }

        [Fact]
        public void Fits_sibling_suffix_within_the_name_length_limit()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var longName = new string('a', 256);
            var children = new List<TreeModel>
            {
                new() { Id = "a", Name = longName + "-first" }, // both truncate to the same 256 characters
                new() { Id = "b", Name = longName + "-second" },
            };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            Assert.Equal([longName, longName[..254] + "_2"], dataPort.TreeNodes.Select(n => n.Name));
        }

        [Fact]
        public void Nests_child_node_under_parent_instead_of_data_port()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, AggregationFunctionCloudInputs>();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Data 1" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel>
            {
                new()
                {
                    Children = [new() { DataConfig = config, Id = "n1", Name = "Data 1" }],
                    Id = "folder1",
                    Name = "Folder One",
                },
            };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, [], result);

            // Assert
            var folderNode = Assert.Single(dataPort.TreeNodes); // only the root folder sits directly under the data port
            Assert.Equal("Folder One", folderNode.Name);
            var dataPointNode = Assert.Single(folderNode.Children); // the data point is nested under the folder, not the data port
            Assert.Equal("Data 1", dataPointNode.Name);
            Assert.Equal("n1", Assert.Single(result.Keys));
        }
    }

    /// <summary>
    /// Tests for the public GenerateCloudDataflow entry point, covering the folder scaffold
    /// (root/edge/device) and the empty-result short-circuit when nothing is logged.
    /// </summary>
    [Collection(NetworkInterfacesCacheCollection.Name)]
    public class GenerateCloudDataflowTests
    {
        private static readonly IReadOnlyList<NetworkInterface> s_hostNetworkInterfaces =
        [
            new NetworkInterface { Name = "lan0", IPv4Address = IPAddress.Parse("192.168.0.1") },
            new NetworkInterface { Name = "lan1", IPv4Address = IPAddress.Parse("10.0.0.5") },
        ];

        private static OpcUaCloudDataflowGenerator CreateGenerator(IReadOnlyList<NetworkInterface>? hostNetworkInterfaces = null)
        {
            var systemConfigurationService = Substitute.For<ISystemConfigurationService>();
            systemConfigurationService.GetNetworkInterfacesAsync(Arg.Any<CancellationToken>()).Returns(hostNetworkInterfaces ?? s_hostNetworkInterfaces);
            OpcUaCloudDataflowGenerator.ResetNetworkInterfacesCache();
            return new OpcUaCloudDataflowGenerator(systemConfigurationService);
        }

        private static ClusterBuilder CreateBuilder(out Dataflow dataflow)
        {
            var resolver = Substitute.For<IDependencyResolver>();
            resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
                .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });

            var builder = new ClusterBuilder(resolver);
            builder.AddDataPortDesign(FunctionBlocks.OpcUaDataPort.DesignId);
            dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));

            return builder;
        }

        private static Connection CreateOpcUaConnection(string name = "MyServer")
        {
            var connection = new Connection { Id = Guid.NewGuid(), Name = name, Type = ConnectionType.OpcUaServer };
            connection.SetOpcUaServerConnection(new OpcUaServerConnection { Port = 4840, NetworkInterface = "lan1" });
            return connection;
        }

        [Fact]
        public void Returns_empty_dictionary_and_creates_no_data_port_when_nothing_is_logged()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateOpcUaConnection();
            var deviceTreeMaster = new DeviceTreeVseDevice { Alias = "Dev", Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = CreateGenerator();

            // Act
            var result = generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [], []);

            // Assert
            Assert.Empty(result);
            Assert.Empty(dataflow.DataPorts);
        }

        [Fact]
        public void Builds_folder_hierarchy_rooted_at_device_host()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateOpcUaConnection();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice
            {
                Alias = "Dev",
                Children = [dataNode],
                Id = "id",
                MacAddress = "aa:bb",
                Name = "Dev",
                Url = new Uri("http://my.server.local:4840"),
            };
            var generator = CreateGenerator();

            // Act
            var result = generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);
            var deviceNode = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("my.server.local", deviceNode.Name); // dots are allowed for OPC UA node names, only control characters are stripped
            var dataPointNode = Assert.Single(deviceNode.Children);
            Assert.Equal("DataPointFloat", dataPointNode.DesignId);
            Assert.Equal("n1", Assert.Single(result.Keys));
        }

        [Fact]
        public void Throws_for_connection_that_is_not_an_opcua_server_connection()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = new Connection { Id = Guid.NewGuid(), Name = "Mqtt", Type = ConnectionType.Mqtt };
            var deviceTreeMaster = new DeviceTreeVseDevice { Alias = "Dev", Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = CreateGenerator();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [], []));
        }

        [Theory]
        [InlineData("lan2")] // unknown interface
        [InlineData("LAN1")] // interface names are case sensitive
        public void Throws_when_the_connection_interface_does_not_exist_on_the_host(string networkInterface)
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = new Connection { Id = Guid.NewGuid(), Name = "MyServer", Type = ConnectionType.OpcUaServer };
            connection.SetOpcUaServerConnection(new OpcUaServerConnection { Port = 4840, NetworkInterface = networkInterface });
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice { Alias = "Dev", Children = [dataNode], Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = CreateGenerator();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []));
            Assert.Contains(networkInterface, exception.Message);
            Assert.Empty(dataflow.DataPorts);
        }

        [Fact]
        public void Throws_when_the_connection_interface_has_no_ipv4_address()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateOpcUaConnection();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice { Alias = "Dev", Children = [dataNode], Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = CreateGenerator([new NetworkInterface { Name = "lan1" }]);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []));
        }

        [Fact]
        public void Maps_opcua_connection_onto_data_port_properties()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = new Connection { Id = Guid.NewGuid(), Name = "MyServer", Type = ConnectionType.OpcUaServer };
            var opcUaConnection = new OpcUaServerConnection
            {
                ApplicationCertificatesPath = "own",
                ApplicationCertificateSubject = "CN=Test Server",
                ApplicationName = "Test Server",
                ApplicationUri = "urn:test:server",
                AuthenticationMode = OpcUaAuthenticationMode.UsernamePassword,
                AutoAcceptUntrustedCertificates = true,
                Endpoint = "ua/dataport",
                MaxPublishingIntervalMilliseconds = 2000,
                MinPublishingIntervalMilliseconds = 250,
                Namespace = "http://example.com/test",
                Password = "secret",
                Port = 4841,
                SecurityPolicy = OpcUaSecurityPolicy.Basic256Sha256SignAndEncrypt,
                NetworkInterface = "lan1",
                TrustedCertificatesPath = "trusted",
                TrustedIssuerCertificatesPath = "issuers",
                UseCustomTransportQuotas = true,
                Username = "user",
            };
            connection.SetOpcUaServerConnection(opcUaConnection);
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice
            {
                Alias = "Dev",
                Children = [dataNode],
                Id = "id",
                MacAddress = "aa:bb",
                Name = "Dev",
                Url = new Uri("http://10.0.0.1"),
            };
            var generator = CreateGenerator();

            // Act
            generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);
            Assert.Equal($"{connection.Name} - {deviceTreeMaster.Url}", dataPort.Name);
            Assert.Equal(DataPortDirection.Out, dataPort.Direction);

            object? Prop(string designId) => dataPort.Properties.Single(p => p.DesignId == designId).Value;

            Assert.Equal(opcUaConnection.ApplicationName, Prop("ApplicationName"));
            Assert.Equal(opcUaConnection.ApplicationUri, Prop("ApplicationUri"));
            Assert.Equal(opcUaConnection.Namespace, Prop("Namespace"));
            Assert.Equal("10.0.0.5", Prop("Server")); // the address of the connection's interface on the host
            Assert.Equal(opcUaConnection.Port, Prop("Port"));
            Assert.Equal(opcUaConnection.Endpoint, Prop("Endpoint"));
            Assert.Equal((byte)opcUaConnection.SecurityPolicy, Prop("SecurityPolicy"));
            Assert.Equal(opcUaConnection.UseCustomTransportQuotas, Prop("TransportQuotas"));
            Assert.Equal(opcUaConnection.MinPublishingIntervalMilliseconds, Prop("MinPublishingInterval"));
            Assert.Equal(opcUaConnection.MaxPublishingIntervalMilliseconds, Prop("MaxPublishingInterval"));
            Assert.Equal((byte)opcUaConnection.AuthenticationMode, Prop("UserAuthenticationType"));
            Assert.Equal(opcUaConnection.Username, Prop("User"));
            Assert.Equal(opcUaConnection.Password, Prop("Password"));
            Assert.Equal(opcUaConnection.ApplicationCertificateSubject, Prop("ApplicationCertificateSubject"));
            Assert.Equal((byte)OpcUaCertificateStoreType.Directory, Prop("ApplicationCertificatesStoreType"));
            Assert.Equal(opcUaConnection.ApplicationCertificatesPath, Prop("ApplicationCertificatesStorePath"));
            Assert.Equal((byte)OpcUaCertificateStoreType.Directory, Prop("TrustedCertificatesStoreType"));
            Assert.Equal(opcUaConnection.TrustedCertificatesPath, Prop("TrustedCertificatesStorePath"));
            Assert.Equal((byte)OpcUaCertificateStoreType.Directory, Prop("TrustedIssuerCertificatesStoreType"));
            Assert.Equal(opcUaConnection.TrustedIssuerCertificatesPath, Prop("TrustedIssuerCertificatesStorePath"));
            Assert.Equal(opcUaConnection.AutoAcceptUntrustedCertificates, Prop("AutoAcceptUntrustedCertificates"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Sets_trusted_certificate_store_types_to_none_when_no_path_is_given(string? path)
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = new Connection { Id = Guid.NewGuid(), Name = "MyServer", Type = ConnectionType.OpcUaServer };
            connection.SetOpcUaServerConnection(new OpcUaServerConnection
            {
                ApplicationCertificatesPath = path ?? string.Empty,
                NetworkInterface = "lan1",
                TrustedCertificatesPath = path,
                TrustedIssuerCertificatesPath = path,
            });
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Real, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice { Alias = "Dev", Children = [dataNode], Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = CreateGenerator();

            // Act
            generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);

            object? Prop(string designId) => dataPort.Properties.Single(p => p.DesignId == designId).Value;

            Assert.Equal((byte)OpcUaCertificateStoreType.Directory, Prop("ApplicationCertificatesStoreType")); // required, never None
            Assert.Equal((byte)OpcUaCertificateStoreType.None, Prop("TrustedCertificatesStoreType"));
            Assert.Equal((byte)OpcUaCertificateStoreType.None, Prop("TrustedIssuerCertificatesStoreType"));
        }
    }
}
