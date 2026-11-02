using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.Grid.Columns;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class TreeListViewTests
{
    private sealed class OrgUnit
    {
        public OrgUnit(string name, int headcount)
        {
            Name = name;
            Headcount = headcount;
        }

        public string Name { get; set; }

        public int Headcount { get; set; }

        public List<OrgUnit> Children { get; } = new();
    }

    private static ObservableCollection<OrgUnit> CreateOrg()
    {
        var engineering = new OrgUnit("Engineering", 50);
        engineering.Children.Add(new OrgUnit("Platform", 20));
        engineering.Children.Add(new OrgUnit("Apps", 30));
        var apps = engineering.Children[1];
        apps.Children.Add(new OrgUnit("Mobile", 10));
        apps.Children.Add(new OrgUnit("Desktop", 20));

        var sales = new OrgUnit("Sales", 10);
        sales.Children.Add(new OrgUnit("EMEA", 6));
        sales.Children.Add(new OrgUnit("APAC", 4));

        return new ObservableCollection<OrgUnit> { engineering, sales };
    }

    private static TreeListView CreateTree(Window window)
    {
        var tree = new TreeListView { ChildMemberPath = "Children", RowHeight = 28 };
        window.Content = tree;
        tree.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Unit", Width = new GridColumnWidth(2, GridUnitType.Star) });
        tree.Columns.Add(new GridNumericColumn { FieldName = "Headcount", Header = "Headcount", Width = 120 });
        tree.ItemsSource = CreateOrg();
        TestWindowHost.Pump();
        return tree;
    }

    [StaFact]
    public void RootsOnly_WhenCollapsed()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            Assert.Equal(2, tree.Items.Count); // two roots, children hidden
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Expand_RevealsChildren_AndCollapseHidesThem()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var engineering = (OrgUnit)((Grid.Data.GridDataRow)tree.Items[0]!).Item;

            tree.ToggleNode(engineering, expanded: true);
            TestWindowHost.Pump();
            Assert.Equal(4, tree.Items.Count); // 2 roots + 2 engineering children
            Assert.True(tree.IsExpanded(engineering));

            tree.ToggleNode(engineering, expanded: false);
            TestWindowHost.Pump();
            Assert.Equal(2, tree.Items.Count);
            Assert.False(tree.IsExpanded(engineering));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DeepExpansion_KeepsHierarchyOrder()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var engineering = (OrgUnit)((Grid.Data.GridDataRow)tree.Items[0]!).Item;

            tree.ToggleNode(engineering, true);
            var apps = engineering.Children[1];
            tree.ToggleNode(apps, true);
            TestWindowHost.Pump();

            Assert.Equal(6, tree.Items.Count); // 2 roots + Eng + Platform + Apps + Mobile + Desktop
            var names = tree.Items.Cast<Grid.Data.GridDataRow>().Select(r => ((OrgUnit)r.Item).Name).ToList();
            Assert.Equal(new List<string> { "Engineering", "Platform", "Apps", "Mobile", "Desktop", "Sales" }, names);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ExpandAll_CollapseAll()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            tree.ExpandAll();
            TestWindowHost.Pump();
            Assert.Equal(8, tree.Items.Count); // 2 roots + 2 + 2 + 2

            tree.CollapseAll();
            TestWindowHost.Pump();
            Assert.Equal(2, tree.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Rows_CarryTreeState()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            var engineering = (OrgUnit)((Grid.Data.GridDataRow)tree.Items[0]!).Item;
            tree.ToggleNode(engineering, true);
            TestWindowHost.Pump();

            var rootRow = (GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0);

            Assert.True(rootRow.TreeHasChildren);
            Assert.True(rootRow.TreeIsExpanded);
            Assert.Equal(0, rootRow.TreeLevel);

            // Index 1 is the first child of Engineering ("Apps" — sortable order may change it),
            // index 2 its first child: level 2.
            var childRow = (GridRow)tree.ItemContainerGenerator.ContainerFromIndex(1);
            Assert.Equal(1, childRow.TreeLevel);

            tree.ExpandAll();
            TestWindowHost.Pump();
            // Natural (unsorted) order: Engineering, Platform, Apps, Mobile, Desktop, ...
            var grandChildRow = (GridRow)tree.ItemContainerGenerator.ContainerFromIndex(3);
            Assert.Equal(2, grandChildRow.TreeLevel);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Sorting_OrdersSiblings()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = CreateTree(window);
            tree.ExpandAll();
            TestWindowHost.Pump();

            tree.SortBy("Headcount", Grid.Data.GridSortDirection.Descending);
            TestWindowHost.Pump();

            var headcounts = tree.Items.Cast<Grid.Data.GridDataRow>().Select(r => ((OrgUnit)r.Item).Headcount).ToList();
            // Roots sorted: Engineering(50) before Sales(10); children sorted within each parent.
            // DFS with siblings sorted descending within every parent:
            // Engineering 50, Apps 30, [Desktop 20, Mobile 10], Platform 20, Sales 10, [EMEA 6, APAC 4]
            Assert.Equal(new List<int> { 50, 30, 20, 10, 20, 10, 6, 4 }, headcounts);
        }
        finally
        {
            window.Close();
        }
    }
}
