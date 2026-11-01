using System.Windows;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Primitives;
using Xunit;

namespace VCWpfControls.Core.Tests.Grid.Primitives;

public class GridLayoutControllerTests
{
    private static List<GridColumnBase> Columns(params GridColumnWidth[] widths)
    {
        var result = new List<GridColumnBase>();
        foreach (var width in widths)
        {
            var column = new GridTextColumn { Width = width };
            result.Add(column);
        }

        return result;
    }

    [Fact]
    public void Compute_StarsFillViewport()
    {
        var columns = Columns(100, new GridColumnWidth(1, GridUnitType.Star), new GridColumnWidth(3, GridUnitType.Star));
        var controller = new GridLayoutController { ViewportWidth = 500 };

        controller.Compute(columns);

        Assert.Equal(100d, controller.Slots[0].Width);
        Assert.Equal(100d, controller.Slots[1].Width); // 400 remaining / 4 weight
        Assert.Equal(300d, controller.Slots[2].Width);
        Assert.Equal(500d, controller.TotalWidth, 2);
    }

    [Fact]
    public void Compute_AbsoluteColumns_ExceedingViewportProduceExtent()
    {
        var columns = Columns(300, 400);
        var controller = new GridLayoutController { ViewportWidth = 500 };

        controller.Compute(columns);

        Assert.Equal(700d, controller.TotalWidth);
        Assert.Equal(400d, controller.Slots[1].X);
    }

    [Fact]
    public void Compute_AutoColumns_UseReportedWidth()
    {
        var columns = Columns(GridColumnWidth.Auto, 100);
        var controller = new GridLayoutController { ViewportWidth = 500 };

        controller.ReportAutoWidth(columns[0], 42);
        controller.Compute(columns);

        // Auto width + padding of 4.
        Assert.Equal(46d, controller.Slots[0].Width, 2);
    }

    [Fact]
    public void Compute_AutoWidths_GrowOnly()
    {
        var columns = Columns(GridColumnWidth.Auto);
        var controller = new GridLayoutController { ViewportWidth = 500 };

        controller.ReportAutoWidth(columns[0], 100);
        controller.ReportAutoWidth(columns[0], 50);
        controller.Compute(columns);

        Assert.Equal(104d, controller.Slots[0].Width, 2);
    }

    [Fact]
    public void Compute_MinMaxWidths_Applied()
    {
        var columns = Columns(GridColumnWidth.Auto);
        columns[0].MinWidth = 50;
        columns[0].MaxWidth = 60;
        var controller = new GridLayoutController { ViewportWidth = 500 };

        controller.ReportAutoWidth(columns[0], 1000);
        controller.Compute(columns);
        Assert.Equal(60d, controller.Slots[0].Width);

        controller = new GridLayoutController { ViewportWidth = 500 };
        var fresh = Columns(GridColumnWidth.Auto);
        fresh[0].MinWidth = 50;
        controller.Compute(fresh); // no report -> clamp up to min
        Assert.Equal(50d, controller.Slots[0].Width);
    }

    [Fact]
    public void Compute_FrozenColumns_ComeFirstAndReportFrozenWidth()
    {
        var columns = Columns(50, 60, 70);
        var controller = new GridLayoutController { ViewportWidth = 500, FrozenColumnCount = 2 };

        controller.Compute(columns);

        Assert.True(controller.Slots[0].IsFrozen);
        Assert.True(controller.Slots[1].IsFrozen);
        Assert.False(controller.Slots[2].IsFrozen);
        Assert.Equal(110d, controller.FrozenWidth);
        Assert.Equal(180d, controller.TotalWidth);
    }

    [Fact]
    public void Compute_WidthChange_IsReported()
    {
        var columns = Columns(100);
        var controller = new GridLayoutController { ViewportWidth = 500 };

        Assert.True(controller.Compute(columns));  // from default 0 to 100
        Assert.False(controller.Compute(columns)); // unchanged
        columns[0].Width = 200;
        Assert.True(controller.Compute(columns));
    }

    [Fact]
    public void FindSlotAt_ReturnsContainingSlot()
    {
        var columns = Columns(100, 100, 100);
        var controller = new GridLayoutController { ViewportWidth = 500 };
        controller.Compute(columns);

        Assert.Same(controller.Slots[1], controller.FindSlotAt(150));
        Assert.Same(controller.Slots[2], controller.FindSlotAt(299.9));
    }
}
