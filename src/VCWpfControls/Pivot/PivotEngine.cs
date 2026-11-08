using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Pivot;

/// <summary>One aggregate shown in the pivot cells: a field plus a function.</summary>
public sealed class PivotMeasure
{
    /// <summary>Creates a measure.</summary>
    public PivotMeasure(string fieldName, GridSummaryFunction function, string? caption = null)
    {
        FieldName = fieldName;
        Function = function;
        Caption = caption;
    }

    /// <summary>Field the aggregate is computed over (ignored for Count).</summary>
    public string FieldName { get; set; }

    /// <summary>Aggregate function.</summary>
    public GridSummaryFunction Function { get; set; }

    /// <summary>Optional display caption; defaults to <c>Function(Field)</c>.</summary>
    public string? Caption { get; set; }

    /// <inheritdoc />
    public override string ToString() => Caption ?? $"{Function}({FieldName})";
}

/// <summary>One distinct value on a pivot axis (row or column).</summary>
public sealed class PivotAxisValue
{
    /// <summary>Creates the axis value.</summary>
    public PivotAxisValue(object? value)
    {
        Value = value;
        Caption = value is null
            ? string.Empty
            : Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    }

    /// <summary>The raw field value (null groups together).</summary>
    public object? Value { get; }

    /// <summary>Culture-formatted caption.</summary>
    public string Caption { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is PivotAxisValue other && GridValueCompare.Compare(Value, other.Value, null) == 0;

    /// <inheritdoc />
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    /// <inheritdoc />
    public override string ToString() => Caption;
}

/// <summary>The computed pivot table: axis values, cells and totals (all UI-free).</summary>
public sealed class PivotResult
{
    /// <summary>Row axis values, one entry per row-field combination (sorted).</summary>
    public IReadOnlyList<IReadOnlyList<PivotAxisValue>> Rows { get; init; } = Array.Empty<IReadOnlyList<PivotAxisValue>>();

    /// <summary>Column axis values, one entry per column-field combination (sorted).</summary>
    public IReadOnlyList<IReadOnlyList<PivotAxisValue>> Columns { get; init; } = Array.Empty<IReadOnlyList<PivotAxisValue>>();

    /// <summary>Cell aggregates: [row][column][measure]; null where no source rows intersect.</summary>
    public object?[][][] Cells { get; init; } = Array.Empty<object?[][][]>();

    /// <summary>Row totals (across all columns): [row][measure].</summary>
    public object?[][] RowTotals { get; init; } = Array.Empty<object?[][]>();

    /// <summary>Column totals (across all rows): [column][measure].</summary>
    public object?[][] ColumnTotals { get; init; } = Array.Empty<object?[][]>();

    /// <summary>The grand total per measure.</summary>
    public object?[] GrandTotals { get; init; } = Array.Empty<object?>();

    /// <summary>
    /// Source rows per intersection [row][column] (null when none) — computed only when
    /// requested; powers drill-down.
    /// </summary>
    public IReadOnlyList<object>?[][]? SourceRows { get; init; }
}

/// <summary>
/// Computes pivot tables from any non-generic <see cref="System.Collections.IEnumerable"/> source: groups items by the
/// row/column field values and aggregates the intersections with the shared
/// <see cref="GridSummaryCalculator"/> (Sum/Min/Max/Avg/Count). UI-free — unit-testable
/// without STA. Multiple row/column fields produce composite keys; null field values form
/// their own group; axis values sort ascending with nulls first; empty intersections
/// aggregate to null (Count to 0).
/// </summary>
public static class PivotEngine
{
    /// <summary>Computes the pivot table.</summary>
    public static PivotResult Compute(
        System.Collections.IEnumerable items,
        IReadOnlyList<string> rowFields,
        IReadOnlyList<string> columnFields,
        IReadOnlyList<PivotMeasure> measures,
        bool includeTotals = true,
        Func<string, object?, bool>? fieldFilter = null,
        bool includeSourceRows = false)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(rowFields);
        ArgumentNullException.ThrowIfNull(columnFields);
        ArgumentNullException.ThrowIfNull(measures);

        var materialized = items.Cast<object?>().Where(item => item is not null).Cast<object>().ToList();
        if (measures.Count == 0)
        {
            throw new ArgumentException("At least one measure is required.", nameof(measures));
        }

        var accessorCache = new Dictionary<(Type, string), PropertyAccessor>();
        PropertyAccessor Accessor(Type type, string field)
            => accessorCache.TryGetValue((type, field), out var cached)
                ? cached
                : accessorCache[(type, field)] = PropertyAccessor.GetOrCreate(type, field);

        object?[] KeyOf(object item, IReadOnlyList<string> fields)
        {
            var key = new object?[fields.Count];
            for (var i = 0; i < fields.Count; i++)
            {
                key[i] = Accessor(item.GetType(), fields[i]).GetValue(item);
            }

            return key;
        }

        // Bucket the source rows by (rowKey, columnKey).
        var rowKeys = new List<object?[]>();
        var columnKeys = new List<object?[]>();
        var buckets = new Dictionary<(int Row, int Column), List<object>>();
        var rowIndexByKey = new Dictionary<object?[], int>(new AxisKeyComparer());
        var columnIndexByKey = new Dictionary<object?[], int>(new AxisKeyComparer());

        var sourceBuckets = includeSourceRows
            ? new Dictionary<(int Row, int Column), List<object>>()
            : null;
        var includedItems = new List<object>();

        foreach (var item in materialized)
        {
            if (fieldFilter is not null)
            {
                var excluded = false;
                foreach (var field in rowFields)
                {
                    if (!fieldFilter(field, Accessor(item.GetType(), field).GetValue(item)))
                    {
                        excluded = true;
                        break;
                    }
                }

                if (!excluded)
                {
                    foreach (var field in columnFields)
                    {
                        if (!fieldFilter(field, Accessor(item.GetType(), field).GetValue(item)))
                        {
                            excluded = true;
                            break;
                        }
                    }
                }

                if (excluded)
                {
                    continue;
                }
            }

            includedItems.Add(item);
            var rowKey = KeyOf(item, rowFields);
            var columnKey = KeyOf(item, columnFields);
            if (!rowIndexByKey.TryGetValue(rowKey, out var r))
            {
                r = rowKeys.Count;
                rowIndexByKey[rowKey] = r;
                rowKeys.Add(rowKey);
            }

            if (!columnIndexByKey.TryGetValue(columnKey, out var c))
            {
                c = columnKeys.Count;
                columnIndexByKey[columnKey] = c;
                columnKeys.Add(columnKey);
            }

            if (!buckets.TryGetValue((r, c), out var list))
            {
                buckets[(r, c)] = list = new List<object>();
            }

            list.Add(item);
            if (sourceBuckets is not null)
            {
                if (!sourceBuckets.TryGetValue((r, c), out var sourceList))
                {
                    sourceBuckets[(r, c)] = sourceList = new List<object>();
                }

                sourceList.Add(item);
            }
        }

        var sortedRows = SortKeys(rowKeys);
        var sortedColumns = SortKeys(columnKeys);

        // Re-index after sorting.
        var rowOrder = new int[sortedRows.Count];
        for (var i = 0; i < sortedRows.Count; i++)
        {
            rowOrder[i] = rowIndexByKey[sortedRows[i]];
        }

        var columnOrder = new int[sortedColumns.Count];
        for (var i = 0; i < sortedColumns.Count; i++)
        {
            columnOrder[i] = columnIndexByKey[sortedColumns[i]];
        }

        var rows = sortedRows.Select(k => k.Select(v => new PivotAxisValue(v)).ToList() as IReadOnlyList<PivotAxisValue>).ToList();
        var columns = sortedColumns.Select(k => k.Select(v => new PivotAxisValue(v)).ToList() as IReadOnlyList<PivotAxisValue>).ToList();

        var sourceRows = includeSourceRows ? new IReadOnlyList<object>?[sortedRows.Count][] : null;
        var cells = new object?[sortedRows.Count][][];
        for (var r = 0; r < sortedRows.Count; r++)
        {
            cells[r] = new object?[sortedColumns.Count][];
            for (var c = 0; c < sortedColumns.Count; c++)
            {
                cells[r][c] = Aggregate(buckets.GetValueOrDefault((rowOrder[r], columnOrder[c])), measures, Accessor);
            }
        }

        if (sourceRows is not null)
        {
            for (var r = 0; r < sortedRows.Count; r++)
            {
                sourceRows[r] = new IReadOnlyList<object>?[sortedColumns.Count];
                for (var c = 0; c < sortedColumns.Count; c++)
                {
                    sourceRows[r][c] = sourceBuckets!.GetValueOrDefault((rowOrder[r], columnOrder[c]));
                }
            }
        }

        if (!includeTotals)
        {
            return new PivotResult
            {
                Rows = rows,
                Columns = columns,
                Cells = cells,
                RowTotals = Array.Empty<object?[]>(),
                ColumnTotals = Array.Empty<object?[]>(),
                GrandTotals = Array.Empty<object?>(),
                SourceRows = sourceRows,
            };
        }

        var rowTotals = new object?[sortedRows.Count][];
        for (var r = 0; r < sortedRows.Count; r++)
        {
            var rowItems = columnOrder
                .SelectMany(c => buckets.GetValueOrDefault((rowOrder[r], c)) ?? Enumerable.Empty<object>())
                .ToList();
            rowTotals[r] = Aggregate(rowItems, measures, Accessor);
        }

        var columnTotals = new object?[sortedColumns.Count][];
        for (var c = 0; c < sortedColumns.Count; c++)
        {
            var columnItems = rowOrder
                .SelectMany(r => buckets.GetValueOrDefault((r, columnOrder[c])) ?? Enumerable.Empty<object>())
                .ToList();
            columnTotals[c] = Aggregate(columnItems, measures, Accessor);
        }

        return new PivotResult
        {
            Rows = rows,
            Columns = columns,
            Cells = cells,
            RowTotals = rowTotals,
            ColumnTotals = columnTotals,
            GrandTotals = Aggregate(fieldFilter is null ? materialized : includedItems, measures, Accessor),
            SourceRows = sourceRows,
        };
    }

    private static object?[] Aggregate(List<object>? items, IReadOnlyList<PivotMeasure> measures, Func<Type, string, PropertyAccessor> accessor)
    {
        var values = new object?[measures.Count];
        for (var m = 0; m < measures.Count; m++)
        {
            // Empty intersections: Count = 0, everything else null (mirrors the calculator).
            values[m] = GridSummaryCalculator.Calculate(
                measures[m].Function,
                measures[m].FieldName,
                items ?? new List<object>(),
                accessor);
        }

        return values;
    }

    private static List<object?[]> SortKeys(List<object?[]> keys)
    {
        var sorted = keys.ToList();
        sorted.Sort((x, y) =>
        {
            for (var i = 0; i < x.Length; i++)
            {
                var order = GridValueCompare.Compare(x[i], y[i], null);
                if (order != 0)
                {
                    return order;
                }
            }

            return 0;
        });
        return sorted;
    }

    /// <summary>Comparer of composite axis keys (value-wise, handles nulls).</summary>
    private sealed class AxisKeyComparer : IEqualityComparer<object?[]>
    {
        public bool Equals(object?[]? x, object?[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (GridValueCompare.Compare(x[i], y[i], null) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(object?[] obj)
        {
            var hash = 17;
            foreach (var value in obj)
            {
                hash = (hash * 31) + (value?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }
}
