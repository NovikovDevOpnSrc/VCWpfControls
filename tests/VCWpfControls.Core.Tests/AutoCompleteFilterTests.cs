using VCWpfControls.Editors;
using Xunit;

namespace VCWpfControls.Core.Tests;

/// <summary>UI-free matching of the AutoComplete editor.</summary>
public class AutoCompleteFilterTests
{
    private sealed class City
    {
        public City(string name, string country)
        {
            Name = name;
            Country = country;
        }

        public string Name { get; }

        public string Country { get; }
    }

    [Fact]
    public void Match_CaseInsensitiveContains_CapsAtMax()
    {
        var source = new[] { "Moscow", "München", "münster", "Minsk", "Malmö", "Milan", "Marseille", "Madrid", "Machala" };
        var result = AutoCompleteFilter.Match("m", source, minimumPrefixLength: 1, maxCount: 3, customMatcher: null, itemTextPath: null);
        Assert.Equal(3, result.Count); // capped
        Assert.All(result, item => Assert.StartsWith("M", (string)item, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Match_BelowMinimumPrefix_Empty()
    {
        var result = AutoCompleteFilter.Match("m", new[] { "Moscow" }, minimumPrefixLength: 2, maxCount: 5, customMatcher: null, itemTextPath: null);
        Assert.Empty(result);
    }

    [Fact]
    public void Match_ItemTextPath_UsePropertyForObjects()
    {
        var source = new object[] { new City("Moscow", "RU"), new City("Berlin", "DE") };
        var result = AutoCompleteFilter.Match("ber", source, 1, 5, customMatcher: null, itemTextPath: "Name");
        var berlin = Assert.Single(result);
        Assert.Equal("Berlin", ((City)berlin).Name);
    }

    [Fact]
    public void Match_CustomMatcher_OverrulesContains()
    {
        var source = new object[] { new City("Moscow", "RU"), new City("Paris", "FR") };
        var result = AutoCompleteFilter.Match("xx", source, 1, 5, c => ((City)c).Country == "FR", itemTextPath: null);
        var paris = Assert.Single(result);
        Assert.Equal("Paris", ((City)paris).Name);
    }

    [Fact]
    public void ItemText_StringAsIs_NullSafeFallback()
    {
        Assert.Equal("abc", AutoCompleteFilter.ItemText("abc", path: null));
        // Unresolved path falls back to ToString (documented behavior).
        Assert.Contains("City", AutoCompleteFilter.ItemText(new City("X", "Y"), "NoSuchProperty"));
        Assert.Contains("City", AutoCompleteFilter.ItemText(new City("X", "Y"), path: null));
    }

    [Fact]
    public void Match_NullItems_Skipped()
    {
        var source = new object?[] { null, "Moscow" };
        var result = AutoCompleteFilter.Match("mo", source!, 1, 5, customMatcher: null, itemTextPath: null);
        var moscow = Assert.Single(result);
        Assert.Equal("Moscow", moscow);
    }
}
