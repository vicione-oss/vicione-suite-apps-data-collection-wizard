using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Services;

internal sealed class DeviceTreeBaseComparer : IComparer<IDeviceTreeBase>
{
    public static DeviceTreeBaseComparer Default { get; } = new DeviceTreeBaseComparer();

    public int Compare(IDeviceTreeBase? x, IDeviceTreeBase? y)
    {
        if (x is null && y is null)
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var nameX = x.Name;
        var nameY = y.Name;

        if (x is IDeviceTreeUserAliasNode deviceTreeAliasNodeX && !string.IsNullOrWhiteSpace(deviceTreeAliasNodeX.Alias))
            nameX = deviceTreeAliasNodeX.Alias + "." + x.Name;

        if (y is IDeviceTreeUserAliasNode deviceTreeAliasNodeY && !string.IsNullOrWhiteSpace(deviceTreeAliasNodeY.Alias))
            nameY = deviceTreeAliasNodeY.Alias + "." + y.Name;

        return AlphanumericComparer.Default.Compare(nameX, nameY);
    }
}

internal sealed class AlphanumericComparer(StringComparison textComparison = StringComparison.OrdinalIgnoreCase, bool ignoreSeparators = false) : IComparer<string>
{
    private readonly StringComparison _textComparison = textComparison;
    private readonly bool _ignoreSeparators = ignoreSeparators;

    public static AlphanumericComparer Default { get; } = new AlphanumericComparer();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // Regel: Strings, die mit Ziffern beginnen, kommen vor Strings, die mit Buchstaben beginnen
        if (x.Length > 0 && y.Length > 0)
        {
            var xStartsDigit = char.IsDigit(x[0]);
            var yStartsDigit = char.IsDigit(y[0]);
            if (xStartsDigit != yStartsDigit) return xStartsDigit ? -1 : 1;
        }

        int i = 0, j = 0;
        int nx = x.Length, ny = y.Length;

        while (i < nx && j < ny)
        {
            var cx = x[i];
            var cy = y[j];

            // KATEGORIEN: Separator < Letter < Digit
            var sx = IsSeparator(cx);
            var sy = IsSeparator(cy);
            var dx = char.IsDigit(cx);
            var dy = char.IsDigit(cy);

            // optional: Separatoren ignorieren
            if (_ignoreSeparators)
            {
                if (sx) { i++; continue; }
                if (sy) { j++; continue; }
            }

            // 1) Einer ist Separator, der andere nicht -> Separator kommt VOR (z.B. "A>1" < "A1")
            if (sx != sy) return sx ? -1 : 1;

            // 2) Beide Separatoren -> normal vergleichen und weiter
            if (sx && sy)
            {
                var csep = cx.CompareTo(cy);
                if (csep != 0) return csep;
                i++; j++;
                continue;
            }

            // 3) Beide Ziffern-Runs -> numerischer Vergleich (führende Nullen ignorieren)
            if (dx && dy)
            {
                var digitRunResult = CompareDigitRun(x, y, ref i, ref j);
                if (digitRunResult is not null) return digitRunResult.Value;

                continue; // nächster Run
            }

            // 4) Genau einer ist Ziffer -> Buchstabe vor Ziffer (z.B. "A" < "A1")
            if (dx != dy) return dx ? 1 : -1;

            // 5) Beide Text-Runs (Buchstaben/sonstige Nicht-Ziffern, keine Separatoren)
            var textRunResult = CompareTextRun(x, y, ref i, ref j, _textComparison);
            if (textRunResult is not null) return textRunResult.Value;
        }

        return nx.CompareTo(ny);
    }

    private static int? CompareDigitRun(string x, string y, ref int i, ref int j)
    {
        var nx = x.Length;
        var ny = y.Length;

        var zsx = i; while (i < nx && x[i] == '0') i++;
        var sxNum = i; while (i < nx && char.IsDigit(x[i])) i++;

        var zsy = j; while (j < ny && y[j] == '0') j++;
        var syNum = j; while (j < ny && char.IsDigit(y[j])) j++;

        var lenX = i - sxNum; // ohne führende Nullen
        var lenY = j - syNum;

        if (lenX != lenY) return lenX < lenY ? -1 : 1;

        for (var k = 0; k < lenX; k++)
        {
            var diff = x[sxNum + k] - y[syNum + k];
            if (diff != 0) return diff < 0 ? -1 : 1;
        }

        // numerisch gleich -> kürzere Gesamtdarstellung (inkl. Nullen) zuerst
        int totalX = i - zsx, totalY = j - zsy;
        if (totalX != totalY) return totalX < totalY ? -1 : 1;

        return null;
    }

    private static int? CompareTextRun(string x, string y, ref int i, ref int j, StringComparison textComparison)
    {
        var nx = x.Length;
        var ny = y.Length;

        var tx = i; while (i < nx && !char.IsDigit(x[i]) && !IsSeparator(x[i])) i++;
        var ty = j; while (j < ny && !char.IsDigit(y[j]) && !IsSeparator(y[j])) j++;

        var common = Math.Min(i - tx, j - ty);
        var cmp = string.Compare(x, tx, y, ty, common, textComparison);
        if (cmp != 0) return cmp;

        if ((i - tx) != (j - ty)) return (i - tx) < (j - ty) ? -1 : 1;

        return null;
    }

    private static bool IsSeparator(char c) => !char.IsLetterOrDigit(c);
}
