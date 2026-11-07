using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Wizard;

namespace VCWpfControls.Demo.Views;

/// <summary>Wizard sample: page validation via CanLeave, live status line.</summary>
public partial class WizardSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public WizardSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "WizardSample");

        // Page 1 blocks forward navigation until the checkbox is ticked (CanLeave binding).
        var first = (WizardPage)TheWizard.Pages[0];
        first.SetBinding(WizardPage.CanLeaveProperty, new System.Windows.Data.Binding
        {
            Source = AllowLeave,
            Path = new System.Windows.PropertyPath(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty),
            Converter = new BooleanToCanLeaveConverter(),
        });

        TheWizard.PageChanged += (_, _) => RefreshStatus();
        TheWizard.Finished += (_, _) => Status.Text = "Finished! (Finished event)";
        TheWizard.Cancelled += (_, _) => Status.Text = "Cancelled (Cancelled event — the app decides)";
        RefreshStatus();
    }

    private void RefreshStatus()
        => Status.Text = $"Page {TheWizard.CurrentPageIndex + 1} of {TheWizard.Pages.Count} — CanLeave gates Next on page 1.";

    private sealed class BooleanToCanLeaveConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is true;

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is true;
    }
}
