using System.Globalization;

namespace VCWpfControls.Grid.Data;

/// <summary>
/// Culture-aware value comparison used by sorting and filtering.
/// Handles <see langword="null"/>, enums (by underlying value via <see cref="IComparable"/>),
/// numeric widening, strings (culture-aware) and an invariant string fallback.
/// </summary>
/// <remarks>
/// Note: a boxed <see cref="Nullable{T}"/> with a value is boxed as its underlying type,
/// so <see langword="null"/> is the only nullable representation that reaches this method.
/// </remarks>
public static class GridValueCompare
{
    /// <summary>
    /// Compares two cell values. <see langword="null"/> sorts before any non-null value,
    /// independent of sort direction (direction handling belongs to the caller).
    /// </summary>
    public static int Compare(object? a, object? b, CultureInfo? culture = null)
    {
        if (a is null && b is null)
        {
            return 0;
        }

        if (a is null)
        {
            return -1;
        }

        if (b is null)
        {
            return 1;
        }

        var ta = a.GetType();
        var tb = b.GetType();

        if (ta == typeof(string) && tb == typeof(string))
        {
            // Culture-aware comparison is intentional (grid sorts follow the user's locale);
            // string.Compare with a culture does not allocate, unlike StringComparer.Create
            // per call which dominated sort memory (see benchmarks/RESULTS.md).
#pragma warning disable CA1309 // culture-aware comparison is the grid's contract
            return string.Compare((string)a, (string)b, culture ?? CultureInfo.CurrentCulture, CompareOptions.None);
#pragma warning restore CA1309
        }

        if (ta == tb && a is IComparable sameType)
        {
            return sameType.CompareTo(b);
        }

        if (IsNumeric(ta) && IsNumeric(tb))
        {
            return Convert.ToDouble(a, CultureInfo.InvariantCulture)
                .CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture));
        }

        if (TryAlignTypes(ref a, ref b) && a is IComparable comparable)
        {
            return comparable.CompareTo(b);
        }

        // Last resort: invariant string comparison.
        return string.CompareOrdinal(
            Convert.ToString(a, CultureInfo.InvariantCulture),
            Convert.ToString(b, CultureInfo.InvariantCulture));
    }

    /// <summary>Converts one operand to the other's type when possible so that both share a type.</summary>
    private static bool TryAlignTypes(ref object a, ref object b)
    {
        if (TryConvert(ref a, b.GetType())
            || TryConvert(ref b, a.GetType()))
        {
            return a.GetType() == b.GetType();
        }

        return false;
    }

    private static bool TryConvert(ref object value, Type targetType)
    {
        if (value.GetType() == targetType)
        {
            return true;
        }

        try
        {
            value = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;
}
