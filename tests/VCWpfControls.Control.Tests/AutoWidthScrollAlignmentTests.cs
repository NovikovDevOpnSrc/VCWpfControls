using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The 100k-page scroll bleed: when a recycled row reveals WIDER content, the auto column
/// grows — but the already-visible rows were never invalidated (their RenderSize did not
/// change), so they kept the old cell positions while recycled rows arranged at the new
/// ones: content visibly slides under the neighboring columns while scrolling.
/// `NotifyAutoWidthsChanged` (previously dead code — no callers) is now raised by the
/// cells/headers panels whenever an auto width actually grew. These tests pin the
/// alignment invariant across recycling.
/// </summary>
public sealed class AutoWidthScrollAlignmentTests
{
    private sealed class Row
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private static GridControl MakeGrid()
    {
        var rows = new ObservableCollection<Row>();
        for (var i = 0; i < 2000; i++)
        {
            // Far-down rows carry a MUCH longer Name: its auto column grows the moment
            // such a row first realizes during scrolling.
            rows.Add(new Row { Id = i, Name = i > 1500 ? $"LONGROWNAME-{i:D6}-X" : $"N{i}" });
        }

        var grid = new GridControl
        {
            Width = 900,
            Height = 260,
            AutoGenerateColumns = true,
            RowHeight = 26,
            FrozenColumnCount = 1,
        };
        grid.ItemsSource = rows;
        return grid;
    }

    [StaFact]
    public void ScrollingIntoWiderRows_AllRowsStayInCurrentSlots()
    {
        var grid = MakeGrid();
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            // Scroll deep into the long-name region: recycling realizes wide rows.
            grid.ScrollHost?.ScrollToVerticalOffset(26 * 1800);
            for (var i = 0; i < 4; i++)
            {
                TestWindowHost.Pump();
            }

            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");
            var slot = grid.Layout.GetSlot(nameColumn);
            Assert.NotNull(slot);

            var panels = FindVisualChildren<Grid.Primitives.GridCellsPanel>(grid).ToList();
            Assert.True(panels.Count >= 5, $"expected realized rows, got {panels.Count}");

            // Cell elements carry their own margins inside the slot, so the invariant is:
            // (a) every realized row's Name cell sits INSIDE the current slot bounds, and
            // (b) ALL rows agree on the exact position — a stale row (never invalidated
            // after the auto-width growth) would keep the old, narrower-column X.
            var positions = new List<double>();
            foreach (var panel in panels)
            {
                var cell = panel.GetCellElement(nameColumn);
                Assert.NotNull(cell);
                var x = cell!.TranslatePoint(new Point(0, 0), panel).X;
                var right = x + cell.ActualWidth;
                Assert.True(x >= slot!.X - 0.5 && right <= slot.X + slot.Width + 0.5,
                    $"a realized row's Name cell ({x:F1}..{right:F1}) escapes the current slot {slot.X:F1}+{slot.Width:F1} — stale layout after auto-width growth");
                positions.Add(x);
            }

            Assert.True(positions.Max() - positions.Min() < 0.5,
                $"realized rows disagree on the Name column position: [{string.Join(",", positions.Select(p => p.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}] vs slot {slot!.X:F1}+{slot.Width:F1}");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AutoWidthGrowth_ReachesHeadersToo()
    {
        var grid = MakeGrid();
        var window = TestWindowHost.Show(grid);
        TestWindowHost.Pump();
        try
        {
            var nameColumn = grid.Columns.First(c => c.FieldName == "Name");
            var before = grid.Layout.GetSlot(nameColumn)!.Width;

            grid.ScrollHost?.ScrollToVerticalOffset(26 * 1800);
            for (var i = 0; i < 4; i++)
            {
                TestWindowHost.Pump();
            }

            var after = grid.Layout.GetSlot(nameColumn)!.Width;
            Assert.True(after > before, "the Name auto column must grow when long rows realize");

            // And the header follows the same slot.
            var header = grid.HeadersPresenter?.GetHeader(nameColumn);
            Assert.NotNull(header);
            Assert.True(Math.Abs(header!.ActualWidth - after) < 0.5,
                $"header width {header.ActualWidth:F1} != grown slot {after:F1}");
        }
        finally
        {
            window.Close();
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject root)
        where T : System.Windows.DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var deeper in FindVisualChildren<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
