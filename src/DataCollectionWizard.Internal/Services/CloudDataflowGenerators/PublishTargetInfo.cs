using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed record PublishTargetInfo(Connection Connection, ConnectionKind Kind)
{
    public bool IsSupported => Kind != ConnectionKind.Unsupported;
}
