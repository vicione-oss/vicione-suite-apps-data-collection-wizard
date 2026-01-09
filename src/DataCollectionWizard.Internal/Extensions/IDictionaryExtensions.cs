namespace DataCollectionWizard.Internal.Extensions;

public static class IDictionaryExtensions
{
    public static void CopyTo<TKey, TValue>(this IDictionary<TKey, TValue> me, IDictionary<TKey, TValue> other)
    {
        foreach (var item in me)
        {
            other[item.Key] = item.Value;
        }
    }
}
