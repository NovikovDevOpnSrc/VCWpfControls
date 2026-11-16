using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Fixes from the sixth acceptance round (TreeList keyboard/a11y/targets + summary
/// honesty): node checkboxes and expanders were mouse-only and unnamed with 16px targets;
/// tree summaries ignored the selection inversion and overprinted the last column's
/// content.
/// </summary>
public class TreeAcceptanceTests
{
    public sealed class Node
    {
        public Node(string name) => Name = name;

        public string Name { get; }

        public ObservableCollection<Node> Children { get; } = new();
    }

    private static TreeListView CreateTree(out Window window, bool checkboxes = true)
    {
        var tree = new TreeListView { Width = 420, Height = 220, ChildMemberPath = "Children", ShowNodeCheckBoxes = checkboxes };
        tree.Columns.Add(new GridTextColumn { FieldName = "Name", Width = new GridColumnWidth(200, GridUnitType.Pixel) });
        tree.Columns.Add(new GridTextColumn { FieldName = "Headcount", Width = new GridColumnWidth(120, GridUnitType.Pixel) });
        var root = new Node("Root");
        root.Children.Add(new Node("Child one"));
        tree.ItemsSource = new ObservableCollection<Node> { root };
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Name", GridSummaryFunction.Count) { Mode = GridTreeSummaryMode.Subtree });
        window = TestWindowHost.Show(tree);
        TestWindowHost.Pump();
        return tree;
    }

    private static void SendKey(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target);
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.PreviewKeyDownEvent,
            Source = target,
        });
    }

    [StaFact]
    public void TreeKeyboard_RightLeftExpandCollapse_SpaceTogglesCheck()
    {
        var tree = CreateTree(out var window);
        try
        {
            var root = ((ObservableCollection<Node>)tree.ItemsSource!)[0];
            tree.SetCurrentCellForTest(((Grid.Data.GridDataRow)tree.DataEngine.View[0]).Item, tree.Columns[0]);
            TestWindowHost.Pump();
            Assert.False(tree.IsExpanded(root));

            SendKey(tree, Key.Right);
            TestWindowHost.Pump();
            Assert.True(tree.IsExpanded(root), "Right must expand the current node");

            SendKey(tree, Key.Left);
            TestWindowHost.Pump();
            Assert.False(tree.IsExpanded(root), "Left must collapse the current node");

            Assert.False(tree.IsNodeChecked(root));
            SendKey(tree, Key.Space); // in the FIRST (tree) column
            TestWindowHost.Pump();
            Assert.True(tree.IsNodeChecked(root), "Space in the tree column must check the node");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TreeChrome_NamedAnd24px()
    {
        var tree = CreateTree(out var window);
        try
        {
            var boxes = new List<CheckBox>();
            Collect(tree, boxes);
            var box = Assert.Single(boxes);
            Assert.True(box.ActualWidth >= 24 && box.ActualHeight >= 24,
                $"node checkbox {box.ActualWidth:F0}x{box.ActualHeight:F0} < 24 (WCAG floor)");
            Assert.False(string.IsNullOrEmpty(AutomationProperties.GetName(box)));

            // The row's OWN tree expander (header filter buttons are 20px — out of scope).
            var row = (Grid.Primitives.GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0);
            var expander = Assert.IsType<System.Windows.Controls.Primitives.ToggleButton>(
                row.Template.FindName("PART_TreeToggle", row));
            Assert.True(expander.ActualWidth >= 24 && expander.ActualHeight >= 24,
                $"expander {expander.ActualWidth:F0}x{expander.ActualHeight:F0} < 24");
            Assert.False(string.IsNullOrEmpty(AutomationProperties.GetName(expander)));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RowPeer_AnnouncesSummaryAndCheckState()
    {
        var tree = CreateTree(out var window);
        try
        {
            var root = ((ObservableCollection<Node>)tree.ItemsSource!)[0];
            var row = (Grid.Primitives.GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0);
            var peer = UIElementAutomationPeer.CreatePeerForElement(row);
            var name = peer?.GetName() ?? string.Empty;
            Assert.Contains("Root", name);
            Assert.DoesNotContain(VCWpfControls.Localization.GridStrings.TreeNodeChecked, name);

            tree.SetNodeChecked(root, true);
            TestWindowHost.Pump();
            var nameAfter = UIElementAutomationPeer.CreatePeerForElement(
                (Grid.Primitives.GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0))?.GetName() ?? string.Empty;
            Assert.Contains(VCWpfControls.Localization.GridStrings.TreeNodeChecked, nameAfter);
            var summary = tree.GetTreeSummaryText(root);
            Assert.NotNull(summary);
            Assert.Contains(summary, nameAfter); // the OnRender summary is announced
        }
        finally
        {
            window.Close();
        }
    }

    private static void Collect(DependencyObject root, List<CheckBox> found)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is CheckBox c)
            {
                found.Add(c);
            }

            Collect(child, found);
        }
    }

    private static void CollectToggles(DependencyObject root, List<System.Windows.Controls.Primitives.ToggleButton> found)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is System.Windows.Controls.Primitives.ToggleButton t)
            {
                found.Add(t);
            }

            CollectToggles(child, found);
        }
    }
}
