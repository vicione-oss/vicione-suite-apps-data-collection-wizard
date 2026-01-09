namespace DataCollectionWizard.Internal.Contracts;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enumerationen müssen einen Wert von null aufweisen.", Justification = "Null wird nicht benötigt.")]
public enum PoolingGrid
{
    OnChange = -1,
    SecondsOne = 1 * 1000,
    SecondsFive = 5 * 1000,
    SecondsTen = 10 * 1000,
    SecondsThirty = 30 * 1000,
    MinutesOne = 1 * 60 * 1000,
    MinutesTwo = 2 * 60 * 1000,
    MinutesFive = 5 * 60 * 1000,
    MinutesTen = 10 * 60 * 1000,
    MinutesThirty = 30 * 60 * 1000,
    HoursOne = 1 * 60 * 60 * 1000,
}
