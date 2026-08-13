namespace DataCollectionWizard.Client.Extensions;

internal static class DaysOfWeekExtension
{
    private const string DaysOfWeek_Everyday = "Everyday";
    private const string DaysOfWeek_Friday = "Friday";
    private const string DaysOfWeek_Monday = "Monday";
    private const string DaysOfWeek_MoToFr = "Monday to Friday";
    private const string DaysOfWeek_MoWeFr = "Monday, Wednesday and Friday";
    private const string DaysOfWeek_SaSu = "Saturday and Sunday";
    private const string DaysOfWeek_Saturday = "Saturday";
    private const string DaysOfWeek_Sunday = "Sunday";
    private const string DaysOfWeek_Thursday = "Thursday";
    private const string DaysOfWeek_Tuesday = "Tuesday";
    private const string DaysOfWeek_TuThu = "Tuesday and Thursday";
    private const string DaysOfWeek_Wednesday = "Wednesday";

    public static IEnumerable<DayOfWeek> AsEnumerable(this DaysOfWeek days)
        => days switch
        {
            DaysOfWeek.Monday => [DayOfWeek.Monday],
            DaysOfWeek.Tuesday => [DayOfWeek.Tuesday],
            DaysOfWeek.Wednesday => [DayOfWeek.Wednesday],
            DaysOfWeek.Thursday => [DayOfWeek.Thursday],
            DaysOfWeek.Friday => [DayOfWeek.Friday],
            DaysOfWeek.Saturday => [DayOfWeek.Saturday],
            DaysOfWeek.Sunday => [DayOfWeek.Sunday],
            DaysOfWeek.MoToFr =>
            [
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
            ],
            DaysOfWeek.TuThu =>
            [
                    DayOfWeek.Tuesday,
                    DayOfWeek.Thursday,
            ],
            DaysOfWeek.MoWeFr =>
            [
                    DayOfWeek.Monday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Friday,
            ],
            DaysOfWeek.SaSu =>
            [
                    DayOfWeek.Saturday,
                    DayOfWeek.Sunday,
            ],
            DaysOfWeek.Everyday =>
            [
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
                    DayOfWeek.Saturday,
                    DayOfWeek.Sunday,
            ],
            _ => throw new NotImplementedException($"Unknown type of {nameof(DaysOfWeek)}.")
        };

    public static string DaysOfWeekToString(this DaysOfWeek days)
        => days switch
        {
            DaysOfWeek.Monday => DaysOfWeek_Monday,
            DaysOfWeek.Tuesday => DaysOfWeek_Tuesday,
            DaysOfWeek.Wednesday => DaysOfWeek_Wednesday,
            DaysOfWeek.Thursday => DaysOfWeek_Thursday,
            DaysOfWeek.Friday => DaysOfWeek_Friday,
            DaysOfWeek.Saturday => DaysOfWeek_Saturday,
            DaysOfWeek.Sunday => DaysOfWeek_Sunday,
            DaysOfWeek.MoToFr => DaysOfWeek_MoToFr,
            DaysOfWeek.TuThu => DaysOfWeek_TuThu,
            DaysOfWeek.MoWeFr => DaysOfWeek_MoWeFr,
            DaysOfWeek.SaSu => DaysOfWeek_SaSu,
            DaysOfWeek.Everyday => DaysOfWeek_Everyday,
            _ => throw new NotImplementedException($"Unknown type of {nameof(DaysOfWeek)}.")
        };

    public static DaysOfWeek ToDaysOfWeek(this string daysOfWeekString)
        => daysOfWeekString switch
        {
            DaysOfWeek_Monday => DaysOfWeek.Monday,
            DaysOfWeek_Tuesday => DaysOfWeek.Tuesday,
            DaysOfWeek_Wednesday => DaysOfWeek.Wednesday,
            DaysOfWeek_Thursday => DaysOfWeek.Thursday,
            DaysOfWeek_Friday => DaysOfWeek.Friday,
            DaysOfWeek_Saturday => DaysOfWeek.Saturday,
            DaysOfWeek_Sunday => DaysOfWeek.Sunday,
            DaysOfWeek_MoToFr => DaysOfWeek.MoToFr,
            DaysOfWeek_TuThu => DaysOfWeek.TuThu,
            DaysOfWeek_MoWeFr => DaysOfWeek.MoWeFr,
            DaysOfWeek_SaSu => DaysOfWeek.SaSu,
            DaysOfWeek_Everyday => DaysOfWeek.Everyday,
            _ => throw new NotImplementedException($"Unknown {nameof(DaysOfWeek)} string.")
        };
}
