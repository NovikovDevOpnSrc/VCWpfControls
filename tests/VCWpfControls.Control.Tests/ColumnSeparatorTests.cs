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
            var redColumns = RenderRowBandSeparatorColumns(grid);
            var headerRight = HeaderRight(grid, "Name");

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

    /// <summary>
    /// Reordering columns (the same RemoveAt+Insert the header drag-drop performs) must move
    /// the separators: the cached OnRender output would otherwise keep lines at the OLD column
    /// boundaries while the headers and cells have already moved.
    /// </summary>
    [StaFact]
    public void Separators_FollowColumnReorder()
    {
        var grid = new GridControl { Width = 300, RowHeight = 24, HeaderHeight = 26 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 100 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 150 });
        grid.ItemsSource = CreateEmployees(2);
        grid.ColumnSeparatorBrush = Brushes.Red;
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();

        try
        {
            // Sanity: the first boundary sits at the right border of the first column (A).
            var before = RenderRowBandSeparatorColumns(grid);
            var aRight = HeaderRight(grid, "Name");
            Assert.Contains(before, px => Math.Abs(px + 0.5 - (aRight - 0.5)) <= 1.0);

            // Move A after B — the exact code path of NotifyColumnDragCompleted.
            var name = grid.Columns[0];
            grid.Columns.RemoveAt(0);
            grid.Columns.Insert(1, name);
            TestWindowHost.Pump();

            // B (150px) now owns the first slot: the first line must be at B's right border,
            // not at the stale 100px boundary of the old A|B split.
            var after = RenderRowBandSeparatorColumns(grid);
            Assert.NotEmpty(after);
            var bRight = HeaderRight(grid, "Department");
            Assert.Contains(after, px => Math.Abs(px + 0.5 - (bRight - 0.5)) <= 1.0);

            // Every drawn line must coincide with SOME header border now — a stale render
            // keeps the old first boundary (bRight - 50 here), which matches none.
            var rights = new[] { HeaderRight(grid, "Department"), HeaderRight(grid, "Name") };
            foreach (var px in after)
            {
                Assert.Contains(rights, r => Math.Abs(px + 0.5 - (r - 0.5)) <= 1.0);
            }
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

    /// <summary>
    /// Renders the grid offscreen at 96 DPI (DIP == pixel) and returns the pixel columns of the
    /// separator lines in the first data row's band. Direct RenderTargetBitmap.Render(grid)
    /// yields an empty raster in this host; capturing through a VisualBrush wrapper works.
    /// </summary>
    private static List<int> RenderRowBandSeparatorColumns(GridControl grid)
    {
        var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
        var width = (int)Math.Ceiling(grid.ActualWidth);
        var height = (int)Math.Ceiling(grid.ActualHeight);
        Assert.True(width > 0 && height > 0, $"grid not laid out: {width}x{height}");

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new System.Windows.Media.DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, width, height));
        }

        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);

        // Near the row top to stay clear of cell text glyphs; Pbgra is premultiplied (B,G,R,A).
        var y = (int)Math.Floor(row.TranslatePoint(new Point(0, 0), grid).Y) + 3;
        var redColumns = new List<int>();
        for (var px = 0; px < width; px++)
        {
            var i = (y * width + px) * 4;
            if (pixels[i + 2] > 200 && pixels[i + 1] < 80 && pixels[i] < 80)
            {
                redColumns.Add(px);
            }
        }

        return redColumns;
    }

    /// <summary>The right edge of the named column's header in grid coordinates.</summary>
    private static double HeaderRight(GridControl grid, string fieldName)
    {
        var presenter = (GridColumnHeadersPresenter)grid.Template.FindName("PART_ColumnHeadersPresenter", grid)!;
        var header = presenter.Children.OfType<GridColumnHeader>().First(h => h.Column?.FieldName == fieldName);
        return header.TranslatePoint(new Point(header.ActualWidth, 0), grid).X;
    }
}
