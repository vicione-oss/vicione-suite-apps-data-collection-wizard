using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Tests.CloudDataflowGenerators;

/// <summary>
/// Tests for MqttCloudDataflowGenerator, focusing on the recursive tree building methods.
/// These tests specifically verify the BuildLoggedTreeRecursively method which constructs
/// a filtered tree model containing only logged nodes and their parent chains.
/// </summary>
public class MqttCloudDataflowGeneratorTests
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
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(node, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Returns_tree_model_when_node_is_logged()
        {
            // Arrange
            var loggedNodeId = "logged-node";
            var node = new DeviceTreeStructureNode { Id = loggedNodeId, Name = "Logged Node" };
            var loggedNodeIds = new HashSet<string> { loggedNodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(node, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loggedNodeId, result.Id);
            Assert.Equal("Logged Node", result.Name);
            var children = result.Children;
            Assert.Empty(children);
            Assert.Null(result.DataConfig);
        }

        [Fact]
        public void Returns_tree_model_with_data_config_when_node_is_logged_with_process_data()
        {
            // Arrange
            var loggedNodeId = "logged-node";
            var mockDataNode = new DeviceTreeProcessData() { DataType = DataType.Float32T, Id = loggedNodeId, Name = "Logged Node" };
            var processDataConfig = new ProcessDataConfiguration(mockDataNode, new CompressorConfiguration() { DataGroupIdentifier = Guid.Parse("12300000-0000-0000-1234-000000000000") });
            var loggedNodeIds = new HashSet<string> { loggedNodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration> { processDataConfig };
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(mockDataNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loggedNodeId, result.Id);
            Assert.NotNull(result.DataConfig);
            Assert.Same(processDataConfig, result.DataConfig);
        }

        [Fact]
        public void Includes_children_when_node_is_logged()
        {
            // Arrange
            var parentNodeId = "parent";
            var childNodeId = "child";
            var grandchildNodeId = "grandchild";

            var grandchildNode = new DeviceTreeStructureNode { Id = grandchildNodeId, Name = "Grandchild" };
            var childNode = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { grandchildNode }, Id = childNodeId, Name = "Child" };
            var parentNode = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { childNode }, Id = parentNodeId, Name = "Parent" };

            var loggedNodeIds = new HashSet<string> { parentNodeId, childNodeId, grandchildNodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(parentNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            var children = result.Children;
            Assert.Single(children);
            var childResult = children[0];
            Assert.Equal(childNodeId, childResult.Id);
            var grandchildren = childResult.Children;
            Assert.Single(grandchildren);
            Assert.Equal(grandchildNodeId, grandchildren[0].Id);
        }

        [Fact]
        public void Includes_parent_when_child_is_logged()
        {
            // Arrange
            var parentNodeId = "parent";
            var childNodeId = "child";

            var childNode = new DeviceTreeStructureNode { Id = childNodeId, Name = "Child" };
            var parentNode = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { childNode }, Id = parentNodeId, Name = "Parent" };

            var loggedNodeIds = new HashSet<string> { childNodeId }; // Only child is logged
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(parentNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result); // Parent should be included because child is logged
            Assert.Equal(parentNodeId, result.Id);
            var children = result.Children;
            Assert.Single(children);
            Assert.Equal(childNodeId, children[0].Id);
        }

        [Fact]
        public void Filters_out_non_logged_siblings()
        {
            // Arrange
            var parentNodeId = "parent";
            var loggedChildNodeId = "logged-child";
            var unloggedChildNodeId = "unlogged-child";

            var loggedChildNode = new DeviceTreeStructureNode { Id = loggedChildNodeId, Name = "Logged Child" };
            var unloggedChildNode = new DeviceTreeStructureNode { Id = unloggedChildNodeId, Name = "Unlogged Child" };
            var parentNode = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { loggedChildNode, unloggedChildNode }, Id = parentNodeId, Name = "Parent" };

            var loggedNodeIds = new HashSet<string> { parentNodeId, loggedChildNodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(parentNode, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            var children = result.Children;
            Assert.Single(children); // Only logged child should be included
            Assert.Equal(loggedChildNodeId, children[0].Id);
        }

        [Fact]
        public void Handles_multiple_levels_of_hierarchy()
        {
            // Arrange
            var level1Id = "level1";
            var level2Id = "level2";
            var level3Id = "level3";
            var level4Id = "level4";

            var level4Node = new DeviceTreeStructureNode { Id = level4Id, Name = "Level 4" };
            var level3Node = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { level4Node }, Id = level3Id, Name = "Level 3" };
            var level2Node = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { level3Node }, Id = level2Id, Name = "Level 2" };
            var level1Node = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { level2Node }, Id = level1Id, Name = "Level 1" };

            var loggedNodeIds = new HashSet<string> { level1Id, level2Id, level3Id, level4Id };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(level1Node, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(level1Id, result.Id);
            var current = result;
            for (int level = 2; level <= 4; level++)
            {
                var currentChildren = current.Children;
                Assert.Single(currentChildren);
                current = currentChildren[0];
                Assert.Equal($"level{level}", current.Id);
            }
        }

        [Fact]
        public void Handles_empty_children_list()
        {
            // Arrange
            var nodeId = "node";
            var node = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase>(), Id = nodeId, Name = "Node" };
            var loggedNodeIds = new HashSet<string> { nodeId };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(node, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            var children = result.Children;
            Assert.Empty(children);
        }

        [Fact]
        public void Finds_process_data_config_among_multiple_nodes()
        {
            // Arrange
            var node1Id = "node1";
            var node2Id = "node2";
            var node3Id = "node3";

            var mockNode2 = new DeviceTreeProcessData { DataType = DataType.StringT, Id = node2Id, Name = "Node 2" };

            var processDataConfig = new ProcessDataConfiguration(mockNode2, new CompressorConfiguration() { DataGroupIdentifier = Guid.Parse("12300000-0000-0000-1234-000000000000") });

            var loggedNodeIds = new HashSet<string> { node1Id, node2Id, node3Id };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration> { processDataConfig };

            var node1 = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { mockNode2 }, Id = node1Id, Name = "Node 1" };
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(node1, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            var children = result.Children;
            Assert.Single(children);
            var childResult = children[0];
            Assert.NotNull(childResult.DataConfig);
            Assert.Same(processDataConfig, childResult.DataConfig);
        }

        [Fact]
        public void Processes_complex_tree_with_mixed_logged_and_unlogged_nodes()
        {
            // Arrange
            // Create a tree structure:
            // root
            //   ├─ logged1
            //   │   └─ logged2
            //   │       └─ unlogged1
            //   └─ unlogged2
            //       └─ logged3

            var unlogged1 = new DeviceTreeStructureNode { Id = "unlogged1", Name = "Unlogged 1" };
            var logged2 = new DeviceTreeProcessData { Children = new List<IDeviceTreeBase> { unlogged1 }, Id = "logged2", Name = "Logged 2" };
            var logged1 = new DeviceTreeProcessData { Children = new List<IDeviceTreeBase> { logged2 }, Id = "logged1", Name = "Logged 1" };

            var logged3 = new DeviceTreeProcessData { Id = "logged3", Name = "Logged 3" };
            var unlogged2 = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { logged3 }, Id = "unlogged2", Name = "Unlogged 2" };

            var root = new DeviceTreeStructureNode { Children = new List<IDeviceTreeBase> { logged1, unlogged2 }, Id = "root", Name = "Root" };

            var loggedNodeIds = new HashSet<string> { "root", "logged1", "logged2", "logged3" };
            var loggedProcessDataNodes = new List<ProcessDataConfiguration>();
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.BuildLoggedTreeRecursively(root, loggedNodeIds, loggedProcessDataNodes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("root", result.Id);
            var resultChildren = result.Children;
            // Should have 2 children: logged1 and unlogged2 (because logged3 is under it)
            Assert.Equal(2, resultChildren.Count);
            // First child should be logged1
            var firstChild = resultChildren.FirstOrDefault(c =>c.Id == "logged1");
            Assert.NotNull(firstChild);
            // logged1 should have logged2 as child
            var firstChildChildren = firstChild.Children;
            Assert.Single(firstChildChildren);
            Assert.Equal("logged2", firstChildChildren[0].Id);
            // logged2 should not have unlogged1 as child (filtered out)
            var logged2Children = firstChildChildren[0].Children;
            Assert.Empty(logged2Children);
        }
    }

    /// <summary>
    /// Tests for BuildDataportNodesRecursively, which creates the actual MQTT topic tree
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
            builder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);

            var dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));
            var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.MqttDataPort.DesignId, "port",
                DataPortDirection.Out, FunctionBlocks.MqttDataPort.Type);

            return (builder, dataPort);
        }

        private static MqttCloudDataflowGenerator CreateGenerator()
            => new(Substitute.For<IInstanceInformationProvider>());

        [Fact]
        public void Creates_folder_node_with_sanitized_name_for_child_without_data_config()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var children = new List<TreeModel> { new() { DataConfig = null, Id = "folder1", Name = "My Folder!" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("Folder", node.DesignId);
            Assert.Equal("My_Folder_", node.Name); // space and '!' are not MQTT-safe
            Assert.Null(node.ValueType);
            Assert.Equal(DataPortTransferMode.None, node.TransferMode);
            Assert.Empty(result); // folders never get an entry in the cloud input dictionary
        }

        [Theory]
        [InlineData(DataType.Float32T)]
        [InlineData(DataType.BooleanT)]
        [InlineData(DataType.UIntegerT)]
        [InlineData(DataType.IntegerT)]
        public void Creates_float_data_point_node_for_numeric_data_types(DataType dataType)
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var dataNode = new DeviceTreeProcessData { DataType = dataType, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("DataPointFloat", node.DesignId);
            Assert.Equal(typeof(float), node.ValueType);
            Assert.Equal(DataPortTransferMode.OnChange, node.TransferMode);
        }

        [Theory]
        [InlineData(DataType.StringT)]
        [InlineData(DataType.OctetStringT)]
        public void Creates_string_data_point_node_for_string_data_types(DataType dataType)
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var dataNode = new DeviceTreeProcessData { DataType = dataType, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

            // Assert
            var node = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("DataPointString", node.DesignId);
            Assert.Equal(typeof(string), node.ValueType);
            Assert.Equal(DataPortTransferMode.OnChange, node.TransferMode);
        }

        [Theory]
        [InlineData(DataType.Invalid)]
        [InlineData(DataType.BlobT)]
        public void Throws_for_unsupported_data_type(DataType dataType)
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var dataNode = new DeviceTreeProcessData { DataType = dataType, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result));
        }

        [Fact]
        public void Populates_result_with_same_input_node_for_all_pooling_modes()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Float32T, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var children = new List<TreeModel> { new() { DataConfig = config, Id = "n1", Name = "Value" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

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
        public void Adds_root_level_node_directly_to_data_port_when_parent_is_null()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var children = new List<TreeModel> { new() { DataConfig = null, Id = "folder1", Name = "Root" } };

            // Act
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

            // Assert
            Assert.Single(dataPort.TreeNodes);
        }

        [Fact]
        public void Nests_child_node_under_parent_instead_of_data_port()
        {
            // Arrange
            var (builder, dataPort) = CreateDataPort();
            var generator = CreateGenerator();
            var result = new Dictionary<string, PoolingModesCloudInput>();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Float32T, Id = "n1", Name = "Data 1" };
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
            generator.BuildDataportNodesRecursively(children, dataPort, null, builder, result);

            // Assert
            var folderNode = Assert.Single(dataPort.TreeNodes); // only the root folder sits directly under the data port
            Assert.Equal("Folder_One", folderNode.Name);
            var dataPointNode = Assert.Single(folderNode.Children); // the data point is nested under the folder, not the data port
            Assert.Equal("Data_1", dataPointNode.Name);
            Assert.Equal("n1", Assert.Single(result.Keys));
        }
    }

    /// <summary>
    /// Tests for the public GenerateCloudDataflow entry point, covering the folder scaffold
    /// (vicione/edge/device) and the empty-result short-circuit when nothing is logged.
    /// </summary>
    public class GenerateCloudDataflowTests
    {
        private static ClusterBuilder CreateBuilder(out Dataflow dataflow)
        {
            var resolver = Substitute.For<IDependencyResolver>();
            resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
                .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });

            var builder = new ClusterBuilder(resolver);
            builder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);
            dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));

            return builder;
        }

        private static Connection CreateMqttConnection(string name = "MyBroker")
        {
            var connection = new Connection { Id = Guid.NewGuid(), Name = name, Type = ConnectionType.Mqtt };
            connection.SetMqttConnection(new MqttConnection { Address = "broker.example.com", Port = 1883 });
            return connection;
        }

        [Fact]
        public void Returns_empty_dictionary_and_creates_no_data_port_when_nothing_is_logged()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateMqttConnection();
            var deviceTreeMaster = new DeviceTreeVseDevice { Id = "id", MacAddress = "aa:bb", Name = "Dev", NameAlias = "Dev", Url = new Uri("http://10.0.0.1") };
            var generator = new MqttCloudDataflowGenerator(Substitute.For<IInstanceInformationProvider>());

            // Act
            var result = generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [], []);

            // Assert
            Assert.Empty(result);
            Assert.Empty(dataflow.DataPorts);
        }

        [Fact]
        public void Builds_folder_hierarchy_rooted_at_vicione_edge_name_and_sanitized_device_host()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateMqttConnection();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Float32T, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice
            {
                Children = [dataNode],
                Id = "id",
                MacAddress = "aa:bb",
                Name = "Dev",
                NameAlias = "Dev",
                Url = new Uri("http://my.broker.local:1883"),
            };
            var instanceInfo = Substitute.For<IInstanceInformationProvider>();
            instanceInfo.Local.Name.Returns("Edge One");
            var generator = new MqttCloudDataflowGenerator(instanceInfo);

            // Act
            var result = generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);
            var vicioneNode = Assert.Single(dataPort.TreeNodes);
            Assert.Equal("vicione", vicioneNode.Name);
            var edgeNode = Assert.Single(vicioneNode.Children);
            Assert.Equal("Edge One", edgeNode.Name); // unlike the device host and data node names below, the edge name is used as-is, not sanitized
            var deviceNode = Assert.Single(edgeNode.Children);
            Assert.Equal("my_broker_local", deviceNode.Name); // dots are not MQTT-safe, port is stripped by DnsSafeHost
            var dataPointNode = Assert.Single(deviceNode.Children);
            Assert.Equal("DataPointFloat", dataPointNode.DesignId);
            Assert.Equal("n1", Assert.Single(result.Keys));
        }

        [Fact]
        public void Falls_back_to_serial_number_when_instance_name_is_null()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = CreateMqttConnection();
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Float32T, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice
            {
                Children = [dataNode],
                Id = "id",
                MacAddress = "aa:bb",
                Name = "Dev",
                NameAlias = "Dev",
                Url = new Uri("http://10.0.0.1"),
            };
            var instanceInfo = Substitute.For<IInstanceInformationProvider>();
            instanceInfo.Local.Name.Returns((string?)null);
            instanceInfo.Local.SerialNumber.Returns("SN-42");
            var generator = new MqttCloudDataflowGenerator(instanceInfo);

            // Act
            generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);
            var vicioneNode = Assert.Single(dataPort.TreeNodes);
            var edgeNode = Assert.Single(vicioneNode.Children);
            Assert.Equal("SN-42", edgeNode.Name);
        }

        [Fact]
        public void Maps_mqtt_connection_and_fixed_settings_onto_data_port_properties()
        {
            // Arrange
            using var builder = CreateBuilder(out var dataflow);
            var connection = new Connection { Id = Guid.NewGuid(), Name = "MyBroker", Type = ConnectionType.Mqtt };
            var mqttConnection = new MqttConnection
            {
                Address = "10.0.0.5",
                CleanSession = true,
                ClientCertificate = "cert.pem",
                ClientCertificateKey = "cert.key",
                ClientId = "client-1",
                Password = "secret",
                Port = 8883,
                Protocol = MqttConnectionType.TCP,
                Username = "user",
                WillMessage = "bye",
                WillRetain = true,
                WillTopic = "will/topic",
            };
            connection.SetMqttConnection(mqttConnection);
            var dataNode = new DeviceTreeProcessData { DataType = DataType.Float32T, Id = "n1", Name = "Value" };
            var config = new ProcessDataConfiguration(dataNode, new CompressorConfiguration { DataGroupIdentifier = Guid.NewGuid() });
            var deviceTreeMaster = new DeviceTreeVseDevice
            {
                Children = [dataNode],
                Id = "id",
                MacAddress = "aa:bb",
                Name = "Dev",
                NameAlias = "Dev",
                Url = new Uri("http://10.0.0.1"),
            };
            var instanceInfo = Substitute.For<IInstanceInformationProvider>();
            instanceInfo.Local.Name.Returns("Edge");
            var generator = new MqttCloudDataflowGenerator(instanceInfo);

            // Act
            generator.GenerateCloudDataflow(connection, deviceTreeMaster, builder, dataflow, "mid",
                [], 1000, null!, [], [config], []);

            // Assert
            var dataPort = Assert.Single(dataflow.DataPorts);
            Assert.Equal($"{connection.Name} - {deviceTreeMaster.Url}", dataPort.Name);
            Assert.Equal(DataPortDirection.Out, dataPort.Direction);

            object? Prop(string designId) => dataPort.Properties.Single(p => p.DesignId == designId).Value;

            Assert.Equal(mqttConnection.ClientId, Prop("ClientId"));
            Assert.Equal(mqttConnection.WillTopic, Prop("WillTopic"));
            Assert.Equal(mqttConnection.WillMessage, Prop("WillMessage"));
            Assert.Equal(mqttConnection.WillRetain, Prop("WillRetain"));
            Assert.Equal((byte)mqttConnection.Protocol, Prop("Protocol"));
            Assert.Equal(mqttConnection.Address, Prop("Host"));
            Assert.Equal((ushort?)mqttConnection.Port, Prop("Port"));
            Assert.Equal((byte)1, Prop("ProtocolVersion"));
            Assert.Equal(mqttConnection.ClientCertificate, Prop("CertificateFile"));
            Assert.Equal(mqttConnection.ClientCertificateKey, Prop("CertificatePrivateKeyFile"));
            Assert.Equal(mqttConnection.CleanSession, Prop("CleanSession"));
            Assert.Equal(true, Prop("DisableCertificateValidation"));
            Assert.Equal(true, Prop("Pooling"));
            Assert.Equal(10000, Prop("MaxPendingMessages"));
            Assert.Equal((byte)1, Prop("QualityOfService")); // hardcoded, independent of mqttConnection.QualityOfService
            Assert.Equal((ushort)100, Prop("BrokerReceiveMaximum"));
            Assert.Equal(mqttConnection.Username, Prop("Username"));
            Assert.Equal(mqttConnection.Password, Prop("Password"));
        }
    }
}
