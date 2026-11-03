using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class TreeListNodeCheckBoxTests
{
    private sealed class Node
    {
        public Node(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public List<Node> Children { get; } = new();
    }

    private static Node Root { get; } = BuildRoot();

    private static Node BuildRoot()
    {
        var root = new Node("root");
        var a = new Node("a");
        var a1 = new Node("a1");
        var a2 = new Node("a2");
        a.Children.Add(a1);
        a.Children.Add(a2);
        var b = new Node("b");
        root.Children.Add(a);
        root.Children.Add(b);
        return root;
    }

    private static TreeListView CreateTree(Window window, bool showCheckBoxes = true)
    {
        var tree = new TreeListView
        {
            ChildMemberPath = "Children",
            RowHeight = 26,
            ShowNodeCheckBoxes = showCheckBoxes,
        };
        window.Content = tree;
        tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Node> { Root };
        TestWindowHost.Pump();
        return tree;
    }

    private static GridRow RowOf(TreeListView tree, Node node)
    {
        for (var i = 0; i < tree.Items.Count; i++)
        {
            if (tree.ItemContainerGenerator.ContainerFromIndex(i) is GridRow row
                && ReferenceEquals(row.DataItem, node))
            {
                return row;
            }
        }

        throw new InvalidOperationException($"row of '{node.Name}' not realized");
    }

    [StaFact]
    public void ShowNodeCheckBoxes_ExposesCheckBoxOnRows()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var rootRow = RowOf(tree, Root);
            Assert.True(rootRow.HasNodeCheckBox);
            Assert.False(rootRow.NodeIsChecked!.Value);

            var checkbox = rootRow.Template.FindName("PART_NodeCheckBox", rootRow) as System.Windows.Controls.CheckBox;
            Assert.NotNull(checkbox);
            Assert.Equal(System.Windows.Visibility.Visible, checkbox!.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DisabledByDefault_NoCheckBoxes()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window, showCheckBoxes: false);
            var rootRow = RowOf(tree, Root);
            Assert.False(rootRow.HasNodeCheckBox);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CheckNode_CascadesToAllDescendants_AndUncheckClears()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            tree.ExpandAll();
            TestWindowHost.Pump();

            var raised = 0;
            tree.NodeCheckChanged += (_, _) => raised++;

            tree.SetNodeChecked(Root, true);
            TestWindowHost.Pump();

            Assert.Equal(1, raised);
            Assert.True(tree.IsNodeChecked(Root));
            Assert.True(tree.IsNodeChecked(Root.Children[0]));
            Assert.True(tree.IsNodeChecked(Root.Children[0].Children[1]));
            Assert.True(tree.IsNodeChecked(Root.Children[1]));

            // Row visuals tri-state true for root after cascade.
            Assert.True(RowOf(tree, Root).NodeIsChecked!.Value);

            tree.SetNodeChecked(Root.Children[0].Children[0], false);
            TestWindowHost.Pump();

            // Parent of the unchecked leaf is indeterminate; the root is indeterminate too.
            Assert.False(tree.IsNodeChecked(Root.Children[0].Children[0]));
            Assert.Null(RowOf(tree, Root.Children[0]).NodeIsChecked);
            Assert.Null(RowOf(tree, Root).NodeIsChecked);

            tree.SetNodeChecked(Root, false);
            TestWindowHost.Pump();
            Assert.Empty(tree.CheckedItems);
            Assert.False(RowOf(tree, Root).NodeIsChecked!.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CheckedItems_TrackVisibleCheckedNodes()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var a = Root.Children[0];
            tree.SetNodeChecked(a, true);
            TestWindowHost.Pump();

            var checkedItems = tree.CheckedItems;
            Assert.Single(checkedItems);
            Assert.Same(a, checkedItems[0]);

            // Collapse state persistence: rebuild keeps the check state.
            tree.CollapseAll();
            tree.ExpandAll();
            TestWindowHost.Pump();
            Assert.True(tree.IsNodeChecked(a.Children[1]));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void IndeterminateParent_ClickChecksEverything()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            tree.ExpandAll();
            TestWindowHost.Pump();

            var a = Root.Children[0];
            tree.SetNodeChecked(a.Children[0], true);
            TestWindowHost.Pump();
            Assert.Null(RowOf(tree, a).NodeIsChecked);

            // Simulate the row check box click on the indeterminate parent.
            RowOf(tree, a).NodeIsChecked = true;
            tree.SetNodeChecked(a, true);
            TestWindowHost.Pump();

            Assert.True(tree.IsNodeChecked(a.Children[0]));
            Assert.True(tree.IsNodeChecked(a.Children[1]));
        }
        finally
        {
            window.Close();
        }
    }
}
