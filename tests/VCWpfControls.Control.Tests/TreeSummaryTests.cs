using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Localization;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Per-level tree summaries (<see cref="TreeListView.TreeSummaryItems"/>): subtree vs
/// direct-children aggregation, level filters, expansion-independence, leaf rows without
/// summaries, and invalidation on collection changes.
/// </summary>
public class TreeSummaryTests
{
    private sealed class Node
    {
        public Node(string name, decimal amount, params Node[] children)
        {
            Name = name;
            Amount = amount;
            Children = children;
        }

        public string Name { get; set; }

        public decimal Amount { get; set; }

        public Node[] Children { get; }
    }

    private static TreeListView CreateTree(out Window window, out Node root)
    {
        // root(10) ─ a(20) ─ a1(5)
        //                 └ a2(7)
        //        └ b(30)
        root = new Node("root", 10m,
            new Node("a", 20m, new Node("a1", 5m), new Node("a2", 7m)),
            new Node("b", 30m));
        var tree = new TreeListView { ChildMemberPath = "Children", RowHeight = 26, Width = 420 };
        tree.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Node", Width = 160 });
        tree.Columns.Add(new GridNumericColumn { FieldName = "Amount", Header = "Amount", Width = 120 });
        window = TestWindowHost.Show(tree);
        tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Node> { root };
        TestWindowHost.Pump();
        return tree;
    }

    private static string? TextOf(TreeListView tree, object item) => tree.GetTreeSummaryText(item);

    [StaFact]
    public void SubtreeSum_AggregatesAllDescendants()
    {
        var tree = CreateTree(out var window, out var root);
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Sum));
        try
        {
            var rootText = TextOf(tree, root);
            Assert.NotNull(rootText);
            Assert.Contains("62", rootText); // 20+5+7+30

            var a = root.Children[0];
            Assert.Contains("12", TextOf(tree, a)); // descendants only: 5+7 (the node itself is excluded)

            var a1 = a.Children[0];
            Assert.Null(TextOf(tree, a1)); // leaf: no summary
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ChildrenMode_OnlyDirectChildren()
    {
        var tree = CreateTree(out var window, out var root);
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Sum)
        {
            Mode = GridTreeSummaryMode.Children,
        });
        try
        {
            Assert.Contains("50", TextOf(tree, root)); // 20+30 (not the grandchildren)
            var a = root.Children[0];
            Assert.Contains("12", TextOf(tree, a)); // 5+7
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void LevelFilter_RestrictsRows()
    {
        var tree = CreateTree(out var window, out var root);
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Count) { Level = 0 });
        try
        {
            Assert.NotNull(TextOf(tree, root)); // level 0 shows
            Assert.Null(TextOf(tree, root.Children[0])); // level 1 hidden
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SubtreeSum_IndependentOfExpansion()
    {
        var tree = CreateTree(out var window, out var root);
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Sum));
        try
        {
            tree.CollapseAll();
            TestWindowHost.Pump();
            Assert.Contains("62", TextOf(tree, root));

            tree.ExpandAll();
            TestWindowHost.Pump();
            Assert.Contains("62", TextOf(tree, root));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RemovingSummaryItem_ClearsText()
    {
        var tree = CreateTree(out var window, out var root);
        var item = new GridTreeSummaryItem("Amount", GridSummaryFunction.Sum);
        tree.TreeSummaryItems.Add(item);
        try
        {
            Assert.Contains("62", TextOf(tree, root));
            tree.TreeSummaryItems.Remove(item);
            TestWindowHost.Pump();
            Assert.Null(TextOf(tree, root));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MinMaxAvg_AllFunctionsWork()
    {
        var tree = CreateTree(out var window, out var root);
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Min));
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Max));
        tree.TreeSummaryItems.Add(new GridTreeSummaryItem("Amount", GridSummaryFunction.Avg));
        try
        {
            var text = TextOf(tree, root);
            Assert.NotNull(text);
            Assert.Contains("5", text!);   // min
            Assert.Contains("30", text);   // max
            Assert.Contains("15.5", text); // avg of 20,5,7,30
        }
        finally
        {
            window.Close();
        }
    }
}
