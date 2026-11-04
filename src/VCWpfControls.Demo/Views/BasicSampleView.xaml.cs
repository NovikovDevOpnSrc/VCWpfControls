using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid;

namespace VCWpfControls.Demo.Views;

/// <summary>Auto-generated columns sample.</summary>
public partial class BasicSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public BasicSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "BasicGrid");
        // On by default so the feature is visible; the named grid exists after InitializeComponent.
        ColumnSeparators.IsChecked = true;
        Grid.ShowColumnSeparators = true;
    }

    private void OnSeparatorsToggled(object sender, System.Windows.RoutedEventArgs e)
    {
        Grid.ShowColumnSeparators = ColumnSeparators.IsChecked == true;
    }
}
