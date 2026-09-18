using System.Globalization;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.LiveGrid.Toolbar;

/// <summary>
/// Says what the live view is actually showing: how long it has been watching and how much has arrived.
/// </summary>
/// <remarks>
/// The view holds no values from before it was opened, so every figure in it - and anything later derived from
/// them - is relative to this window. Naming the window is what keeps that honest; without it "Min 12,4" looks
/// like a property of the device rather than of the last two minutes.
/// </remarks>
public sealed partial class LiveViewSessionInfo : ComponentBase, IDisposable
{
    // The elapsed time needs a second's resolution and the rolling rate is averaged over several, so this is as
    // often as the line can say anything new. It ticks on its own because values may stop arriving entirely -
    // which is precisely the case the line exists for.
    private static readonly TimeSpan s_tick = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource _stopping = new();

    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    private string SessionText
    {
        get
        {
            var session = Service.Session;
            var elapsed = session.Elapsed;

            return string.Format(
                CultureInfo.CurrentCulture,
                Localization.LiveViewPage.SessionInfo,
                $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}",
                session.Received,
                session.AveragePerSecond,
                session.CurrentPerSecond);
        }
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
    }

    protected override void OnInitialized()
        => _ = TickAsync(_stopping.Token);

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(s_tick);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // The view was left; nothing to do.
        }
    }
}
