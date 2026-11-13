using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Editors;
using VCWpfControls.Grid;
using VCWpfControls.Themes;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Fixes from the first two ui-review acceptance rounds that live in the LIBRARY and
/// needed permanent regressions: the implicit themed ComboBox style (stock WPF renders a
/// 22px light-chrome box that sits wrong on dark surfaces and under the 32px field
/// minimum — acceptance finding R2/S5 of the Settings round) and the NumericUpDown
/// acceptance fixes (dead tab stop; spin buttons under the target width and resting on
/// the hover token with no hover trigger — Editors round findings C9/C10/R2).
/// </summary>
public class AcceptanceFixTests
{
    [StaFact]
    public void NumericUpDown_WrapperIsNotATabStop()
    {
        var spinner = new NumericUpDown();
        Assert.False(KeyboardNavigation.GetIsTabStop(spinner), "the wrapper must not steal a Tab press");
        Assert.False(spinner.Focusable, "the inner TextBox owns the caret");
    }

    [StaFact]
    public void NumericUpDown_SpinButtons_ReachTargetWidth_AndHaveHoverState()
    {
        var spinner = new NumericUpDown { Width = 200 };
        var window = TestWindowHost.Show(spinner);
        TestWindowHost.Pump();
        try
        {
            foreach (var part in new[] { "PART_UpButton", "PART_DownButton" })
            {
                var button = Assert.IsType<RepeatButton>(spinner.Template.FindName(part, spinner));
                Assert.True(button.ActualWidth >= 24, $"{part} realized {button.ActualWidth:F0}px, need >= 24");

                // Hover must visibly change the button (the old template RESTED on the
                // hover token with no trigger — hover was indistinguishable by construction).
                var template = (ControlTemplate)button.Template;
                Assert.Contains(template.Triggers, t =>
                    t is Trigger trigger && trigger.Property.Name == "IsMouseOver");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ThemedApps_StockCheckBoxText_FollowsTheTheme()
    {
        // User report: "Show column separators" was black on the dark surface — the stock
        // CheckBox template paints ControlText regardless of the theme. The implicit
        // themed style must carry the text brush.
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        });
        var box = new CheckBox { Content = "Show column separators" };
        host.Children.Add(box);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var text = Assert.IsType<SolidColorBrush>(box.Foreground);
            Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), text.Color); // Dark TextBrush
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ThemedApps_ComboBoxDropDown_ItemsReadableInTheDarkTheme()
    {
        // User report: the Department dropdown's editor showed light text on a light
        // popup — the ComboBox was themed but its ITEMS (ComboBoxItem) kept stock chrome.
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        });
        var combo = new ComboBox();
        combo.ItemsSource = new[] { "IT", "HR", "Sales" };
        combo.SelectedIndex = 0;
        host.Children.Add(combo);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        combo.IsDropDownOpen = true; // containers generate only when the dropdown opens
        TestWindowHost.Pump();
        try
        {
            var item = (ComboBoxItem)combo.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.NotNull(item);
            var foreground = Assert.IsType<SolidColorBrush>(item.Foreground);
            var background = Assert.IsType<SolidColorBrush>(item.Background);
            Assert.True(Contrast(foreground.Color, background.Color) >= 4.5,
                $"dropdown item contrast {Contrast(foreground.Color, background.Color):F2}:1 — text must be readable on its own background");
        }
        finally
        {
            window.Close();
        }
    }

    private static double Lum(Color c)
    {
        double F(int v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * F(c.R) + 0.7152 * F(c.G) + 0.0722 * F(c.B);
    }

    private static double Contrast(Color a, Color b)
    {
        var l1 = Math.Max(Lum(a), Lum(b));
        var l2 = Math.Min(Lum(a), Lum(b));
        return (l1 + 0.05) / (l2 + 0.05);
    }

    [StaFact]
    public void ThemedApps_ComboBoxEditor_TextReadableInTheDarkTheme()
    {
        // User report round 2: the CLOSED editor still showed light chrome under themed
        // light text — the STOCK ComboBox template ignores Background/BorderBrush (the
        // chrome is baked into the system theme), so setter-only styling painted the
        // property, not the pixels. The full themed template must RENDER the surface.
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        });
        var combo = new ComboBox { Width = 160 };
        combo.ItemsSource = new[] { "IT", "HR", "Sales" };
        combo.SelectedIndex = 0;
        host.Children.Add(combo);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var width = (int)Math.Ceiling(combo.ActualWidth);
            var height = (int)Math.Ceiling(combo.ActualHeight);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new VisualBrush(combo), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            // The text band must contain BOTH the dark chrome and light glyphs (the
            // broken state rendered everything light: min luma 232 on the probe).
            var minLuma = 255;
            var maxLuma = 0;
            for (var y = height / 4; y < height * 3 / 4; y++)
            {
                for (var x = 4; x < width / 2; x++)
                {
                    var i = (y * width + x) * 4;
                    var luma = (pixels[i] * 11 + pixels[i + 1] * 59 + pixels[i + 2] * 30) / 100;
                    minLuma = Math.Min(minLuma, luma);
                    maxLuma = Math.Max(maxLuma, luma);
                }
            }

            Assert.True(minLuma < 80, $"editor chrome still light: min luma {minLuma}");
            Assert.True(maxLuma > 200, $"editor text still dark: max luma {maxLuma}");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Editors_DisplayInitialValueAndText()
    {
        // Editor-theme-sweep finding: a Value/Text set BEFORE the template applied never
        // reached the inner box (the DP callback found _textBox null and dropped it) —
        // both editors rendered empty until the first change.
        var spinner = new NumericUpDown { Width = 200, Value = 42m };
        var complete = new AutoComplete { Width = 200, Text = "Alice Johnson" };
        var host = new StackPanel();
        host.Children.Add(spinner);
        host.Children.Add(complete);
        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            var spinnerBox = Assert.IsType<TextBox>(spinner.Template.FindName("PART_TextBox", spinner));
            Assert.Equal("42", spinnerBox.Text);
            var completeBox = Assert.IsType<TextBox>(complete.Template.FindName("PART_TextBox", complete));
            Assert.Equal("Alice Johnson", completeBox.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ThemedApps_TextBoxAndDatePicker_FollowTheDarkTheme()
    {
        // Sweep findings: the stock TextBox (grid text editor, PropertyGrid values, filter
        // row) and the stock DatePicker (GridDateColumn's editor) rendered a WHITE box on
        // the dark surface; the DatePicker's inner box is a DatePickerTextBox — a separate
        // type needing its own style.
        var host = new StackPanel();
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml"),
        });
        var box = new TextBox { Width = 160, Text = "Sample" };
        var picker = new DatePicker { Width = 160, SelectedDate = new DateTime(2026, 11, 13) };
        host.Children.Add(box);
        host.Children.Add(picker);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            AssertBandFollowsTheme(host, box, "TextBox");
            var inner = Assert.IsAssignableFrom<TextBox>(picker.Template.FindName("PART_TextBox", picker));
            AssertBandFollowsTheme(host, inner, "DatePickerTextBox");
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertBandFollowsTheme(FrameworkElement host, FrameworkElement editor, string name)
    {
        ((Panel)host).Background = Brushes.DimGray; // expose chrome vs surface distinctly
        TestWindowHost.Pump();
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

        var topLeft = editor.TranslatePoint(new Point(0, 0), host);
        var yStart = (int)(topLeft.Y + editor.ActualHeight / 4);
        var yEnd = (int)(topLeft.Y + editor.ActualHeight * 3 / 4);
        var lumas = new List<int>();
        for (var y = yStart; y <= yEnd; y++)
        {
            for (var x = (int)topLeft.X + 3; x < topLeft.X + editor.ActualWidth - 3; x++)
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
        // MEDIAN = the chrome (the dominant surface of the band): the broken state was a
        // white box with BLACK text — min/max both pass that; the median does not.
        var median = lumas[lumas.Count / 2];
        var maxLuma = lumas[^1];
        Assert.True(median < 80, $"{name}: chrome still light (median luma {median})");
        Assert.True(maxLuma > 200, $"{name}: text still invisible (max luma {maxLuma})");
    }

    [StaTheory]
    [InlineData(GridTheme.Light)]
    [InlineData(GridTheme.Dark)]
    [InlineData(GridTheme.HighContrast)]
    public void ThemedApps_GetImplicitComboBoxStyle_With32pxMinimum(GridTheme theme)
    {
        _ = new GridControl(); // pack:// registration in STA hosts

        var host = new StackPanel();
        var combo = new ComboBox();
        combo.ItemsSource = new[] { "A", "B" };
        combo.SelectedIndex = 0;
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{theme}.xaml"),
        });
        host.Children.Add(combo);

        var window = TestWindowHost.Show(host);
        TestWindowHost.Pump();
        try
        {
            // The 32px field minimum from the implicit style (was 22px stock). The RENDERED
            // chrome is asserted pixel-wise in ThemedApps_ComboBoxEditor_TextReadableInTheDarkTheme.
            Assert.True(combo.ActualHeight >= 32, $"{theme}: combo realized {combo.ActualHeight:F0}px, need >= 32");
            Assert.NotNull(combo.Template); // the themed template, not the stock chrome
        }
        finally
        {
            window.Close();
        }
    }
}
