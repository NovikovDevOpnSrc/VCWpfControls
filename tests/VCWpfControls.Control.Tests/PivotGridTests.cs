using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The PivotGrid control: computes the engine result and renders it as grid columns/rows
/// (row-field columns, per-combination data columns, totals column and grand-total row).
/// </summary>
public class PivotGridTests
{
    private sealed record Sale(string Region, string Product, decimal Amount);

    private static List<Sale> Sales() => new()
    {
        new("East", "A", 10m),
        new("East", "B", 20m),
        new("West", "A", 5m),
        new("West", "B", 45m),
    };

    private static PivotGrid Create(out Window window)
    {
        var pivot = new PivotGrid { Width = 560, RowHeight = 26, HeaderHeight = 30 };
        pivot.RowFields.Add("Region");
        pivot.ColumnFields.Add("Product");
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
        window = TestWindowHost.Show(pivot);
        pivot.PivotItemsSource = Sales();
        TestWindowHost.Pump();
        return pivot;
    }

    [StaFact]
    public void RendersColumnsAndRows_WithTotals()
    {
        var pivot = Create(out var window);
        try
        {
            Assert.NotNull(pivot.Result);

            // Columns: Region | A | B | Total
            Assert.Equal(4, pivot.Columns.Count);
            Assert.Equal("Region", pivot.Columns[0].Header);
            Assert.Equal("A", pivot.Columns[1].Header);
            Assert.Equal("B", pivot.Columns[2].Header);
            Assert.Equal(GridStrings.PivotTotal, pivot.Columns[3].Header);

            // Rows: East, West, Total (grand).
            var rows = pivot.DataEngine.View
                .OfType<VCWpfControls.Grid.Data.GridDataRow>()
                .Select(d => (PivotRowItem)d.Item)
                .ToList();
            Assert.Equal(3, rows.Count);
            Assert.Equal("East", rows[0].Values[0].Caption);
            Assert.Equal("West", rows[1].Values[0].Caption);
            Assert.True(rows[2].IsTotal);

            // Cells per row follow the generated column order.
            Assert.Equal(10m, rows[0].Cells[0]); // East/A
            Assert.Equal(20m, rows[0].Cells[1]); // East/B
            Assert.Equal(30m, rows[0].Totals[0]);
            Assert.Equal(50m, rows[1].Totals[1 - 1]); // West total
            Assert.Equal(80m, rows[2].Totals[0]);     // grand total
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DisplayElements_BindThroughPaths()
    {
        var pivot = Create(out var window);
        try
        {
            var row = (VCWpfControls.Grid.Primitives.GridRow)pivot.ItemContainerGenerator.ContainerFromIndex(0);
            var panel = row.Template.FindName("PART_Cells", row) as VCWpfControls.Grid.Primitives.GridCellsPanel
                ?? throw new InvalidOperationException("cells panel missing");
            var eastA = (System.Windows.Controls.TextBlock)panel.GetCellElement(pivot.Columns[1])!;
            Assert.Equal("10", eastA.Text); // decimal 10 formatted by ToString
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MultipleMeasures_MultiplyColumns_AndCaptionThem()
    {
        var pivot = new PivotGrid { Width = 700 };
        pivot.RowFields.Add("Region");
        pivot.ColumnFields.Add("Product");
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Count));
        var window = TestWindowHost.Show(pivot);
        pivot.PivotItemsSource = Sales();
        TestWindowHost.Pump();
        try
        {
            // Region | A·Sum | A·Count | B·Sum | B·Count | Total·Sum | Total·Count
            Assert.Equal(7, pivot.Columns.Count);
            Assert.Contains("·", pivot.Columns[1].Header?.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NoTotals_NoColumnNoRow()
    {
        var pivot = new PivotGrid { Width = 500, IncludeTotals = false };
        pivot.RowFields.Add("Region");
        pivot.ColumnFields.Add("Product");
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
        var window = TestWindowHost.Show(pivot);
        pivot.PivotItemsSource = Sales();
        TestWindowHost.Pump();
        try
        {
            Assert.Equal(3, pivot.Columns.Count); // Region, A, B
            Assert.Equal(2, pivot.DataEngine.View.OfType<VCWpfControls.Grid.Data.GridDataRow>().Select(d => d.Item).Count()); // no grand row
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ChangingRowFields_Recomputes()
    {
        var pivot = Create(out var window);
        try
        {
            pivot.RowFields.Add("Product");
            TestWindowHost.Pump();

            // Two leading columns now; composite row keys.
            Assert.Equal(2, ((PivotTextColumn)pivot.Columns[0]).Path.Contains("Values[0]", StringComparison.Ordinal) ? 2 : 0);
            Assert.StartsWith("Values[1]", ((PivotTextColumn)pivot.Columns[1]).Path, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }
}
