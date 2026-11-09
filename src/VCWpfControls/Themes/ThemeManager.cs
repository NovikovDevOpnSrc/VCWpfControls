using System.Windows;
using System.Windows.Media;
using VCWpfControls.Grid;

namespace VCWpfControls.Themes;

/// <summary>
/// The application-level theme manager (the DevExpress/Fiori centralized pattern): swaps
/// the palette dictionary in <see cref="Application.Resources"/> and optionally overlays an
/// accent override — every control brush resolves through the
/// <see cref="GridResources"/> keys with <c>DynamicResource</c>, so the whole UI follows
/// instantly without reloads. Per-control theming stays available through
/// <see cref="GridThemeManager.Apply(GridControl, GridTheme)"/>.
/// </summary>
public static class ThemeManager
{
    private static readonly object ThemeKey = new();
    private static readonly object AccentKey = new();

    /// <summary>The current applied theme (Light before anything is applied).</summary>
    public static GridTheme CurrentTheme { get; private set; } = GridTheme.Light;

    /// <summary>The current accent override, if any (null = the theme's brand colors).</summary>
    public static AccentOverride? CurrentAccent { get; private set; }

    /// <summary>Raised after the theme or the accent changed (re-read the brushes).</summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Applies a built-in palette application-wide. Safe before/without an
    /// <see cref="Application"/> (records the choice; brushes apply when one exists).
    /// </summary>
    public static void Apply(GridTheme theme)
    {
        CurrentTheme = theme;
        var app = Application.Current;
        if (app is not null)
        {
            SwapTheme(app.Resources, theme);
            if (CurrentAccent is { } accent)
            {
                SwapAccent(app.Resources, accent, theme);
            }
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Sets an application-wide accent override (both palette keys and the current-cell
    /// border follow); null restores the theme's brand colors.
    /// </summary>
    public static void SetAccent(AccentOverride? accent)
    {
        CurrentAccent = accent;
        var app = Application.Current;
        if (app is not null)
        {
            if (accent is { } preset)
            {
                SwapAccent(app.Resources, preset, CurrentTheme);
            }
            else
            {
                Remove(app.Resources, AccentKey);
            }
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Applies the dictionaries to an arbitrary resource container (testability: STA test
    /// hosts run without an <see cref="Application"/>) — the same logic
    /// <see cref="Apply(GridTheme)"/> performs on the application.
    /// </summary>
    internal static void ApplyTo(ResourceDictionary resources, GridTheme theme, AccentOverride? accent)
    {
        SwapTheme(resources, theme);
        if (accent is { } preset)
        {
            SwapAccent(resources, preset, theme);
        }
        else
        {
            Remove(resources, AccentKey);
        }
    }

    private static void SwapTheme(ResourceDictionary resources, GridTheme theme)
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                theme == GridTheme.Dark ? "/VCWpfControls;component/Themes/Dark.xaml" : "/VCWpfControls;component/Themes/Light.xaml",
                UriKind.Relative),
        };
        Replace(resources, ThemeKey, dictionary);
    }

    private static void SwapAccent(ResourceDictionary resources, AccentOverride accent, GridTheme theme)
    {
        var color = (Color)ColorConverter.ConvertFromString(theme == GridTheme.Dark ? accent.DarkHex : accent.LightHex);
        var dictionary = new ResourceDictionary
        {
            [GridResources.AccentKey] = Frozen(color),
            [GridResources.CurrentCellBorderBrushKey] = Frozen(color),
        };

        // A readable text variant of the accent: lightened on dark, darkened on light.
        var text = Color.FromRgb(
            theme == GridTheme.Dark ? (byte)Math.Min(color.R + 40, 255) : (byte)Math.Max(color.R - 25, 0),
            theme == GridTheme.Dark ? (byte)Math.Min(color.G + 40, 255) : (byte)Math.Max(color.G - 25, 0),
            theme == GridTheme.Dark ? (byte)Math.Min(color.B + 40, 255) : (byte)Math.Max(color.B - 40, 0));
        dictionary[GridResources.AccentTextKey] = Frozen(text);
        Replace(resources, AccentKey, dictionary);
    }

    private static void Replace(ResourceDictionary resources, object key, ResourceDictionary dictionary)
    {
        Remove(resources, key);
        resources.MergedDictionaries.Add(dictionary);
        resources[key] = dictionary;
    }

    private static void Remove(ResourceDictionary resources, object key)
    {
        if (resources[key] is ResourceDictionary old)
        {
            resources.MergedDictionaries.Remove(old);
            resources.Remove(key);
        }
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

/// <summary>An accent preset: a named color pair for the light and dark palettes.</summary>
public sealed record AccentOverride(string Name, string LightHex, string DarkHex);
