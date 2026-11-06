using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The From/To range mode of the column filter menu (numeric and date columns): applying a
/// range produces Between / &gt;= / &lt;= filter descriptions; string columns get no range
/// boxes; an existing range filter preselects the boxes.
/// </summary>
public class FilterRangeTests
{
    private static GridControl CreateGrid(out Window window)
    {
        var grid = new GridControl { Width = 500, RowHeight = 24, HeaderHeight = 26 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 140 });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 140 });
        grid.Columns.Add(new GridDateColumn { FieldName = "HireDate", Header = "Hired", Width = 140 });
        window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(5);
        TestWindowHost.Pump();
        return grid;
    }

    private static Grid.Primitives.GridColumnFilterMenu OpenMenu(GridControl grid, GridColumnBase column)
    {
        grid.OpenColumnFilterMenu(column);
        TestWindowHost.Pump();
        return grid.OpenFilterMenuForTest ?? throw new InvalidOperationException("filter menu not open");
    }

    [StaFact]
    public void NumericColumn_BothBounds_AppliesBetween()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = OpenMenu(grid, grid.Columns[1]);
            menu.ApplyRangeForTest("10000", "30000");
            TestWindowHost.Pump();

            var filter = Assert.Single(grid.FilterDescriptions);
            Assert.Equal(GridFilterOperator.Between, filter.Operator);
            Assert.Equal(10000m, filter.Value);
            Assert.Equal(30000m, filter.Value2);

            // The view actually shrank to the rows inside the range.
            var inRange = 0;
            foreach (var item in grid.DataEngine.View)
            {
                if (item is GridDataRow data && ((Employee)data.Item).Salary is >= 10000m and <= 30000m)
                {
                    inRange++;
                }
            }

            Assert.Equal(grid.DataEngine.View.Count, inRange);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NumericColumn_OneBound_AppliesComparison()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = OpenMenu(grid, grid.Columns[1]);
            menu.ApplyRangeForTest("25000", null);
            var filter = Assert.Single(grid.FilterDescriptions);
            Assert.Equal(GridFilterOperator.GreaterThanOrEqual, filter.Operator);
            Assert.Equal(25000m, filter.Value);

            var menu2 = OpenMenu(grid, grid.Columns[1]);
            menu2.ApplyRangeForTest(null, "20000");
            var filter2 = Assert.Single(grid.FilterDescriptions);
            Assert.Equal(GridFilterOperator.LessThanOrEqual, filter2.Operator);
            Assert.Equal(20000m, filter2.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DateColumn_RangeParsesCultureAware()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = OpenMenu(grid, grid.Columns[2]);
            var from = new DateTime(2018, 1, 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
            var to = new DateTime(2021, 1, 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
            menu.ApplyRangeForTest(from, to);

            var filter = Assert.Single(grid.FilterDescriptions);
            Assert.Equal(GridFilterOperator.Between, filter.Operator);
            Assert.Equal(new DateTime(2018, 1, 1), filter.Value);
            Assert.Equal(new DateTime(2021, 1, 1), filter.Value2);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ExistingRange_PreselectsBoxes()
    {
        var grid = CreateGrid(out var window);
        try
        {
            grid.FilterDescriptions.Add(new GridFilterDescription(
                "Salary", GridFilterOperator.Between, 12000m, 34000m));
            TestWindowHost.Pump();

            var menu = OpenMenu(grid, grid.Columns[1]);
            Assert.Equal("12000", menu.FromTextForTest);
            Assert.Equal("34000", menu.ToTextForTest);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void StringColumn_NoRangeBoxes_InvalidTextIgnored()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var menu = OpenMenu(grid, grid.Columns[0]);
            Assert.Null(menu.FromTextForTest);
            Assert.Null(menu.ToTextForTest);

            // Unparseable range text on a numeric column applies no range (falls back to the list).
            var numericMenu = OpenMenu(grid, grid.Columns[1]);
            numericMenu.ApplyRangeForTest("not-a-number", string.Empty);
            Assert.Empty(grid.FilterDescriptions);
        }
        finally
        {
            window.Close();
        }
    }
}
