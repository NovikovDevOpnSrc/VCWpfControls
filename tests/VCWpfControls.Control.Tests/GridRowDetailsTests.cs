using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridRowDetailsTests
{
    private static DataTemplate CreateNameTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        return new DataTemplate { VisualTree = factory };
    }

    [StaFact]
    public void VisibleWhenSelected_DetailsFollowSelection()
    {
        var grid = new GridControl
        {
            RowDetailsTemplate = CreateNameTemplate(),
            RowDetailsVisibilityMode = GridRowDetailsVisibilityMode.VisibleWhenSelected,
        };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(6);
        TestWindowHost.Pump();

        try
        {
            var first = ((GridDataRow)grid.DataEngine.View[0]).Item;
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var details = (ContentPresenter)row.Template.FindName("PART_RowDetails", row);

            Assert.Equal(Visibility.Collapsed, details.Visibility);

            grid.SelectItem(first);
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Visible, details.Visibility);
            Assert.Equal(first, details.Content);

            grid.UnselectItem(first);
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Collapsed, details.Visibility);
            Assert.Null(details.Content);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void VisibleMode_DetailsShownForAllRows()
    {
        var grid = new GridControl
        {
            RowDetailsTemplate = CreateNameTemplate(),
            RowDetailsVisibilityMode = GridRowDetailsVisibilityMode.Visible,
        };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(4);
        TestWindowHost.Pump();

        try
        {
            for (var i = 0; i < 4; i++)
            {
                var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(i);
                var details = (ContentPresenter)row.Template.FindName("PART_RowDetails", row);
                Assert.Equal(Visibility.Visible, details.Visibility);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NoTemplate_DetailsStayCollapsed()
    {
        var grid = new GridControl
        {
            RowDetailsVisibilityMode = GridRowDetailsVisibilityMode.Visible,
        };
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var details = (ContentPresenter)row.Template.FindName("PART_RowDetails", row);
            Assert.Equal(Visibility.Collapsed, details.Visibility);
        }
        finally
        {
            window.Close();
        }
    }
}
