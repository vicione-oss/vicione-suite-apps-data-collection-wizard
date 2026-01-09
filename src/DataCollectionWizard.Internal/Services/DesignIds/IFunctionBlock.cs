namespace DataCollectionWizard.Internal.Services.DesignIds;

public interface IFunctionBlock : IDesignIdStore
{
    Guid DesignId { get; }
}
