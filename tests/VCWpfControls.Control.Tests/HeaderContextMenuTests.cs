using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class HeaderContextMenuTests
{
    private static GridControl CreateGrid(Window window)
    {
        var grid = new GridControl();
        window.Content = grid;
        grid.ItemsSource = CreateEmployees(6);
        TestWindowHost.Pump();
        return grid;
    }

    private static void Click(MenuItem item)
        => item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));

    private static MenuItem? FindItem(ContextMenu menu, string header)
        => menu.Items.OfType<MenuItem>().FirstOrDefault(m => Convert.ToString(m.Header, System.Globalization.CultureInfo.CurrentCulture) == header);

    [StaFact]
    public void Menu_ContainsExpectedItems_WithSortState()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            var column = grid.Columns.First(c => c.FieldName == "Name");
            grid.SortBy(column, GridSortDirection.Ascending);
            TestWindowHost.Pump();

            var menu = grid.BuildColumnHeaderMenu(column);
            var headers = menu.Items.OfType<MenuItem>().Select(m => Convert.ToString(m.Header, System.Globalization.CultureInfo.CurrentCulture)).ToList();

            Assert.Contains(GridStrings.SortAscending, headers);
            Assert.Contains(GridStrings.SortDescending, headers);
            Assert.Contains(GridStrings.BestFit, headers);
            Assert.Contains(GridStrings.BestFitAll, headers);
            Assert.Contains(GridStrings.GroupByColumn, headers);
            Assert.Contains(GridStrings.FreezeColumn, headers);
            Assert.Contains(GridStrings.HideColumn, headers);
            Assert.Contains(GridStrings.ColumnsHeader, headers);

            // Sort state is reflected with check marks.
            Assert.True(FindItem(menu, GridStrings.SortAscending)!.IsChecked);
            Assert.False(FindItem(menu, GridStrings.SortDescending)!.IsChecked);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BestFitItem_FitsColumn()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            var column = grid.Columns.First(c => c.FieldName == "Department");

            var menu = grid.BuildColumnHeaderMenu(column);
            Click(FindItem(menu, GridStrings.BestFit)!);
            TestWindowHost.Pump();

            Assert.True(column.Width.IsAbsolute);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupItem_TogglesGrouping()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            var column = grid.Columns.First(c => c.FieldName == "Department");

            var menu = grid.BuildColumnHeaderMenu(column);
            Assert.False(FindItem(menu, GridStrings.GroupByColumn)!.IsChecked);

            Click(FindItem(menu, GridStrings.GroupByColumn)!);
            TestWindowHost.Pump();
            Assert.Single(grid.GroupDescriptions);
            Assert.True(grid.Items.OfType<GridGroupRow>().Any());

            // Second click removes the grouping.
            menu = grid.BuildColumnHeaderMenu(column);
            Assert.True(FindItem(menu, GridStrings.GroupByColumn)!.IsChecked);
            Click(FindItem(menu, GridStrings.GroupByColumn)!);
            TestWindowHost.Pump();
            Assert.Empty(grid.GroupDescriptions);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FreezeAndHideItems_Work()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            var second = grid.Columns.Skip(1).First();

            var menu = grid.BuildColumnHeaderMenu(second);
            Click(FindItem(menu, GridStrings.FreezeColumn)!);
            TestWindowHost.Pump();
            Assert.Equal(2, grid.FrozenColumnCount);

            Click(FindItem(menu, GridStrings.HideColumn)!);
            TestWindowHost.Pump();
            Assert.False(second.Visible);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ColumnChooser_SubmenuTogglesVisibility()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            var target = grid.Columns.First(c => c.FieldName == "Salary");

            var menu = grid.BuildColumnHeaderMenu(grid.Columns.First());
            var chooser = FindItem(menu, GridStrings.ColumnsHeader)!;
            var entries = chooser.Items.OfType<MenuItem>().ToList();
            // The dialog entry leads the submenu; column toggles follow the separator.
            Assert.Equal(GridStrings.ColumnChooserTitle + "...", entries[0].Header);
            var columnEntries = entries.Skip(1).ToList();
            Assert.Equal(grid.Columns.Count, columnEntries.Count);

            var salaryEntry = columnEntries.First(m => Convert.ToString(m.Header, System.Globalization.CultureInfo.CurrentCulture) == "Salary");
            Assert.True(salaryEntry.IsChecked);
            Click(salaryEntry);
            TestWindowHost.Pump();
            Assert.False(target.Visible);

            // Toggling back shows it again.
            Click(salaryEntry);
            TestWindowHost.Pump();
            Assert.True(target.Visible);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Hide_DisabledForLastVisibleColumn()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var grid = CreateGrid(window);
            foreach (var column in grid.Columns.Skip(1).ToArray())
            {
                column.Visible = false;
            }

            TestWindowHost.Pump();
            var menu = grid.BuildColumnHeaderMenu(grid.Columns[0]);
            Assert.False(FindItem(menu, GridStrings.HideColumn)!.IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }
}
