namespace DataCollectionWizard.Internal.Services.DesignIds;

// Both ids come from the OPC-UA Server DataPort's ruleset (OpcUaServer.yaml): DesignId is the server node
// under Root.ChildNodes, Type is Root.Id, which cluster management resolves the ruleset by.
public class OpcUaDataPort : IDataPort
{
    public string DesignId => "OPCUA-Server";
    public string Type => "OpcUaServerDataPort";
}
