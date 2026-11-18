using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Themed chrome gallery: tabs, expander, groupbox, slider, progress, lists, trees, tooltips.</summary>
public partial class ChromeSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public ChromeSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "ChromeSample");
        for (var i = 1; i <= 6; i++)
        {
            List.Items.Add($"Item {i}");
        }

        List.SelectedIndex = 0;
    }
}
