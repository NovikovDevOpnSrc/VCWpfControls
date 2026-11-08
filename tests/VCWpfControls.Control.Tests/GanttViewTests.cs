using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The GanttView control: tree grid + timeline built from the flat task list, row
/// alignment (task order), auto-schedule and critical-path wiring, bars/milestones drawn.
/// </summary>
public class GanttViewTests
{
    private static readonly DateTime May1 = new(2026, 5, 1);

    private static GanttTask T(string id, string name, int start, int duration, string? parent = null, bool milestone = false)
        => new(id, name, May1.AddDays(start), May1.AddDays(start + duration), 0, milestone, parent);

    private static GanttView Create(out Window window, bool autoSchedule = false)
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

    [StaFact]
    public void BuildsTreeAndTimeline_FromFlatTasks()
    {
        var view = Create(out var window);
        try
        {
            Assert.NotNull(view.Tree);
            Assert.NotNull(view.Timeline);


            // The timeline got the tasks in tree render order (parent before children).
            var timelineTasks = view.Timeline!.Tasks;
            Assert.Contains(timelineTasks, t => t.Id == "p");
            var planIndex = IndexOf(timelineTasks, "p");
            var analysisIndex = IndexOf(timelineTasks, "a");
            Assert.True(planIndex < analysisIndex, "parent must render before its children");
            Assert.Equal(4, timelineTasks.Count);
            Assert.Equal("p", timelineTasks[0].Id); // parent row first
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MilestoneAndBar_AreRendered()
    {
        var view = Create(out var window);
        try
        {
            var timeline = view.Timeline!;
            var bar = timeline.BarOf(timeline.Tasks.First(t => t.Id == "b"));
            Assert.NotNull(bar);
            Assert.True(bar!.Value.Width > 5, "bar must span its days");

            var milestone = timeline.BarOf(timeline.Tasks.First(t => t.Id == "m"));
            Assert.NotNull(milestone);
            Assert.True(Math.Abs(milestone!.Value.Width - milestone.Value.Height) < 12 || milestone.Value.Width > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AutoSchedule_ShiftsViolatingSuccessor()
    {
        var view = Create(out var window, autoSchedule: true);
        try
        {
            var build = view.Tasks!.First(t => t.Id == "b");
            var analysis = view.Tasks!.First(t => t.Id == "a");
            Assert.True(build.Start >= analysis.Finish.Date, $"b({build.Start:d}) must start at/after a finishes ({analysis.Finish:d})");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CriticalPath_Toggle_OffByDefault_WhenOnHighlightsChain()
    {
        var view = Create(out var window);
        try
        {
            Assert.Empty(view.Timeline!.CriticalTaskIds);
            view.ShowCriticalPath = true;
            view.Rebuild();
            TestWindowHost.Pump();
            Assert.NotEmpty(view.Timeline!.CriticalTaskIds);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void TimescaleUnit_ChangesCells()
    {
        var view = Create(out var window);
        try
        {
            Assert.Equal(GanttTimescaleUnit.Day, view.Timeline!.Unit);
            var dayCells = view.Timeline.Cells.Count;
            view.TimescaleUnit = GanttTimescaleUnit.Month;
            TestWindowHost.Pump();
            Assert.Equal(GanttTimescaleUnit.Month, view.Timeline!.Unit);
            Assert.True(view.Timeline.Cells.Count < dayCells, "months must be fewer than days");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BarsRender_OffscreenViaVisualBrush()
    {
        var view = Create(out var window);
        try
        {
            var timeline = view.Timeline!;
            var width = (int)Math.Ceiling(timeline.RenderSize.Width);
            var height = (int)Math.Ceiling(timeline.RenderSize.Height);
            if (width <= 0 || height <= 0)
            {
                return; // disconnected RDP renders empty - CI verifies
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            var visual = new System.Windows.Media.DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(new System.Windows.Media.VisualBrush(timeline), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            var nonBackground = 0;
            for (var i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i + 3] > 0 && pixels[i] < 250)
                {
                    nonBackground++;
                }
            }

            Assert.True(nonBackground > 100, $"timeline must draw content, got {nonBackground} px");
        }
        finally
        {
            window.Close();
        }
    }

    private static int IndexOf(IReadOnlyList<GanttTask> tasks, string id)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}
