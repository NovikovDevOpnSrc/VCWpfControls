using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
            // The 32px field minimum from the implicit style (was 22px stock).
            Assert.True(combo.ActualHeight >= 32, $"{theme}: combo realized {combo.ActualHeight:F0}px, need >= 32");

            // The chrome follows the theme: background resolves to the palette surface,
            // not the stock light box (the dark-surface failure mode).
            var background = Assert.IsType<SolidColorBrush>(combo.Background);
            var dictionary = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/VCWpfControls;component/Themes/{theme}.xaml"),
            };
            var surface = Assert.IsType<SolidColorBrush>(dictionary[GridResources.SurfaceBackgroundKey]);
            Assert.Equal(surface.Color, background.Color);
        }
        finally
        {
            window.Close();
        }
    }
}
