using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services;

internal class EventTriggerIgnoreDataGroupEqualityComparer : IEqualityComparer<EventTrigger>
{
    public static EventTriggerIgnoreDataGroupEqualityComparer Instance = new();

    public bool Equals(EventTrigger? x, EventTrigger? y)
    {
        if (x is null && y is null)
            return true;

        if (x is null || y is null)
            return false;

        return x.Enabled == y.Enabled
            && x.OnWarning == y.OnWarning
            && x.OnDamage == y.OnDamage;
    }

    public int GetHashCode(EventTrigger obj)
    {
        var result = 0;

        if (obj.Enabled)
            result += 1;

        if (obj.OnWarning)
            result += 2;

        if (obj.OnDamage)
            result += 4;

        return result;
    }
}
