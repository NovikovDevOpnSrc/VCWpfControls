using System.Windows;
using System.Windows.Controls;

namespace VCWpfControls.Demo.Views;

/// <summary>TreeListView sample: an organization chart with columns.</summary>
public partial class TreeListSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public TreeListSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "TreeListSample");
    }

    private void OnExpandAll(object sender, RoutedEventArgs e) => Tree.ExpandAll();

    private void OnCollapseAll(object sender, RoutedEventArgs e) => Tree.CollapseAll();
}
