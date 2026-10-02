using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

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

    public class GetProtocolVersionTests
    {
        [Theory]
        [InlineData(MqttProtocolVersion.V311, MqttDataPortProperties.ProtocolVersionV311)]
        [InlineData(MqttProtocolVersion.V500, MqttDataPortProperties.ProtocolVersionV500)]
        public void Maps_the_protocol_version_to_the_matching_data_port_value(MqttProtocolVersion protocolVersion, byte expected)
        {
            var connection = new MqttConnection { ProtocolVersion = protocolVersion };

            Assert.Equal(expected, MqttDataPortProperties.GetProtocolVersion(connection));
        }

        [Fact]
        public void Throws_for_an_unknown_protocol_version()
        {
            var connection = new MqttConnection { ProtocolVersion = (MqttProtocolVersion)42 };

            Assert.Throws<NotSupportedException>(() => MqttDataPortProperties.GetProtocolVersion(connection));
        }
    }

    public class GetQualityOfServiceTests
    {
        [Theory]
        [InlineData(MqttQualityOfServiceLevel.AtMostOnce, MqttDataPortProperties.QualityOfServiceAtMostOnce)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, MqttDataPortProperties.QualityOfServiceAtLeastOnce)]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, MqttDataPortProperties.QualityOfServiceExactlyOnce)]
        public void Maps_the_quality_of_service_level_to_the_matching_data_port_value(MqttQualityOfServiceLevel qualityOfService, byte expected)
            => Assert.Equal(expected, MqttDataPortProperties.GetQualityOfService(qualityOfService));

        [Fact]
        public void Throws_for_an_unknown_quality_of_service_level()
            => Assert.Throws<NotSupportedException>(() => MqttDataPortProperties.GetQualityOfService((MqttQualityOfServiceLevel)42));
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

    public class AddTests
    {
        private static object? GetValidateCertificateChain(MqttConnection connection, bool? validateCertificateChain = null)
        {
            var resolver = Substitute.For<IDependencyResolver>();
            resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
                .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });

            using var builder = new ClusterBuilder(resolver);
            builder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);
            var dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));
            var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.MqttDataPort.DesignId, "port",
                DataPortDirection.Out, FunctionBlocks.MqttDataPort.Type);

            MqttDataPortProperties.Add(builder, dataPort, connection, validateCertificateChain);

            return dataPort.Properties.Single(p => p.DesignId == "ValidateCertificateChain").Value;
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Validates_the_certificate_chain_unless_the_connection_allows_untrusted_certificates(bool allowUntrustedCertificates, bool expected)
        {
            var connection = new MqttConnection { Address = "broker", AllowUntrustedCertificates = allowUntrustedCertificates };

            Assert.Equal(expected, GetValidateCertificateChain(connection));
        }

        [Fact]
        public void Lets_the_caller_override_certificate_chain_validation()
        {
            // Used by Moneo, whose broker is connected to without validation regardless of the connection settings.
            var connection = new MqttConnection { Address = "broker", AllowUntrustedCertificates = false };

            Assert.Equal(false, GetValidateCertificateChain(connection, validateCertificateChain: false));
        }
    }
}
