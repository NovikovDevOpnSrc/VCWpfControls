using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The first column's node text must render right of the expander toggle and the node
/// check box: the indent used to be level*16 only, so the text ran straight under the
/// toggle/check box drawn on top of the cells panel.
/// </summary>
public class TreeIndentTests
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

    [StaFact]
    public void NodeText_ClearsToggleAndCheckBox()
    {
        var root = new Node("root");
        var a = new Node("a");
        a.Children.Add(new Node("a1"));
        root.Children.Add(a);

        var tree = new TreeListView
        {
            ChildMemberPath = "Children",
            RowHeight = 26,
            ShowNodeCheckBoxes = true,
        };
        var window = TestWindowHost.Show(new Canvas());
        window.Content = tree;
        tree.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<Node> { root };
        TestWindowHost.Pump();
        tree.ExpandAll();
        TestWindowHost.Pump();

        try
        {
            foreach (var item in tree.Items)
            {
                var row = (GridRow)tree.ItemContainerGenerator.ContainerFromItem(item);
                Assert.NotNull(row);

                var toggle = (ToggleButton)row.Template.FindName("PART_TreeToggle", row)!;
                var checkBox = (CheckBox)row.Template.FindName("PART_NodeCheckBox", row)!;
                var panel = (GridCellsPanel)row.Template.FindName("PART_Cells", row)!;
                var firstColumn = tree.VisibleColumns[0];
                var cell = panel.GetCellElement(firstColumn);
                Assert.NotNull(cell);

                var rowOrigin = row.TranslatePoint(new Point(0, 0), tree).X;
                var textX = cell!.TranslatePoint(new Point(0, 0), tree).X;
                var toggleRight = toggle.TranslatePoint(new Point(toggle.ActualWidth, 0), tree).X;
                var checkBoxRight = checkBox.TranslatePoint(new Point(checkBox.ActualWidth, 0), tree).X;

                Assert.True(textX > toggleRight + 1,
                    $"level {row.TreeLevel}: node text at {textX - rowOrigin:F0} overlaps the toggle (ends {toggleRight - rowOrigin:F0})");
                Assert.True(textX > checkBoxRight + 1,
                    $"level {row.TreeLevel}: node text at {textX - rowOrigin:F0} overlaps the check box (ends {checkBoxRight - rowOrigin:F0})");
            }
        }
        finally
        {
            window.Close();
        }
    }
}
