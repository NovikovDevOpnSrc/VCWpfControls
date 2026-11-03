using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class GridSortStressTests
{
    private sealed class Row
    {
        public int Group { get; set; }

        public int Value { get; set; }

        public int InsertionOrder { get; set; }
    }

    [Fact]
    public void LargeSort_TwoLevels_IsStableOnTies()
    {
        const int count = 50_000;
        var random = new Random(7);
        var data = new List<Row>(count);
        for (var i = 0; i < count; i++)
        {
            data.Add(new Row
            {
                Group = random.Next(50),      // many ties on level 1
                Value = random.Next(20),      // many ties on level 2
                InsertionOrder = i,
            });
        }

        var manager = new GridDataManager();
        manager.SetSource(data);
        manager.Sorts = new[]
        {
            new GridSortDescription("Group"),
            new GridSortDescription("Value"),
        };
        manager.Refresh();

        var sorted = manager.DataRows.Select(r => (Row)r.Item).ToList();

        // Total order: Group asc, Value asc, original insertion order for full ties.
        for (var i = 1; i < sorted.Count; i++)
        {
            var previous = sorted[i - 1];
            var current = sorted[i];
            if (previous.Group == current.Group && previous.Value == current.Value)
            {
                Assert.True(previous.InsertionOrder < current.InsertionOrder,
                    $"tie broken at {i}: {previous.InsertionOrder} should precede {current.InsertionOrder}");
            }
            else
            {
                Assert.False(previous.Group > current.Group
                    || (previous.Group == current.Group && previous.Value > current.Value),
                    "ordering violated");
            }
        }
    }

    [Fact]
    public void LargeSort_DescendingWithNulls_OrdersCorrectly()
    {
        var data = new List<Row>();
        for (var i = 0; i < 10_000; i++)
        {
            data.Add(new Row { Group = i % 100, Value = i, InsertionOrder = i });
        }

        var manager = new GridDataManager();
        manager.SetSource(data);
        manager.Sorts = new[] { new GridSortDescription("Group", GridSortDirection.Descending) };
        manager.Refresh();

        var groups = manager.DataRows.Select(r => ((Row)r.Item).Group).ToList();
        Assert.Equal(groups.OrderByDescending(g => g), groups);
    }
}
