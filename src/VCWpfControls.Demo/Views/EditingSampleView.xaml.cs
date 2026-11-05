using System.Windows;
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
        // On by default so the add-row is immediately visible.
        AddRowsToggle.IsChecked = true;
        Grid.CanUserAddRows = true;
    }

    private void OnAddRowsToggled(object sender, RoutedEventArgs e)
    {
        Grid.CanUserAddRows = AddRowsToggle.IsChecked == true;
    }

    private void OnPasteClick(object sender, RoutedEventArgs e)
    {
        Grid.PasteFromClipboard();
    }
}
