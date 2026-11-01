using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridControlRenderingTests
{
    [StaFact]
    public void ItemsSource_AutoGeneratesColumns_AndBuildsView()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);

        try
        {
            Assert.Empty(grid.Columns);
            grid.ItemsSource = CreateEmployees();
            TestWindowHost.Pump();

            Assert.True(grid.Columns.Count >= 5, $"expected auto columns, got {grid.Columns.Count}");
            Assert.Equal(24, grid.Items.Count);
            Assert.Equal(grid.Columns.Count(c => c.Visible), grid.VisibleColumns.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Template_ProducesHeadersAndRealizedRowsWithCells()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            // Headers: one per visible column.
            var headers = grid.HeadersPresenter;
            Assert.NotNull(headers);
            Assert.Equal(grid.VisibleColumns.Count, headers.Children.Count);

            // At least one realized row container with populated cells.
            var realizedRow = FindRealizedRow(grid);
            Assert.NotNull(realizedRow);
            Assert.NotNull(realizedRow.CellsPanel);
            Assert.Equal(grid.VisibleColumns.Count + 1, realizedRow.CellsPanel.Children.Count); // + focus visual

            // The first cell element is bound to the row item.
            var firstCell = realizedRow.CellsPanel.Children.OfType<TextBlock>().FirstOrDefault();
            Assert.NotNull(firstCell);
            Assert.NotNull(firstCell.Text);
            Assert.NotEqual(string.Empty, firstCell.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Layout_ComputesRealSlotsAndCellSizes()
    {
        var grid = new GridControl
        {
            RowHeight = 26,
        };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = new GridColumnWidth(2, GridUnitType.Star) });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 120 });
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            Assert.True(grid.ActualWidth > 0);
            Assert.True(grid.Layout.TotalWidth > 0);

            // Star column takes the viewport minus the absolute column.
            var nameSlot = grid.Layout.Slots[0];
            Assert.Equal(grid.ViewportWidth - 120, nameSlot.Width, 0);

            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.True(row.ActualHeight > 0);
            var cell = row.CellsPanel!.Children.OfType<TextBlock>().First();
            Assert.True(cell.ActualHeight > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SortBy_UpdatesEngineViewAndColumnState()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            grid.SortBy("Name", GridSortDirection.Ascending);
            TestWindowHost.Pump();

            var names = grid.DataEngine.View.OfType<GridDataRow>().Select(r => (string)((Employee)r.Item).Name!).ToList();
            Assert.Equal(names.OrderBy(n => n, StringComparer.CurrentCulture), names);

            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");
            Assert.Equal(GridSortDirection.Ascending, nameColumn.SortDirection);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Selection_ApiUpdatesRowContainers()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            var first = ((GridDataRow)grid.DataEngine.View[0]).Item;

            grid.SelectItem(first);
            TestWindowHost.Pump();

            Assert.Single(grid.SelectedItems.Cast<object>());
            Assert.Equal(first, grid.SelectedItem);
            var row = FindRealizedRow(grid);
            Assert.NotNull(row);
            Assert.True(row.IsSelected);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Editing_CommitPushesValueIntoItem()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            var first = (Employee)((GridDataRow)grid.DataEngine.View[0]).Item;
            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");

            Assert.True(grid.BeginEdit(first, nameColumn));
            Assert.True(grid.IsEditing);

            var row = FindRealizedRow(grid);
            Assert.NotNull(row);
            var editor = row.CellsPanel!.Children.OfType<TextBox>().FirstOrDefault();
            Assert.NotNull(editor);
            editor.Text = "Edited Name";
            Assert.True(grid.CommitEdit());
            Assert.False(grid.IsEditing);
            Assert.Equal("Edited Name", first.Name);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Editing_CancelRestoresOldValue()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            var first = (Employee)((GridDataRow)grid.DataEngine.View[0]).Item;
            var original = first.Name;
            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");

            grid.BeginEdit(first, nameColumn);
            var row = FindRealizedRow(grid);
            var editor = row!.CellsPanel!.Children.OfType<TextBox>().First();
            editor.Text = "Should Not Persist";
            grid.CancelEdit();

            Assert.False(grid.IsEditing);
            Assert.Equal(original, first.Name);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Editing_InvalidNumericStaysInEditMode()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            var first = (Employee)((GridDataRow)grid.DataEngine.View[0]).Item;
            var original = first.Id;
            var idColumn = grid.Columns.First(c => c.FieldName == "Id");

            grid.BeginEdit(first, idColumn);
            var row = FindRealizedRow(grid);
            var editor = row!.CellsPanel!.Children.OfType<TextBox>().First();
            editor.Text = "not a number";
            Assert.False(grid.CommitEdit());
            Assert.True(grid.IsEditing, "invalid value must keep the cell in edit mode");

            grid.CancelEdit();
            Assert.Equal(original, first.Id);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FrozenColumnCount_LayoutKeepsFrozenSlotsFirst()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees();
        TestWindowHost.Pump();

        try
        {
            grid.FrozenColumnCount = 2;
            TestWindowHost.Pump();

            Assert.True(grid.Layout.Slots[0].IsFrozen);
            Assert.True(grid.Layout.Slots[1].IsFrozen);
            Assert.False(grid.Layout.Slots[2].IsFrozen);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ObservableCollection_Add_TriggersRowSynchronization()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = new System.Collections.ObjectModel.ObservableCollection<Employee>(CreateEmployees());
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            Assert.Equal(24, grid.Items.Count);
            data.Add(new Employee { Id = 999, Name = "New Person" });
            TestWindowHost.Pump();
            Assert.Equal(25, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    private static GridRow? FindRealizedRow(GridControl grid)
    {
        for (var i = 0; i < grid.Items.Count; i++)
        {
            if (grid.ItemContainerGenerator.ContainerFromIndex(i) is GridRow row && row.CellsPanel is not null)
            {
                return row;
            }
        }

        return null;
    }
}
