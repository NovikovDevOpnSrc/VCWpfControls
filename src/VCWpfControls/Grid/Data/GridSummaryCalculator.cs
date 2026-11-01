using System.Globalization;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid.Data;

/// <summary>Computes aggregates over data rows for footer and group summaries.</summary>
public static class GridSummaryCalculator
{
    /// <summary>
    /// Calculates the aggregate of <paramref name="function"/> over the field
    /// <paramref name="fieldName"/> of the given items, using the provided accessor cache.
    /// </summary>
    public static object? Calculate(GridSummaryFunction function, string fieldName, IEnumerable<object> items, Func<Type, string, PropertyAccessor> accessorResolver)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (function == GridSummaryFunction.Count)
        {
            long count = 0;
            foreach (var _ in items)
            {
                count++;
            }

            return count;
        }

        PropertyAccessor? accessor = null;
        double sum = 0;
        long numericCount = 0;
        object? min = null;
        object? max = null;
        var culture = CultureInfo.CurrentCulture;

        foreach (var item in items)
        {
            accessor ??= accessorResolver(item.GetType(), fieldName);
            var value = accessor.GetValue(item);
            if (value is null || value is DBNull)
            {
                continue;
            }

            if (min is null || GridValueCompare.Compare(value, min, culture) < 0)
            {
                min = value;
            }

            if (max is null || GridValueCompare.Compare(value, max, culture) > 0)
            {
                max = value;
            }

            if (IsNumericValue(value, out var number))
            {
                sum += number;
                numericCount++;
            }
        }

        return function switch
        {
            GridSummaryFunction.Sum => RoundLikeSource(min, sum),
            GridSummaryFunction.Min => min,
            GridSummaryFunction.Max => max,
            GridSummaryFunction.Avg => numericCount > 0 ? RoundLikeSource(min, sum / numericCount) : null,
            _ => null,
        };
    }

    private static bool IsNumericValue(object value, out double number)
    {
        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            default:
                number = 0;
                return false;
        }
    }

    /// <summary>
    /// Converts a double aggregate back to the source-like type so that <c>"42"</c> formats as an
    /// integer while decimal sums keep their scale.
    /// </summary>
    private static object? RoundLikeSource(object? sampleValue, double value)
    {
        if (sampleValue is null)
        {
            return null;
        }

        return Type.GetTypeCode(sampleValue.GetType()) switch
        {
            TypeCode.Int32 => (int)Math.Round(value),
            TypeCode.Int64 => (long)Math.Round(value),
            TypeCode.Int16 => (short)Math.Round(value),
            TypeCode.Byte => (byte)Math.Clamp(Math.Round(value), 0, 255),
            TypeCode.UInt32 => (uint)Math.Max(0, Math.Round(value)),
            TypeCode.UInt64 => (ulong)Math.Max(0, Math.Round(value)),
            TypeCode.UInt16 => (ushort)Math.Clamp(Math.Round(value), 0, ushort.MaxValue),
            TypeCode.Decimal => (decimal)value,
            TypeCode.Single => (float)value,
            _ => value,
        };
    }
}
