namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// The values a raw-data recording and an event trigger can be set to.
/// <para>Shared by the single-row editors and the bulk panel so both offer exactly the same choices.</para>
/// </summary>
public static class RawDataOptions
{
    /// <summary>
    /// Smallest trigger delay, in hours.
    /// </summary>
    public const int MinimumDelayHours = 1;

    /// <summary>
    /// Largest trigger delay, in hours.
    /// </summary>
    public const int MaximumDelayHours = 48;

    /// <summary>
    /// Recording lengths in milliseconds.
    /// </summary>
    public static IReadOnlyList<int> Durations { get; } = [1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000];

    /// <summary>
    /// Sample rates in samples per second.
    /// </summary>
    public static IReadOnlyList<int> Frequencies { get; } = [50000, 100000];

    /// <summary>
    /// A duration as it is shown in the editor ("10s").
    /// </summary>
    public static string DurationToString(int milliseconds)
        => $"{milliseconds / 1000}s";

    /// <summary>
    /// A sample rate as it is shown in the editor ("100k Sample/s").
    /// </summary>
    public static string FrequencyToString(int samplesPerSecond)
        => $"{samplesPerSecond / 1000}k Sample/s";
}
