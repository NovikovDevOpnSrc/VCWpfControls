using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using Xunit;

namespace VCWpfControls.Core.Tests;

/// <summary>
/// The UI-free pivot engine: grouping, aggregation (through the shared calculator),
/// sorting of axis values, composite keys, null groups, totals and empty intersections.
/// </summary>
public class PivotEngineTests
{
    private sealed record Sale(string Region, string Product, decimal Amount, int Qty)
    {
        public static Sale Of(string region, string product, decimal amount, int qty = 1)
            => new(region, product, amount, qty);
    }

    private static List<Sale> Sales() => new()
    {
        Sale.Of("East", "A", 10m),
        Sale.Of("East", "B", 20m),
        Sale.Of("West", "A", 5m),
        Sale.Of("West", "B", 15m),
        Sale.Of("West", "B", 30m), // duplicate intersection -> aggregation
        Sale.Of("East", "A", 7m),  // duplicate intersection
    };

    [Fact]
    public void RowsColumns_GroupAndAggregate()
    {
        var result = PivotEngine.Compute(
            Sales(),
            rowFields: new[] { "Region" },
            columnFields: new[] { "Product" },
            measures: new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) });

        // Axis: sorted rows East, West; columns A, B.
        Assert.Equal(new[] { "East", "West" }, result.Rows.Select(r => r[0].Caption));
        Assert.Equal(new[] { "A", "B" }, result.Columns.Select(c => c[0].Caption));

        // Cells: East/A = 10+7, East/B = 20, West/A = 5, West/B = 15+30.
        Assert.Equal(17m, result.Cells[0][0][0]);
        Assert.Equal(20m, result.Cells[0][1][0]);
        Assert.Equal(5m, result.Cells[1][0][0]);
        Assert.Equal(45m, result.Cells[1][1][0]);
    }

    [Fact]
    public void Totals_SumAcrossAxes_AndGrand()
    {
        var result = PivotEngine.Compute(
            Sales(),
            new[] { "Region" },
            new[] { "Product" },
            new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) });

        Assert.Equal(37m, result.RowTotals[0][0]);    // East: 17+20
        Assert.Equal(50m, result.RowTotals[1][0]);    // West: 5+45
        Assert.Equal(22m, result.ColumnTotals[0][0]); // A: 17+5
        Assert.Equal(65m, result.ColumnTotals[1][0]); // B: 20+45
        Assert.Equal(87m, result.GrandTotals[0]);     // everything
    }

    [Fact]
    public void AllFunctions_AndMultipleMeasures()
    {
        var result = PivotEngine.Compute(
            Sales(),
            new[] { "Region" },
            Array.Empty<string>(),
            new[]
            {
                new PivotMeasure("Amount", GridSummaryFunction.Avg),
                new PivotMeasure("Amount", GridSummaryFunction.Min),
                new PivotMeasure("Amount", GridSummaryFunction.Max),
                new PivotMeasure("Amount", GridSummaryFunction.Count),
            });

        Assert.Single(result.Columns);                          // single (empty) column group
        Assert.Equal(2, result.Rows.Count);
        // East amounts: 10, 20, 7 -> avg 37/3, min 7, max 20, count 3.
        // Avg is rounded back to the source-like type (decimal, calculator semantics).
        Assert.Equal(37m / 3m, Assert.IsType<decimal>(result.Cells[0][0][0]), precision: 12);
        Assert.Equal(7m, result.Cells[0][0][1]);
        Assert.Equal(20m, result.Cells[0][0][2]);
        Assert.Equal(3L, result.Cells[0][0][3]);
    }

    [Fact]
    public void CompositeRowKeys_TwoLevels()
    {
        var data = new[]
        {
            new { Region = "East", City = "X", Amount = 1m },
            new { Region = "East", City = "Y", Amount = 2m },
            new { Region = "West", City = "X", Amount = 4m },
        };
        var result = PivotEngine.Compute(
            data,
            new[] { "Region", "City" },
            Array.Empty<string>(),
            new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) });

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(new[] { "East", "X" }, result.Rows[0].Select(v => v.Caption));
        Assert.Equal(new[] { "East", "Y" }, result.Rows[1].Select(v => v.Caption));
        Assert.Equal(new[] { "West", "X" }, result.Rows[2].Select(v => v.Caption));
        Assert.Equal(7m, result.GrandTotals[0]);
    }

    [Fact]
    public void NullFieldValues_FormTheirOwnGroup()
    {
        var data = new[]
        {
            new { Region = (string?)"East", Amount = 1m },
            new { Region = (string?)null, Amount = 2m },
            new { Region = (string?)null, Amount = 3m },
        };
        var result = PivotEngine.Compute(
            data,
            new[] { "Region" },
            Array.Empty<string>(),
            new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) });

        Assert.Equal(2, result.Rows.Count);
        var nullRowIndex = result.Rows.ToList().FindIndex(r => r[0].Value is null);
        Assert.True(nullRowIndex >= 0, "null group must exist");
        Assert.Equal(5m, result.RowTotals[nullRowIndex][0]); // null group sums 2+3
        Assert.Contains(result.Rows, r => r[0].Value is null && r[0].Caption.Length == 0);
    }

    [Fact]
    public void EmptyIntersection_CountZero_OthersNull()
    {
        var data = new[]
        {
            new { Region = "East", Product = "A", Amount = 1m },
            new { Region = "West", Product = "B", Amount = 2m },
        };
        var result = PivotEngine.Compute(
            data,
            new[] { "Region" },
            new[] { "Product" },
            new[]
            {
                new PivotMeasure("Amount", GridSummaryFunction.Sum),
                new PivotMeasure("Amount", GridSummaryFunction.Count),
            });

        // East/B and West/A are empty intersections.
        Assert.Null(result.Cells[0][1][0]);
        Assert.Equal(0L, result.Cells[0][1][1]);
        Assert.Null(result.Cells[1][0][0]);
        Assert.Equal(0L, result.Cells[1][0][1]);
    }

    [Fact]
    public void NoTotalsRequested_TotalsEmpty()
    {
        var result = PivotEngine.Compute(
            Sales(),
            new[] { "Region" },
            new[] { "Product" },
            new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) },
            includeTotals: false);

        Assert.Empty(result.RowTotals);
        Assert.Empty(result.ColumnTotals);
        Assert.Empty(result.GrandTotals);
        Assert.Equal(2, result.Rows.Count); // cells intact
    }

    [Fact]
    public void EmptySource_SingleNullColumns_EmptyRows()
    {
        var result = PivotEngine.Compute(
            Array.Empty<Sale>(),
            new[] { "Region" },
            new[] { "Product" },
            new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) });

        Assert.Empty(result.Rows);
        Assert.Empty(result.Columns);
        Assert.Empty(result.Cells);
        Assert.Null(result.GrandTotals[0]); // Sum of nothing is null (Count would be 0)
    }

    [Fact]
    public void NoMeasures_Throws()
    {
        Assert.Throws<ArgumentException>(() => PivotEngine.Compute(
            Sales(),
            new[] { "Region" },
            Array.Empty<string>(),
            Array.Empty<PivotMeasure>()));
    }

    [Fact]
    public void AxisValues_NullEqualityAndCaption()
    {
        var a = new PivotAxisValue(null);
        var b = new PivotAxisValue(null);
        Assert.Equal(a, b);
        Assert.Equal(string.Empty, a.Caption);
        Assert.Equal("42", new PivotAxisValue(42).Caption);
    }
}
