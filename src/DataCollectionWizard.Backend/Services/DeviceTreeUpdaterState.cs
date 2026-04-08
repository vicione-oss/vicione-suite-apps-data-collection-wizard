namespace DataCollectionWizard.Backend.Services;

public sealed class DeviceTreeUpdaterState : IAsyncDisposable
{
    public CancellationTokenSource Cts { get; } = new();

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Cts.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Ignore gracefully
        }
        finally
        {
            Cts.Dispose();
        }
    }
}
