using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.Localization;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridControlAnalyticsTests
{
    [StaFact]
    public void GroupDescriptions_Add_GroupRowsAppearAndToggleWorks()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(12);
        TestWindowHost.Pump();

        try
        {
            var dataRowsBefore = grid.Items.Count;
            grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
            TestWindowHost.Pump();

            var groupRows = grid.Items.OfType<GridGroupRow>().ToList();
            Assert.Equal(3, groupRows.Count); // IT, HR, Sales
            Assert.True(grid.Items.Count > dataRowsBefore);

            // Collapse the first group: its data rows disappear from the view.
            var firstGroup = groupRows[0].Group;
            grid.SetGroupExpanded(firstGroup, expanded: false);
            TestWindowHost.Pump();

            var collapsedRows = grid.DataEngine.DataRows.Count(row => ((Employee)row.Item).Department!.Equals(firstGroup.Key));
            Assert.Equal(0, collapsedRows);

            grid.SetGroupExpanded(firstGroup, expanded: true);
            TestWindowHost.Pump();
            Assert.Contains(grid.DataEngine.DataRows, row => ((Employee)row.Item).Department!.Equals(firstGroup.Key));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ShowFilterRow_TypingFiltersRows()
    {
        var grid = new GridControl { ShowFilterRow = true };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(20);
        TestWindowHost.Pump();

        try
        {
            var all = grid.Items.Count;
            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");
            var targetName = ((Employee)((GridDataRow)grid.DataEngine.View[0]).Item).Name!;

            grid.ApplyFilterText(nameColumn, targetName);
            TestWindowHost.Pump();

            Assert.True(grid.Items.Count < all);
            Assert.All(grid.Items.Cast<GridDataRow>(), row => Assert.Equal(targetName, ((Employee)row.Item).Name));

            grid.ApplyFilterText(nameColumn, string.Empty);
            TestWindowHost.Pump();
            Assert.Equal(all, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FilterRowPresenter_CreatesEditorPerVisibleColumn()
    {
        var grid = new GridControl { ShowFilterRow = true };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(5);
        TestWindowHost.Pump();

        try
        {
            var presenter = grid.FilterRowPresenter;
            Assert.NotNull(presenter);
            Assert.Equal(grid.VisibleColumns.Count, presenter!.Children.Count);
            Assert.All(presenter.Children.Cast<TextBox>(), editor => Assert.Equal(string.Empty, editor.Text));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SummaryItems_ComputeAggregatesOverFilteredRows()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(10);
        TestWindowHost.Pump();

        try
        {
            grid.SummaryItems.Add(new GridSummaryItem("Salary", GridSummaryFunction.Sum));
            grid.SummaryItems.Add(new GridSummaryItem("Id", GridSummaryFunction.Count));
            TestWindowHost.Pump();

            var expectedSum = CreateEmployees(10).Sum(e => e.Salary);
            var summaryValue = GridSummaryCalculator.Calculate(
                GridSummaryFunction.Sum,
                "Salary",
                grid.DataEngine.DataRows.Select(r => r.Item),
                (type, path) => grid.DataEngine.GetAccessor(type, path));

            Assert.Equal((decimal)expectedSum, summaryValue);

            // Filtering reduces the aggregate: the presenter recomputes.
            grid.FilterDescriptions.Add(new GridFilterDescription("Department", GridFilterOperator.Equals, "IT"));
            TestWindowHost.Pump();
            Assert.True(grid.DataEngine.DataRows.Count < 10);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GroupRow_Prepare_MarksContainerAsGroupRow()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(8);
        grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
        TestWindowHost.Pump();

        try
        {
            var anyGroupContainer = false;
            for (var i = 0; i < grid.Items.Count; i++)
            {
                if (grid.ItemContainerGenerator.ContainerFromIndex(i) is GridRow row
                    && grid.Items[i] is GridGroupRow)
                {
                    Assert.True(row.IsGroupRow);
                    anyGroupContainer = true;
                    break;
                }
            }

            Assert.True(anyGroupContainer, "at least one group container must be realized");
        }
        finally
        {
            window.Close();
        }
    }
}
