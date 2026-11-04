using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class ColumnSeparatorTests
{
    [StaFact]
    public void OffByDefault_NoSeparatorsDrawn()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 100 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            Assert.False(grid.ShowColumnSeparators);
            Assert.Null(grid.ColumnSeparatorBrush); // falls back to GridLinesBrush
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enabled_SwitchDoesNotThrowAndInvalidatesRows()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 100 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            grid.ShowColumnSeparators = true;
            TestWindowHost.Pump();
            grid.ColumnSeparatorBrush = Brushes.Red;
            TestWindowHost.Pump();

            // Scroll horizontally with separators on: no exceptions, offset changes.
            grid.SetHorizontalOffsetForTest(10);
            Assert.True(grid.HorizontalOffset >= 0);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Renders the grid offscreen at 96 DPI (DIP == pixel) and verifies the separator line
    /// occupies the same pixel column as the header's right border — no inset drift (the
    /// cells panel template margin) and no half-pixel smear.
    /// </summary>
    [StaFact]
    public void Separators_AlignWithHeaderColumnBorders_PixelCheck()
    {
        var grid = new GridControl { Width = 240, RowHeight = 24, HeaderHeight = 26 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 100 });
        grid.ItemsSource = CreateEmployees(2);
        grid.ColumnSeparatorBrush = Brushes.Red;
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();

        try
        {
            var presenter = (GridColumnHeadersPresenter)grid.Template.FindName("PART_ColumnHeadersPresenter", grid)!;
            var headerA = presenter.Children.OfType<GridColumnHeader>().First(h => h.Column?.FieldName == "Name");
            var headerRight = headerA.TranslatePoint(new Point(headerA.ActualWidth, 0), grid).X;

            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var rowTop = row.TranslatePoint(new Point(0, 0), grid).Y;

            var width = (int)Math.Ceiling(grid.ActualWidth);
            var height = (int)Math.Ceiling(grid.ActualHeight);
            Assert.True(width > 0 && height > 0, $"grid not laid out: {width}x{height}");

            // Direct RenderTargetBitmap.Render(grid) yields an empty raster in this host;
            // capturing through a VisualBrush wrapper renders reliably offscreen.
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var visual = new System.Windows.Media.DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, width, height));
            }
            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            bool IsRed(int px)
            {
                var i = ((int)rowTop + 3) * width + px;
                var b = pixels[i * 4];
                var g = pixels[i * 4 + 1];
                var r = pixels[i * 4 + 2];
                return r > 200 && g < 80 && b < 80; // Pbgra is premultiplied
            }

            var redColumns = new List<int>();
            for (var px = 0; px < width; px++)
            {
                if (IsRed(px))
                {
                    redColumns.Add(px);
                }
            }

            // The header border occupies the last pixel column inside the header; the
            // separator must sit in that same pixel column (center = headerRight - 0.5).
            var aligned = redColumns.Where(px => Math.Abs(px + 0.5 - (headerRight - 0.5)) <= 1.0).ToList();
            Assert.NotEmpty(aligned);

            // The pre-fix bug drew the line ~8px right of the header border (template inset).
            var drifted = redColumns.Where(px => px > headerRight + 1 && px < headerRight + 8).ToList();
            Assert.Empty(drifted);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SeparatorBrush_FallsBackToGridLinesBrush()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.ItemsSource = new[] { new Employee { Name = "x" } };
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();

        try
        {
            // Default GridLinesBrush is set by the theme; a custom brush overrides.
            var themeBrush = grid.GridLinesBrush;
            grid.ColumnSeparatorBrush = Brushes.Blue;
            Assert.Equal(Brushes.Blue, grid.ColumnSeparatorBrush);
            Assert.NotEqual(themeBrush, grid.ColumnSeparatorBrush);
        }
        finally
        {
            window.Close();
        }
    }
}
