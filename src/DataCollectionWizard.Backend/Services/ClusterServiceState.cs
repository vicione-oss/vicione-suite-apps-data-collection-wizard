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
    private ElapsedEventHandler? _currentTimerHandler;

    public ConcurrentDictionary<Guid, TaskCompletionSource<ErrorInfo?>> TaskCompletionSourceMap { get; } = new();
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public CancellationTokenSource Cts { get; } = new();
    public Ticket? IssuedTicket { get; private set; }

    public Guid IssueTicket(Guid? correlationId = null, TimeSpan? validity = null)
    {
        lock (_lock)
        {
            var ticketId = Guid.NewGuid();
            IssuedTicket = new Ticket(ticketId, correlationId ?? ticketId);
            StartTimer(ticketId, validity);
            return ticketId;
        }
    }

    public Guid ValidateTicketAndStopTimer(Guid ticketId)
    {
        lock (_lock)
        {
            if (IssuedTicket is null)
                throw new InvalidOperationException($"Ticket '{ticketId}' is invalid or expired");

            if (IssuedTicket.Value.Id != ticketId)
                throw new InvalidOperationException($"'{ticketId}' is not the last issued ticket ({IssuedTicket.Value.Id})");

            StopTimer();
            return IssuedTicket.Value.CorrelationId;
        }
    }

    public bool SetResult(Guid? correlationId, ErrorInfo? errorInfo = null)
    {
        if (correlationId is null
            || !TaskCompletionSourceMap.TryGetValue(correlationId.Value, out var taskCompletionSource))
        {
            return false;
        }

        taskCompletionSource.TrySetResult(errorInfo);
        lock (_lock)
        {
            if (IssuedTicket is not null && IssuedTicket.Value.CorrelationId == correlationId.Value)
                DiscardUpdateRequestCore(IssuedTicket.Value.Id);
        }
        return true;
    }

    private void StartTimer(Guid ticketId, TimeSpan? interval)
    {
        StopTimer();
        _currentTimerHandler = (_, _) => OnTimerElapsed(ticketId);
        _timer.Elapsed += _currentTimerHandler;
        _timer.Interval = (interval ?? s_defaultTicketValidity).TotalMilliseconds;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_currentTimerHandler is not null)
        {
            _timer.Elapsed -= _currentTimerHandler;
            _currentTimerHandler = null;
        }
        _timer.Stop();
    }

    public void DiscardUpdateRequest(Guid ticketId)
    {
        lock (_lock)
        {
            DiscardUpdateRequestCore(ticketId);
        }
    }

    /// <summary>
    /// Must be called while holding <see cref="_lock"/>.
    /// </summary>
    private void DiscardUpdateRequestCore(Guid ticketId)
    {
        if (IssuedTicket?.Id != ticketId)
            return;
        TaskCompletionSourceMap.TryRemove(IssuedTicket.Value.CorrelationId, out _);
        StopTimer();
        IssuedTicket = null;
        Semaphore.Release();
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
            StopTimer();
            Cts.Dispose();
            _timer.Dispose();
            Semaphore.Dispose();
            IssuedTicket = null;
        }
    }

    private void OnTimerElapsed(Guid ticketId)
    {
        lock (_lock)
        {
            if (IssuedTicket?.Id == ticketId)
                DiscardUpdateRequestCore(ticketId);
        }
    }
}

public record struct Ticket(Guid Id, Guid CorrelationId);
