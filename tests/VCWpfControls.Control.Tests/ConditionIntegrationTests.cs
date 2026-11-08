using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Conditions;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>Stage C: the condition editor wired into the grid (predicate + dialog) and the pivot.</summary>
public class ConditionIntegrationTests
{
    private static GridControl CreateGrid(out Window window)
    {
        var grid = new GridControl { Width = 500, RowHeight = 24, HeaderHeight = 26 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 150 });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 150 });
        window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(6);
        TestWindowHost.Pump();
        return grid;
    }

    [StaFact]
    public void CustomFilterPredicate_FiltersView_AndCombinesWithEngineFilters()
    {
        var grid = CreateGrid(out var window);
        try
        {
            var before = grid.Items.Count;
            Assert.True(before > 2);

            grid.CustomFilterPredicate = item => ((Employee)item!).Salary > 20000m;
            TestWindowHost.Pump();
            var afterPredicate = grid.Items.Count;
            Assert.True(afterPredicate < before);
            foreach (var row in grid.DataEngine.View)
            {
                if (row is GridDataRow data)
                {
                    Assert.True(((Employee)data.Item).Salary > 20000m);
                }
            }

            // ANDs with engine filters: an impossible engine filter empties the view.
            grid.FilterDescriptions.Add(new GridFilterDescription("Name", GridFilterOperator.Equals, "no such name"));
            TestWindowHost.Pump();
            Assert.Empty(grid.Items);

            // Clearing the predicate restores engine-only filtering.
            grid.FilterDescriptions.Clear();
            grid.CustomFilterPredicate = null;
            TestWindowHost.Pump();
            Assert.Equal(before, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void HeaderMenu_ConditionEntry_OpensDialog_OkApplies_ClearRemoves()
    {
        var grid = CreateGrid(out var window);
        var menu = grid.BuildColumnHeaderMenu(grid.Columns[0]);
        // The condition dialog lives under "Filter..." (preseeded with the column); the
        // old duplicate "Filter......" entry is gone.
        Assert.DoesNotContain(menu.Items.OfType<MenuItem>(),
            m => m.Header as string == GridStrings.FilterEllipsis + "...");
        Assert.Contains(menu.Items.OfType<MenuItem>(),
            m => m.Header as string == GridStrings.FilterEllipsis);
        try
        {
            var before = grid.Items.Count;

            var dialog = grid.OpenConditionDialogForTest();
            TestWindowHost.Pump();
            Assert.NotNull(dialog);
            var builder = grid.ConditionBuilderForTest;
            Assert.NotNull(builder);

            // Build [Salary] > 50000 via the text tab: nothing matches -> empty grid on OK.
            builder!.IsTextMode = true;
            TestWindowHost.Pump();
            var box = FindTextBoxes(builder).First(t => t.Width != 130);
            box.Text = "[Salary] > 50000";
            box.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
                PresentationSource.FromDependencyObject(box)!, 0, System.Windows.Input.Key.Enter)
            {
                RoutedEvent = UIElement.KeyDownEvent,
                Source = box,
            });
            TestWindowHost.Pump();
            Assert.Equal(string.Empty, builder.ErrorText);

            ClickButton(dialog!, GridStrings.FilterMenuOk);
            TestWindowHost.Pump();
            Assert.Null(grid.ConditionDialogForTest);
            Assert.NotNull(grid.CustomFilterPredicate);
            Assert.Empty(grid.Items);
            Assert.False(grid.ConditionTreeForTest!.IsEmpty);

            // Reopen and Clear restores everything.
            grid.OpenConditionDialogForTest();
            TestWindowHost.Pump();
            ClickButton(grid.ConditionDialogForTest!, GridStrings.FilterMenuClear);
            TestWindowHost.Pump();
            Assert.Null(grid.CustomFilterPredicate);
            Assert.Equal(before, grid.Items.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void HeaderMenu_ConditionDialog_CancelKeepsFilter()
    {
        var grid = CreateGrid(out var window);
        try
        {
            grid.CustomFilterPredicate = _ => false;
            TestWindowHost.Pump();
            Assert.Empty(grid.Items);

            grid.OpenConditionDialogForTest();
            TestWindowHost.Pump();
            ClickButton(grid.ConditionDialogForTest!, GridStrings.WizardCancel);
            TestWindowHost.Pump();

            Assert.NotNull(grid.CustomFilterPredicate); // untouched
            Assert.Empty(grid.Items);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PivotGrid_Condition_FiltersSourceRows()
    {
        var sales = new[]
        {
            new Sale("East", "A", 10m),
            new Sale("East", "B", 20m),
            new Sale("West", "A", 5m),
        };
        var pivot = new PivotGrid { Width = 500, RowHeight = 26, HeaderHeight = 30 };
        pivot.RowFields.Add("Region");
        pivot.ColumnFields.Add("Product");
        pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
        var window = TestWindowHost.Show(pivot);
        pivot.PivotItemsSource = sales;
        TestWindowHost.Pump();
        try
        {
            Assert.Equal(35m, pivot.Result!.GrandTotals[0]);

            pivot.Condition = new ConditionGroup(ConditionGroupOperator.And)
            {
                Children = { new ConditionExpression("Region", GridFilterOperator.Equals, "East") },
            };
            TestWindowHost.Pump();
            Assert.Single(pivot.Result!.Rows);      // East only
            Assert.Equal(30m, pivot.Result.GrandTotals[0]);

            pivot.Condition = null;
            TestWindowHost.Pump();
            Assert.Equal(35m, pivot.Result!.GrandTotals[0]); // restored
        }
        finally
        {
            window.Close();
        }
    }

    private sealed record Sale(string Region, string Product, decimal Amount);

    private static List<TextBox> FindTextBoxes(DependencyObject root)
    {
        var result = new List<TextBox>();
        void Walk(DependencyObject node)
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
                if (child is TextBox box)
                {
                    result.Add(box);
                }

                Walk(child);
            }
        }

        Walk(root);
        return result;
    }

    private static void ClickButton(Window dialog, string caption)
    {
        Button Find(DependencyObject node)
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
                if (child is Button button && Equals(button.Content as string, caption))
                {
                    return button;
                }

                if (Find(child) is { } nested)
                {
                    return nested;
                }
            }

            return null!;
        }

        Find(dialog).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
