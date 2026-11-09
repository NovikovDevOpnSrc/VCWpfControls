using System.Windows;

namespace VCWpfControls.Grid;

/// <summary>
/// Resource keys of the default grid theme. All keys are <see cref="ComponentResourceKey"/>s,
/// so themes can override any visual aspect by re-defining these resources in merged dictionaries.
/// </summary>
public static class GridResources
{
    /// <summary>Background brush of the whole grid surface.</summary>
    public static readonly ComponentResourceKey SurfaceBackgroundKey = new(typeof(GridResources), "SurfaceBackground");

    /// <summary>Background brush of column headers.</summary>
    public static readonly ComponentResourceKey HeaderBackgroundKey = new(typeof(GridResources), "HeaderBackground");

    /// <summary>Foreground brush of column headers.</summary>
    public static readonly ComponentResourceKey HeaderForegroundKey = new(typeof(GridResources), "HeaderForeground");

    /// <summary>Border brush of column headers.</summary>
    public static readonly ComponentResourceKey HeaderBorderKey = new(typeof(GridResources), "HeaderBorder");

    /// <summary>Brush of the row selection background.</summary>
    public static readonly ComponentResourceKey SelectionBackgroundKey = new(typeof(GridResources), "SelectionBackground");

    /// <summary>Brush of the current cell border.</summary>
    public static readonly ComponentResourceKey CurrentCellBorderBrushKey = new(typeof(GridResources), "CurrentCellBorderBrush");

    /// <summary>Brush of the row separator (grid line).</summary>
    public static readonly ComponentResourceKey GridLineKey = new(typeof(GridResources), "GridLine");

    /// <summary>Brush of the accent (sort glyph, frozen edge).</summary>
    public static readonly ComponentResourceKey AccentKey = new(typeof(GridResources), "Accent");

    /// <summary>Foreground brush of data text (switches with the theme).</summary>
    public static readonly ComponentResourceKey TextBrushKey = new(typeof(GridResources), "TextBrush");

    /// <summary>Hover background brush of rows and headers.</summary>
    public static readonly ComponentResourceKey HoverBackgroundKey = new(typeof(GridResources), "HoverBackground");

    /// <summary>Accent text color (headers when pressed, links).</summary>
    public static readonly ComponentResourceKey AccentTextKey = new(typeof(GridResources), "AccentText");

    /// <summary>The Fluent 2 type ramp font (Segoe UI Variable with fallbacks).</summary>
    public static readonly ComponentResourceKey FontFamilyKey = new(typeof(GridResources), "FontFamily");

    /// <summary>Fluent 2 ramp: Caption (12).</summary>
    public static readonly ComponentResourceKey FontSizeCaptionKey = new(typeof(GridResources), "FontSizeCaption");

    /// <summary>Fluent 2 ramp: Body (14) — the default control text size.</summary>
    public static readonly ComponentResourceKey FontSizeBodyKey = new(typeof(GridResources), "FontSizeBody");

    /// <summary>Fluent 2 ramp: Subtitle (16).</summary>
    public static readonly ComponentResourceKey FontSizeSubtitleKey = new(typeof(GridResources), "FontSizeSubtitle");

    /// <summary>Fluent 2 ramp: Title (20).</summary>
    public static readonly ComponentResourceKey FontSizeTitleKey = new(typeof(GridResources), "FontSizeTitle");

    /// <summary>The Fluent 2 focus ring style (2px accent, rounded).</summary>
    public static readonly ComponentResourceKey FocusVisualKey = new(typeof(GridResources), "FocusVisual");
}
