using System.Collections.ObjectModel;
using System.Collections.Specialized;
using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class GridDataManagerTests
{
    private enum Department { It, Hr, Sales }

    private sealed class Employee
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Department Department { get; set; }
    }

    private static List<Employee> Sample() => new()
    {
        new() { Name = "Carol", Age = 25, Department = Department.Hr },
        new() { Name = "Alice", Age = 30, Department = Department.It },
        new() { Name = "Bob", Age = 40, Department = Department.Sales },
        new() { Name = "Dave", Age = 35, Department = Department.It },
    };

    private static GridDataManager Create(List<Employee> data) => Create(data, out _);

    private static GridDataManager Create(List<Employee> data, out int resetCount)
    {
        var manager = new GridDataManager();
        var count = 0;
        manager.ViewReset += (_, _) => count++;
        manager.SetSource(data);
        resetCount = count;
        return manager;
    }

    [Fact]
    public void SetSource_BuildsFlatViewOfDataRows()
    {
        var manager = Create(Sample());

        Assert.Equal(4, manager.View.Count);
        Assert.All(manager.View, row => Assert.Equal(GridRowKind.Data, row.Kind));
    }

    [Fact]
    public void SetSource_Null_ClearsView()
    {
        var manager = Create(Sample());
        manager.SetSource(null);

        Assert.Empty(manager.View);
    }

    [Fact]
    public void Sort_SingleColumn_OrdersRows()
    {
        var manager = Create(Sample());
        manager.Sorts = new[] { new GridSortDescription("Name") };
        manager.Refresh();

        var names = manager.DataRows.Select(r => ((Employee)r.Item).Name).ToList();
        Assert.Equal(new List<string> { "Alice", "Bob", "Carol", "Dave" }, names);
    }

    [Fact]
    public void Sort_Descending_ReversesOrder()
    {
        var manager = Create(Sample());
        manager.Sorts = new[] { new GridSortDescription("Age", GridSortDirection.Descending) };
        manager.Refresh();

        var ages = manager.DataRows.Select(r => ((Employee)r.Item).Age).ToList();
        Assert.Equal(new List<int> { 40, 35, 30, 25 }, ages);
    }

    [Fact]
    public void Sort_MultipleColumns_IsStable()
    {
        var manager = Create(Sample());
        manager.Sorts = new[]
        {
            new GridSortDescription("Department"),
            new GridSortDescription("Age", GridSortDirection.Descending),
        };
        manager.Refresh();

        var names = manager.DataRows.Select(r => ((Employee)r.Item).Name).ToList();
        // It(Dave 35, Alice 30), Hr(Carol), Sales(Bob)
        Assert.Equal(new List<string> { "Dave", "Alice", "Carol", "Bob" }, names);
    }

    [Fact]
    public void Sort_NullValues_SortedFirst()
    {
        var data = new List<Employee>
        {
            new() { Name = "X", Age = 1 },
            new() { Name = null!, Age = 2 },
            new() { Name = "A", Age = 3 },
        };
        var manager = Create(data);
        manager.Sorts = new[] { new GridSortDescription("Name") };
        manager.Refresh();

        var names = manager.DataRows.Select(r => (string?)((Employee)r.Item).Name).ToList();
        Assert.Equal(new List<string?> { null, "A", "X" }, names);
    }

    [Fact]
    public void Filter_AndSort_Compose()
    {
        var manager = Create(Sample());
        manager.Sorts = new[] { new GridSortDescription("Age", GridSortDirection.Descending) };
        manager.Filters = new[] { new GridFilterDescription("Department", GridFilterOperator.Equals, Department.It) };
        manager.Refresh();

        var names = manager.DataRows.Select(r => ((Employee)r.Item).Name).ToList();
        Assert.Equal(new List<string> { "Dave", "Alice" }, names);
    }

    [Fact]
    public void ObservableCollection_Change_TriggersViewReset()
    {
        var data = new ObservableCollection<Employee>(Sample());
        var manager = Create(data.ToList());
        manager.SetSource(data);

        var resets = 0;
        manager.ViewReset += (_, _) => resets++;
        data.Add(new Employee { Name = "Eve", Age = 20 });

        Assert.Equal(1, resets);
        Assert.Equal(5, manager.View.Count);
    }

    [Fact]
    public void Grouping_ProducesInterleavedGroupAndDataRows()
    {
        var manager = Create(Sample());
        manager.Sorts = new[] { new GridSortDescription("Department") };
        manager.Groups = new[] { new GridGroupDescription("Department") };
        manager.Refresh();

        Assert.Equal(7, manager.View.Count);

        var groupRows = manager.View.OfType<GridGroupRow>().ToList();
        Assert.Equal(3, groupRows.Count);
        Assert.Equal(Department.It, groupRows[0].Group.Key);
        Assert.Equal(2, groupRows[0].Group.TotalItems);
        Assert.Equal(0, groupRows[0].Level);

        // It group row followed by its two data rows.
        Assert.Equal(GridRowKind.Data, manager.View[1].Kind);
        Assert.Equal(GridRowKind.Data, manager.View[2].Kind);
    }

    [Fact]
    public void Grouping_CollapsedGroup_HidesDataRows()
    {
        var manager = Create(Sample());
        manager.Sorts = new[] { new GridSortDescription("Department") };
        manager.Groups = new[] { new GridGroupDescription("Department") };
        manager.Refresh();

        var itGroup = manager.View.OfType<GridGroupRow>().First(g => g.Group.Key!.Equals(Department.It)).Group;
        manager.SetGroupExpanded(itGroup, expanded: false);

        var names = manager.DataRows.Select(r => ((Employee)r.Item).Name).ToList();
        Assert.DoesNotContain("Alice", names);
        Assert.DoesNotContain("Dave", names);

        // Re-expanding restores rows and state survives an unrelated refresh.
        manager.SetGroupExpanded(itGroup, expanded: true);
        manager.Refresh();
        Assert.Contains(manager.DataRows, r => ((Employee)r.Item).Name == "Alice");
    }

    [Fact]
    public void Grouping_TwoLevels_IndentsCorrectly()
    {
        var data = new List<Employee>
        {
            new() { Name = "A", Age = 30, Department = Department.It },
            new() { Name = "B", Age = 30, Department = Department.It },
            new() { Name = "C", Age = 25, Department = Department.It },
        };
        var manager = Create(data);
        manager.Sorts = new[] { new GridSortDescription("Age") };
        manager.Groups = new[] { new GridGroupDescription("Department"), new GridGroupDescription("Age") };
        manager.Refresh();

        var levels = manager.View.OfType<GridGroupRow>().Select(g => g.Level).ToList();
        Assert.Equal(new List<int> { 0, 1, 1 }, levels);
    }

    [Fact]
    public void GetCellValue_ReadsThroughAccessors()
    {
        var manager = Create(Sample());
        var first = manager.View[0];

        Assert.Equal("Carol", manager.GetCellValue(((GridDataRow)first).Item, "Name"));
    }

    [Fact]
    public void ViewReset_RaisedOnEveryRefresh()
    {
        var manager = Create(Sample(), out var initialResets);
        Assert.Equal(1, initialResets);

        var resets = 0;
        manager.ViewReset += (_, _) => resets++;
        manager.Refresh();
        manager.Refresh();

        Assert.Equal(2, resets);
    }
}
