using System.Collections.Concurrent;
using System.Timers;
using Sdk.Messaging;
using Timer = System.Timers.Timer;

namespace DataCollectionWizard.Backend.Services;

public sealed class ClusterServiceState : IAsyncDisposable
{
    private static readonly TimeSpan s_defaultTicketValidity = TimeSpan.FromSeconds(10);
    private readonly Lock _lock = new();
    private readonly Timer _timer = new() { AutoReset = false };

    public ConcurrentDictionary<Guid, TaskCompletionSource<ErrorInfo?>> TaskCompletionSourceMap { get; } = new();
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public CancellationTokenSource Cts { get; } = new();
    public Ticket? IssuedTicket { get; set; }

    public bool SetResult(Guid? correlationId, ErrorInfo? errorInfo = null)
    {
        if (correlationId is null
            || !TaskCompletionSourceMap.TryGetValue(correlationId.Value, out var taskCompletionSource))
        {
            return false;
        }

        taskCompletionSource.TrySetResult(errorInfo);
        if (IssuedTicket is not null && IssuedTicket.Value.CorrelationId == correlationId.Value)
            DiscardUpdateRequest(IssuedTicket.Value.Id);
        return true;
    }

    public void StartTimer(TimeSpan? interval)
    {
        _timer.Interval = (interval ?? s_defaultTicketValidity).TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    public void StopTimer()
    {
        _timer.Elapsed -= TimerOnElapsed;
        _timer.Stop();
    }

    public void DiscardUpdateRequest(Guid ticketId)
    {
        lock (_lock)
        {
            if (IssuedTicket?.Id != ticketId)
                return;
            TaskCompletionSourceMap.TryRemove(IssuedTicket.Value.CorrelationId, out _);
            StopTimer();
            IssuedTicket = null;
            Semaphore.Release();
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
            _timer.Dispose();
            Semaphore.Dispose();
            IssuedTicket = null;
        }
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (IssuedTicket is not null)
            DiscardUpdateRequest(IssuedTicket.Value.Id);
    }
}

public record struct Ticket(Guid Id, Guid CorrelationId);
