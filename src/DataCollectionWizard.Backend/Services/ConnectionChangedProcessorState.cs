using Timer = System.Timers.Timer;

namespace DataCollectionWizard.Backend.Services;

public sealed class ConnectionChangedProcessorState : IDisposable
{
    public Timer Timer { get; } = new();

    public void Dispose() => Timer.Dispose();
}
