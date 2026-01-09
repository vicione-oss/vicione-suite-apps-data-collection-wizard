namespace DataCollectionWizard.Internal.Extensions;

public static class IEnumerableExtensions
{
    public static bool None<T>(this IEnumerable<T> me, Func<T, bool> predicate)
        => !me.Any(predicate);
}
