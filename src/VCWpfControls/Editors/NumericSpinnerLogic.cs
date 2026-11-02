using System.Globalization;

namespace VCWpfControls.Editors;

/// <summary>
/// UI-free parse/format/clamp logic shared by <see cref="NumericUpDown"/> and its tests.
/// </summary>
public static class NumericSpinnerLogic
{
    /// <summary>Clamps the value to the given bounds.</summary>
    public static decimal Clamp(decimal value, decimal min, decimal max)
        => value < min ? min : value > max ? max : value;

    /// <summary>Parses user text with the given culture; <see langword="null"/> when invalid.</summary>
    public static decimal? Parse(string? text, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        culture ??= CultureInfo.CurrentCulture;
        var styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowThousands;
        if (!decimal.TryParse(text, styles, culture, out var value)
            && !decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value))
        {
            return null;
        }

        return value;
    }

    /// <summary>Formats the value: the explicit format string, or fixed decimal places.</summary>
    public static string Format(decimal value, int decimalPlaces, string? formatString, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return string.IsNullOrEmpty(formatString)
            ? value.ToString("F" + Math.Max(0, decimalPlaces), culture)
            : value.ToString(formatString, culture);
    }

    /// <summary>Derives the display precision: explicit places, or the increment's scale.</summary>
    public static int DeriveDecimalPlaces(decimal increment, int decimalPlaces)
    {
        if (decimalPlaces >= 0)
        {
            return decimalPlaces;
        }

        var scale = 0;
        var scaled = increment;
        while (scaled != decimal.Truncate(scaled) && scale < 10)
        {
            scaled *= 10;
            scale++;
        }

        return scale;
    }
}
