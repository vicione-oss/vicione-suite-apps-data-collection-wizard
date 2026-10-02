using Sdk.Connections.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

/// <summary>
/// Sets the client properties of an MQTT DataPort (<c>MqttClientInstance</c> of <c>ViciOne.Suite.DataPort.Mqtt</c> 2.x)
/// from an <see cref="MqttConnection"/>.
/// </summary>
internal static class MqttDataPortProperties
{
    // Element values of the DataPort's Protocol property.
    internal const byte ProtocolTcp = 0;
    internal const byte ProtocolWebSocket = 1;

    // Element values of the DataPort's TlsMode property.
    internal const byte TlsModeNone = 0;
    internal const byte TlsModeAutomatic = 1;
    internal const byte TlsModeTls12 = 2;
    internal const byte TlsModeTls13 = 3;

    // Element value of the DataPort's ProtocolVersion property for MQTT v5.0.
    internal const byte ProtocolVersionV500 = 1;

    /// <param name="validateCertificateChain">
    /// Overrides whether the broker's certificate chain is validated. By default it is validated unless the connection
    /// allows untrusted certificates.
    /// </param>
    public static void Add(ClusterBuilder builder, DataPort dataPort, MqttConnection mqttConnection, bool? validateCertificateChain = null)
    {
        var isWebSocket = mqttConnection.Protocol == MqttConnectionType.WebSocket;
        var tlsMode = GetTlsMode(mqttConnection);

        builder.Editors.DataPort.AddProperty("ClientId", dataPort, null, mqttConnection.ClientId);
        builder.Editors.DataPort.AddProperty("Protocol", dataPort, null, isWebSocket ? ProtocolWebSocket : ProtocolTcp);

        if (isWebSocket)
        {
            builder.Editors.DataPort.AddProperty("Url", dataPort, null, GetWebSocketUrl(mqttConnection, tlsMode != TlsModeNone));
        }
        else
        {
            builder.Editors.DataPort.AddProperty("Host", dataPort, null, mqttConnection.Address);
            builder.Editors.DataPort.AddProperty("Port", dataPort, null, (ushort?)mqttConnection.Port);
            builder.Editors.DataPort.AddProperty("TlsMode", dataPort, null, tlsMode);
        }

        builder.Editors.DataPort.AddProperty("ProtocolVersion", dataPort, null, ProtocolVersionV500);
        builder.Editors.DataPort.AddProperty("Pooling", dataPort, null, true);
        builder.Editors.DataPort.AddProperty("Username", dataPort, null, mqttConnection.Username);
        builder.Editors.DataPort.AddProperty("Password", dataPort, null, mqttConnection.Password);

        builder.Editors.DataPort.AddProperty("CleanSession", dataPort, null, mqttConnection.CleanSession);
        builder.Editors.DataPort.AddProperty("MaxPendingMessages", dataPort, null, 10000);
        builder.Editors.DataPort.AddProperty("BrokerReceiveMaximum", dataPort, null, (ushort)100);

        // The DataPort only sends a last will when it is enabled, and it needs a topic to publish it to.
        builder.Editors.DataPort.AddProperty("LastWillEnabled", dataPort, null, !string.IsNullOrWhiteSpace(mqttConnection.WillTopic));
        builder.Editors.DataPort.AddProperty("WillTopic", dataPort, null, mqttConnection.WillTopic);
        builder.Editors.DataPort.AddProperty("WillMessage", dataPort, null, mqttConnection.WillMessage);
        builder.Editors.DataPort.AddProperty("WillRetain", dataPort, null, mqttConnection.WillRetain);

        builder.Editors.DataPort.AddProperty("CertificateFile", dataPort, null, mqttConnection.ClientCertificate);
        builder.Editors.DataPort.AddProperty("CertificateFilePassword", dataPort, null, mqttConnection.ClientCertificateKeyPassword);
        builder.Editors.DataPort.AddProperty("CertificatePrivateKeyFile", dataPort, null, mqttConnection.ClientCertificateKey);
        builder.Editors.DataPort.AddProperty("ValidateCertificateChain", dataPort, null, validateCertificateChain ?? !mqttConnection.AllowUntrustedCertificates);

        builder.Editors.DataPort.AddProperty("QualityOfService", dataPort, null, (byte)1);
    }

    /// <summary>
    /// Maps the connection's TLS settings to the DataPort's <c>TlsMode</c>. The DataPort defaults to TLS, so a
    /// connection without TLS has to be mapped to <see cref="TlsModeNone"/> explicitly.
    /// </summary>
    internal static byte GetTlsMode(MqttConnection mqttConnection)
        => mqttConnection.SslProtocol switch
        {
            MqttSslProtocol.Tls12 => TlsModeTls12,
            MqttSslProtocol.Tls13 => TlsModeTls13,
#pragma warning disable CS0618 // TCPWithTLS is obsolete, but existing connections may still use it
            _ => mqttConnection.Protocol == MqttConnectionType.TCPWithTLS ? TlsModeAutomatic : TlsModeNone,
#pragma warning restore CS0618
        };

    /// <summary>
    /// Returns the broker url for the WebSocket protocol, which the DataPort only accepts with the 'ws' or 'wss' scheme.
    /// </summary>
    internal static Uri GetWebSocketUrl(MqttConnection mqttConnection, bool useTls)
    {
        if (Uri.TryCreate(mqttConnection.Address, UriKind.Absolute, out var url)
            && (url.Scheme == "ws" || url.Scheme == "wss"))
        {
            return url;
        }

        return new UriBuilder(useTls ? "wss" : "ws", mqttConnection.Address, mqttConnection.Port).Uri;
    }
}
