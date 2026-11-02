using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Property grid bound to the selected row of a grid.</summary>
public partial class PropertyGridSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public PropertyGridSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "PropertyGridSample");
    }
}
