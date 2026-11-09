using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;

namespace VCWpfControls.Demo.Views;

/// <summary>Global demo settings: theme, accent, language.</summary>
public partial class SettingsSampleView : UserControl
{
    /// <summary>Creates the page.</summary>
    public SettingsSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SettingsSample");
    }

    private void OnReset(object sender, RoutedEventArgs e)
        => ((SettingsSampleViewModel)DataContext).Reset();
}
