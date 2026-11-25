using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.PropertyGrid;
using VCWpfControls.TreeList;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The DevExpress-style live property explorer contract: a PropertyGrid with
/// <see cref="PropertyGrid.ShowDeclaredPropertiesOnly"/> bound to a LIBRARY control
/// instance lists that control's API (the whole same-assembly hierarchy — TreeListView
/// includes GridControl's properties) without the inherited ItemsControl noise, and
/// edits land on the live instance (dependency properties update the control directly).
/// </summary>
public sealed class LivePropertyExplorerTests
{
    private static GridControl MakeGrid()
    {
        var grid = new GridControl { Width = 600, Height = 280, AutoGenerateColumns = true };
        grid.ItemsSource = new ObservableCollection<TestModels.Employee>(TestModels.CreateEmployees(4));
        return grid;
    }

    [StaFact]
    public void DeclaredOnly_ListsTheControlApi_WithoutPlatformNoise()
    {
        var pg = new VCWpfControls.PropertyGrid.PropertyGrid { Width = 300, Height = 400, ShowDeclaredPropertiesOnly = true };
        var grid = MakeGrid();
        pg.SelectedObject = grid;

        var window = TestWindowHost.Show(pg);
        TestWindowHost.Pump();
        try
        {
            var names = pg.Items.Select(i => i.DisplayName).ToHashSet();

            // The grid's own API is there — checkable/editable live.
            Assert.Contains("CanUserSortColumns", names);
            Assert.Contains("SelectionMode", names);
            Assert.Contains("RowHeight", names);
            Assert.Contains("ShowGroupPanel", names);

            // The inherited ItemsControl/Control surface stays out.
            Assert.DoesNotContain("Items", names);
            Assert.DoesNotContain("Background", names);
            Assert.DoesNotContain("ItemTemplate", names);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DeclaredOnly_FollowsTheSameAssemblyHierarchy()
    {
        // TreeListView extends GridControl INSIDE the library: its property list must
        // carry the grid's API too (ChildMemberPath + RowHeight together).
        var items = PropertyItemBuilder.Build(new TreeListView(), declaredPropertiesOnly: true);
        var names = items.Select(i => i.DisplayName).ToHashSet();
        Assert.Contains("ChildMemberPath", names); // TreeListView's own
        Assert.Contains("RowHeight", names); // GridControl's, same assembly
        Assert.DoesNotContain("Items", names); // platform noise excluded
    }

    [StaFact]
    public void Edit_LandsOnTheLiveInstance()
    {
        var pg = new VCWpfControls.PropertyGrid.PropertyGrid { Width = 300, Height = 400 };
        var grid = MakeGrid();
        pg.SelectedObject = grid;

        var window = TestWindowHost.Show(pg);
        TestWindowHost.Pump();
        try
        {
            Assert.True(grid.ShowGroupPanel);

            var item = Assert.Single(pg.Items, i => i.DisplayName == "ShowGroupPanel");
            item.SetValue(false);
            TestWindowHost.Pump();
            Assert.False(grid.ShowGroupPanel); // the live instance changed

            var mode = Assert.Single(pg.Items, i => i.DisplayName == "SelectionMode");
            mode.SetValue(GridSelectionMode.SingleRow);
            TestWindowHost.Pump();
            Assert.Equal(GridSelectionMode.SingleRow, grid.SelectionMode);
        }
        finally
        {
            window.Close();
        }
    }
}