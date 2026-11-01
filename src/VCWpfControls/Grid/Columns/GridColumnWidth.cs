using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace VCWpfControls.Grid.Columns;

/// <summary>
/// The width of a grid column: absolute pixels, star (proportional) or auto (fit content).
/// Supports XAML strings like <c>"120"</c>, <c>"2*"</c>, <c>"*"</c> and <c>"Auto"</c>.
/// </summary>
[TypeConverter(typeof(GridColumnWidthConverter))]
public readonly struct GridColumnWidth : IEquatable<GridColumnWidth>
{
    /// <summary>Auto-sizes the column to its content.</summary>
    public static readonly GridColumnWidth Auto = new(1, GridUnitType.Auto);

    /// <summary>One star: fills the remaining viewport proportionally.</summary>
    public static readonly GridColumnWidth Star = new(1, GridUnitType.Star);

    /// <summary>Creates a width value.</summary>
    public GridColumnWidth(double value, GridUnitType type)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
        UnitType = type;
    }

    /// <summary>The numeric component (pixels or star weight).</summary>
    public double Value { get; }

    /// <summary>The unit of the width.</summary>
    public GridUnitType UnitType { get; }

    /// <summary>Absolute width in device-independent pixels.</summary>
    public bool IsAbsolute => UnitType == GridUnitType.Pixel;

    /// <summary>Star (proportional) width.</summary>
    public bool IsStar => UnitType == GridUnitType.Star;

    /// <summary>Auto (fit content) width.</summary>
    public bool IsAuto => UnitType == GridUnitType.Auto;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(GridColumnWidth left, GridColumnWidth right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(GridColumnWidth left, GridColumnWidth right) => !left.Equals(right);

    /// <summary>Converts pixels to an absolute width.</summary>
    public static implicit operator GridColumnWidth(double pixels) => new(pixels, GridUnitType.Pixel);

    /// <summary>Converts an integer number of pixels to an absolute width.</summary>
    public static implicit operator GridColumnWidth(int pixels) => new(pixels, GridUnitType.Pixel);

    /// <summary>Parses forms "Auto", "120", "*", "2*".</summary>
    public static GridColumnWidth Parse(string text)
    {
        var converted = new GridColumnWidthConverter().ConvertFromInvariantString(text ?? string.Empty);
        return converted is GridColumnWidth width
            ? width
            : throw new FormatException($"'{text}' is not a valid column width.");
    }

    /// <inheritdoc />
    public bool Equals(GridColumnWidth other) => Value.Equals(other.Value) && UnitType == other.UnitType;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GridColumnWidth other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Value, UnitType);

    /// <inheritdoc />
    public override string ToString() => UnitType switch
    {
        GridUnitType.Auto => "Auto",
        GridUnitType.Star => FormattableString.Invariant($"{Value:0.###}*"),
        _ => FormattableString.Invariant($"{Value:0.###}"),
    };
}

/// <summary>Converts <see cref="string"/> to <see cref="GridColumnWidth"/> and back.</summary>
public sealed class GridColumnWidthConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            text = text.Trim();
            if (text.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return GridColumnWidth.Auto;
            }

            culture ??= CultureInfo.InvariantCulture;
            if (text.EndsWith('*'))
            {
                var starText = text[..^1].Trim();
                var weight = starText.Length == 0 ? 1.0 : double.Parse(starText, culture);
                return new GridColumnWidth(weight, GridUnitType.Star);
            }

            return new GridColumnWidth(double.Parse(text, culture), GridUnitType.Pixel);
        }

        return base.ConvertFrom(context, culture, value)!;
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        => destinationType == typeof(string) && value is GridColumnWidth width
            ? width.ToString()
            : base.ConvertTo(context, culture, value, destinationType)!;
}
