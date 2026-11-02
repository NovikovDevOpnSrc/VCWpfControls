using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

public class TreeListLazyTests
{
    private sealed class Item
    {
        public Item(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    [StaFact]
    public void ChildrenProvider_LoadsChildrenOnlyOnExpansion()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var providerCalls = 0;
            var tree = new TreeListView { RowHeight = 26 };
            window.Content = tree;

            var root = new Item("root");
            tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Item> { root };
            tree.HasChildrenPredicate = _ => true; // expander visible without loading
            tree.ChildrenProvider = item =>
            {
                providerCalls++;
                return new[] { new Item("child-1"), new Item("child-2") };
            };
            TestWindowHost.Pump();

            Assert.Single(tree.Items);            // root only, nothing loaded yet
            Assert.Equal(0, providerCalls);       // lazy: provider not called on build
            var rootRow = (GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.True(rootRow.TreeHasChildren); // hint drove the expander

            tree.ToggleNode(root, true);
            TestWindowHost.Pump();
            Assert.Equal(1, providerCalls);
            Assert.Equal(3, tree.Items.Count);    // root + two children

            // Collapse and expand again: cached, no second call.
            tree.ToggleNode(root, false);
            tree.ToggleNode(root, true);
            TestWindowHost.Pump();
            Assert.Equal(1, providerCalls);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void HasChildrenPredicate_False_HidesExpanderWithoutLoading()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var providerCalls = 0;
            var tree = new TreeListView { RowHeight = 26 };
            window.Content = tree;

            var leaf = new Item("leaf");
            tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Item> { leaf };
            tree.HasChildrenPredicate = _ => false;
            tree.ChildrenProvider = _ =>
            {
                providerCalls++;
                return null;
            };
            TestWindowHost.Pump();

            var row = (GridRow)tree.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.False(row.TreeHasChildren);
            Assert.Equal(0, providerCalls);

            // Expanding a childless node is a no-op view-wise.
            tree.ToggleNode(leaf, true);
            TestWindowHost.Pump();
            Assert.Single(tree.Items);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ProviderNullChildren_TreatedAsChildless()
    {
        var window = TestWindowHost.Show(new System.Windows.Controls.Grid());
        try
        {
            var tree = new TreeListView { RowHeight = 26 };
            window.Content = tree;

            var empty = new Item("empty");
            tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Item> { empty };
            tree.HasChildrenPredicate = _ => true; // optimistic hint...
            tree.ChildrenProvider = _ => null;     // ...but nothing to load
            TestWindowHost.Pump();

            tree.ToggleNode(empty, true);
            TestWindowHost.Pump();
            Assert.Single(tree.Items); // still just the node itself
        }
        finally
        {
            window.Close();
        }
    }
}
