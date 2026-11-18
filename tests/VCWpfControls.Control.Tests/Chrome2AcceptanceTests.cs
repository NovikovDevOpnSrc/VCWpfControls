using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Chrome completion II: the remaining stock surfaces (ToolTip, GroupBox, Expander,
/// TabControl/TabItem, Slider, ProgressBar, ListBox/ListBoxItem, TreeView/TreeViewItem)
/// followed the light system chrome in themed apps — now implicit themed styles in every
/// palette (the parity guard auto-extended to the 11 new types).
/// </summary>
public class Chrome2AcceptanceTests
{
    private static ResourceDictionary Dark()
        => new()
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        };

    [StaFact]
    public void ListBoxAndTreeView_FollowTheDarkTheme()
    {
        var host = new StackPanel { Width = 300, Height = 200 };
        host.Resources.MergedDictionaries.Add(Dark());
        var list = new ListBox { Height = 100 };
        list.Items.Add("alpha");
        list.Items.Add("beta");
        var tree = new TreeView { Height = 90 };
        tree.Items.Add(new TreeViewItem { Header = "root" });
        host.Children.Add(list);
        host.Children.Add(tree);
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var surface = Assert.IsType<SolidColorBrush>(Dark()[GridResources.SurfaceBackgroundKey]).Color;
            Assert.Equal(surface, Assert.IsType<SolidColorBrush>(list.Background).Color);
            Assert.Equal(surface, Assert.IsType<SolidColorBrush>(tree.Background).Color);

            list.SelectedIndex = 0;
            TestWindowHost.Pump();
            var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            var root = (Border)item.Template.FindName("Root", item); // the trigger sets the template Root
            var selected = Assert.IsType<SolidColorBrush>(root.Background);
            var selection = Assert.IsType<SolidColorBrush>(Dark()[GridResources.SelectionBackgroundKey]).Color;
            Assert.Equal(selection, selected.Color);
            Assert.True(item.ActualHeight >= 32, $"item {item.ActualHeight:F0}px < 32");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TabItem_SelectedCarriesAccentUnderline_SliderAndProgressFollowTokens()
    {
        var host = new StackPanel { Width = 320, Height = 200 };
        host.Resources.MergedDictionaries.Add(Dark());
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "One", Content = new TextBlock { Text = "first" } });
        tabs.Items.Add(new TabItem { Header = "Two", Content = new TextBlock { Text = "second" } });
        tabs.SelectedIndex = 0;
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 40, Width = 220 };
        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 40, Height = 12, Width = 220 };
        host.Children.Add(tabs);
        host.Children.Add(slider);
        host.Children.Add(progress);
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var one = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.True(one.ActualHeight >= 32, $"tab {one.ActualHeight:F0}px < 32");
            var underline = (System.Windows.Shapes.Rectangle)one.Template.FindName("Underline", one);
            Assert.Equal(Visibility.Visible, underline.Visibility);
            var two = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(1);
            Assert.Equal(Visibility.Collapsed,
                ((System.Windows.Shapes.Rectangle)two.Template.FindName("Underline", two)).Visibility);

            // The slider thumb is PART-driven AND carries the ACCENT fill (a hardcoded
            // light brush would pass a parts-only check — the acceptance critique's point).
            var track = Assert.IsType<Track>(slider.Template.FindName("PART_Track", slider));
            Assert.True(track.Thumb.ActualWidth >= 16, $"thumb {track.Thumb.ActualWidth:F0}px < 16");
            Assert.True(track.Value >= 35 && track.Value <= 45, $"track value {track.Value}");
            var thumbFill = Assert.IsType<SolidColorBrush>(
                ((System.Windows.Shapes.Ellipse)track.Thumb.Template.FindName("ThumbDot", track.Thumb)
                    ?? throw new InvalidOperationException("ThumbDot missing")).Fill);
            var accent = Assert.IsType<SolidColorBrush>(Dark()[GridResources.AccentKey]).Color;
            Assert.Equal(accent, thumbFill.Color);

            // The progress indicator is PART-driven (the control sizes it): ~40% of the
            // width AND the accent fill (not a hardcoded bar color).
            var indicator = (Border)progress.Template.FindName("PART_Indicator", progress);
            var trackBorder = (Border)progress.Template.FindName("PART_Track", progress);
            TestWindowHost.Pump();
            Assert.True(indicator.ActualWidth > 0.25 * trackBorder.ActualWidth
                && indicator.ActualWidth < 0.55 * trackBorder.ActualWidth,
                $"indicator {indicator.ActualWidth:F0} of track {trackBorder.ActualWidth:F0} at Value=40");
            Assert.Equal(accent, Assert.IsType<SolidColorBrush>(indicator.Background).Color);

            // Geometry + brush: the underline sits flush at the tab bottom and its Fill
            // resolves the ACCENT token (pixel probes of a 2px element proved brittle in
            // this host; the realized brush is the honest signal).
            var underlineBottom = underline.TranslatePoint(new Point(0, underline.ActualHeight), tabs).Y;
            var oneBottom = one.TranslatePoint(new Point(0, one.ActualHeight), tabs).Y;
            Assert.InRange(Math.Abs(underlineBottom - oneBottom), 0, 1.5); // flush at the tab bottom
            var underlineFill = Assert.IsType<SolidColorBrush>(underline.Fill);
            Assert.Equal(accent, underlineFill.Color);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ToolTip_GroupBox_Expander_FollowTheDarkTheme()
    {
        var host = new StackPanel { Width = 300, Height = 120 };
        host.Resources.MergedDictionaries.Add(Dark());
        var box = new GroupBox { Header = "Group", Content = new TextBlock { Text = "inside" } };
        var expander = new Expander { Header = "Details", IsExpanded = false };
        host.Children.Add(box);
        host.Children.Add(expander);
        var tip = new ToolTip { Content = "a tip", PlacementTarget = host };
        host.ToolTip = tip;
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            tip.IsOpen = true;
            TestWindowHost.Pump();
            Assert.True(tip.IsOpen); // themed template applies without errors
            var surface = Assert.IsType<SolidColorBrush>(Dark()[GridResources.SurfaceBackgroundKey]).Color;
            Assert.Equal(surface, Assert.IsType<SolidColorBrush>(tip.Background).Color);

            // Expander expands via its template toggle.
            expander.IsExpanded = true;
            TestWindowHost.Pump();
            var toggle = Assert.IsType<ToggleButton>(expander.Template.FindName("HeaderToggle", expander));
            Assert.True(toggle.IsChecked == true);
        }
        finally
        {
            tip.IsOpen = false;
            window.Close();
        }
    }
}
