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
        ColumnSeparators.IsChecked = false; // set in code: XAML IsChecked=True fires before the grid exists
    }

    private void OnSeparatorsToggled(object sender, System.Windows.RoutedEventArgs e)
    {
        if (Content is GridControl grid)
        {
            grid.ShowColumnSeparators = ColumnSeparators.IsChecked == true;
        }
    }
}
