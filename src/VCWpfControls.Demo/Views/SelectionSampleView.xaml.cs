using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;

namespace VCWpfControls.Demo.Views;

/// <summary>Selection sample: binds SelectedItems count to the view model.</summary>
public partial class SelectionSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public SelectionSampleView()
    {
        InitializeComponent();
        Grid.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "SelectionGrid");
        Grid.SelectionChanged += (_, _) =>
        {
            if (DataContext is SelectionSampleViewModel vm)
            {
                vm.SelectedCount = Grid.SelectedItems.Count;
            }
        };
    }
}
