using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using NSubstitute;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
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
}
