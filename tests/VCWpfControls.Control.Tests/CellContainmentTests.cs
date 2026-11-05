using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Column-frame consistency: the cells panel, the headers presenter and the separator lines
/// must all share ONE coordinate origin. The row template's PART_Cells once carried
/// Margin="8,0,8,0" while the headers presenter had none — every cell (and its text) sat 8px
/// right of its column header, so values visibly climbed into the neighboring column.
/// </summary>
public class CellContainmentTests
{
    private sealed class Row
    {
        public string Long { get; set; } = "OVERFLOWOVERFLOWOVERFLOWOVERFLOWOVERFLOW";

        public string Side { get; set; } = "SIDE";
    }

    [StaFact]
    public void CellsPanelOrigin_MatchesHeadersPresenter()
    {
        var grid = CreateGrid(out var window, out var row);
        try
        {
            var presenter = (Grid.Primitives.GridColumnHeadersPresenter)grid.Template.FindName("PART_ColumnHeadersPresenter", grid)!;
            var panel = (Grid.Primitives.GridCellsPanel)row.Template.FindName("PART_Cells", row)!;

            var headerOrigin = presenter.TranslatePoint(new Point(0, 0), grid).X;
            var cellsOrigin = panel.TranslatePoint(new Point(0, 0), grid).X;
            Assert.True(Math.Abs(headerOrigin - cellsOrigin) <= 0.5,
                $"cell columns shifted {cellsOrigin - headerOrigin:F1}px relative to headers (template margin asymmetry)");
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Every cell element carries a clip that ends at its own slot edge.</summary>
    [StaFact]
    public void CellElements_AreClippedToTheirSlots()
    {
        var grid = CreateGrid(out var window, out var row);
        try
        {
            var panel = (Grid.Primitives.GridCellsPanel)row.Template.FindName("PART_Cells", row)!;
            foreach (var column in grid.VisibleColumns)
            {
                var element = panel.GetCellElement(column);
                Assert.NotNull(element);
                var clip = element!.Clip as RectangleGeometry;
                Assert.NotNull(clip); // hard containment: nothing can render past the slot
                var slot = grid.Layout.GetSlot(column)!;
                Assert.True(clip.Rect.Width <= slot.Width + 0.5,
                    $"{column.FieldName}: clip width {clip.Rect.Width} exceeds slot {slot.Width}");
            }
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Defense in depth: no dark (text) pixel right of a separator line before the neighbor's own text.</summary>
    [StaFact]
    public void NoTextImmediatelyRightOfSeparatorLines()
    {
        var grid = CreateGrid(out var window, out var row);
        try
        {
            var (width, _, pixels) = RenderGrid(grid);
            var y = (int)Math.Floor(row.TranslatePoint(new Point(0, 0), grid).Y) + 12;

            bool IsRed(int x)
            {
                var i = (y * width + x) * 4;
                return pixels[i + 2] > 200 && pixels[i + 1] < 80 && pixels[i] < 80;
            }

            bool IsDarkNonRed(int x)
            {
                var i = (y * width + x) * 4;
                var b = pixels[i];
                var g = pixels[i + 1];
                var r = pixels[i + 2];
                if ((b + g + r) / 3 >= 110)
                {
                    return false;
                }

                return !(r > 90 && r > g + 30 && r > b + 30); // the line's own AA is reddish
            }

            var redLines = new List<int>();
            for (var x = 0; x < width; x++)
            {
                if (IsRed(x))
                {
                    redLines.Add(x);
                }
            }

            Assert.NotEmpty(redLines);

            // The neighbor's own text starts at +8 (default element margin); +1..+5 must be empty.
            foreach (var line in redLines)
            {
                for (var dx = 1; dx <= 5; dx++)
                {
                    var x = line + dx;
                    if (x < width)
                    {
                        Assert.False(IsDarkNonRed(x),
                            $"dark pixel at line+{dx} (x={x}): content in the separator margin zone");
                    }
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static GridControl CreateGrid(out Window window, out Grid.Primitives.GridRow row)
    {
        var grid = new GridControl { Width = 240, RowHeight = 24, HeaderHeight = 26 };
        window = TestWindowHost.Show(grid);

        var template = new DataTemplate();
        var root = new FrameworkElementFactory(typeof(StackPanel));
        root.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetValue(TextBlock.TextProperty, "OVERFLOWOVERFLOWOVERFLOWOVERFLOWOVERFLOW");
        text.SetValue(TextBlock.WidthProperty, 200d);
        root.AppendChild(text);
        template.VisualTree = root;

        grid.Columns.Add(new GridTemplateColumn { FieldName = "Long", Header = "L", Width = 80, CellTemplate = template });
        grid.Columns.Add(new GridTextColumn { FieldName = "Side", Header = "S", Width = 140 });
        grid.ItemsSource = new[] { new Row() };
        grid.ShowColumnSeparators = true;
        grid.ColumnSeparatorBrush = Brushes.Red;
        TestWindowHost.Pump();

        row = (Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
        return grid;
    }

    private static (int Width, int Height, byte[] Pixels) RenderGrid(GridControl grid)
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
        return (width, height, pixels);
    }
}
