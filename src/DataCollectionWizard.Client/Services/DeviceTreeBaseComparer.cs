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

        // Strings starting with a digit sort before strings starting with a letter.
        if (x.Length > 0 && y.Length > 0)
        {
            var xStartsDigit = char.IsDigit(x[0]);
            var yStartsDigit = char.IsDigit(y[0]);
            if (xStartsDigit != yStartsDigit) return xStartsDigit ? -1 : 1;
        }

        int indexX = 0, indexY = 0;

        while (indexX < x.Length && indexY < y.Length)
        {
            var characterX = x[indexX];
            var characterY = y[indexY];

            // Categories: separator < letter < digit.
            var isSeparatorX = IsSeparator(characterX);
            var isSeparatorY = IsSeparator(characterY);
            var isDigitX = char.IsDigit(characterX);
            var isDigitY = char.IsDigit(characterY);

            if (_ignoreSeparators)
            {
                if (isSeparatorX) { indexX++; continue; }
                if (isSeparatorY) { indexY++; continue; }
            }

            // Exactly one is a separator: the separator comes first ("A>1" < "A1").
            if (isSeparatorX != isSeparatorY) return isSeparatorX ? -1 : 1;

            // Both are separators: compare them as characters and move on.
            if (isSeparatorX && isSeparatorY)
            {
                var separatorOrder = characterX.CompareTo(characterY);
                if (separatorOrder != 0) return separatorOrder;
                indexX++; indexY++;
                continue;
            }

            // Both start a run of digits: compare the numbers, ignoring leading zeros.
            if (isDigitX && isDigitY)
            {
                var digitRunResult = CompareDigitRun(x, y, ref indexX, ref indexY);
                if (digitRunResult is not null) return digitRunResult.Value;

                continue; // next run
            }

            // Exactly one is a digit: the letter comes first ("A" < "A1").
            if (isDigitX != isDigitY) return isDigitX ? 1 : -1;

            // Both start a run of text (anything that is neither a digit nor a separator).
            var textRunResult = CompareTextRun(x, y, ref indexX, ref indexY, _textComparison);
            if (textRunResult is not null) return textRunResult.Value;
        }

        return x.Length.CompareTo(y.Length);
    }

    private static int? CompareDigitRun(string x, string y, ref int indexX, ref int indexY)
    {
        var runStartX = indexX; while (indexX < x.Length && x[indexX] == '0') indexX++;
        var digitsStartX = indexX; while (indexX < x.Length && char.IsDigit(x[indexX])) indexX++;

        var runStartY = indexY; while (indexY < y.Length && y[indexY] == '0') indexY++;
        var digitsStartY = indexY; while (indexY < y.Length && char.IsDigit(y[indexY])) indexY++;

        // Without leading zeros, a number with more digits is the larger one.
        var digitCountX = indexX - digitsStartX;
        var digitCountY = indexY - digitsStartY;

        if (digitCountX != digitCountY) return digitCountX < digitCountY ? -1 : 1;

        for (var offset = 0; offset < digitCountX; offset++)
        {
            var difference = x[digitsStartX + offset] - y[digitsStartY + offset];
            if (difference != 0) return difference < 0 ? -1 : 1;
        }

        // Numerically equal: the shorter spelling, leading zeros included, comes first.
        int runLengthX = indexX - runStartX, runLengthY = indexY - runStartY;
        if (runLengthX != runLengthY) return runLengthX < runLengthY ? -1 : 1;

        return null;
    }

    private static int? CompareTextRun(string x, string y, ref int indexX, ref int indexY, StringComparison textComparison)
    {
        var runStartX = indexX; while (indexX < x.Length && !char.IsDigit(x[indexX]) && !IsSeparator(x[indexX])) indexX++;
        var runStartY = indexY; while (indexY < y.Length && !char.IsDigit(y[indexY]) && !IsSeparator(y[indexY])) indexY++;

        var runLengthX = indexX - runStartX;
        var runLengthY = indexY - runStartY;

        var sharedLength = Math.Min(runLengthX, runLengthY);
        var textOrder = string.Compare(x, runStartX, y, runStartY, sharedLength, textComparison);
        if (textOrder != 0) return textOrder;

        if (runLengthX != runLengthY) return runLengthX < runLengthY ? -1 : 1;

        return null;
    }

    private static bool IsSeparator(char character) => !char.IsLetterOrDigit(character);
}
