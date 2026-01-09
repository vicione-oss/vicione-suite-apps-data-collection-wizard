using System.Collections.Concurrent;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Services;

public sealed class DeviceTreeUpdaterState : IAsyncDisposable
{
    public ConcurrentDictionary<Guid, TaskCompletionSource<ErrorInfo?>> TaskCompletionSourceMap { get; } = new();
    public CancellationTokenSource Cts { get; } = new();

    public void SetResult(Guid? correlationId, ErrorInfo? errorInfo = null)
    {
        if (correlationId is not null
            && TaskCompletionSourceMap.TryGetValue(correlationId.Value, out var taskCompletionSource))
        {
            taskCompletionSource.TrySetResult(errorInfo);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Cts.CancelAsync();

            foreach (var taskCompletionSource in TaskCompletionSourceMap.Values)
                taskCompletionSource.SetCanceled();
            TaskCompletionSourceMap.Clear();
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
