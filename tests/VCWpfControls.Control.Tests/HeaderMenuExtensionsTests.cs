using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Conditions;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Header menu extensions: the "Filter..." entry opens the condition builder preseeded
/// with the clicked column, "Filter by values..." opens the value-check list, the
/// column chooser dialog toggles visibility and reorders, and ResetLayout restores the
/// captured order/widths/visibility/frozen count.
/// </summary>
public class HeaderMenuExtensionsTests
{
    private static GridControl CreateGrid(out Window window)
    {
        var grid = new GridControl { Width = 500, RowHeight = 24, HeaderHeight = 26 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 150 });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 120 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 150 });
        window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();
        TestWindowHost.Pump(); // let the dispatcher-priority snapshot capture run
        return grid;
    }

    private static ContextMenu BuildMenu(GridControl grid, GridColumnBase column)
        => grid.BuildColumnHeaderMenu(column);

    private static MenuItem FindItem(ContextMenu menu, string header)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            if (string.Equals(item.Header as string, header, StringComparison.Ordinal))
            {
                return item;
            }

            foreach (var child in item.Items.OfType<MenuItem>())
            {
                if (string.Equals(child.Header as string, header, StringComparison.Ordinal))
                {
                    return child;
                }
            }
        }

        throw new InvalidOperationException($"menu item '{header}' not found");
    }

    private static void Click(MenuItem item)
        => item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));

    [StaFact]
    public void FilterEntry_OpensConditionDialog_PreseededWithColumnField()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = BuildMenu(grid, grid.Columns[1]); // Salary
            var filter = FindItem(menu, GridStrings.FilterEllipsis);
            Assert.True(filter.IsEnabled); // CanUserFilter defaults to true

            // The old duplicate text ("Filter......") entry is gone.
            Assert.DoesNotContain(menu.Items.OfType<MenuItem>(),
                m => m.Header as string == GridStrings.FilterEllipsis + "...");

            grid.ShowConditionDialogModally = false; // the entry opens the modal dialog
            Click(filter);
            TestWindowHost.Pump();
            var dialog = grid.ConditionDialogForTest;
            Assert.NotNull(dialog);

            // The builder starts with ONE condition on the clicked column's field.
            var builder = grid.ConditionBuilderForTest;
            Assert.NotNull(builder);
            var group = Assert.IsType<ConditionGroup>(builder!.Condition);
            var expression = Assert.IsType<ConditionExpression>(Assert.Single(group.Children));
            Assert.Equal("Salary", expression.FieldName);

            dialog!.Close();
            TestWindowHost.Pump();

            // A non-filterable column disables the entry.
            grid.Columns[1].CanUserFilter = false;
            var menu2 = BuildMenu(grid, grid.Columns[1]);
            Assert.False(FindItem(menu2, GridStrings.FilterEllipsis).IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FilterByValuesEntry_OpensValueListMenu()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = BuildMenu(grid, grid.Columns[0]);
            var values = FindItem(menu, GridStrings.FilterByValues);
            Assert.True(values.IsEnabled);

            Click(values);
            TestWindowHost.Pump();
            Assert.NotNull(grid.OpenFilterMenuForTest);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ResetLayout_RestoresOrderWidthsVisibilityFrozen()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var name = grid.Columns[0];
            var salary = grid.Columns[1];
            var dept = grid.Columns[2];

            // Mutate everything the snapshot holds.
            name.Width = new GridColumnWidth(300, GridUnitType.Pixel);
            dept.Visible = false;
            grid.Columns.RemoveAt(1); // move Salary first
            grid.Columns.Insert(0, salary);
            grid.FrozenColumnCount = 1;
            TestWindowHost.Pump();

            grid.ResetLayout();
            TestWindowHost.Pump();

            Assert.Equal(150, name.Width.Value);
            Assert.True(dept.Visible);
            Assert.Equal(0, grid.FrozenColumnCount);
            Assert.Same(name, grid.Columns[0]);
            Assert.Same(salary, grid.Columns[1]);
            Assert.Same(dept, grid.Columns[2]);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ResetLayout_DisabledBeforeSnapshot()
    {
        var grid = new GridControl();
        var menu = BuildMenu(grid, new GridTextColumn { FieldName = "Name" });
        // No items/no layout captured yet: the entry exists but disabled (no crash).
        Assert.False(FindItem(menu, GridStrings.ResetLayout).IsEnabled);
    }

    [StaFact]
    public void ColumnChooser_TogglesVisibilityFromDialog()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = BuildMenu(grid, grid.Columns[0]);
            var chooserEntry = FindItem(menu, GridStrings.ColumnChooserTitle + "...");
            Click(chooserEntry);
            TestWindowHost.Pump();

            var dialog = grid.OpenColumnChooserForTest();
            var checkbox = dialog.GetColumnCheckboxes().Single(cb => ReferenceEquals(cb.Tag, grid.Columns[2]));
            checkbox.IsChecked = false;
            TestWindowHost.Pump();
            Assert.False(grid.Columns[2].Visible);

            checkbox.IsChecked = true;
            TestWindowHost.Pump();
            Assert.True(grid.Columns[2].Visible);
            dialog.Close();
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ColumnChooser_ReordersWithButtons()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var name = grid.Columns[0];
            var salary = grid.Columns[1];
            var dept = grid.Columns[2];

            var menu = BuildMenu(grid, grid.Columns[0]);
            Click(FindItem(menu, GridStrings.ColumnChooserTitle + "..."));
            TestWindowHost.Pump();

            var dialog = grid.OpenColumnChooserForTest();
            dialog.SelectColumn(grid.Columns[2]);
            dialog.MoveSelectedForTest(1); // Dept is last — no-op
            Assert.Same(dept, grid.Columns[2]);

            dialog.SelectColumn(grid.Columns[0]); // Name
            dialog.MoveSelectedForTest(1); // Name down: Salary, Name, Dept
            Assert.Same(salary, grid.Columns[0]);
            Assert.Same(name, grid.Columns[1]);
            dialog.Close();
        }
        finally
        {
            window.Close();
        }
    }
}

