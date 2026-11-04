using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Automation;
using VCWpfControls.Grid.Primitives;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class IssueRegressionsRound4Tests
{
    private sealed class Instrument
    {
        public string? Code { get; set; }

        public decimal? Strike { get; set; }

        public List<string> Bars { get; set; } = new() { "Call 12", "Put 7" };
    }

    [StaFact]
    public void Issue13_HorizontalScroll_SetCurrentValueOnReadOnly_DoesNotThrow()
    {
        var grid = new GridControl { Width = 300, RowHeight = 24 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = 200 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = 200 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            var maxOffset = grid.Layout.TotalWidth - grid.ViewportWidth;
            Assert.True(maxOffset > 0, $"expected overflow, max={maxOffset}");

            // Scroll right: previously InvalidOperationException (read-only DP via SetCurrentValue).
            grid.SetHorizontalOffsetForTest(150);
            Assert.True(grid.HorizontalOffset > 0 && grid.HorizontalOffset <= maxOffset);

            // EnsureColumnVisible path (keyboard/current cell) uses the same core.
            grid.EnsureColumnVisible(grid.Columns[1]);
            Assert.True(grid.HorizontalOffset > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue12_EscapedCompositeFormat_DoesNotCrash()
    {
        var column = new GridNumericColumn { FieldName = "Strike", StringFormat = "{}{0:N0}" };
        var formatted = GridControl.FormatCellText(column, 1234567.89m);
        Assert.True(formatted.Contains('1'), $"formatted: '{formatted}'");
        Assert.DoesNotContain("{}", formatted);

        // Bare and already-composite forms keep working.
        Assert.Equal("42", GridControl.FormatCellText(new GridNumericColumn { StringFormat = "N0" }, 42m));
        Assert.Equal("42", GridControl.FormatCellText(new GridNumericColumn { StringFormat = "{0:N0}" }, 42m));
    }

    [StaFact]
    public void Issue12_EscapedFormat_InCellPeerName_AndBestFit()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridNumericColumn
        {
            FieldName = "Strike",
            Header = "Strike",
            StringFormat = "{}{0:N2}",
        });
        grid.ItemsSource = new[] { new Instrument { Strike = 3.25m } };
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var rowPeer = new GridRowAutomationPeer(row);
            var cellPeer = rowPeer.GetCellPeer(0);
            Assert.NotNull(cellPeer);
            var name = cellPeer!.GetName();
            Assert.Contains("3.25", name);   // formatted, not crashed
            Assert.DoesNotContain("{}", name);

            // BestFit goes through the same formatter and must not throw.
            grid.BestFitColumn(grid.Columns[0]);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue14_StarColumnsResize_ShowsScrollbar_AndStarsRespectMinWidth()
    {
        var grid = new GridControl { Width = 400, RowHeight = 24 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "A", Width = new GridColumnWidth(1, GridUnitType.Star) });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "B", Width = new GridColumnWidth(1, GridUnitType.Star), MinWidth = 80 });
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            var scrollbar = (System.Windows.Controls.Primitives.ScrollBar)grid.Template.FindName("PART_HorizontalScrollBar", grid)!;
            Assert.Equal(Visibility.Collapsed, scrollbar.Visibility); // fits exactly

            // User drags the resize thumb: star -> pixel 500, overflow -> scrollbar.
            GridControl.ResizeColumn(grid.Columns[0], 500);
            TestWindowHost.Pump();
            Assert.Equal(Visibility.Visible, scrollbar.Visibility);
            Assert.True(grid.Layout.TotalWidth > grid.ViewportWidth);

            // Stars clamp to their own MinWidth (not the internal 10px floor).
            Assert.True(grid.Layout.Slots[1].Width >= 80, $"star width {grid.Layout.Slots[1].Width} must respect MinWidth 80");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Issue11_TemplateSubtree_ItemsControlAndPanelContent_Reachable()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);

        // Root is a PANEL (peerless): TextBlock + ItemsControl with bar labels inside.
        var template = new DataTemplate();
        var root = new FrameworkElementFactory(typeof(StackPanel));
        var summary = new FrameworkElementFactory(typeof(TextBlock));
        summary.SetValue(TextBlock.TextProperty, "Summary line");
        root.AppendChild(summary);
        var items = new FrameworkElementFactory(typeof(ItemsControl));
        items.SetValue(ItemsControl.ItemsSourceProperty, new[] { "Call 12", "Put 7" });
        root.AppendChild(items);
        template.VisualTree = root;

        grid.Columns.Add(new GridTemplateColumn { FieldName = "Code", Header = "Out", CellTemplate = template });
        grid.ItemsSource = new[] { new Instrument { Code = "SI" } };
        TestWindowHost.Pump();

        try
        {
            var row = (GridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
            var cellPeer = new GridRowAutomationPeer(row).GetCellPeer(0);
            Assert.NotNull(cellPeer);

            var texts = CollectNames(cellPeer!);
            Assert.Contains("Summary line", texts);

            // ItemsControl items render via their own peers (hoisted through the panel).
            Assert.Contains("Call 12", texts);
            Assert.Contains("Put 7", texts);
        }
        finally
        {
            window.Close();
        }
    }

    private static List<string> CollectNames(System.Windows.Automation.Peers.AutomationPeer peer)
    {
        var names = new List<string> { peer.GetName() ?? string.Empty };
        var children = peer.GetChildren() ?? new List<System.Windows.Automation.Peers.AutomationPeer>();
        foreach (var child in children)
        {
            names.AddRange(CollectNames(child));
        }

        return names;
    }
}
