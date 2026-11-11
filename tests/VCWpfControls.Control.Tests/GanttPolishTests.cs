using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Gantt theming round: the timeline's chart artifacts (milestones, baselines, links,
/// the today line, non-working tints, bar borders) drew with literal brushes — the
/// milestone diamond was Gray-on-Black regardless of the palette (invisible in dark), and
/// nothing followed high contrast. Everything resolves through GridResources tokens now.
/// </summary>
public class GanttPolishTests
{
    [StaFact]
    public void MilestoneDiamond_IsVisibleInDarkTheme()
    {
        var day = DateTime.Today.AddDays(1); // off the today line so its orange dash can't mask the diamond
        var task = new GanttTask("m", "Kickoff done", day, day, 0, isMilestone: true);
        var panel = new GanttTimelinePanel
        {
            Width = 420,
            Height = 140,
            ShowBarLabels = false, // labels are themed white — they would mask the diamond
            Tasks = new List<GanttTask> { task },
        };
        ThemeManager.ApplyTo(panel.Resources, GridTheme.Dark, null);
        var window = TestWindowHost.Show(panel);
        TestWindowHost.Pump();
        try
        {
            var bar = panel.BarOf(task);
            Assert.NotNull(bar);
            // The diamond spans ±7px around the bar center; clamp below the header so the
            // timescale's white date text (directly above row 0) cannot mask the assertion.
            var region = Rect.Inflate(bar!.Value, 9, 9);
            region.Y = Math.Max(region.Y, panel.HeaderHeight);

            // Offscreen render through a VisualBrush (direct RenderTargetBitmap.Render of an
            // element yields an empty raster in this host — see ColumnSeparatorTests).
            var width = (int)Math.Ceiling(panel.RenderSize.Width);
            var height = (int)Math.Ceiling(panel.RenderSize.Height);
            Assert.True(width > 0 && height > 0, $"timeline not laid out: {width}x{height}");
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(panel), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            // The diamond's outline is the theme text brush (#FFFFFF in dark): with the old
            // hardcoded Gray-on-Black the brightest artifact in the region was the fill
            // (luma ~105); the themed stroke puts glyph pixels near 255.
            var maxLuma = 0;
            for (var y = (int)Math.Max(region.Y, 0); y < Math.Min(region.Bottom, height); y++)
            {
                for (var x = (int)Math.Max(region.X, 0); x < Math.Min(region.Right, width); x++)
                {
                    var i = (y * width + x) * 4; // Pbgra: B,G,R,A
                    var luma = (pixels[i] * 11 + pixels[i + 1] * 59 + pixels[i + 2] * 30) / 100;
                    maxLuma = Math.Max(maxLuma, luma);
                }
            }

            Assert.True(maxLuma > 180, $"milestone diamond invisible in dark theme: brightest luma {maxLuma}");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GanttTokens_FollowSystemColorsInHighContrast()
    {
        _ = new GridControl(); // registers pack:// in STA hosts without an Application
        var dictionary = new ResourceDictionary
        {
            Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/HighContrast.xaml"),
        };

        var baseline = Assert.IsType<SolidColorBrush>(dictionary[GridResources.BaselineFillKey]);
        Assert.Equal(SystemColors.WindowColor, baseline.Color); // flat: the separator border conveys the band
    }
}
