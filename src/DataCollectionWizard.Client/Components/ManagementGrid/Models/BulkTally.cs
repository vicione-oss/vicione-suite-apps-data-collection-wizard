namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// How many of the configurations in scope have a flag set, out of how many there are.
/// </summary>
/// <remarks>
/// The panel used to know only whether the selection agreed - a <see langword="bool"/>? that was
/// <see langword="null"/> as soon as one configuration differed. That is the state the user most needs to see
/// through: whether 180 of 243 values are on or 3 decides whether one wants to enable or disable, and "neither
/// button marked" says neither.
/// </remarks>
/// <param name="On">How many have the flag set.</param>
/// <param name="Total">How many configurations are in scope at all.</param>
public readonly record struct BulkTally(int On, int Total)
{
    /// <summary>
    /// Every configuration in scope has it set.
    /// </summary>
    public bool All => Total > 0 && On == Total;

    /// <summary>
    /// No configuration in scope has it set.
    /// </summary>
    public bool None => Total > 0 && On == 0;

    /// <summary>
    /// Some do and some do not - there is no single value to show.
    /// </summary>
    public bool IsMixed => On > 0 && On < Total;

    /// <summary>
    /// How many do not have it set.
    /// </summary>
    public int Off => Total - On;

    /// <summary>
    /// 0 to 1, for the meter beside the buttons. Empty when there is nothing in scope.
    /// </summary>
    public double Share => Total > 0 ? (double)On / Total : 0;
}
