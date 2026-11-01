using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Large data set virtualization sample.</summary>
public partial class VirtualizationSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public VirtualizationSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "VirtualizationGrid");
    }
}
