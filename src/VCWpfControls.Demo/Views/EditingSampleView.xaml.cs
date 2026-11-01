using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Editing &amp; validation sample.</summary>
public partial class EditingSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public EditingSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "EditingGrid");
    }
}
