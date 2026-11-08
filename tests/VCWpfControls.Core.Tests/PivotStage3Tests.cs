using System.Globalization;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using Xunit;

namespace VCWpfControls.Core.Tests;

/// <summary>Stage 3: dimension filtering, source-row buckets (drill-down), CSV export.</summary>
public class PivotStage3Tests
{
    private sealed record Sale(string Region, string Product, decimal Amount);

    private static List<Sale> Sales() => new()
    {
        new("East", "A", 10m),
        new("East", "B", 20m),
        new("West", "A", 5m),
        new("West", "B", 45m),
    };

    private static readonly IReadOnlyList<string> Rows = new[] { "Region" };
    private static readonly IReadOnlyList<string> Cols = new[] { "Product" };
    private static readonly IReadOnlyList<PivotMeasure> Sum = new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) };

    [Fact]
    public void FieldFilter_ExcludesSourceRows()
    {
        var result = PivotEngine.Compute(Sales(), Rows, Cols, Sum, fieldFilter: (_, v) => !Equals(v, "West"));

        Assert.Single(result.Rows);                       // only East
        Assert.Equal("East", result.Rows[0][0].Caption);
        Assert.Equal(10m, result.Cells[0][0][0]);         // East/A
        Assert.Equal(30m, result.GrandTotals[0]);
    }

    [Fact]
    public void FieldFilter_Null_Unchanged()
    {
        var result = PivotEngine.Compute(Sales(), Rows, Cols, Sum, fieldFilter: null);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(80m, result.GrandTotals[0]);
    }

    [Fact]
    public void SourceRows_BucketsPerIntersection()
    {
        var result = PivotEngine.Compute(Sales(), Rows, Cols, Sum, includeSourceRows: true);

        Assert.NotNull(result.SourceRows);
        // East/A bucket holds exactly the East+A source row.
        var eastA = result.SourceRows![0][0]!;
        Assert.Single(eastA);
        Assert.Equal(10m, Assert.IsType<Sale>(eastA[0]).Amount);

        // Without the flag: null (memory not spent).
        Assert.Null(PivotEngine.Compute(Sales(), Rows, Cols, Sum).SourceRows);
    }

    [Fact]
    public void ToCsv_HeadersRowsAndGrandTotal()
    {
        var result = PivotEngine.Compute(Sales(), Rows, Cols, Sum);
        var csv = PivotExporter.ToCsv(result, CultureInfo.InvariantCulture, Rows);

        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');
        Assert.Equal(4, lines.Length); // header + East + West + grand
        Assert.Equal("Region,A,B,Total", lines[0]);
        Assert.Equal("East,10,20,30", lines[1]);
        Assert.Equal("West,5,45,50", lines[2]);
        Assert.Equal("Total,15,65,80", lines[3]);
    }

    [Fact]
    public void ToCsv_CultureSeparatorAndNullCells()
    {
        var data = new[]
        {
            new { Region = "East", Product = "A", Amount = 1m },
            new { Region = "West", Product = "B", Amount = 2m },
        };
        var result = PivotEngine.Compute(data, Rows, Cols, Sum);
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var csv = PivotExporter.ToCsv(result, ru, Rows);

        // ru-RU list separator is ';' and empty intersections export as empty cells.
        Assert.Contains("East;1;;1", csv, StringComparison.Ordinal);
        Assert.Contains("West;;2;2", csv, StringComparison.Ordinal);
    }
}
