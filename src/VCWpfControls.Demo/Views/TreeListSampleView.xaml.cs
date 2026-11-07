using System.Windows;
using VCWpfControls.TreeList;
using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>TreeListView sample: an organization chart with columns.</summary>
public partial class TreeListSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public TreeListSampleView()
    {
        InitializeComponent();
        Tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Headcount", VCWpfControls.Localization.GridSummaryFunction.Sum));
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "TreeListSample");
        Tree.ShowNodeCheckBoxes = true;
        Tree.NodeCheckChanged += (_, _) => UpdateCheckedCount();

        // Set in code: IsChecked="True" in XAML would raise Checked during
        // InitializeComponent while the Tree field is still null.
        NodeCheckBoxes.IsChecked = true;
    }

    private void OnExpandAll(object sender, RoutedEventArgs e) => Tree.ExpandAll();

    private void OnCollapseAll(object sender, RoutedEventArgs e) => Tree.CollapseAll();

    private void OnCheckBoxesToggled(object sender, RoutedEventArgs e)
    {
        Tree.ShowNodeCheckBoxes = NodeCheckBoxes.IsChecked == true;
        UpdateCheckedCount();
    }

    private void UpdateCheckedCount()
    {
        var count = Tree.ShowNodeCheckBoxes ? Tree.CheckedItems.Count : 0;
        CheckedCount.Text = count == 0 ? string.Empty : $"Selected units: {count}";
    }
}
