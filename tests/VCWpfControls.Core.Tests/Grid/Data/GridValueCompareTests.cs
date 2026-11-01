using System.Globalization;
using VCWpfControls.Grid.Data;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Data;

public class GridValueCompareTests
{
    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(null, 5, -1)]
    [InlineData(5, null, 1)]
    [InlineData(1, 2, -1)]
    [InlineData(2, 1, 1)]
    [InlineData(3, 3, 0)]
    public void Compare_Integers_OrdersCorrectly(object? a, object? b, int expectedSign)
    {
        var result = GridValueCompare.Compare(a, b);
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void Compare_MixedNumericTypes_Widens()
    {
        Assert.Equal(0, Math.Sign(GridValueCompare.Compare(1, 1.0)));
        Assert.True(GridValueCompare.Compare(2, 1.5) > 0);
    }

    [Fact]
    public void Compare_Strings_UsesGivenCulture()
    {
        // In invariant culture 'i' > 'I'; a case-sensitive comparison must be stable.
        var result = GridValueCompare.Compare("a", "B", CultureInfo.InvariantCulture);
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_SameTypeEnums_ComparesByValue()
    {
        // DayOfWeek.Wednesday = 3, DayOfWeek.Friday = 5.
        Assert.True(GridValueCompare.Compare(DayOfWeek.Wednesday, DayOfWeek.Friday) < 0);
        Assert.True(GridValueCompare.Compare(DayOfWeek.Friday, DayOfWeek.Wednesday) > 0);
    }

    [Fact]
    public void Compare_Bool_FalseBeforeTrue()
    {
        Assert.True(GridValueCompare.Compare(true, false) > 0);
    }

    [Fact]
    public void Compare_UnrelatedTypes_FallsBackToInvariantString()
    {
        Assert.Equal(0, GridValueCompare.Compare(TimeSpan.FromDays(1), TimeSpan.FromDays(1)));
    }
}
