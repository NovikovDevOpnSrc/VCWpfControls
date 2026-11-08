using System.Windows;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>PivotGrid stage 3: FieldFilter recompute, drill-down rows and the double-click event.</summary>
public class PivotStage3ControlTests
{
    private sealed record Sale(string Region, string Product, decimal Amount);

    private static PivotGrid Create(out Window window)
    {
        var pivot = new PivotGrid { Width = 560, RowHeight = 26, HeaderHeight = 30 };
        pivot.RowFields.Add("Region");
        pivot.ColumnFields.Add("Product");
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
        window = TestWindowHost.Show(pivot);
        pivot.PivotItemsSource = new List<Sale>
        {
            new("East", "A", 10m),
            new("East", "B", 20m),
            new("West", "A", 5m),
            new("West", "B", 45m),
        };
        TestWindowHost.Pump();
        return pivot;
    }

    [StaFact]
    public void FieldFilter_Change_RecomputesView()
    {
        var pivot = Create(out var window);
        try
        {
            Assert.Equal(80m, pivot.Result!.GrandTotals[0]);

            pivot.FieldFilter = (_, v) => !Equals(v, "West");
            TestWindowHost.Pump();

            Assert.Single(pivot.Result!.Rows);
            Assert.Equal(30m, pivot.Result.GrandTotals[0]);
            Assert.Equal(2, pivot.DataEngine.View.Count); // East + grand row
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DrillDownRows_ReturnSourceBehindCell()
    {
        var pivot = Create(out var window);
        try
        {
            // Row 0 = East, column 0 = A -> the single East/A sale.
            var rows = pivot.GetDrillDownRows(0, 0);
            var sale = Assert.IsType<Sale>(Assert.Single(rows));
            Assert.Equal(10m, sale.Amount);

            Assert.Empty(pivot.GetDrillDownRows(99, 99)); // out of range -> empty
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DoubleClick_RaisesDrillDownWithSourceRows()
    {
        var pivot = Create(out var window);
        PivotDrillDownEventArgs? args = null;
        pivot.DrillDownRequested += (_, e) => args = e;
        try
        {
            // Set the current cell (mouse-down normally does) then simulate a double click.
            var east = pivot.DataEngine.View
                .OfType<VCWpfControls.Grid.Data.GridDataRow>()
                .Single(d => ((PivotRowItem)d.Item).Values[0].Caption == "East").Item;
            pivot.SetCurrentCellForTest(east, pivot.Columns[1]); // East | A
            TestWindowHost.Pump();

            pivot.RaiseDrillDownForCurrentCell();
            TestWindowHost.Pump();

            Assert.NotNull(args);
            Assert.Equal("East", args!.Row.Values[0].Caption);
            var sale = Assert.IsType<Sale>(Assert.Single(args.SourceRows));
            Assert.Equal(10m, sale.Amount);
            Assert.Equal(10m, args.Aggregates[0]);
        }
        finally
        {
            window.Close();
        }
    }
}
