using System.Reflection;
using System.Windows;
using System.Windows.Media;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The centralized ThemeManager and the palette completeness contract: every theme
/// dictionary must define EVERY GridResources token (a missing key = a future dark-theme
/// bug like the hardcoded colors this round removed).
/// </summary>
public class ThemeManagerTests
{
    private static ResourceDictionary LoadTheme(string name)
    {
        // Touching a control first registers the pack:// scheme (no Application exists in
        // STA hosts; a bare pack URI construction throws "invalid port" without this).
        _ = new GridControl();
        return new ResourceDictionary
        {
            Source = new System.Uri($"pack://application:,,,/VCWpfControls;component/Themes/{name}.xaml"),
        };
    }

    [StaFact]
    public void EveryTheme_DefinesEveryToken()
    {
        var tokenFields = typeof(GridResources)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(ComponentResourceKey))
            .ToList();
        Assert.True(tokenFields.Count >= 16, $"expected the full token set, found {tokenFields.Count}");

        // Generic defines EVERYTHING (palette + typography + focus). Light/Dark override
        // the PALETTE (brush tokens) only — typography defaults flow from Generic.
        var generic = LoadTheme("Generic");
        var brushFields = new List<FieldInfo>();
        foreach (var field in tokenFields)
        {
            var key = (ComponentResourceKey)field.GetValue(null)!;
            Assert.True(generic.Contains(key), $"Generic.xaml is missing the '{field.Name}' token");
            if (generic[key] is Brush)
            {
                brushFields.Add(field);
            }
        }

        Assert.True(brushFields.Count >= 12, $"expected the brush token set, found {brushFields.Count}");
        foreach (var themeName in new[] { "Light", "Dark", "HighContrast" })
        {
            var dictionary = LoadTheme(themeName);
            foreach (var field in brushFields)
            {
                var key = (ComponentResourceKey)field.GetValue(null)!;
                Assert.True(dictionary.Contains(key), $"{themeName}.xaml is missing the '{field.Name}' brush token");
            }
        }
    }

    [StaFact]
    public void ApplyTo_SwapsPaletteAndAccent()
    {
        var resources = new ResourceDictionary();
        ThemeManager.ApplyTo(resources, GridTheme.Dark, null);
        var darkAccent = Assert.IsType<SolidColorBrush>(resources[GridResources.AccentKey]);
        Assert.Equal(Color.FromRgb(0x47, 0x9E, 0xF5), darkAccent.Color); // Fluent dark brand

        ThemeManager.ApplyTo(resources, GridTheme.Dark, new AccentOverride("Purple", "#8B5CF6", "#B9A2F8"));
        var purple = Assert.IsType<SolidColorBrush>(resources[GridResources.AccentKey]);
        Assert.Equal(Color.FromRgb(0xB9, 0xA2, 0xF8), purple.Color); // dark variant of the preset

        ThemeManager.ApplyTo(resources, GridTheme.Light, null); // accent cleared -> brand restored
        var lightAccent = Assert.IsType<SolidColorBrush>(resources[GridResources.AccentKey]);
        Assert.Equal(Color.FromRgb(0x0F, 0x6C, 0xBD), lightAccent.Color);
    }

    [StaFact]
    public void NewTokens_ResolveDistinctThemeValues()
    {
        var light = LoadTheme("Light");
        var dark = LoadTheme("Dark");

        var mutedLight = Assert.IsType<SolidColorBrush>(light[GridResources.MutedTextKey]).Color;
        var mutedDark = Assert.IsType<SolidColorBrush>(dark[GridResources.MutedTextKey]).Color;
        Assert.True(mutedLight.R < mutedDark.R, "dark muted text must be lighter than light-theme muted");

        var barLight = Assert.IsType<SolidColorBrush>(light[GridResources.BarFillKey]).Color;
        var barDark = Assert.IsType<SolidColorBrush>(dark[GridResources.BarFillKey]).Color;
        Assert.True(barLight.R > barDark.R, "dark bar fill must be darker than light-theme");
    }
}
