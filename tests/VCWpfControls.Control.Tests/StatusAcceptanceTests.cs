using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The status bar feature: implicit themed StatusBar/StatusBarItem styles in every
/// palette (caption-size text on the surface, 1px top separator); item separators are
/// themed Rectangles (StatusBarItem has NO SeparatorStyleKey in WPF — unlike MenuItem,
/// whose keyed style exists; the initial StatusBarItem.SeparatorStyleKey XAML compiled
/// by silently resolving to MenuItem's key — verify member existence by reflection, not
/// by whether XAML compiles). The demo shell docks a live bar (page + theme).
/// </summary>
public class StatusAcceptanceTests
{
    private static ResourceDictionary Load(string name)
    {
        _ = new GridControl();
        return new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{name}.xaml"),
        };
    }

    [StaFact]
    public void StatusBar_FollowsTheDarkTheme_28px_CaptionText()
    {
        var host = new DockPanel { Width = 400, Height = 120 };
        host.Resources.MergedDictionaries.Add(Load("Dark"));
        var bar = new StatusBar();
        var left = new StatusBarItem { Content = "Ready" };
        var right = new StatusBarItem { Content = "3 items", HorizontalAlignment = HorizontalAlignment.Right };
        bar.Items.Add(left);
        bar.Items.Add(right);
        DockPanel.SetDock(bar, Dock.Bottom);
        host.Children.Add(new TextBlock { Text = "content" });
        host.Children.Add(bar);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            Assert.True(bar.ActualHeight >= 28, $"bar {bar.ActualHeight:F0}px < 28");
            Assert.True(left.ActualHeight >= 28, $"item {left.ActualHeight:F0}px < 28");

            // The bar's chrome: background resolves to the dark surface (the stock chrome
            // would stay system-light).
            var background = Assert.IsType<SolidColorBrush>(bar.Background);
            var surface = Assert.IsType<SolidColorBrush>(Load("Dark")[GridResources.SurfaceBackgroundKey]);
            Assert.Equal(surface.Color, background.Color);

            // Caption-size text (12) per the type ladder.
            Assert.Equal(12.0, bar.FontSize, 0);

            // Item separators are themed 1px Rectangles (StatusBarItem has NO
            // SeparatorStyleKey in WPF — a plain <Separator/> draws a horizontal line).
            var rule = new System.Windows.Shapes.Rectangle
            {
                Width = 1,
                Height = 16,
                Margin = new Thickness(4, 4, 4, 4),
            };
            rule.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, GridResources.SeparatorLineKey);
            bar.Items.Insert(1, new StatusBarItem { Content = rule });
            TestWindowHost.Pump();
            Assert.Equal(1.0, rule.ActualWidth, 0.5);
            Assert.True(rule.ActualHeight >= 8,
                $"the separator rule is invisible: {rule.ActualWidth:F1}x{rule.ActualHeight:F1} (no Height = 0px tall)");
        }
        finally
        {
            window.Close();
        }
    }
}
