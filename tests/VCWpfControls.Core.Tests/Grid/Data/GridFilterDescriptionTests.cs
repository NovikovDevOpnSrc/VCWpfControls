using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class GridFilterDescriptionTests
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Born { get; set; }
        public string? Nickname { get; set; }
    }

    private static readonly Item[] Items =
    {
        new() { Name = "Alice", Age = 30, Born = new DateTime(1990, 5, 1), Nickname = "ali" },
        new() { Name = "Bob", Age = 40, Born = new DateTime(1980, 1, 15), Nickname = null },
        new() { Name = "Carol", Age = 25, Born = new DateTime(1995, 12, 30), Nickname = "caro" },
    };

    private static PropertyAccessor Accessor(string path)
        => PropertyAccessor.GetOrCreate(typeof(Item), path);

    private static List<object> Apply(string path, GridFilterOperator op, object? value, object? value2 = null)
    {
        var filter = new GridFilterDescription(path, op, value, value2);
        var accessor = Accessor(path);
        return Items.Where(item => filter.Matches(item, accessor)).Cast<object>().ToList();
    }

    private static List<string> NamesOf(IEnumerable<object> rows)
        => rows.Select(r => ((Item)r).Name).ToList();

    [Fact]
    public void Contains_IsCaseInsensitiveByDefault()
    {
        var result = Apply("Name", GridFilterOperator.Contains, "ALI");
        Assert.Equal(new List<string> { "Alice" }, NamesOf(result));
    }

    [Fact]
    public void StartsWith_And_EndsWith()
    {
        Assert.Equal(new List<string> { "Carol" }, NamesOf(Apply("Name", GridFilterOperator.StartsWith, "ca")));
        Assert.Equal(new List<string> { "Bob" }, NamesOf(Apply("Name", GridFilterOperator.EndsWith, "ob")));
    }

    [Fact]
    public void NotContains_ExcludesMatches()
    {
        var result = Apply("Name", GridFilterOperator.NotContains, "a");
        Assert.Equal(new List<string> { "Bob" }, NamesOf(result));
    }

    [Fact]
    public void GreaterThan_WorksOnNumbers()
    {
        var result = Apply("Age", GridFilterOperator.GreaterThan, 26);
        Assert.Equal(new List<string> { "Alice", "Bob" }, NamesOf(result));
    }

    [Fact]
    public void Between_InclusiveRange()
    {
        var result = Apply("Age", GridFilterOperator.Between, 25, 30);
        Assert.Equal(new List<string> { "Alice", "Carol" }, NamesOf(result));
    }

    [Fact]
    public void NotBetween_ExcludesRange()
    {
        var result = Apply("Age", GridFilterOperator.NotBetween, 26, 45);
        Assert.Equal(new List<string> { "Carol" }, NamesOf(result));
    }

    [Fact]
    public void Between_WorksOnDates()
    {
        var result = Apply("Born", GridFilterOperator.Between, new DateTime(1985, 1, 1), new DateTime(1994, 1, 1));
        Assert.Equal(new List<string> { "Alice" }, NamesOf(result));
    }

    [Fact]
    public void In_MatchesListValues()
    {
        var result = Apply("Name", GridFilterOperator.In, new[] { "alice", "carol" });
        Assert.Equal(new List<string> { "Alice", "Carol" }, NamesOf(result));
    }

    [Fact]
    public void NotIn_ExcludesListValues()
    {
        var result = Apply("Name", GridFilterOperator.NotIn, new[] { "alice", "carol" });
        Assert.Equal(new List<string> { "Bob" }, NamesOf(result));
    }

    [Fact]
    public void IsNull_And_IsNotNull()
    {
        Assert.Equal(new List<string> { "Bob" }, NamesOf(Apply("Nickname", GridFilterOperator.IsNull, null)));
        Assert.Equal(new List<string> { "Alice", "Carol" }, NamesOf(Apply("Nickname", GridFilterOperator.IsNotNull, null)));
    }

    [Fact]
    public void Equals_OnNumbers()
    {
        var result = Apply("Age", GridFilterOperator.Equals, 40);
        Assert.Equal(new List<string> { "Bob" }, NamesOf(result));
    }

    [Fact]
    public void Custom_PredicateReceivesWholeItem()
    {
        var filter = new GridFilterDescription("Name", GridFilterOperator.Custom)
        {
            CustomPredicate = item => ((Item)item).Age > 28,
        };
        var accessor = Accessor("Name");
        var result = Items.Where(i => filter.Matches(i, accessor)).Select(i => i.Name).ToList();

        Assert.Equal(new List<string> { "Alice", "Bob" }, result);
    }
}
