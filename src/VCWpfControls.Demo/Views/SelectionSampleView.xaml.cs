using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;
using VCWpfControls.Grid;

namespace VCWpfControls.Demo.Views;

/// <summary>Selection sample: binds SelectedItems count to the view model.</summary>
public partial class SelectionSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public SelectionSampleView()
    {
        InitializeComponent();
        Grid.CellSelectionChanged += (_, _) => CellCount.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} cells selected", Grid.SelectedCells.Count);
        Grid.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SelectionGrid");
        Grid.SelectionChanged += (_, _) =>
        {
            if (DataContext is SelectionSampleViewModel vm)
            {
                vm.SelectedCount = Grid.SelectedItems.Count;
            }
        };
    }

    private void OnCellUnitToggled(object sender, RoutedEventArgs e)
    {
        if (Grid is null)
        {
            return;
        }

        Grid.SelectionUnit = CellUnitToggle.IsChecked == true
            ? GridSelectionUnit.CellOrRow
            : GridSelectionUnit.Row;
        CellCount.Text = "0 cells selected";
    }
}
