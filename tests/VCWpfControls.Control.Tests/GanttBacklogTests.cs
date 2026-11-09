using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Gantt backlog round: baseline bars (planned vs actual with a variance tooltip),
/// inline tree date editing (write-through nodes re-running the schedule) and timeline
/// render virtualization (true extent + viewport-banded culling).
/// </summary>
public class GanttBacklogTests
{
    private static readonly DateTime Nov9 = new(2026, 11, 9); // Monday

    private static GanttView Create(out Window window, ObservableCollection<GanttTask>? tasks = null, bool autoSchedule = true)
    {
        var view = new GanttView
        {
            Width = 900,
            Height = 360,
            AutoSchedule = autoSchedule,
            Tasks = tasks ?? new ObservableCollection<GanttTask>
            {
                new("a", "A", Nov9, Nov9.AddDays(3)),
                new("b", "B", Nov9.AddDays(10), Nov9.AddDays(15)),
            },
            Dependencies = new ObservableCollection<GanttDependency>(),
        };
        window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        return view;
    }

    private static GanttTask TaskOf(GanttView view, string id) => view.Tasks!.First(t => t.Id == id);

    [StaFact]
    public void BaselineBar_MatchesBaselineDates_NotActual()
    {
        var b = new GanttTask("b", "B", Nov9.AddDays(10), Nov9.AddDays(15))
        {
            BaselineStart = Nov9.AddDays(2),
            BaselineFinish = Nov9.AddDays(4),
        };
        var view = Create(out var window, new ObservableCollection<GanttTask> { new("a", "A", Nov9, Nov9.AddDays(3)), b });
        try
        {
            var baseline = view.Timeline!.BaselineBarOf(b);
            var actual = view.Timeline!.BarOf(b);
            Assert.NotNull(baseline);
            Assert.NotNull(actual);

            // The baseline band sits where the BASELINE dates are: earlier and shorter
            // than the actual bar (which starts 8 days later and spans 5 days).
            Assert.True(baseline!.Value.X < actual!.Value.X - 100, "baseline must start at its own date, not the actual start");
            Assert.True(baseline.Value.Width < actual.Value.Width, "2 baseline days vs 5 actual days");
            Assert.True(baseline.Value.Y < actual.Value.Y, "the baseline band sits on top of the row");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BaselineBar_NullWithoutBaseline_OrWhenHidden()
    {
        var withBaseline = new GanttTask("b", "B", Nov9.AddDays(1), Nov9.AddDays(3))
        {
            BaselineStart = Nov9,
            BaselineFinish = Nov9.AddDays(2),
        };
        var without = new GanttTask("a", "A", Nov9, Nov9.AddDays(2));
        var view = Create(out var window, new ObservableCollection<GanttTask> { without, withBaseline });
        try
        {
            Assert.NotNull(view.Timeline!.BaselineBarOf(withBaseline));
            Assert.Null(view.Timeline!.BaselineBarOf(without));

            view.ShowBaseline = false;
            TestWindowHost.Pump();
            Assert.Null(view.Timeline!.BaselineBarOf(withBaseline));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NodeProperties_WriteThroughToTask()
    {
        var task = new GanttTask("a", "A", Nov9, Nov9.AddDays(3));
        var node = new GanttTaskNode(task);

        node.Name = "Renamed";
        node.Start = Nov9.AddDays(1);
        node.Finish = Nov9.AddDays(6);
        node.Completion = 40;

        Assert.Equal("Renamed", task.Name);
        Assert.Equal(Nov9.AddDays(1), task.Start);
        Assert.Equal(Nov9.AddDays(6), task.Finish);
        Assert.Equal(40, task.Completion);
    }

    [StaFact]
    public void InlineDateEdit_ReschedulesSuccessors_AndRaisesTaskEdited()
    {
        var a = new GanttTask("a", "A", Nov9, Nov9.AddDays(3));
        var b = new GanttTask("b", "B", Nov9.AddDays(3), Nov9.AddDays(5));
        var view = Create(out var window, new ObservableCollection<GanttTask> { a, b });
        view.Dependencies!.Add(new GanttDependency("a", "b"));
        try
        {
            var raised = 0;
            view.TaskEdited += (_, _) => raised++;

            // Drive the REAL grid editing path on A's Finish: the DatePicker writes
            // through the node into the task, the view rebuilds, AutoSchedule shifts B.
            var tree = view.Tree!;
            var dataRow = tree.Items.OfType<VCWpfControls.Grid.Data.GridDataRow>()
                .First(r => (r.Item as GanttTaskNode)!.Task.Id == "a");
            var finishColumn = tree.Columns.First(c => c.FieldName == "Finish");
            tree.SetCurrentCellForTest(dataRow.Item, finishColumn);
            Assert.True(tree.BeginEdit(dataRow.Item, finishColumn));

            var container = (VCWpfControls.Grid.Primitives.GridRow)tree.ItemContainerGenerator.ContainerFromIndex(tree.Items.IndexOf(dataRow));
            var cells = container.Template.FindName("PART_Cells", container) as VCWpfControls.Grid.Primitives.GridCellsPanel;
            var editor = (DatePicker?)cells?.EditingElement;
            Assert.NotNull(editor);
            editor!.SelectedDate = Nov9.AddDays(6);
            Assert.True(tree.CommitEdit());
            TestWindowHost.Pump();

            Assert.Equal(Nov9.AddDays(6), a.Finish);
            Assert.Equal(Nov9.AddDays(6), b.Start); // auto-scheduled off the new finish
            Assert.Equal(1, raised);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Timeline_ReportsTrueExtent()
    {
        var tasks = new ObservableCollection<GanttTask>();
        for (var i = 0; i < 300; i++)
        {
            tasks.Add(new GanttTask($"t{i}", $"Task {i}", Nov9.AddDays(i), Nov9.AddDays(i + 2)));
        }

        var view = Create(out var window, tasks, autoSchedule: false);
        try
        {
            var panel = view.Timeline!;
            // The true extent = header + rows + the legend strip (on by default).
            Assert.Equal(panel.HeaderHeight + 300 * panel.RowHeight + panel.LegendHeight, panel.DesiredSize.Height, 1);
            Assert.Equal(300, panel.Tasks.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Render_CullsRowsOutsideTheViewport()
    {
        var tasks = new ObservableCollection<GanttTask>();
        for (var i = 0; i < 300; i++)
        {
            tasks.Add(new GanttTask($"t{i}", $"Task {i}", Nov9.AddDays(i), Nov9.AddDays(i + 2)));
        }

        var view = Create(out var window, tasks, autoSchedule: false);
        try
        {
            var panel = view.Timeline!;
            panel.ViewportHeight = 400; // ~15 rows visible of the 300
            TestWindowHost.Pump();

            Assert.Equal(0, panel.FirstRenderedRow);
            Assert.True(panel.LastRenderedRow - panel.FirstRenderedRow <= 20,
                $"rendered {panel.FirstRenderedRow}..{panel.LastRenderedRow} of 300 — culling broken");
            Assert.True(panel.LastRenderedRow < 300);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TreeScroll_MirrorsIntoTheTimelineViewer()
    {
        var tasks = new ObservableCollection<GanttTask>();
        for (var i = 0; i < 300; i++)
        {
            tasks.Add(new GanttTask($"t{i}", $"Task {i}", Nov9.AddDays(i), Nov9.AddDays(i + 2)));
        }

        var view = Create(out var window, tasks, autoSchedule: false);
        try
        {
            var panel = view.Timeline!;
            var treeScroll = FindScrollViewer(view.Tree!);
            Assert.NotNull(treeScroll);

            // The tree scrolls in ITEM units (its virtualizing panel), the timeline in pixels.
            treeScroll!.ScrollToVerticalOffset(100); // 100 rows down
            TestWindowHost.Pump();

            // The timeline's OWN viewer must scroll (not just the culling band): the band
            // following while the content stays at 0 leaves a blank timeline.
            Assert.Equal(100 * panel.RowHeight, view.TimelineScroll!.VerticalOffset, 1);
            Assert.True(Math.Abs(panel.VerticalSyncOffset - view.TimelineScroll.VerticalOffset) < 1,
                "the culling band must match the actually scrolled position");
            Assert.InRange(panel.FirstRenderedRow, 94, 100);

            // The reverse direction converts pixels back to the tree's item units.
            view.TimelineScroll!.ScrollToVerticalOffset(50 * panel.RowHeight);
            TestWindowHost.Pump();
            Assert.Equal(50, treeScroll.VerticalOffset, 1);
        }
        finally
        {
            window.Close();
        }
    }

    private static System.Windows.Controls.ScrollViewer? FindScrollViewer(System.Windows.DependencyObject root)
    {
        if (root is System.Windows.Controls.ScrollViewer viewer)
        {
            return viewer;
        }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            if (FindScrollViewer(System.Windows.Media.VisualTreeHelper.GetChild(root, i)) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    [StaFact]
    public void Render_WindowFollowsTheScrollOffset()
    {
        var tasks = new ObservableCollection<GanttTask>();
        for (var i = 0; i < 300; i++)
        {
            tasks.Add(new GanttTask($"t{i}", $"Task {i}", Nov9.AddDays(i), Nov9.AddDays(i + 2)));
        }

        var view = Create(out var window, tasks, autoSchedule: false);
        try
        {
            var panel = view.Timeline!;
            panel.ViewportHeight = 400;
            panel.VerticalSyncOffset = 100 * panel.RowHeight; // scrolled to row ~100
            TestWindowHost.Pump();

            Assert.InRange(panel.FirstRenderedRow, 94, 100);
            Assert.InRange(panel.LastRenderedRow, 112, 120);
        }
        finally
        {
            window.Close();
        }
    }
}
