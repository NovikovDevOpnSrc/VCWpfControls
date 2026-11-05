using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Star columns in a viewport that cannot fit them must keep a readable minimum width
/// (default MinWidth 20, DataGrid convention) and hand the overflow to the horizontal
/// scrollbar — not collapse into 10px slivers.
/// </summary>
public class NarrowViewportTests
{
    [StaFact]
    public void StarColumns_KeepReadableMinimum_InNarrowViewport()
    {
        var grid = new GridControl { Width = 50, RowHeight = 24, HeaderHeight = 26 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = new GridColumnWidth(1, GridUnitType.Star) });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = new GridColumnWidth(1, GridUnitType.Star) });
        grid.Columns.Add(new GridTextColumn { FieldName = "Salary", Header = "C", Width = new GridColumnWidth(1, GridUnitType.Star) });
        grid.ItemsSource = CreateEmployees(2);
        TestWindowHost.Pump();

        try
        {
            foreach (var slot in grid.Layout.Slots)
            {
                Assert.True(slot.Width >= 20, $"{slot.Column.FieldName}: star collapsed to {slot.Width} in a narrow viewport");
            }

            // The overflow goes to the scrollbar, not into sliver columns.
            Assert.True(grid.Layout.TotalWidth > grid.ViewportWidth, "expected horizontal overflow");
            var scrollbar = (System.Windows.Controls.Primitives.ScrollBar)grid.Template.FindName("PART_HorizontalScrollBar", grid)!;
            Assert.Equal(Visibility.Visible, scrollbar.Visibility);
        }
        finally
        {
            window.Close();
        }
    }
}
