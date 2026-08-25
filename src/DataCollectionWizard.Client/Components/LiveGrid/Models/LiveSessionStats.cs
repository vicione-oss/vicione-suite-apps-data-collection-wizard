using System.Diagnostics;

namespace DataCollectionWizard.Client.Components.LiveGrid.Models;

/// <summary>
/// What the live view has seen since it was opened: how long it has been watching, how many values arrived, and
/// at what rate.
/// </summary>
/// <remarks>
/// <para>Counted where the values land rather than where they are drawn. The view coalesces its rendering to
/// twice a second, so anything counted at render time would count pictures, not values.</para>
/// <para>Two rates, because they answer different questions. The average says how much arrived altogether; after
/// ten minutes of watching an outage barely moves it. The rolling one says whether anything is still arriving.
/// </para>
/// <para>The rolling figure uses fixed buckets rather than a list of arrival times: a busy master can deliver
/// hundreds of values a second, and a list would be allocated and trimmed continuously for a number that is only
/// read twice a second.</para>
/// </remarks>
internal sealed class LiveSessionStats
{
    private const int BucketMilliseconds = 500;
    private const int BucketCount = 10;

    private readonly long[] _counts = new long[BucketCount];
    private readonly long[] _slots = new long[BucketCount];
    private readonly Lock _gate = new();

    private long _received;
    private long _startedAt = Stopwatch.GetTimestamp();

    /// <summary>
    /// How far back <see cref="CurrentPerSecond"/> looks.
    /// </summary>
    public static TimeSpan RecentWindow { get; } = TimeSpan.FromMilliseconds(BucketMilliseconds * BucketCount);

    /// <summary>
    /// Whether anything is subscribed at all.
    /// </summary>
    /// <remarks>
    /// Until a node is picked in the tree nothing is subscribed and no value can arrive, so there is nothing to
    /// report - a window counting up from zero next to "0 values" would only say that the view is idle, which
    /// the empty grid says already.
    /// </remarks>
    public bool IsWatching { get; private set; }

    /// <summary>
    /// How long the view has been watching.
    /// </summary>
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startedAt);

    /// <summary>
    /// How many values arrived since then.
    /// </summary>
    public long Received => Interlocked.Read(ref _received);

    /// <summary>
    /// Values per second over the whole session.
    /// </summary>
    public double AveragePerSecond
    {
        get
        {
            var seconds = Elapsed.TotalSeconds;

            return seconds > 0 ? Received / seconds : 0;
        }
    }

    /// <summary>
    /// Values per second over the last few seconds.
    /// </summary>
    public double CurrentPerSecond
    {
        get
        {
            var elapsed = Elapsed;
            var slot = CurrentSlot(elapsed);
            long total = 0;

            lock (_gate)
            {
                for (var i = 0; i < BucketCount; i++)
                {
                    // A bucket older than the window is a leftover from a previous lap round the ring.
                    if (slot - _slots[i] < BucketCount)
                        total += _counts[i];
                }
            }

            // Early on the window is not full yet, so divide by what has actually elapsed - otherwise the first
            // seconds show a rate several times too low.
            var seconds = Math.Min(RecentWindow.TotalSeconds, elapsed.TotalSeconds);

            return seconds > 0 ? total / seconds : 0;
        }
    }

    /// <summary>
    /// Records one arriving value. Called from the subscription handlers, so from arbitrary threads.
    /// </summary>
    public void Note()
    {
        Interlocked.Increment(ref _received);

        var slot = CurrentSlot(Elapsed);
        var index = (int)(slot % BucketCount);

        lock (_gate)
        {
            if (_slots[index] != slot)
            {
                _slots[index] = slot;
                _counts[index] = 0;
            }

            _counts[index]++;
        }
    }

    /// <summary>
    /// Reports whether anything is subscribed, once the view has finished (re)subscribing.
    /// </summary>
    /// <remarks>
    /// The window starts when the first subscription is established, not when the page opens, and it is not
    /// restarted while watching continues - picking a different node tears the subscriptions down and builds
    /// them again, and typing in the filter does the same on every keystroke. Called after that has settled, so
    /// the moment in between with nothing subscribed is not mistaken for the end of the window.
    /// </remarks>
    public void SetWatching(bool watching)
    {
        if (watching == IsWatching)
            return;

        IsWatching = watching;

        // Whichever way it went, the figures now describe a different set of values than a moment ago.
        Restart();
    }

    /// <summary>
    /// Starts the observation window over, without reloading the view.
    /// </summary>
    public void Restart()
    {
        lock (_gate)
        {
            Array.Clear(_counts);
            Array.Clear(_slots);
        }

        Interlocked.Exchange(ref _received, 0);
        Interlocked.Exchange(ref _startedAt, Stopwatch.GetTimestamp());
    }

    private static long CurrentSlot(TimeSpan elapsed)
        => (long)(elapsed.TotalMilliseconds / BucketMilliseconds);
}
