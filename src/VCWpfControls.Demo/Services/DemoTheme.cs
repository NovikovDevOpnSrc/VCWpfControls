using VCWpfControls.Themes;

namespace VCWpfControls.Demo.Services;

/// <summary>Named accent presets shown in the settings (Fiori-style qualitative picks).</summary>
public sealed record DemoAccent(string Name, string LightHex, string DarkHex)
{
    /// <summary>All presets offered by the demo.</summary>
    public static readonly DemoAccent[] All =
    {
        new("Default", "#0F6CBD", "#479EF5"), // the Fluent 2 brand
        new("Purple", "#8B5CF6", "#B9A2F8"),
        new("Green", "#107C10", "#6CBF6C"),
        new("Orange", "#CA5010", "#F7B183"),
    };
}

/// <summary>
/// Demo-side theming: resolves the "System" mode to the actual OS preference and delegates
/// to the LIBRARY <see cref="ThemeManager"/> (the centralized application-level switch).
/// </summary>
public static class DemoTheme
{
    /// <summary>Applies the theme mode (Light/Dark/System) and the accent preset.</summary>
    public static void Apply(string themeMode, string accentName)
    {
        var theme = themeMode == "System"
            ? (SystemThemeWatcher.IsLightPreferred() ? GridTheme.Light : GridTheme.Dark)
            : themeMode == "Dark" ? GridTheme.Dark : GridTheme.Light;

        var accent = DemoAccent.All.FirstOrDefault(a => a.Name == accentName) ?? DemoAccent.All[0];
        ThemeManager.SetAccent(accent.Name == "Default" ? null : new AccentOverride(accent.Name, accent.LightHex, accent.DarkHex));
        ThemeManager.Apply(theme); // apply last so the accent overlay lands on the new palette
    }

    /// <summary>Sets the thread cultures for library string localization.</summary>
    public static void ApplyLanguage(string cultureName)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
    }
}
