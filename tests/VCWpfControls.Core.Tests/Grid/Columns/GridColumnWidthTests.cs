using System.Windows;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Columns;

public class GridColumnWidthTests
{
    [Fact]
    public void Parse_SupportsAutoPixelAndStar()
    {
        Assert.Equal(GridColumnWidth.Auto, GridColumnWidth.Parse("Auto"));
        Assert.Equal(GridColumnWidth.Parse("AUTO"), GridColumnWidth.Parse("auto"));
        Assert.True(GridColumnWidth.Parse("120").IsAbsolute);
        Assert.Equal(120d, GridColumnWidth.Parse("120").Value);
        Assert.True(GridColumnWidth.Parse("2*").IsStar);
        Assert.Equal(2d, GridColumnWidth.Parse("2*").Value);
        Assert.True(GridColumnWidth.Parse("*").IsStar);
        Assert.Equal(1d, GridColumnWidth.Parse("*").Value);
    }

    [Fact]
    public void ImplicitConversion_FromNumbers_CreatesAbsolute()
    {
        GridColumnWidth fromDouble = 80;
        GridColumnWidth fromInt = 80;

        Assert.True(fromDouble.IsAbsolute);
        Assert.True(fromInt.IsAbsolute);
        Assert.Equal(80d, fromDouble.Value);
    }

    [Fact]
    public void Parse_RoundTripsThroughToString()
    {
        foreach (var text in new[] { "Auto", "120", "2*", "*" })
        {
            Assert.Equal(text, GridColumnWidth.Parse(text).ToString());
        }
    }

    [Fact]
    public void Ctor_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridColumnWidth(-1, GridUnitType.Pixel));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridColumnWidth(double.NaN, GridUnitType.Pixel));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridColumnWidth(double.PositiveInfinity, GridUnitType.Pixel));
    }
}
