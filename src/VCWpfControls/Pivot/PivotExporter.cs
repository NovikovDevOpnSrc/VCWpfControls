using System.Globalization;
using System.Text;

namespace VCWpfControls.Pivot;

/// <summary>
/// Exports a <see cref="PivotResult"/> to CSV: row-field columns (headed by field names),
/// one column per column-field combination (x measure, caption as header) and the totals
/// column; a grand-total row closes the table when totals are present. Values are quoted
/// when needed; the list separator follows the culture.
/// </summary>
public static class PivotExporter
{
    /// <summary>Exports the pivot result to a CSV string.</summary>
    public static string ToCsv(PivotResult result, CultureInfo? culture = null, IReadOnlyList<string>? rowFieldNames = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        culture ??= CultureInfo.CurrentCulture;
        var separator = culture.TextInfo.ListSeparator;
        var builder = new StringBuilder();

        // Header: row fields, then column captions (repeated per measure), then Total.
        var headers = new List<string>();
        var fieldNames = rowFieldNames ?? Array.Empty<string>();
        var rowFieldCount = result.Rows.Count > 0 ? result.Rows[0].Count : fieldNames.Count;
        for (var i = 0; i < rowFieldCount; i++)
        {
            headers.Add(i < fieldNames.Count ? fieldNames[i] : $"Field {i + 1}");
        }

        foreach (var column in result.Columns)
        {
            var caption = string.Join(" / ", column.Select(v => v.Caption.Length == 0 ? "—" : v.Caption));
            headers.Add(caption);
        }

        var hasTotals = result.RowTotals.Length > 0;
        if (hasTotals)
        {
            headers.Add("Total");
        }

        builder.AppendLine(string.Join(separator, headers.Select(h => Quote(h, separator))));

        string Cell(object? value)
            => value is null ? string.Empty
                : Convert.ToString(value, culture) ?? string.Empty;

        for (var r = 0; r < result.Rows.Count; r++)
        {
            var cells = new List<string>();
            cells.AddRange(result.Rows[r].Select(v => Quote(v.Caption, separator)));
            for (var c = 0; c < result.Columns.Count; c++)
            {
                // Multiple measures per combination are joined into one cell (m1 / m2).
                var measures = string.Join(" / ", result.Cells[r][c].Select(Cell));
                cells.Add(Quote(measures, separator));
            }

            if (hasTotals)
            {
                var totals = string.Join(" / ", result.RowTotals[r].Select(Cell));
                cells.Add(Quote(totals, separator));
            }

            builder.AppendLine(string.Join(separator, cells));
        }

        if (hasTotals && result.Rows.Count > 0)
        {
            var grand = new List<string> { Quote("Total", separator) };
            for (var c = 0; c < result.Columns.Count; c++)
            {
                grand.Add(Quote(string.Join(" / ", result.ColumnTotals[c].Select(Cell)), separator));
            }

            grand.Add(Quote(string.Join(" / ", result.GrandTotals.Select(Cell)), separator));
            builder.AppendLine(string.Join(separator, grand));
        }

        return builder.ToString();
    }

    private static string Quote(string value, string separator)
        => value.Contains('"') || value.Contains(separator) || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
