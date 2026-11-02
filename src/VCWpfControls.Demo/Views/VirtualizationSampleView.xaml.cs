using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>Large data set virtualization sample with an async-refresh toggle.</summary>
public partial class VirtualizationSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public VirtualizationSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "VirtualizationGrid");
    }

    private void OnAsyncToggle(object sender, RoutedEventArgs e)
        => Grid.UseAsyncRefresh = AsyncToggle.IsChecked == true;
}
