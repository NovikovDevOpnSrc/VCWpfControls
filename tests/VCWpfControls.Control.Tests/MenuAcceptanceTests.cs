using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The menus feature: implicit themed Menu/MenuItem/ContextMenu in every palette (the
/// stock chrome ignores Background setters — the proven class; Win11 metrics: 32px rows,
/// 8px-radius surface cards, accent underline on open top-levels, check/gesture/arrow
/// columns collapsing per role, themed separators via the Separator role template).
/// </summary>
public class MenuAcceptanceTests
{
    private static ResourceDictionary Dark()
        => new()
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        };

    private static Menu CreateBar(StackPanel host)
    {
        host.Background = System.Windows.Media.Brushes.DimGray; // expose chrome vs surface

        var menu = new Menu();
        var file = new MenuItem { Header = "_File" };
        var open = new MenuItem { Header = "_Open", InputGestureText = "Ctrl+O" };
        var autosave = new MenuItem { Header = "Autosave", IsCheckable = true, IsChecked = true };
        var exit = new MenuItem { Header = "E_xit", InputGestureText = "Alt+F4" };
        var sep = new Separator();
        file.Items.Add(open);
        file.Items.Add(autosave);
        file.Items.Add(sep);
        file.Items.Add(exit);
        menu.Items.Add(file);
        host.Children.Add(menu);
        return menu;
    }

    [StaFact]
    public void MenuBar_TopLevelFollowsDarkTheme_32px()
    {
        var host = new StackPanel { Width = 400 };
        host.Resources.MergedDictionaries.Add(Dark());
        var menu = CreateBar(host);
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var file = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.Equal(MenuItemRole.TopLevelHeader, file.Role);
            Assert.True(file.ActualHeight >= 32, $"top-level item {file.ActualHeight:F0}px < 32");

            // Transparent bar over the dark host: the item band must stay DARK (the stock
            // chrome would paint a light system band).
            var width = (int)Math.Ceiling(host.ActualWidth);
            var height = (int)Math.Ceiling(host.ActualHeight);
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(host), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            var tl = file.TranslatePoint(new Point(0, 0), host);
            var lumas = new List<int>();
            for (var y = (int)tl.Y + 2; y < tl.Y + file.ActualHeight - 2; y++)
            {
                for (var x = (int)tl.X + 2; x < tl.X + file.ActualWidth - 2; x++)
                {
                    var i = (y * width + x) * 4;
                    if (pixels[i + 3] < 200)
                    {
                        continue;
                    }

                    lumas.Add((pixels[i] * 11 + pixels[i + 1] * 59 + pixels[i + 2] * 30) / 100);
                }
            }

            Assert.NotEmpty(lumas);
            lumas.Sort();
            Assert.True(lumas[lumas.Count / 2] < 150, $"menu bar still light chrome: median {lumas[lumas.Count / 2]}");

            // The accent underline marks an open top-level menu (property-driven trigger).
            // An inactive test window coerces IsSubmenuOpen back (popups need an active
            // window) — assert the OPEN STATE WIRING structurally: the template carries an
            // IsSubmenuOpen trigger that reveals the accent underline.
            Assert.Contains(file.Template.Triggers, t =>
                t is System.Windows.Trigger trigger
                && trigger.Property.Name == "IsSubmenuOpen"
                && trigger.Setters.OfType<System.Windows.Setter>()
                    .Any(setter => setter.TargetName == "Underline"));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SubmenuItems_CheckGestureAndCheckGlyphs_ThroughAContextMenu()
    {
        // Submenu popups of a Menu bar need an ACTIVE window (the test host shows windows
        // inactive); a ContextMenu opens fine — same SubmenuItem role and template.
        var host = new StackPanel { Width = 300, Height = 120 };
        host.Resources.MergedDictionaries.Add(Dark());
        host.Background = System.Windows.Media.Brushes.DimGray;
        var button = new Button { Content = "Right-click me" };
        var menu = new ContextMenu();
        var cut = new MenuItem { Header = "Cu_t", InputGestureText = "Ctrl+X" };
        var autosave = new MenuItem { Header = "Autosave", IsCheckable = true, IsChecked = true };
        var send = new MenuItem { Header = "Send to", InputGestureText = "" };
        send.Items.Add(new MenuItem { Header = "Mail" });
        menu.Items.Add(cut);
        menu.Items.Add(autosave);
        menu.Items.Add(send);
        button.ContextMenu = menu;
        host.Children.Add(button);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
            TestWindowHost.Pump();

            var open = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.NotNull(open);
            Assert.Equal(MenuItemRole.SubmenuItem, open.Role);
            Assert.True(open.ActualHeight >= 32, $"submenu item {open.ActualHeight:F0}px < 32");

            // The gesture column renders the shortcut; the header of a parent item shows
            // the arrow affordance.
            var gesture = Assert.IsType<TextBlock>(open.Template.FindName("Gesture", open));
            Assert.Equal("Ctrl+X", gesture.Text);
            Assert.Equal(Visibility.Visible, gesture.Visibility);

            var sendItem = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(2);
            var arrow = (System.Windows.Shapes.Path)sendItem.Template.FindName("Arrow", sendItem);
            Assert.Equal(Visibility.Visible, arrow.Visibility);
            var plainArrow = (System.Windows.Shapes.Path)open.Template.FindName("Arrow", open);
            Assert.Equal(Visibility.Collapsed, plainArrow.Visibility);

            // Checkable: the glyph follows IsChecked.
            var autosaveItem = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(1);
            var check = (System.Windows.Shapes.Path)autosaveItem.Template.FindName("Check", autosaveItem);
            Assert.Equal(Visibility.Visible, check.Visibility);
            autosaveItem.IsChecked = false;
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Hidden, check.Visibility);

            // The glyph is a small centered mark, NOT stretched over the whole 24x32 cell
            // (the stock Path default Stretch filled it — a distorted smear, user report).
            autosaveItem.IsChecked = true;
            TestWindowHost.Pump();
            var glyphTop = check.TranslatePoint(new Point(0, 0), autosaveItem).Y;
            var centered = (autosaveItem.ActualHeight - check.ActualHeight) / 2;
            Assert.InRange(check.ActualHeight, 4, 16); // glyph-sized, not cell-sized
            Assert.InRange(Math.Abs(glyphTop - centered), 0, 1.5); // vertically centered

            // The submenu arrow is vertically centered too (the same stretch-at-origin
            // bug the check glyph had — caught by the acceptance critique).
            var arrowTop = arrow.TranslatePoint(new Point(0, 0), sendItem).Y;
            var arrowCentered = (sendItem.ActualHeight - arrow.ActualHeight) / 2;
            Assert.InRange(arrow.ActualHeight, 4, 16);
            Assert.InRange(Math.Abs(arrowTop - arrowCentered), 0, 1.5);

            // MenuItem.Icon renders (Win11: the check replaces the icon on checkable items).
            var iconPresenter = (ContentPresenter)sendItem.Template.FindName("IconPresenter", sendItem);
            sendItem.Icon = new System.Windows.Shapes.Ellipse { Width = 12, Height = 12, Fill = Brushes.Black };
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Visible, iconPresenter.Visibility);
            var checkableIcon = (ContentPresenter)autosaveItem.Template.FindName("IconPresenter", autosaveItem);
            Assert.Equal(Visibility.Collapsed, checkableIcon.Visibility); // the check replaces the icon
            var plainIcon = (ContentPresenter)open.Template.FindName("IconPresenter", open);
            Assert.Equal(Visibility.Visible, plainIcon.Visibility); // non-checkable keeps its icon slot

            // The hover trigger is ENABLED-gated (a disabled item must not light up) and
            // pairs the background with the SelectionText foreground (native HC pairing).
            Assert.Contains(open.Template.Triggers, t =>
                t is System.Windows.MultiTrigger multi
                && multi.Conditions.Any(c => c.Property.Name == "IsHighlighted")
                && multi.Conditions.Any(c => c.Property.Name == "IsEnabled")
                && multi.Setters.OfType<System.Windows.Setter>()
                    .Any(setter => setter.Property.Name == "Foreground"));
        }
        finally
        {
            menu.IsOpen = false;
            window.Close();
        }
    }

    [StaFact]
    public void HighContrast_MenuHover_IsTheNativeHighlight()
    {
        // In HC the generic hover token aliases Window (invisible on the Window
        // surface) — menus carry a DEDICATED hover token aliasing Highlight, so hover
        // feedback survives every contrast scheme.
        _ = new GridControl();
        var hc = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/HighContrast.xaml"),
        };
        var hover = Assert.IsType<SolidColorBrush>(hc[GridResources.MenuHoverBackgroundKey]);
        Assert.Equal(SystemColors.HighlightColor, hover.Color);
    }

    [StaFact]
    public void ContextMenu_FollowsTheTheme_WhenOpened()
    {
        var host = new StackPanel { Width = 300, Height = 100 };
        host.Resources.MergedDictionaries.Add(Dark());
        var button = new Button { Content = "Right-click me" };
        var menu = new ContextMenu();
        var cut = new MenuItem { Header = "Cu_t", InputGestureText = "Ctrl+X" };
        menu.Items.Add(cut);
        button.ContextMenu = menu;
        host.Children.Add(button);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
            TestWindowHost.Pump();

            // The implicit style resolved through the host's themed resources.
            var background = Assert.IsType<SolidColorBrush>(menu.Background);
            var surface = Assert.IsType<SolidColorBrush>(Dark()[GridResources.SurfaceBackgroundKey]);
            Assert.Equal(surface.Color, background.Color);

            var item = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.Equal(MenuItemRole.SubmenuItem, item.Role);
            Assert.True(item.ActualHeight >= 32, $"context item {item.ActualHeight:F0}px < 32");
        }
        finally
        {
            menu.IsOpen = false;
            window.Close();
        }
    }
}
