using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Wizard;
using WizardControl = VCWpfControls.Wizard.Wizard;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The wizard gate must react to PROGRAMMATIC CanLeave changes (bindings, the UIA Toggle
/// pattern): page propagation via the descriptor subscription and the command re-query
/// after RefreshNavigation — the 2026-11-09 a11y round found both links broken.
/// </summary>
public class WizardProgrammaticGateTests
{
    [StaFact]
    public void ProgrammaticGate_EnablesNextWithoutUserInput()
    {
        var wizard = new WizardControl();
        var gate = new CheckBox();
        var page1 = new WizardPage { Header = "Welcome", Content = gate };
        page1.SetBinding(WizardPage.CanLeaveProperty, new System.Windows.Data.Binding
        {
            Source = gate,
            Path = new PropertyPath(ToggleButton.IsCheckedProperty),
            Converter = new BoolConverter(),
        });
        wizard.Pages.Add(page1);
        wizard.Pages.Add(new WizardPage { Header = "Two" });
        wizard.Pages.Add(new WizardPage { Header = "Three" });

        var window = TestWindowHost.Show(wizard);
        TestWindowHost.Pump();

        var next = wizard.Template.FindName("PART_NextButton", wizard) as Button;
        Assert.NotNull(next);
        Assert.False(next!.IsEnabled); // gated

        gate.IsChecked = true; // programmatic — same property path the UIA Toggle takes
        TestWindowHost.Pump();
        System.Threading.Thread.Sleep(200);
        TestWindowHost.Pump();

        Assert.True(page1.CanLeave, "binding did not push CanLeave");
        Assert.True(wizard.CanGoNext, "wizard engine truth");

        Assert.True(next.IsEnabled, "BUTTON did not enable (the real bug)");
        window.Close();
    }

    private sealed class BoolConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => value is true;
        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => value is true;
    }
}
