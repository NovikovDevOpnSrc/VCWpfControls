using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Localization;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Fixes from the batch acceptance round (PropertyGrid template + compact chrome under
/// the implicit themed Button style): the search hint used to sit in Tag (never rendered
/// by any template) and the box measured 18px; a null SelectedObject rendered a blank
/// pane with a live search box; nested-property expanders were mouse-only; the docking
/// chrome buttons turned into 14x32 slivers under the implicit Button style's 32px floor.
/// </summary>
public class BatchAcceptanceTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "Alpha";

        public int Age { get; set; } = 42;
    }

    private static ResourceDictionary Theme()
        => new()
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Light.xaml"),
        };

    [StaFact]
    public void PropertyGrid_SearchHint_ShowsWatermark_And_EscClears()
    {
        var pg = new VCWpfControls.PropertyGrid.PropertyGrid { Width = 300, Height = 220 };
        pg.Resources.MergedDictionaries.Add(Theme());
        pg.SelectedObject = new Person();
        var window = TestWindowHost.Show(pg);
        TestWindowHost.Pump();
        try
        {
            var search = (TextBox)pg.Template.FindName("PART_SearchBox", pg)!;
            Assert.True(search.ActualHeight >= 32, $"search box {search.ActualHeight:F0}px < 32");

            // The watermark renders while empty (the hint string used to be dead Tag content).
            var hint = FindTextBlock(pg, GridStrings.PropertyGridSearchHint);
            Assert.NotNull(hint);
            Assert.Equal(Visibility.Visible, hint!.Visibility);

            // Typing hides it; Esc clears the filter (the search-field convention).
            search.Text = "Name";
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Collapsed, hint.Visibility);

            search.Text = "N";
            search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(search)!, 0, Key.Escape)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
                Source = search,
            });
            TestWindowHost.Pump();
            Assert.Equal(string.Empty, search.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PropertyGrid_NoObject_ShowsEmptyState_AndHidesSearch()
    {
        var pg = new VCWpfControls.PropertyGrid.PropertyGrid { Width = 300, Height = 220 };
        pg.Resources.MergedDictionaries.Add(Theme());
        var window = TestWindowHost.Show(pg);
        TestWindowHost.Pump();
        try
        {
            Assert.Null(pg.SelectedObject);
            var empty = (TextBlock)pg.Template.FindName("PART_EmptyState", pg)!;
            Assert.Equal(Visibility.Visible, empty.Visibility);
            Assert.Equal(GridStrings.PropertyGridNoObject, empty.Text);

            var search = (TextBox)pg.Template.FindName("PART_SearchBox", pg)!;
            Assert.Equal(Visibility.Collapsed, search.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Docking_ChromeButtons_StayCompact_UnderTheImplicitButtonStyle()
    {
        var docking = new VCWpfControls.Docking.DockingManager { Width = 500, Height = 300 };
        docking.Resources.MergedDictionaries.Add(Theme());
        var pane = new VCWpfControls.Docking.DockPane { Header = "Tools" };
        pane.Content = new TextBlock { Text = "tool content" };
        docking.Panes.Add(pane);
        var window = TestWindowHost.Show(docking);
        TestWindowHost.Pump();
        try
        {
            var close = FindButtons(docking).FirstOrDefault(b => b.Content?.ToString() == "\u2715");
            Assert.NotNull(close);
            // The blast-radius regression: the implicit 32px floor turned this into 14x32.
            Assert.True(close!.ActualHeight <= 24, $"close button height {close.ActualHeight:F0} > 24 (sliver chrome)");
            Assert.True(close.ActualWidth <= 24 || close.ActualHeight >= close.ActualWidth,
                $"close button proportions {close.ActualWidth:F0}x{close.ActualHeight:F0}");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ThemedApps_TextBox_GetsThe24pxFloor()
    {
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(Theme());
        var box = new TextBox { Width = 160 };
        host.Children.Add(box);
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            Assert.True(box.ActualHeight >= 24, $"bare TextBox {box.ActualHeight:F0}px < 24");
        }
        finally
        {
            window.Close();
        }
    }

    private static TextBlock? FindTextBlock(DependencyObject root, string text)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && tb.Text == text)
            {
                return tb;
            }

            if (FindTextBlock(child, text) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private static List<Button> FindButtons(DependencyObject root)
    {
        var found = new List<Button>();
        Collect(root, found);
        return found;
    }

    private static void Collect(DependencyObject root, List<Button> found)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button b)
            {
                found.Add(b);
            }

            Collect(child, found);
        }
    }
}
