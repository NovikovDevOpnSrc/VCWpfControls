using System.Windows;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Fluent 2 alignment of the default themes: the type-ramp/font tokens, the 2px
/// accent focus ring, and the Fluent light/dark palettes (brand #0F6CBD/#479EF5, the
/// tinted neutral ramp).
/// </summary>
public class FluentAlignmentTests
{
    private static object Resolve(string dictionary, object key)
    {
        var grid = new GridControl();
        grid.Resources.MergedDictionaries.Add(
            new ResourceDictionary { Source = new System.Uri($"pack://application:,,,/VCWpfControls;component/Themes/{dictionary}.xaml") });
        var window = TestWindowHost.Show(grid);
        try
        {
            return grid.TryFindResource(key);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TypeRampTokens_ResolveFluentValues()
    {
        Assert.Equal(12d, Resolve("Generic", GridResources.FontSizeCaptionKey));
        Assert.Equal(14d, Resolve("Generic", GridResources.FontSizeBodyKey));
        Assert.Equal(16d, Resolve("Generic", GridResources.FontSizeSubtitleKey));
        Assert.Equal(20d, Resolve("Generic", GridResources.FontSizeTitleKey));

        var font = Assert.IsType<FontFamily>(Resolve("Generic", GridResources.FontFamilyKey));
        Assert.Contains("Segoe UI", font.Source, StringComparison.Ordinal);
    }

    [StaFact]
    public void FocusRingToken_ResolvesAStyle()
    {
        Assert.NotNull(Assert.IsType<Style>(Resolve("Generic", GridResources.FocusVisualKey)));
    }

    [StaFact]
    public void LightPalette_UsesFluentBrandAndNeutrals()
    {
        var accent = Assert.IsType<SolidColorBrush>(Resolve("Light", GridResources.AccentKey));
        Assert.Equal(Color.FromRgb(0x0F, 0x6C, 0xBD), accent.Color); // Fluent brand primary

        var text = Assert.IsType<SolidColorBrush>(Resolve("Light", GridResources.TextBrushKey));
        Assert.Equal(Color.FromRgb(0x24, 0x24, 0x24), text.Color); // neutralForeground1
    }

    [StaFact]
    public void DarkPalette_UsesFluentBrandAndLightText()
    {
        var accent = Assert.IsType<SolidColorBrush>(Resolve("Dark", GridResources.AccentKey));
        Assert.Equal(Color.FromRgb(0x47, 0x9E, 0xF5), accent.Color); // Fluent brand dark

        var text = Assert.IsType<SolidColorBrush>(Resolve("Dark", GridResources.TextBrushKey));
        var brightness = text.Color.R + text.Color.G + text.Color.B;
        Assert.True(brightness > 600, $"dark-theme text must stay light, got #{text.Color}"); // #FFFFFF
    }
}
