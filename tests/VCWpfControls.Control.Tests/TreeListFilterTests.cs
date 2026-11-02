using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid.Data;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class TreeListFilterTests
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

    private static TreeListView CreateTree(Window window)
    {
        var tree = new TreeListView { ChildMemberPath = "Children", RowHeight = 26 };
        window.Content = tree;
        var root = new Node("root");
        var a = new Node("alpha");
        a.Children.Add(new Node("target-one"));
        a.Children.Add(new Node("other"));
        var b = new Node("beta");
        b.Children.Add(new Node("target-two"));
        root.Children.Add(a);
        root.Children.Add(b);
        tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Node> { root };
        TestWindowHost.Pump();
        return tree;
    }

    private static List<string> Names(TreeListView tree)
        => tree.Items.Cast<GridDataRow>().Select(r => ((Node)r.Item).Name).ToList();

    [StaFact]
    public void Filter_ShowsMatchesAndAncestors_AutoExpanded()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            Assert.Single(Names(tree)); // root only, collapsed

            tree.FilterPredicate = item => ((Node)item).Name.StartsWith("target", StringComparison.Ordinal);
            TestWindowHost.Pump();

            // root + alpha + beta expanded, both matching children visible, "other" hidden.
            Assert.Equal(new List<string> { "root", "alpha", "target-one", "beta", "target-two" }, Names(tree));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void FilterCleared_RestoresManualExpansionState()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var root = (Node)((GridDataRow)tree.Items[0]).Item;
            tree.ToggleNode(root, true);
            TestWindowHost.Pump();
            Assert.Equal(3, tree.Items.Count); // root + alpha + beta

            tree.FilterPredicate = item => ((Node)item).Name.Contains("two", StringComparison.Ordinal);
            TestWindowHost.Pump();
            Assert.Equal(new List<string> { "root", "beta", "target-two" }, Names(tree));

            tree.FilterPredicate = null;
            TestWindowHost.Pump();
            // Manual state (root expanded, alpha collapsed) is back.
            Assert.Equal(new List<string> { "root", "alpha", "beta" }, Names(tree));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Filter_NoMatches_EmptyView()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            tree.ExpandAll();
            TestWindowHost.Pump();

            tree.FilterPredicate = _ => false;
            TestWindowHost.Pump();
            Assert.Empty(tree.Items);

            tree.FilterPredicate = null;
            TestWindowHost.Pump();
            Assert.Equal(6, tree.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }
}
