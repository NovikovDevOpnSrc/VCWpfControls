using System.Windows;
using VCWpfControls.Grid;

namespace VCWpfControls.Themes;

/// <summary>Built-in themes.</summary>
public enum GridTheme
{
    /// <summary>Light theme (default).</summary>
    Light = 0,

    /// <summary>Dark theme.</summary>
    Dark = 1,
}

/// <summary>
/// Switches grid themes by merging the matching resource dictionary into the control's resources.
/// </summary>
public static class GridThemeManager
{
    private static readonly object ThemeDictionaryKey = new();

    /// <summary>Applies a built-in theme to the given grid.</summary>
    public static void Apply(GridControl grid, GridTheme theme)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var uri = theme switch
        {
            GridTheme.Dark => MakeUri("/VCWpfControls;component/Themes/Dark.xaml"),
            _ => MakeUri("/VCWpfControls;component/Themes/Light.xaml"),
        };

        var dictionary = new ResourceDictionary { Source = uri };
        var existing = grid.Resources[ThemeDictionaryKey];
        if (existing is ResourceDictionary old)
        {
            grid.Resources.MergedDictionaries.Remove(old);
        }

        grid.Resources.MergedDictionaries.Add(dictionary);
        grid.Resources[ThemeDictionaryKey] = dictionary;
    }

    private static Uri MakeUri(string relative)
        => new(relative, UriKind.Relative);
}
