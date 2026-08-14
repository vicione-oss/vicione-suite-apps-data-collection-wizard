using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Tests.CloudDataflowGenerators;

/// <summary>
/// Tests for MqttCloudFilter, which advertises MQTT dataflow support to the generator
/// pipeline and selects which connections are eligible for MQTT cloud generation.
/// </summary>
public class MqttCloudFilterTests
{
    public class GetCloudConnectionsTests
    {
        private static Connection CreateConnection(ConnectionType type, bool managed = false, params Tag[] tags)
        {
            var connection = new Connection { Id = Guid.NewGuid(), Managed = managed, Name = "Test", Type = type };

            foreach (var tag in tags)
            {
                connection.Tags.Add(tag);
            }

            return connection;
        }

        [Fact]
        public void Includes_unmanaged_mqtt_connection_without_moneo_tag()
        {
            // Arrange
            var filter = new MqttCloudFilter();
            var connection = CreateConnection(ConnectionType.Mqtt);

            // Act
            var result = filter.GetCloudConnections([connection]);

            // Assert
            Assert.Equal([connection], result);
        }

        [Fact]
        public void Excludes_non_mqtt_connections()
        {
            // Arrange
            var filter = new MqttCloudFilter();
            var httpConnection = CreateConnection(ConnectionType.Http);

            // Act
            var result = filter.GetCloudConnections([httpConnection]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Excludes_managed_mqtt_connections()
        {
            // Arrange
            var filter = new MqttCloudFilter();
            var managedConnection = CreateConnection(ConnectionType.Mqtt, managed: true);

            // Act
            var result = filter.GetCloudConnections([managedConnection]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Excludes_mqtt_connections_tagged_as_moneo_connect_cloud()
        {
            // Arrange
            var filter = new MqttCloudFilter();
            var moneoConnection = CreateConnection(ConnectionType.Mqtt, tags: [Constants.MoneoConnectCloud]);

            // Act
            var result = filter.GetCloudConnections([moneoConnection]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Filters_mixed_connection_list_to_only_eligible_mqtt_connections()
        {
            // Arrange
            var filter = new MqttCloudFilter();
            var eligible = CreateConnection(ConnectionType.Mqtt);
            var httpConnection = CreateConnection(ConnectionType.Http);
            var managedConnection = CreateConnection(ConnectionType.Mqtt, managed: true);
            var moneoConnection = CreateConnection(ConnectionType.Mqtt, tags: [Constants.MoneoConnectCloud]);

            // Act
            var result = filter.GetCloudConnections([eligible, httpConnection, managedConnection, moneoConnection]);

            // Assert
            Assert.Equal([eligible], result);
        }

        [Fact]
        public void Returns_empty_when_no_connections_are_given()
        {
            // Arrange
            var filter = new MqttCloudFilter();

            // Act
            var result = filter.GetCloudConnections([]);

            // Assert
            Assert.Empty(result);
        }
    }
}
