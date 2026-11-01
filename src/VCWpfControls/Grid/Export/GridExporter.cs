using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid.Export;

/// <summary>
/// Exports the current view of a <see cref="GridControl"/> (visible columns, filtered and sorted
/// data rows) to CSV, Excel (xlsx) and HTML. Group rows are not exported; export reflects what
/// the data engine currently shows.
/// </summary>
public static class GridExporter
{
    /// <summary>
    /// Exports to a CSV string. Values are quoted when needed; the list separator follows
    /// <paramref name="culture"/> (semicolon in ru-RU, comma in invariant).
    /// </summary>
    public static string ToCsv(GridControl grid, bool includeHeader = true, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        culture ??= CultureInfo.CurrentCulture;
        var separator = culture.TextInfo.ListSeparator;

        var builder = new StringBuilder();
        var columns = grid.VisibleColumns;

        if (includeHeader)
        {
            builder.AppendLine(string.Join(separator, columns.Select(c => Quote(Convert.ToString(c.Header, culture) ?? string.Empty, separator))));
        }

        foreach (var row in grid.DataEngine.DataRows)
        {
            builder.AppendLine(string.Join(separator, columns.Select(c =>
                Quote(CellText(c, row.Item, culture), separator))));
        }

        return builder.ToString();
    }

    /// <summary>Exports to a CSV file (UTF-8 with BOM so Excel opens it correctly).</summary>
    public static void ExportToCsv(GridControl grid, string path, bool includeHeader = true, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        File.WriteAllText(path, ToCsv(grid, includeHeader, culture), new UTF8Encoding(true));
    }

    /// <summary>Exports to an HTML table string with inline styles matching the grid look.</summary>
    public static string ToHtml(GridControl grid, bool includeHeader = true, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        culture ??= CultureInfo.CurrentCulture;

        var builder = new StringBuilder();
        builder.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
        builder.Append("<style>body{font-family:'Segoe UI',Arial,sans-serif;color:#1f2937;}");
        builder.Append("table{border-collapse:collapse;font-size:13px;}");
        builder.Append("th{background:#f3f4f6;border:1px solid #d1d5db;padding:4px 8px;text-align:left;}");
        builder.Append("td{border:1px solid #e5e7eb;padding:3px 8px;}");
        builder.Append("td.num{text-align:right;}</style></head><body>");
        builder.Append("<table>");

        var columns = grid.VisibleColumns;
        if (includeHeader)
        {
            builder.Append("<thead><tr>");
            foreach (var column in columns)
            {
                builder.Append("<th>").Append(Escape(Convert.ToString(column.Header, culture) ?? string.Empty)).Append("</th>");
            }

            builder.Append("</tr></thead>");
        }

        builder.Append("<tbody>");
        foreach (var row in grid.DataEngine.DataRows)
        {
            builder.Append("<tr>");
            foreach (var column in columns)
            {
                var cssClass = column.HorizontalContentAlignment == System.Windows.HorizontalAlignment.Right ? " class=\"num\"" : string.Empty;
                builder.Append("<td").Append(cssClass).Append('>')
                    .Append(Escape(CellText(column, row.Item, culture)))
                    .Append("</td>");
            }

            builder.Append("</tr>");
        }

        builder.Append("</tbody></table></body></html>");
        return builder.ToString();
    }

    /// <summary>Exports to an HTML file.</summary>
    public static void ExportToHtml(GridControl grid, string path, bool includeHeader = true, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        File.WriteAllText(path, ToHtml(grid, includeHeader, culture), new UTF8Encoding(false));
    }

    /// <summary>
    /// Exports to an Excel workbook (xlsx). Header row is frozen and bold; numeric and date
    /// cells are written with their native types where possible.
    /// </summary>
    public static XLWorkbook ToWorkbook(GridControl grid, bool includeHeader = true, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        culture ??= CultureInfo.CurrentCulture;

        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Data");
        var columns = grid.VisibleColumns;
        var rowIndex = 1;

        if (includeHeader)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = sheet.Cell(rowIndex, i + 1);
                cell.Value = Convert.ToString(columns[i].Header, culture) ?? string.Empty;
                cell.Style.Font.Bold = true;
            }

            sheet.SheetView.FreezeRows(1);
            rowIndex++;
        }

        foreach (var row in grid.DataEngine.DataRows)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                WriteCell(sheet.Cell(rowIndex, i + 1), columns[i], row.Item, culture);
            }

            rowIndex++;
        }

        sheet.Columns().AdjustToContents(1, Math.Min(rowIndex, 200));
        return workbook;
    }

    /// <summary>Exports to an xlsx file.</summary>
    public static void ExportToXlsx(GridControl grid, string path, bool includeHeader = true, CultureInfo? culture = null)
    {
        using var workbook = ToWorkbook(grid, includeHeader, culture);
        workbook.SaveAs(path);
    }

    private static void WriteCell(IXLCell cell, GridColumnBase column, object item, CultureInfo culture)
    {
        var itemType = item.GetType();
        var value = PropertyAccessor.GetOrCreate(itemType, column.FieldName).GetValue(item);
        if (value is null or DBNull)
        {
            return;
        }

        if (IsNumeric(value))
        {
            cell.Value = Convert.ToDecimal(value, culture);
        }
        else if (value is DateTime dateTime)
        {
            cell.Value = dateTime;
        }
        else if (value is bool boolean)
        {
            cell.Value = boolean;
        }
        else
        {
            cell.Value = CellText(column, item, culture);
        }
    }

    private static bool IsNumeric(object value) => Type.GetTypeCode(value.GetType()) is
        >= TypeCode.SByte and <= TypeCode.Decimal;

    private static string CellText(GridColumnBase column, object item, CultureInfo culture)
    {
        var itemType = item.GetType();
        var value = PropertyAccessor.GetOrCreate(itemType, column.FieldName).GetValue(item);
        if (value is null or DBNull)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(column.StringFormat)
            ? Convert.ToString(value, culture) ?? string.Empty
            : string.Format(culture, "{0:" + column.StringFormat + "}", value);
    }

    private static string Quote(string value, string separator)
    {
        if (value.Contains('"') || value.Contains(separator) || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string Escape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
