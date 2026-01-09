using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class DataOutputInfo
{
    public Dictionary<Guid, string> DataPointIdentifiers { get; } = [];
    public required ConnectorOutput Output { get; set; }
    public required ConnectorOutput AvailableOutput { get; set; }

    /// <summary>
    /// Used to name the compressor functionblock, has to be unique by datapoint
    /// </summary>
    public required string Suffix { get; set; }
    public ConnectorOutput? ValidOutput { get; set; }
}
