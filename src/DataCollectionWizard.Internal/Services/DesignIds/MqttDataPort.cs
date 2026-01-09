namespace DataCollectionWizard.Internal.Services.DesignIds;

public class MqttDataPort : IDataPort
{
    public string DesignId => "MQTT-Broker";
    public string Type => "MQTTDataPort";
}
