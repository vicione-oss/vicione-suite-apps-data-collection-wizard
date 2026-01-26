using Sdk.Connections.Events;

namespace DataCollectionWizard.Backend.Services;

public interface IConnectionChangedProcessor
{
    void Enqueue(ConnectionChanged connectionChanged);
}
