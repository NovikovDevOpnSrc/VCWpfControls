using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridControlStage12Tests
{
    [StaFact]
    public void GroupByColumn_AddsGroupDescription_AndPanelShowsChip()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(10);
        TestWindowHost.Pump();

        try
        {
            var departmentColumn = grid.Columns.First(c => c.FieldName == "Department");
            grid.GroupByColumn(departmentColumn);
            TestWindowHost.Pump();

            Assert.Single(grid.GroupDescriptions);
            Assert.True(grid.Items.OfType<GridGroupRow>().Any());
            Assert.True(grid.GroupPanel!.HasChips);

            // Duplicate grouping by the same column is ignored.
            grid.GroupByColumn(departmentColumn);
            Assert.Single(grid.GroupDescriptions);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CanUserGroupColumns_False_BlocksGroupByColumn()
    {
        var grid = new GridControl { CanUserGroupColumns = false };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(5);
        TestWindowHost.Pump();

        try
        {
            grid.GroupByColumn(grid.Columns.First(c => c.FieldName == "Department"));
            Assert.Empty(grid.GroupDescriptions);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupDescriptionRemoved_ViewReturnsToFlat()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(8);
        grid.GroupByColumn(grid.Columns.First(c => c.FieldName == "Department"));
        TestWindowHost.Pump();

        try
        {
            grid.GroupDescriptions.RemoveAt(0);
            TestWindowHost.Pump();

            Assert.False(grid.Items.OfType<GridGroupRow>().Any());
            Assert.Equal(8, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FilterMenu_AppliesValueListFilter()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(12);
        TestWindowHost.Pump();

        try
        {
            var departmentColumn = grid.Columns.First(c => c.FieldName == "Department");
            var values = grid.DataEngine.GetDistinctValues("Department");
            Assert.Equal(3, values.Count);

            // Simulate the menu's OK action for a partial selection.
            var selected = new List<object?> { values[0] };
            grid.FilterDescriptions.Add(new GridFilterDescription("Department", GridFilterOperator.In, selected));
            TestWindowHost.Pump();

            Assert.True(grid.Items.Count < 12);
            Assert.All(
                grid.Items.Cast<GridDataRow>(),
                row => Assert.Equal(values[0], ((Employee)row.Item).Department));

            // The funnel indicator reflects the active filter.
            var header = grid.HeadersPresenter!.GetHeader(departmentColumn);
            Assert.NotNull(header);
            Assert.True(header!.IsFilterApplied);

            // Clearing the filter restores rows and the indicator.
            grid.FilterDescriptions.Clear();
            TestWindowHost.Pump();
            Assert.Equal(12, grid.Items.Count);
            Assert.False(grid.HeadersPresenter!.GetHeader(departmentColumn)!.IsFilterApplied);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void UseAsyncRefresh_SortingProducesSortedView()
    {
        var grid = new GridControl { UseAsyncRefresh = true };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(50);
        TestWindowHost.Pump();

        try
        {
            grid.SortBy("Salary", GridSortDirection.Descending);

            // Generous deadline: the pipeline is deterministic, but CI runners starve the
            // threadpool while STA test classes run in parallel (happy path: milliseconds).
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (DateTime.UtcNow < deadline && !IsSortedBySalaryDescending(grid))
            {
                TestWindowHost.Pump();
                System.Threading.Thread.Sleep(20);
            }

            Assert.True(IsSortedBySalaryDescending(grid),
                $"not sorted after deadline; pipeline error: {grid.LastAsyncRefreshError?.ToString() ?? "none"}");
        }
        finally
        {
            window.Close();
        }
    }

    private static bool IsSortedBySalaryDescending(GridControl grid)
    {
        decimal? previous = null;
        foreach (var row in grid.DataEngine.DataRows)
        {
            var salary = ((Employee)row.Item).Salary;
            if (previous is { } value && salary > value)
            {
                return false;
            }

            previous = salary;
        }

        return true;
    }

    [StaFact]
    public void ObservableCollectionAdd_GridViewUpdatesWithoutReset()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = new ObservableCollection<Employee>(CreateEmployees(10));
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            var countBefore = grid.Items.Count;
            var resets = 0;
            grid.DataEngine.ViewReset += (_, _) => resets++;

            data.Add(new Employee { Id = 999, Name = "New Row" });
            TestWindowHost.Pump();

            Assert.Equal(0, resets); // incremental path, no full rebuild
            Assert.Equal(countBefore + 1, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ObservableCollectionRemove_SelectionPruned()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = new ObservableCollection<Employee>(CreateEmployees(10));
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            var victim = data[3];
            grid.SelectItem(victim);
            TestWindowHost.Pump();
            Assert.Equal(victim, grid.SelectedItem);

            data.Remove(victim);
            TestWindowHost.Pump();

            Assert.DoesNotContain(victim, grid.SelectedItems.Cast<object>());
            Assert.Null(grid.SelectedItem);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ShowGroupPanel_False_HidesPanel()
    {
        var grid = new GridControl { ShowGroupPanel = false };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(5);
        TestWindowHost.Pump();

        try
        {
            Assert.NotNull(grid.GroupPanel);
            Assert.Equal(Visibility.Collapsed, grid.GroupPanel!.Visibility);
        }
        finally
        {
            window.Close();
        }
    }
}
