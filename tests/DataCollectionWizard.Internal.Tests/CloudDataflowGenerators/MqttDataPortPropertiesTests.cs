using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Tests.CloudDataflowGenerators;

/// <summary>
/// Tests for MqttDataPortProperties, which maps an MQTT connection onto the properties of the MQTT DataPort.
/// </summary>
public class MqttDataPortPropertiesTests
{
    public class GetTlsModeTests
    {
        [Fact]
        public void Maps_a_connection_without_tls_to_no_tls()
        {
            // The DataPort defaults to TLS, so a plaintext connection has to be mapped explicitly.
            var connection = new MqttConnection { Protocol = MqttConnectionType.TCP, SslProtocol = null };

            Assert.Equal(MqttDataPortProperties.TlsModeNone, MqttDataPortProperties.GetTlsMode(connection));
        }

        [Theory]
        [InlineData(MqttSslProtocol.Tls12, MqttDataPortProperties.TlsModeTls12)]
        [InlineData(MqttSslProtocol.Tls13, MqttDataPortProperties.TlsModeTls13)]
        public void Maps_the_ssl_protocol_to_the_matching_tls_mode(MqttSslProtocol sslProtocol, byte expected)
        {
            var connection = new MqttConnection { Protocol = MqttConnectionType.TCP, SslProtocol = sslProtocol };

            Assert.Equal(expected, MqttDataPortProperties.GetTlsMode(connection));
        }

        [Fact]
        public void Maps_the_obsolete_tcp_with_tls_protocol_to_automatic_tls()
        {
#pragma warning disable CS0618 // Existing connections may still use the obsolete value
            var connection = new MqttConnection { Protocol = MqttConnectionType.TCPWithTLS, SslProtocol = null };
#pragma warning restore CS0618

            Assert.Equal(MqttDataPortProperties.TlsModeAutomatic, MqttDataPortProperties.GetTlsMode(connection));
        }
    }

    public class GetWebSocketUrlTests
    {
        [Theory]
        [InlineData("ws://broker:8080/mqtt")]
        [InlineData("wss://broker/mqtt")]
        public void Keeps_an_address_that_already_is_a_websocket_url(string address)
        {
            var connection = new MqttConnection { Address = address, Protocol = MqttConnectionType.WebSocket };

            Assert.Equal(new Uri(address), MqttDataPortProperties.GetWebSocketUrl(connection, useTls: false));
        }

        [Theory]
        [InlineData(false, "ws://broker:8080/")]
        [InlineData(true, "wss://broker:8080/")]
        public void Builds_a_websocket_url_from_a_host_name(bool useTls, string expected)
        {
            var connection = new MqttConnection { Address = "broker", Port = 8080, Protocol = MqttConnectionType.WebSocket };

            Assert.Equal(new Uri(expected), MqttDataPortProperties.GetWebSocketUrl(connection, useTls));
        }
    }
}
