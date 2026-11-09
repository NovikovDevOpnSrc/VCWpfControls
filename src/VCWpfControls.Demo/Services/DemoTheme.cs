using System.Windows;
using VCWpfControls.Grid;
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

/// <summary>Applies the demo-wide theme + accent override at the APPLICATION level.</summary>
public static class DemoTheme
{
    private static readonly object ThemeKey = new();
    private static readonly object AccentKey = new();

    /// <summary>Applies the built-in Light/Dark palette and the accent preset app-wide.</summary>
    public static void Apply(string themeName, string accentName)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var theme = themeName == "Dark" ? GridTheme.Dark : GridTheme.Light;
        Swap(
            app,
            ThemeKey,
            new ResourceDictionary
            {
                Source = new Uri(
                    theme == GridTheme.Dark ? "/VCWpfControls;component/Themes/Dark.xaml" : "/VCWpfControls;component/Themes/Light.xaml",
                    UriKind.Relative),
            });

        var accent = DemoAccent.All.FirstOrDefault(a => a.Name == accentName) ?? DemoAccent.All[0];
        var dark = theme == GridTheme.Dark;

        // The override dictionary re-defines the accent-carrying keys (ComponentResourceKey
        // instances must be created in code to match the library's key objects).
        var accentDictionary = new ResourceDictionary();
        if (accent.Name != "Default")
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark ? accent.DarkHex : accent.LightHex);
            var accentBrush = new System.Windows.Media.SolidColorBrush(color);
            accentBrush.Freeze();
            accentDictionary[GridResources.AccentKey] = accentBrush;
            accentDictionary[GridResources.CurrentCellBorderBrushKey] = accentBrush;

            // A readable text variant of the accent: lightened on dark themes, darkened on light.
            var text = System.Windows.Media.Color.FromRgb(
                dark ? (byte)Math.Min(color.R + 40, 255) : (byte)Math.Max(color.R - 25, 0),
                dark ? (byte)Math.Min(color.G + 40, 255) : (byte)Math.Max(color.G - 25, 0),
                dark ? (byte)Math.Min(color.B + 40, 255) : (byte)Math.Max(color.B - 40, 0));
            var accentText = new System.Windows.Media.SolidColorBrush(text);
            accentText.Freeze();
            accentDictionary[GridResources.AccentTextKey] = accentText;
        }

        Swap(app, AccentKey, accentDictionary);
    }

    /// <summary>Sets the thread cultures for library string localization.</summary>
    public static void ApplyLanguage(string cultureName)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static void Swap(Application app, object key, ResourceDictionary dictionary)
    {
        if (app.Resources[key] is ResourceDictionary old)
        {
            app.Resources.MergedDictionaries.Remove(old);
        }

        if (dictionary.Count > 0 || key == ThemeKey)
        {
            app.Resources.MergedDictionaries.Add(dictionary);
            app.Resources[key] = dictionary;
        }
        else
        {
            app.Resources.Remove(key); // "Default" accent removes the override entirely
        }
    }
}
