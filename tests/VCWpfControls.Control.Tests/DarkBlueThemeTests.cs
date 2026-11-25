using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Dark Blue palette (the customization showcase): a chained dictionary — Dark.xaml
/// merged for every template and implicit style, brush tokens overridden only — must be a
/// COMPLETE, READABLE theme. This file pins the readability contract (WCAG pairs from the
/// realized brushes) and the chained-palette mechanism itself: a Hyperlink realizes the
/// palette's AccentText, and the token values actually DIFFER from Dark's (a silent chain
/// failure would fall back to Dark's neutrals and pass everything else).
/// </summary>
public class DarkBlueThemeTests
{
    private static ResourceDictionary Load()
    {
        _ = new GridControl(); // pack:// registration
        return new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/DarkBlue.xaml"),
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

        // Body and header text on their surfaces.
        Assert.True(Contrast(Brush(theme, GridResources.TextBrushKey).Color, surface) >= 4.5, "text/surface");
        Assert.True(Contrast(Brush(theme, GridResources.HeaderForegroundKey).Color, headerBg) >= 4.5, "header fg/bg");
        Assert.True(Contrast(Brush(theme, GridResources.MutedTextKey).Color, surface) >= 4.5, "muted/surface");

        // Selection and interactive accents.
        Assert.True(Contrast(Brush(theme, GridResources.SelectionTextKey).Color, Brush(theme, GridResources.SelectionBackgroundKey).Color) >= 4.5, "selection");
        Assert.True(Contrast(Brush(theme, GridResources.AccentTextKey).Color, surface) >= 4.5, "accent text (links)/surface");
        Assert.True(Contrast(Brush(theme, GridResources.ErrorTextKey).Color, surface) >= 4.5, "error/surface");
    }

    [StaFact]
    public void ChainOverridesDark_ValuesActuallyDiffer()
    {
        _ = new GridControl();
        var dark = new ResourceDictionary { Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml") };
        var blue = Load();

        // The chain must be LIVE: own entries win over the merged Dark's.
        Assert.NotEqual(Brush(dark, GridResources.SurfaceBackgroundKey).Color, Brush(blue, GridResources.SurfaceBackgroundKey).Color);
        Assert.NotEqual(Brush(dark, GridResources.HeaderBackgroundKey).Color, Brush(blue, GridResources.HeaderBackgroundKey).Color);

        // ...while non-token resources still arrive through the chain (the implicit chrome).
        var implicitTypes = 0;
        foreach (var key in AllKeys(blue))
        {
            if (key is Type)
            {
                implicitTypes++;
            }
        }

        Assert.True(implicitTypes >= 20, $"chained palette must expose Dark's implicit styles, found {implicitTypes}");
    }

    [StaFact]
    public void Hyperlink_RealizesTheDarkBlueAccent()
    {
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(Load());
        var link = new Hyperlink(new Run("docs"));
        var text = new TextBlock();
        text.Inlines.Add(link);
        host.Children.Add(text);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var foreground = Assert.IsType<SolidColorBrush>(link.Foreground);
            Assert.Equal(Brush(Load(), GridResources.AccentTextKey).Color, foreground.Color);
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
