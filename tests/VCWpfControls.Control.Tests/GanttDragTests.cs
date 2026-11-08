using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Gantt stage 3 — drag interactions: the timeline's move/resize/link hooks (day
/// snapping, clamps, milestone skips) and the GanttView wiring that turns them into
/// re-scheduling and new dependencies.
/// </summary>
public class GanttDragTests
{
    private static readonly DateTime May1 = new(2026, 5, 1);

    private static GanttTask T(string id, string name, int start, int duration, string? parent = null, bool milestone = false)
        => new(id, name, May1.AddDays(start), May1.AddDays(start + duration), 0, milestone, parent);

    private static GanttTimelinePanel CreatePanel()
        => new()
        {
            Tasks = new List<GanttTask>
            {
                T("a", "Analysis", 0, 3),
                T("b", "Build", 3, 5),
                T("m", "Kickoff done", 3, 0, milestone: true),
            },
        };

    private static GanttView CreateView(out Window window, bool autoSchedule = false)
    {
        var view = new GanttView
        {
            Width = 900,
            Height = 360,
            AutoSchedule = autoSchedule,
            Tasks = new ObservableCollection<GanttTask>
            {
                T("p", "Plan", 0, 2),
                T("a", "Analysis", 0, 3, "p"),
                T("b", "Build", 3, 5, "p"),
                T("m", "Kickoff done", 3, 0, "p", milestone: true),
            },
            Dependencies = new ObservableCollection<GanttDependency>
            {
                new("a", "b"),
            },
        };
        window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        return view;
    }

    // --- Panel hooks: move/resize semantics ---

    [StaFact]
    public void MoveTask_ShiftsBothDates_RaisesTaskEdited()
    {
        var panel = CreatePanel();
        var build = panel.Tasks.First(t => t.Id == "b");
        var raised = 0;
        panel.TaskEdited += (_, task) =>
        {
            raised++;
            Assert.Same(build, task);
        };

        panel.MoveTask(build, 3);

        Assert.Equal(1, raised);
        Assert.Equal(May1.AddDays(6), build.Start);
        Assert.Equal(May1.AddDays(11), build.Finish);
    }

    [StaFact]
    public void MoveTask_SkipsMilestones()
    {
        var panel = CreatePanel();
        var milestone = panel.Tasks.First(t => t.Id == "m");
        var raised = 0;
        panel.TaskEdited += (_, _) => raised++;

        panel.MoveTask(milestone, 2);

        Assert.Equal(0, raised);
        Assert.Equal(May1.AddDays(3), milestone.Start);
    }

    [StaFact]
    public void ResizeTaskStart_ExtendsDuration_FinishKept()
    {
        var panel = CreatePanel();
        var build = panel.Tasks.First(t => t.Id == "b"); // May4..May9
        var raised = 0;
        panel.TaskEdited += (_, _) => raised++;

        panel.ResizeTaskStart(build, -2);

        Assert.Equal(1, raised);
        Assert.Equal(May1.AddDays(1), build.Start);  // moved back 2 days
        Assert.Equal(May1.AddDays(8), build.Finish); // untouched
    }

    [StaFact]
    public void ResizeTaskStart_ClampedToAtLeastOneDay()
    {
        var panel = CreatePanel();
        var build = panel.Tasks.First(t => t.Id == "b"); // May4..May9
        var raised = 0;
        panel.TaskEdited += (_, _) => raised++;

        panel.ResizeTaskStart(build, 100); // would swallow the whole bar

        Assert.Equal(0, raised);
        Assert.Equal(May1.AddDays(3), build.Start);
        Assert.Equal(May1.AddDays(8), build.Finish);
    }

    [StaFact]
    public void ResizeTaskFinish_ExtendsDuration_StartKept()
    {
        var panel = CreatePanel();
        var build = panel.Tasks.First(t => t.Id == "b"); // May4..May9
        var raised = 0;
        panel.TaskEdited += (_, _) => raised++;

        panel.ResizeTaskFinish(build, 2);

        Assert.Equal(1, raised);
        Assert.Equal(May1.AddDays(3), build.Start);   // untouched
        Assert.Equal(May1.AddDays(10), build.Finish); // +2 days
    }

    [StaFact]
    public void ResizeTaskFinish_ClampedToAtLeastOneDay()
    {
        var panel = CreatePanel();
        var build = panel.Tasks.First(t => t.Id == "b"); // May4..May9
        var raised = 0;
        panel.TaskEdited += (_, _) => raised++;

        panel.ResizeTaskFinish(build, -100); // would collapse the bar

        Assert.Equal(0, raised);
        Assert.Equal(May1.AddDays(8), build.Finish);
    }

    [StaFact]
    public void CreateLink_RaisesLinkCreated_WithTheDrawnLink()
    {
        var panel = CreatePanel();
        var analysis = panel.Tasks.First(t => t.Id == "a");
        var build = panel.Tasks.First(t => t.Id == "b");
        (GanttTask Predecessor, GanttTask Successor, GanttLinkType Type)? link = null;
        panel.LinkCreated += (_, drawn) => link = drawn;

        panel.CreateLink(analysis, build, GanttLinkType.StartToStart);

        Assert.NotNull(link);
        Assert.Same(analysis, link!.Value.Predecessor);
        Assert.Same(build, link.Value.Successor);
        Assert.Equal(GanttLinkType.StartToStart, link.Value.Type);
    }

    // --- GanttView wiring: edits reschedule, drawn links land in Dependencies ---

    [StaFact]
    public void View_TaskEdited_RerunsAutoSchedule_AndRaisesViewEvent()
    {
        var view = CreateView(out var window, autoSchedule: true);
        try
        {
            var analysis = view.Tasks!.First(t => t.Id == "a"); // May1..May4
            var build = view.Tasks!.First(t => t.Id == "b");    // starts May4 (satisfied)
            GanttTask? edited = null;
            view.TaskEdited += (_, task) => edited = task;

            view.Timeline!.MoveTask(analysis, 2); // now May3..May6 -> violates a->b

            Assert.Same(analysis, edited);
            Assert.Equal(May1.AddDays(5), build.Start); // pushed to a's new finish (May6)
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void View_LinkCreated_AddsDependency_ReschedulesAndRaises()
    {
        var view = CreateView(out var window, autoSchedule: true);
        try
        {
            var milestone = view.Timeline!.Tasks.First(t => t.Id == "m"); // May4
            var analysis = view.Tasks!.First(t => t.Id == "a");           // May1..May4
            var build = view.Tasks!.First(t => t.Id == "b");
            GanttDependency? created = null;
            view.LinkCreated += (_, dependency) => created = dependency;

            view.Timeline!.CreateLink(milestone, analysis, GanttLinkType.FinishToStart);

            // The drawn link landed in Dependencies...
            Assert.NotNull(created);
            Assert.Equal(2, view.Dependencies!.Count);
            Assert.Equal(("m", "a", GanttLinkType.FinishToStart),
                (created!.PredecessorId, created.SuccessorId, created.Type));

            // ...and immediately scheduled: a must start at m, then b follows a.
            Assert.Equal(May1.AddDays(3), analysis.Start); // pushed to m (May4)
            Assert.Equal(analysis.Finish, build.Start);    // a->b cascade
        }
        finally
        {
            window.Close();
        }
    }
}
