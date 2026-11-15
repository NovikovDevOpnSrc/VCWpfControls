using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Localization;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Fixes from the Wizard-template acceptance round (ui-review, verdict REWORK): stock
/// light button chrome on the dark surface (luma 221), no primary CTA, no Enter/Esc
/// keyboard surface, a Header property the default template never rendered, no step
/// indication, off-grid footer spacing and no empty state.
/// </summary>
public class WizardAcceptanceTests
{
    private static VCWpfControls.Wizard.Wizard Create(GridTheme theme, bool addPages = true)
    {
        var wizard = new VCWpfControls.Wizard.Wizard { Width = 520, Height = 300 };
        wizard.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{theme}.xaml"),
        });
        if (addPages)
        {
            var page1 = new VCWpfControls.Wizard.WizardPage { Header = "Step one" };
            page1.Content = new TextBlock { Text = "First page content" };
            var page2 = new VCWpfControls.Wizard.WizardPage { Header = "Step two" };
            page2.Content = new TextBlock { Text = "Second page content" };
            wizard.Pages.Add(page1);
            wizard.Pages.Add(page2);
        }

        return wizard;
    }

    [StaFact]
    public void DarkThemedWizard_ButtonsAreThemed_AndPrimaryCarriesAccent()
    {
        var wizard = Create(GridTheme.Dark);
        var window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();
        try
        {
            var next = (Button)wizard.Template.FindName("PART_NextButton", wizard)!;

            var band = RenderBand(wizard, next);
            // The stock chrome measured median 221 (light box on dark); the themed surface
            // is #292929 (~41), the accent ~82 — both must keep the band DARK, and the
            // white primary glyphs must peak light.
            Assert.True(band.Median < 150, $"Next button still light chrome: median {band.Median}");
            Assert.True(band.Max > 200, $"primary glyphs unreadable: max {band.Max}");

            // The primary style is in place: accent background resolved from the palette.
            var accent = Assert.IsType<SolidColorBrush>(wizard.Resources.MergedDictionaries[0][GridResources.AccentKey]).Color;
            var background = Assert.IsType<SolidColorBrush>(next.Background).Color;
            Assert.Equal(accent, background);

            // The 32px interactive minimum (was 30px via Padding 12,6).
            Assert.True(next.ActualHeight >= 32, $"button height {next.ActualHeight:F0} < 32");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Enter_Confirms_And_Esc_Cancels()
    {
        var wizard = Create(GridTheme.Light);
        var window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();
        try
        {
            var finished = 0;
            var cancelled = 0;
            wizard.Finished += (_, _) => finished++;
            wizard.Cancelled += (_, _) => cancelled++;

            SendKey(wizard, Key.Enter); // advances 1 -> 2
            Assert.Equal(1, wizard.CurrentPageIndex);

            SendKey(wizard, Key.Enter); // on the last page: Finish
            Assert.Equal(1, finished);

            SendKey(wizard, Key.Escape);
            Assert.Equal(1, cancelled);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Header_And_Step_Render_AndFollowNavigation()
    {
        var wizard = Create(GridTheme.Light);
        var window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();
        try
        {
            var step = Assert.IsType<TextBlock>(wizard.Template.FindName("PART_StepText", wizard)!);
            var expectedStep = GridStrings.WizardStepFormat(1, 2);
            Assert.Equal(expectedStep, step.Text);

            Assert.True(FindText(wizard, "Step one"), "the page Header must render");
            Assert.False(FindText(wizard, "Second page content"), "only the current page renders");

            wizard.GoNext();
            TestWindowHost.Pump();
            Assert.True(FindText(wizard, "Step two"), "the new page's Header renders");
            Assert.True(FindText(wizard, "Second page content"), "the page content must follow navigation");
            var expectedStep2 = GridStrings.WizardStepFormat(2, 2);
            Assert.Equal(expectedStep2, ((TextBlock)wizard.Template.FindName("PART_StepText", wizard)!).Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void EmptyWizard_ShowsEmptyState_AndHidesNavigation()
    {
        var wizard = Create(GridTheme.Light, addPages: false);
        var window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();
        try
        {
            var empty = Assert.IsType<TextBlock>(wizard.Template.FindName("PART_EmptyState", wizard)!);
            Assert.Equal(Visibility.Visible, empty.Visibility);
            Assert.Equal(GridStrings.WizardNoPages, empty.Text);

            var footer = (FrameworkElement)wizard.Template.FindName("PART_Footer", wizard)!;
            Assert.Equal(Visibility.Collapsed, footer.Visibility);
            Assert.False(wizard.HasPages);
        }
        finally
        {
            window.Close();
        }
    }

    private static (int Median, int Max) RenderBand(FrameworkElement host, FrameworkElement editor)
    {
        var width = (int)Math.Ceiling(host.ActualWidth);
        var height = (int)Math.Ceiling(host.ActualHeight);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            ctx.DrawRectangle(new VisualBrush(host), null, new Rect(0, 0, width, height));
        }

        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        var tl = editor.TranslatePoint(new Point(0, 0), host);
        var lumas = new List<int>();
        for (var y = (int)tl.Y + 4; y < tl.Y + editor.ActualHeight - 4; y++)
        {
            for (var x = (int)tl.X + 4; x < tl.X + editor.ActualWidth - 4; x++)
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
        return (lumas[lumas.Count / 2], lumas[^1]);
    }

    private static void SendKey(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target);
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.KeyDownEvent,
            Source = target,
        });
    }

    private static bool FindText(DependencyObject root, string text)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock tb && tb.Text == text)
            {
                return true;
            }

            if (FindText(child, text))
            {
                return true;
            }
        }

        return false;
    }
}
