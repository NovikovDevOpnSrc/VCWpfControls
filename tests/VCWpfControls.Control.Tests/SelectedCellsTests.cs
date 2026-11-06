using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Cell-level selection (SelectionUnit=CellOrRow): click/Shift/Ctrl ranges, Shift+arrow
/// extension, item-based ranges surviving refresh, bounding-box TSV copy.
/// </summary>
[Collection("Clipboard")]
public class SelectedCellsTests
{
    private static (GridControl Grid, ObservableCollection<Employee> Source, Window Window) Create(
        GridSelectionUnit unit = GridSelectionUnit.CellOrRow)
    {
        var source = new ObservableCollection<Employee>(CreateEmployees(6));
        var grid = new GridControl { Width = 500, RowHeight = 24, HeaderHeight = 26, SelectionUnit = unit };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 120 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 120 });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 120 });
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = source;
        TestWindowHost.Pump();
        return (grid, source, window);
    }

    [StaFact]
    public void RowUnit_ByDefault_NoCellSelection()
    {
        var (grid, source, window) = Create(unit: GridSelectionUnit.Row);
        try
        {
            grid.SelectCellRange(new GridCellInfo(source[0], grid.Columns[0]), new GridCellInfo(source[0], grid.Columns[0]));
            // Programmatic selection is allowed even in Row mode (it just is not driven by input).
            Assert.Single(grid.SelectedCells);
            Assert.Empty(grid.SelectedItems); // row selection untouched
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MouseClick_SelectsSingleCell_NotRow()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.HandleCellMousePress(source[2], grid.Columns[1], isCtrl: false, isShift: false);
            Assert.Single(grid.SelectedCells);
            Assert.Equal(source[2], grid.SelectedCells[0].Item);
            Assert.Equal(grid.Columns[1], grid.SelectedCells[0].Column);
            Assert.Empty(grid.SelectedItems);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ShiftClick_SelectsRectangle()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.HandleCellMousePress(source[1], grid.Columns[0], isCtrl: false, isShift: false);
            grid.HandleCellMousePress(source[3], grid.Columns[2], isCtrl: false, isShift: true);
            Assert.Equal(3 * 3, grid.SelectedCells.Count); // rows 1..3 × columns 0..2
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CtrlClick_TogglesSingleCell()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.HandleCellMousePress(source[0], grid.Columns[0], isCtrl: false, isShift: false);
            grid.HandleCellMousePress(source[1], grid.Columns[1], isCtrl: true, isShift: false);
            Assert.Equal(2, grid.SelectedCells.Count);

            grid.HandleCellMousePress(source[1], grid.Columns[1], isCtrl: true, isShift: false);
            Assert.Single(grid.SelectedCells); // toggled off
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ShiftArrows_ExtendFromAnchor()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SetCurrentCellForTest(source[1], grid.Columns[1]);
            grid.HandleCellMousePress(source[1], grid.Columns[1], isCtrl: false, isShift: false);
            grid.MoveCurrentCell(1, 1); // plain move — no extend
            Assert.Single(grid.SelectedCells);

            grid.MoveCurrentCell(1, 0, extend: true); // Shift+Down-стрелка
            Assert.Equal(4, grid.SelectedCells.Count); // 2 rows × 2 cols
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SelectAll_InCellMode_SelectsEveryCell()
    {
        var (grid, _, window) = Create();
        try
        {
            grid.SelectAll();
            Assert.Equal(6 * 3, grid.SelectedCells.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Ranges_SurviveRefresh_AndPruneRemovedItems()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SelectCellRange(new GridCellInfo(source[1], grid.Columns[0]), new GridCellInfo(source[2], grid.Columns[1]));
            Assert.Equal(4, grid.SelectedCells.Count);

            // A view rebuild (sort) must not lose the item-based ranges. The rectangle is
            // item-based: after re-sorting it still covers both endpoints (and possibly
            // different rows in between), never fewer than the endpoint cells.
            var start = new GridCellInfo(source[1], grid.Columns[0]);
            var end = new GridCellInfo(source[2], grid.Columns[1]);
            grid.SortBy(grid.Columns[0], VCWpfControls.Grid.Data.GridSortDirection.Ascending);
            TestWindowHost.Pump();
            Assert.True(grid.IsCellSelected(start), "start cell lost after sort");
            Assert.True(grid.IsCellSelected(end), "end cell lost after sort");
            Assert.True(grid.SelectedCells.Count >= 2);

            // Removing a selected row from the source prunes its ranges.
            source.Remove(source[1]);
            source.Remove(source[2]);
            TestWindowHost.Pump();
            Assert.Empty(grid.SelectedCells);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Copy_BoundingBoxTsv_WithEmptyHoles()
    {
        var (grid, source, window) = Create();
        try
        {
            // Two disjoint ranges: (row1,col0) and (row2,col2); bounding box 2×3 with holes.
            grid.SelectCellRange(new GridCellInfo(source[1], grid.Columns[0]), new GridCellInfo(source[1], grid.Columns[0]));
            grid.AddCellRange(new GridCellInfo(source[2], grid.Columns[2]), new GridCellInfo(source[2], grid.Columns[2]));
            grid.CopyToClipboard();
            TestWindowHost.Pump();

            var text = Clipboard.GetText();
            var lines = text.Split('\n');
            Assert.Equal(3, lines.Length); // header + 2 rows
            var header = lines[0].TrimEnd('\r').Split('\t');
            Assert.Equal("Salary", header[2]); // bounding box ends at the Salary column
            var row1 = lines[1].TrimEnd('\r').Split('\t');
            var row2 = lines[2].TrimEnd('\r').Split('\t');
            Assert.Equal(source[1].Name, row1[0]);
            Assert.Equal(string.Empty, row1[1]); // hole
            Assert.Equal(string.Empty, row1[2]); // hole
            Assert.Equal(Convert.ToString(source[2].Salary, System.Globalization.CultureInfo.CurrentCulture), row2[2]);
            Assert.Equal(string.Empty, row2[0]); // hole
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SelectedSpans_MergeAndRespectBoundaries()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SelectCellRange(new GridCellInfo(source[0], grid.Columns[0]), new GridCellInfo(source[0], grid.Columns[0]));
            grid.AddCellRange(new GridCellInfo(source[0], grid.Columns[1]), new GridCellInfo(source[0], grid.Columns[2]));
            TestWindowHost.Pump();

            var spans = grid.GetSelectedCellSpans(0);
            Assert.Single(spans); // adjacent ranges merge into one span 0..2
            Assert.Equal((0, 2), spans[0]);
            Assert.Empty(grid.GetSelectedCellSpans(1));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RealMouseDown_InCellMode_SelectsCellNotRow()
    {
        var (grid, source, window) = Create();
        try
        {
            var row = (Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(2);
            Assert.NotNull(row);
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = row,
            };
            row.RaiseEvent(args);
            TestWindowHost.Pump();

            // The click went through the real mouse path (HandleRowMousePress wiring).
            Assert.Single(grid.SelectedCells);
            Assert.Equal(source[2], grid.SelectedCells[0].Item);
            Assert.Empty(grid.SelectedItems);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SwitchingToRowUnit_ClearsCells()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SelectCellRange(new GridCellInfo(source[0], grid.Columns[0]), new GridCellInfo(source[1], grid.Columns[1]));
            Assert.Equal(4, grid.SelectedCells.Count);

            grid.SelectionUnit = GridSelectionUnit.Row;
            Assert.Empty(grid.SelectedCells);
        }
        finally
        {
            window.Close();
        }
    }
}
