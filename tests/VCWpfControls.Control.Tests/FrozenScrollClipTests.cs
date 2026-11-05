using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression tests for the frozen-zone clip: with FrozenColumnCount &gt; 0 and horizontal
/// scrolling, scrollable cells/headers must not render inside the frozen columns' territory.
/// Before the fix they slid under the frozen cells and their text drew on top of them.
/// </summary>
public class FrozenScrollClipTests
{
    private sealed class Row
    {
        public string Name { get; set; } = "AB";

        public string Department { get; set; } = "SCROLLSCROLLSCROLLSCROLLSCROLL";
    }

    [StaFact]
    public void ScrolledCells_DoNotBleedIntoFrozenColumns()
    {
        using var host = CreateScrolledHost(out var grid);
        var row = (Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
        var y = BandY(grid, row);

        // "AB" ends around x=25; the frozen column spans up to ~97 in grid coordinates.
        // Any dark pixel in [30..96] is scrolled content bleeding under the frozen column.
        var bleed = DarkColumns(grid, y, 30, 96);
        Assert.True(bleed.Count == 0, $"scrolled content bled into the frozen zone at [{string.Join(",", bleed)}]");

        // The scrollable text itself is still visible right of the frozen boundary.
        var visible = DarkColumns(grid, y, 98, 180);
        Assert.NotEmpty(visible);
    }

    [StaFact]
    public void ScrolledHeaders_DoNotBleedIntoFrozenColumns()
    {
        using var host = CreateScrolledHost(out var grid);
        var presenter = (Grid.Primitives.GridColumnHeadersPresenter)grid.Template.FindName("PART_ColumnHeadersPresenter", grid)!;
        var y = BandY(grid, presenter);

        // The "Name" header ends around x=45; the frozen column spans up to ~97, but its
        // trailing filter/sort affordance sits near x~92 — scan up to 84 to skip it.
        var bleed = DarkColumns(grid, y, 50, 84);
        Assert.True(bleed.Count == 0, $"scrolled headers bled into the frozen zone at [{string.Join(",", bleed)}]");
    }

    private sealed class Host : IDisposable
    {
        public Window Window { get; set; } = null!;

        public void Dispose() => Window.Close();
    }

    private static Host CreateScrolledHost(out GridControl grid)
    {
        grid = new GridControl { Width = 200, RowHeight = 24, HeaderHeight = 26, FrozenColumnCount = 1 };
        var host = new Host { Window = TestWindowHost.Show(grid) };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 90 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 150 });
        grid.ItemsSource = new[] { new Row() };
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();
        grid.SetHorizontalOffsetForTest(30);
        TestWindowHost.Pump();
        Assert.True(grid.HorizontalOffset > 0, "test requires actual horizontal scrolling");
        return host;
    }

    private static int BandY(GridControl grid, FrameworkElement band)
        => (int)Math.Floor(band.TranslatePoint(new Point(0, 0), grid).Y) + 12;

    /// <summary>Renders the grid offscreen at 96 DPI (VisualBrush capture — see ColumnSeparatorTests) and returns the dark (text) pixel columns in [from..to].</summary>
    private static List<int> DarkColumns(GridControl grid, int y, int from, int to)
    {
        var width = (int)Math.Ceiling(grid.ActualWidth);
        var height = (int)Math.Ceiling(grid.ActualHeight);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, width, height));
        }

        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);

        var dark = new List<int>();
        for (var px = from; px <= Math.Min(to, width - 1); px++)
        {
            var i = (y * width + px) * 4;
            if ((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3 < 110)
            {
                dark.Add(px);
            }
        }

        return dark;
    }
}
