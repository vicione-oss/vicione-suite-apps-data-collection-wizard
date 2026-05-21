using System.Linq.Expressions;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class BlobDataCell : ComponentBase
{
    private static readonly Expression<Func<ComboBoxOption<DaysOfWeek>, string>> s_daysOfWeekTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<DaysOfWeek>, DaysOfWeek>> s_daysOfWeekValueSelector = e => e.Value;
    private static readonly Expression<Func<ComboBoxOption<int>, string>> s_timesADayTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<int>, int>> s_timesADayValueSelector = e => e.Value;
    private readonly ComboBoxOption<DaysOfWeek>[] _daysOfWeek =
    [
        new() { Text = DaysOfWeek.Monday.DaysOfWeekToString(), Value = DaysOfWeek.Monday, },
        new() { Text = DaysOfWeek.Tuesday.DaysOfWeekToString(), Value = DaysOfWeek.Tuesday, },
        new() { Text = DaysOfWeek.Wednesday.DaysOfWeekToString(), Value = DaysOfWeek.Wednesday, },
        new() { Text = DaysOfWeek.Thursday.DaysOfWeekToString(), Value = DaysOfWeek.Thursday, },
        new() { Text = DaysOfWeek.Friday.DaysOfWeekToString(), Value = DaysOfWeek.Friday, },
        new() { Text = DaysOfWeek.Saturday.DaysOfWeekToString(), Value = DaysOfWeek.Saturday, },
        new() { Text = DaysOfWeek.Sunday.DaysOfWeekToString(), Value = DaysOfWeek.Sunday, },
        new() { Text = DaysOfWeek.MoToFr.DaysOfWeekToString(), Value = DaysOfWeek.MoToFr, },
        new() { Text = DaysOfWeek.TuThu.DaysOfWeekToString(), Value = DaysOfWeek.TuThu, },
        new() { Text = DaysOfWeek.MoWeFr.DaysOfWeekToString(), Value = DaysOfWeek.MoWeFr, },
        new() { Text = DaysOfWeek.SaSu.DaysOfWeekToString(), Value = DaysOfWeek.SaSu, },
        new() { Text = DaysOfWeek.Everyday.DaysOfWeekToString(), Value = DaysOfWeek.Everyday, },
    ];
    private int _maxTimesADay = -1;
    private ComboBoxOption<int>[] _timesADay = [];
    private SchedulerConfiguration? _cachedSchedulerConfig;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Parameter]
    public IDeviceTreeSchedulableDataNode? BlobDataNode { get; set; }

    [Parameter]
    public PublishTargetInfo? Configuration { get; set; }

    [Parameter]
    public EventCallback<(IDeviceTreeSchedulableDataNode BlobDataNode, Connection Configuration)> DownloadButtonClicked { get; set; }

    [Parameter]
    public int MaxTimesADay { get; set; } = 12;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private SchedulerConfiguration SchedulerConfig
        => _cachedSchedulerConfig ??= BlobDataNode!.SchedulerConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration!.Connection.Id);

    public static Dictionary<DayOfWeek, TimeSpan[]> GetNewScheduling(int timesADay, IEnumerable<DayOfWeek> daysOfWeek)
    {
        var newTiming = new TimeSpan[timesADay];
        for (var i = 0; i < newTiming.Length; i++)
            newTiming[i] = TimeSpan.FromDays(1).Divide(timesADay).Multiply(i);

        var newScheduling = new Dictionary<DayOfWeek, TimeSpan[]>();
        foreach (var day in daysOfWeek)
            newScheduling.Add(day, newTiming);

        return newScheduling;
    }

    private int GetTimesADay()
        => SchedulerConfig.Times.First().Value.Length;

    private DaysOfWeek GetSelectedDaysOfWeek()
    {
        var times = SchedulerConfig.Times;

        foreach (var daysOfWeek in _daysOfWeek)
        {
            var possibleSelectedDaysOfWeek = daysOfWeek.Value.AsEnumerable().ToArray();

            if (times.Count != possibleSelectedDaysOfWeek.Length)
                continue;

            foreach (var time in times)
            {
                if (!possibleSelectedDaysOfWeek.Contains(time.Key))
                    continue;
            }

            return daysOfWeek.Value;
        }

        return DaysOfWeek.Monday;
    }

    private static bool IsDownloadButtonSupported()
        => false;

    private bool IsRecordingEnabled()
        => SchedulerConfig.Enabled;

    private bool IsSupportedConnection()
        => Configuration?.IsSupportedForConfiguration(BlobDataNode) ?? false;

    protected override void OnParametersSet()
    {
        _cachedSchedulerConfig = null;

        if (MaxTimesADay >= 1 && MaxTimesADay != _maxTimesADay)
        {
            _maxTimesADay = MaxTimesADay;
            var timesADay = GetTimesADay();
            var timesADayInBounds = timesADay <= _maxTimesADay;
            _timesADay = new ComboBoxOption<int>[timesADayInBounds ? _maxTimesADay : _maxTimesADay + 1];

            for (var i = 0; i < _maxTimesADay; i++)
                _timesADay[i] = new() { Text = $"{i + 1}x", Value = i + 1, };

            if (!timesADayInBounds)
                _timesADay[^1] = new() { Text = $"{timesADay}x", Value = timesADay, };
        }
    }

    private void OnDaysOfWeekChanged(DaysOfWeek daysOfWeek)
    {
        var times = SchedulerConfig.Times;

        SchedulingChanged(times.Values.First().Length, daysOfWeek.AsEnumerable(), times);
    }

    private void OnTimesADayChanged(int timesADay)
    {
        var times = SchedulerConfig.Times;

        SchedulingChanged(timesADay, times.Keys, times);
    }

    private void SchedulingChanged(int timesADay, IEnumerable<DayOfWeek> daysOfWeek, Dictionary<DayOfWeek, TimeSpan[]> times)
    {
        var newScheduling = GetNewScheduling(timesADay, daysOfWeek);
        times.Clear();

        foreach (var newSchedulingItem in newScheduling)
            times[newSchedulingItem.Key] = newSchedulingItem.Value;

        OnDeviceTreeChanged.InvokeAsync();
    }

    private void SchedulingEnabledChanged(bool isEnabled)
    {
        SchedulerConfig.Enabled = isEnabled;

        Service.InvokeDataPointEnabledChanged(isEnabled);
        OnDeviceTreeChanged.InvokeAsync();
    }
}
