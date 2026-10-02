namespace DataCollectionWizard.Internal.Tests;

/// <summary>
/// OpcUaCloudDataflowGenerator caches the host's network interfaces in a static field. Test classes that fill
/// that cache with their own fake interfaces join this collection, so they do not run in parallel and read each
/// other's interfaces.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NetworkInterfacesCacheCollection
{
    public const string Name = "OPC UA network interfaces cache";
}
