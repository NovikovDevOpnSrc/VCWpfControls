using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Demo.Views;

/// <summary>Sorting sample.</summary>
public partial class SortingSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public SortingSampleView()
    {
        InitializeComponent();
        Grid.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SortingGrid");
    }

    private void OnSorted(object sender, EventArgs e)
    {
        if (DataContext is not SortingSampleViewModel vm)
        {
            return;
        }

        vm.SortInfo = Grid.SortDescriptions.Count == 0
            ? "Unsorted"
            : "Sorted by: " + string.Join(", ", Grid.SortDescriptions.Select(Describe));
    }

    private static string Describe(GridSortDescription sort)
        => $"{sort.FieldName} ({(sort.Direction == GridSortDirection.Ascending ? "asc" : "desc")})";
}
