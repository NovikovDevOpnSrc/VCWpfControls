using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Regression (user report): after the Fluent 2 alignment the demo's column separators
/// became invisible — GridLine (#F0F0F0) is nearly white on the white surface. Separators
/// are opt-in decoration and now draw with their own darker `SeparatorLine` token
/// (#E0E0E0 light / #3D3D3D dark) unless ColumnSeparatorBrush overrides.
/// </summary>
public class SeparatorVisibilityTests
{
    private sealed class Row
    {
        public int Id { get; set; } = 1;
        public string Name { get; set; } = "AB";
        public string Dept { get; set; } = "IT";
    }

    private static (GridControl Grid, Window Window) Create()
    {
        var grid = new GridControl { Width = 600, Height = 200, RowHeight = 28, FrozenColumnCount = 1 };
        grid.Columns.Add(new GridNumericColumn { FieldName = "Id", Width = new GridColumnWidth(70, System.Windows.GridUnitType.Pixel), ReadOnly = true });
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Width = new GridColumnWidth(2, System.Windows.GridUnitType.Star) });
        grid.Columns.Add(new GridTextColumn { FieldName = "Dept", Width = new GridColumnWidth(1, System.Windows.GridUnitType.Star) });
        grid.ItemsSource = new[] { new Row(), new Row() };
        var window = TestWindowHost.Show(grid);
        grid.ShowColumnSeparators = true;
        TestWindowHost.Pump();
        return (grid, window);
    }

    private static HashSet<int> DarkColumns(GridControl grid, int y, int threshold = 235)
    {
        var width = (int)grid.ActualWidth;
        var height = (int)grid.ActualHeight;
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, width, height));
        }

        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        var dark = new HashSet<int>();
        for (var px = 0; px < width; px++)
        {
            var i = (y * width + px) * 4;
            var r = pixels[i + 2]; var g = pixels[i + 1]; var b = pixels[i]; // BGRA
            if (pixels[i + 3] > 0 && (r + g + b) / 3 < threshold)
            {
                dark.Add(px);
            }
        }

        return dark;
    }

    [StaFact]
    public void Separators_AreVisible_AgainstTheSurface()
    {
        var (grid, window) = Create();
        try
        {
            // Apply the Fluent light palette like the demo does (app-level theming).
            grid.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new System.Uri("/VCWpfControls;component/Themes/Light.xaml", UriKind.Relative),
            });
            TestWindowHost.Pump();

            var row = (Grid.Primitives.GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var y = (int)row.TranslatePoint(new Point(0, 0), grid).Y + 14;
            var dark = DarkColumns(grid, y);

            // The frozen column's right boundary (x=70) must carry a separator pixel —
            // with the old GridLine brush it was #F0F0F0 (invisible on white).
            Assert.Contains(70, dark);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SeparatorLineToken_IsDarkerThanGridLine()
    {
        var light = Assert.IsType<SolidColorBrush>(new ResourceDictionary
        {
            Source = new System.Uri("/VCWpfControls;component/Themes/Light.xaml", UriKind.Relative),
        }[GridResources.SeparatorLineKey]);
        var gridLine = Assert.IsType<SolidColorBrush>(new ResourceDictionary
        {
            Source = new System.Uri("/VCWpfControls;component/Themes/Light.xaml", UriKind.Relative),
        }[GridResources.GridLineKey]);

        Assert.True(light.Color.R < gridLine.Color.R,
            $"SeparatorLine ({light.Color}) must be darker than GridLine ({gridLine.Color})");
    }
}
