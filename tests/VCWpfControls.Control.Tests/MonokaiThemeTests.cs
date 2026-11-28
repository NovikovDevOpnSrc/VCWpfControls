using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Monokai palette (the second chained dictionary — Dark.xaml merged for every
/// template and implicit style, brush tokens overridden only) must be a COMPLETE,
/// READABLE theme: the key WCAG pairs from the realized brushes, the chain must be live
/// (values differ from Dark), and a Hyperlink realizes the palette's cyan.
/// </summary>
public class MonokaiThemeTests
{
    private static ResourceDictionary Load()
    {
        _ = new GridControl(); // pack:// registration
        return new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Monokai.xaml"),
        };
    }

    private static SolidColorBrush Brush(ResourceDictionary theme, ComponentResourceKey key)
        => Assert.IsType<SolidColorBrush>(theme[key]);

    private static double Contrast(Color a, Color b)
    {
        double L(Color c)
        {
            double Ch(byte v)
            {
                var s = v / 255d;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Ch(c.R) + 0.7152 * Ch(c.G) + 0.0722 * Ch(c.B);
        }

        var la = L(a);
        var lb = L(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [StaFact]
    public void KeyPairs_MeetWCAG()
    {
        var theme = Load();
        var surface = Brush(theme, GridResources.SurfaceBackgroundKey).Color;
        var headerBg = Brush(theme, GridResources.HeaderBackgroundKey).Color;

        Assert.True(Contrast(Brush(theme, GridResources.TextBrushKey).Color, surface) >= 4.5, "text/surface");
        Assert.True(Contrast(Brush(theme, GridResources.HeaderForegroundKey).Color, headerBg) >= 4.5, "header fg/bg");
        Assert.True(Contrast(Brush(theme, GridResources.MutedTextKey).Color, surface) >= 4.5, "muted/surface (comment gray brightened for the gate)");
        Assert.True(Contrast(Brush(theme, GridResources.SelectionTextKey).Color, Brush(theme, GridResources.SelectionBackgroundKey).Color) >= 4.5, "selection");
        Assert.True(Contrast(Brush(theme, GridResources.AccentTextKey).Color, surface) >= 4.5, "accent text (links)/surface");
        Assert.True(Contrast(Brush(theme, GridResources.ErrorTextKey).Color, surface) >= 4.5, "error/surface");
    }

    [StaFact]
    public void ChainOverridesDark_ValuesActuallyDiffer()
    {
        _ = new GridControl();
        var dark = new ResourceDictionary { Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml") };
        var monokai = Load();

        Assert.NotEqual(Brush(dark, GridResources.SurfaceBackgroundKey).Color, Brush(monokai, GridResources.SurfaceBackgroundKey).Color);
        Assert.NotEqual(Brush(dark, GridResources.AccentKey).Color, Brush(monokai, GridResources.AccentKey).Color);

        var implicitTypes = 0;
        foreach (var key in AllKeys(monokai))
        {
            if (key is Type)
            {
                implicitTypes++;
            }
        }

        Assert.True(implicitTypes >= 20, $"chained palette must expose Dark's implicit styles, found {implicitTypes}");
    }

    [StaFact]
    public void Hyperlink_RealizesTheMonokaiAccent()
    {
        var host = new System.Windows.Controls.StackPanel();
        host.Resources.MergedDictionaries.Add(Load());
        var link = new Hyperlink(new Run("docs"));
        var text = new System.Windows.Controls.TextBlock();
        text.Inlines.Add(link);
        host.Children.Add(text);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var foreground = Assert.IsType<SolidColorBrush>(link.Foreground);
            Assert.Equal(Brush(Load(), GridResources.AccentTextKey).Color, foreground.Color); // cyan #66D9EF
        }
        finally
        {
            window.Close();
        }
    }

    private static IEnumerable<object> AllKeys(ResourceDictionary dictionary)
    {
        foreach (var key in dictionary.Keys)
        {
            yield return key;
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            foreach (var key in AllKeys(merged))
            {
                yield return key;
            }
        }
    }
}
