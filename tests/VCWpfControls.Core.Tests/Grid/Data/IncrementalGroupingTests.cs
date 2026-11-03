using System.Collections.ObjectModel;
using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class IncrementalGroupingTests
{
    private sealed class Item
    {
        public string Asset { get; set; } = string.Empty;

        public int Qty { get; set; }
    }

    [Fact]
    public void GroupingActive_AddBuildsGroups_Incrementally()
    {
        // Typical app init: group descriptions first (empty source), data arrives later.
        var data = new ObservableCollection<Item>();
        var manager = new GridDataManager();
        manager.SetSource(data);
        manager.Groups = new[] { new GridGroupDescription("Asset") };
        manager.Refresh();

        Assert.Empty(manager.View);

        for (var i = 0; i < 5; i++)
        {
            data.Add(new Item { Asset = "Si", Qty = i });
            Assert.True(manager.View.OfType<GridGroupRow>().Any(),
                $"group row must exist after add #{i}");
        }

        data.Add(new Item { Asset = "GOLD", Qty = 1 });
        Assert.Equal(2, manager.View.OfType<GridGroupRow>().Count());
        Assert.Equal(1, manager.View.OfType<GridGroupRow>().Single(g => Equals(g.Group.Key, "GOLD")).Group.TotalItems);
        Assert.Equal(6, manager.View.Count(g => g is GridDataRow));
    }

    [Fact]
    public void GroupingActive_RemoveRebuildsCounters()
    {
        var data = new ObservableCollection<Item>
        {
            new() { Asset = "Si", Qty = 1 },
            new() { Asset = "Si", Qty = 2 },
            new() { Asset = "GOLD", Qty = 3 },
        };
        var manager = new GridDataManager();
        manager.SetSource(data);
        manager.Groups = new[] { new GridGroupDescription("Asset") };
        manager.Refresh();

        Assert.Equal(2, manager.View.OfType<GridGroupRow>().Count());

        data.RemoveAt(2); // remove GOLD
        Assert.Single(manager.View.OfType<GridGroupRow>());
        Assert.Equal(2, manager.View.Count(g => g is GridDataRow));

        data.RemoveAt(0);
        var si = manager.View.OfType<GridGroupRow>().Single();
        Assert.Equal(1, si.Group.TotalItems);
    }

    [Fact]
    public void WithoutGrouping_AddStaysIncremental()
    {
        var data = new ObservableCollection<Item> { new() { Asset = "Si" } };
        var manager = new GridDataManager();
        manager.SetSource(data);

        var resets = 0;
        manager.ViewReset += (_, _) => resets++;
        data.Add(new Item { Asset = "GOLD" });

        Assert.Equal(0, resets); // no full rebuild without grouping
        Assert.Equal(2, manager.View.Count);
    }
}
