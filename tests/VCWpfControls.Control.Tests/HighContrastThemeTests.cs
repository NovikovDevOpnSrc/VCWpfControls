using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The HighContrast palette (UX profile A11Y-003 — support Windows contrast preferences):
/// every brush aliases a LIVE system color, and the selected row flips its text to
/// HighlightText — in inverted schemes (white Highlight, black WindowText) the normal
/// data color would be invisible on the selected background.
/// </summary>
public class HighContrastThemeTests
{
    private sealed class Row
    {
        public string Name { get; set; } = "Row";
    }

    private static ResourceDictionary LoadHighContrast()
    {
        _ = new GridControl(); // registers pack:// in STA hosts without an Application
        return new ResourceDictionary
        {
            Source = new System.Uri("pack://application:,,,/VCWpfControls;component/Themes/HighContrast.xaml"),
        };
    }

    [StaFact]
    public void Palette_FollowsLiveSystemColors()
    {
        var dictionary = LoadHighContrast();

        AssertColor(dictionary, GridResources.TextBrushKey, SystemColors.WindowTextColor);
        AssertColor(dictionary, GridResources.MutedTextKey, SystemColors.WindowTextColor);
        AssertColor(dictionary, GridResources.HeaderForegroundKey, SystemColors.WindowTextColor);
        AssertColor(dictionary, GridResources.SurfaceBackgroundKey, SystemColors.WindowColor);
        AssertColor(dictionary, GridResources.HeaderBackgroundKey, SystemColors.WindowColor);
        AssertColor(dictionary, GridResources.HoverBackgroundKey, SystemColors.WindowColor); // decorative tint collapsed
        AssertColor(dictionary, GridResources.AlternatingRowBackgroundKey, SystemColors.WindowColor);
        AssertColor(dictionary, GridResources.HeaderBorderKey, SystemColors.WindowFrameColor);
        AssertColor(dictionary, GridResources.GridLineKey, SystemColors.WindowFrameColor);
        AssertColor(dictionary, GridResources.SeparatorLineKey, SystemColors.WindowFrameColor);
        AssertColor(dictionary, GridResources.SelectionBackgroundKey, SystemColors.HighlightColor);
        AssertColor(dictionary, GridResources.SelectionTextKey, SystemColors.HighlightTextColor);
        AssertColor(dictionary, GridResources.AccentKey, SystemColors.HotTrackColor);
    }

    [StaFact]
    public void Accent_OverlayIgnoredInHighContrast()
    {
        var purple = new AccentOverride("Purple", "#8B5CF6", "#B9A2F8");

        var resources = new ResourceDictionary();
        ThemeManager.ApplyTo(resources, GridTheme.HighContrast, purple);
        AssertColor(resources, GridResources.AccentKey, SystemColors.HotTrackColor); // system palette wins

        // Leaving high contrast brings the accent back (the preset survived the HC interlude).
        ThemeManager.ApplyTo(resources, GridTheme.Light, purple);
        var applied = Assert.IsType<SolidColorBrush>(resources[GridResources.AccentKey]);
        Assert.Equal(Color.FromRgb(0x8B, 0x5C, 0xF6), applied.Color);
    }

    [StaFact]
    public void SelectedRow_TextFlipsToHighlightText()
    {
        var grid = new GridControl { Width = 400, Height = 200 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Width = new GridColumnWidth(150, GridUnitType.Pixel) });
        grid.ItemsSource = new[] { new Row(), new Row() };
        ThemeManager.ApplyTo(grid.Resources, GridTheme.HighContrast, null);
        var window = TestWindowHost.Show(grid);
        try
        {
            var firstItem = ((Grid.Data.GridDataRow)grid.DataEngine.View[0]).Item;
            grid.SelectItem(firstItem);
            TestWindowHost.Pump();

            var row = FindDescendants<GridRow>(grid).Single(r => r.IsSelected);
            var cellText = FindDescendants<TextBlock>(row).First(t => t.Text == "Row");
            var foreground = Assert.IsType<SolidColorBrush>(cellText.Foreground);
            Assert.Equal(SystemColors.HighlightTextBrush.Color, foreground.Color);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PerControlThemeManager_AppliesHighContrast()
    {
        var grid = new GridControl();
        GridThemeManager.Apply(grid, GridTheme.HighContrast);
        AssertColor(grid.Resources, GridResources.TextBrushKey, SystemColors.WindowTextColor);
    }

    private static void AssertColor(ResourceDictionary dictionary, ComponentResourceKey key, Color expected)
    {
        var brush = Assert.IsType<SolidColorBrush>(dictionary[key]);
        Assert.Equal(expected, brush.Color);
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T matched)
            {
                yield return matched;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
