namespace DataCollectionWizard.Internal.Services.DesignIds;

// DesignId taken from the OPC-UA Server DataPort's own node id ("OPCUA-Server" under Root.ChildNodes).
// Type follows the MQTTDataPort/ANNADataPort naming convention (protocol acronym + "DataPort") but is
// not confirmed against the actual DataPort registration — verify before relying on this in a real deploy.
public class OpcUaDataPort : IDataPort
{
    public string DesignId => "OPCUA-Server";
    public string Type => "OPCUADataPort";
}
