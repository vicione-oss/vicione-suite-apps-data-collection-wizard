namespace DataCollectionWizard.Client.Components;

/// <summary>
/// One entry of a grouped grid body: either a group header (<see cref="Item"/> null) or a data row.
/// </summary>
/// <typeparam name="TItem">The grid's row model.</typeparam>
/// <param name="Item">The row's data, or <see langword="null"/> for a header.</param>
/// <param name="GroupLabel">The header's caption - the shared path of the rows below it.</param>
/// <param name="GroupItems">The rows a header stands for, so a header can act on all of them at once.</param>
/// <param name="IsGrouped">Whether this row sits under a header.</param>
/// <param name="GroupRelativePath">
/// The part of the row's path below its header, empty when the header already names its parent.
/// </param>
/// <param name="IsEvenDataRow">Whether this is an even data row, counted without the headers in between.</param>
internal sealed record GroupedRow<TItem>(
    TItem? Item,
    string? GroupLabel,
    IReadOnlyList<TItem>? GroupItems,
    bool IsGrouped,
    string? GroupRelativePath,
    bool IsEvenDataRow)
    where TItem : class
{
    public bool IsHeader => Item is null;
}

/// <summary>
/// Turns a flat, tree-ordered list of rows into a grouped grid body.
/// </summary>
/// <remarks>
/// Shared by the configuration grid and the live view so both group identically; a second copy of these rules
/// would drift, and the grouping is what tells same-named values apart.
/// </remarks>
internal static class GridGrouping
{
    /// <summary>
    /// Groups <paramref name="items"/> by their path.
    /// </summary>
    /// <remarks>
    /// A parent with more than one row gets a header at the parent level. Consecutive single-row parents that
    /// share a grandparent are rolled up under one header at that grandparent - so many one-value alarms collapse
    /// into a single "Alarms" group - while a lone single-row parent stays a plain row without a header.
    /// </remarks>
    /// <param name="items">The rows, in tree order.</param>
    /// <param name="breadcrumbOf">The path of a row, with " / " between its steps.</param>
    public static List<GroupedRow<TItem>> Build<TItem>(IReadOnlyList<TItem> items, Func<TItem, string> breadcrumbOf)
        where TItem : class
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(breadcrumbOf);

        // Step 1: runs of consecutive rows sharing the same immediate parent.
        var parentGroups = new List<(string Key, List<TItem> Items)>();
        var itemIndex = 0;
        while (itemIndex < items.Count)
        {
            var key = breadcrumbOf(items[itemIndex]);
            var group = new List<TItem>();
            while (itemIndex < items.Count && breadcrumbOf(items[itemIndex]) == key)
            {
                group.Add(items[itemIndex]);
                itemIndex++;
            }

            parentGroups.Add((key, group));
        }

        // Step 2: emit headers and rows.
        var rows = new List<GroupedRow<TItem>>();
        var dataRowIndex = 0;
        var groupIndex = 0;
        while (groupIndex < parentGroups.Count)
        {
            if (parentGroups[groupIndex].Items.Count > 1)
            {
                AddGroup(parentGroups[groupIndex].Key, parentGroups[groupIndex].Items);
                groupIndex++;
                continue;
            }

            var rollupKey = ParentPath(parentGroups[groupIndex].Key);
            var merged = new List<TItem>(parentGroups[groupIndex].Items);
            var nextGroupIndex = groupIndex + 1;
            while (nextGroupIndex < parentGroups.Count
                   && parentGroups[nextGroupIndex].Items.Count == 1
                   && ParentPath(parentGroups[nextGroupIndex].Key) == rollupKey)
            {
                merged.AddRange(parentGroups[nextGroupIndex].Items);
                nextGroupIndex++;
            }

            if (merged.Count > 1)
                AddGroup(rollupKey, merged);
            else
                AddRows(groupKey: null, merged);

            groupIndex = nextGroupIndex;
        }

        return rows;

        void AddGroup(string key, List<TItem> groupItems)
        {
            rows.Add(new GroupedRow<TItem>(null, key, groupItems, false, null, false));
            AddRows(key, groupItems);
        }

        void AddRows(string? groupKey, List<TItem> groupItems)
        {
            foreach (var item in groupItems)
            {
                rows.Add(new GroupedRow<TItem>(
                    item,
                    null,
                    null,
                    groupKey is not null,
                    groupKey is null ? null : RelativePath(breadcrumbOf(item), groupKey),
                    dataRowIndex % 2 == 0));

                dataRowIndex++;
            }
        }
    }

    // The parent path is the breadcrumb without its last " / segment".
    private static string ParentPath(string breadcrumb)
    {
        var index = breadcrumb.LastIndexOf(" / ", StringComparison.Ordinal);
        return index > 0 ? breadcrumb[..index] : breadcrumb;
    }

    // The part of a row's breadcrumb below its group (empty when the row is directly inside the group's container).
    private static string RelativePath(string breadcrumb, string groupKey)
        => breadcrumb.Length > groupKey.Length
            ? breadcrumb[groupKey.Length..].TrimStart(' ', '/')
            : string.Empty;
}
