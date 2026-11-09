using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The GanttView wiring of <see cref="GanttCalendar"/>: the timeline lists the tinted
/// non-working days, auto-scheduling runs in working days, and the DP switch re-runs both.
/// </summary>
public class GanttCalendarViewTests
{
    private static readonly DateTime May1 = new(2026, 5, 1); // Friday
    private static readonly DateTime May2 = new(2026, 5, 2); // Saturday
    private static readonly DateTime May3 = new(2026, 5, 3); // Sunday
    private static readonly DateTime May4 = new(2026, 5, 4); // Monday
    private static readonly DateTime May6 = new(2026, 5, 6); // Wednesday

    private static GanttView Create(out Window window, bool autoSchedule, GanttCalendar? calendar)
    {
        var view = new GanttView
        {
            Width = 900,
            Height = 360,
            AutoSchedule = autoSchedule,
            Calendar = calendar,
            Tasks = new ObservableCollection<GanttTask>
            {
                new("a", "A", new DateTime(2026, 4, 30), May4), // Thu, finishes Monday
                new("b", "B", May1, May1.AddDays(2)), // starts Friday — violates the FS link
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
    public void Calendar_TintsWeekendColumns()
    {
        var view = Create(out var window, autoSchedule: false, calendar: new GanttCalendar());
        try
        {
            Assert.Contains(view.Timeline!.NonWorkingDays, d => d == May2);
            Assert.Contains(view.Timeline!.NonWorkingDays, d => d == May3);
            Assert.DoesNotContain(view.Timeline!.NonWorkingDays, new GanttCalendar().IsWorkingDay); // all listed days are non-working
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void WithoutCalendar_NoNonWorkingDays()
    {
        var view = Create(out var window, autoSchedule: false, calendar: null);
        try
        {
            Assert.Empty(view.Timeline!.NonWorkingDays);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AutoSchedule_WithCalendar_CountsWorkingDays()
    {
        // Lag 1 makes the two maths diverge: raw Fri+1 = Sat start/Sun finish,
        // working Fri+1 = Mon start/Tue finish.
        var view = new GanttView
        {
            Width = 900,
            Height = 360,
            AutoSchedule = true,
            Calendar = new GanttCalendar(),
            Tasks = new ObservableCollection<GanttTask>
            {
                new("a", "A", new DateTime(2026, 4, 30), May1), // finishes Friday
                new("b", "B", May1, May1.AddDays(1)),
            },
            Dependencies = new ObservableCollection<GanttDependency>
            {
                new("a", "b", lagDays: 1),
            },
        };
        var window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        try
        {
            var b = view.Tasks!.First(t => t.Id == "b");
            Assert.Equal(May4, b.Start); // moved past the weekend
            Assert.Equal(new DateTime(2026, 5, 5), b.Finish); // one working day from Monday
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void SettingCalendarAtRuntime_Reschedules()
    {
        // Lag 1 discriminates the math: raw Fri+1 = Saturday, working Fri+1 = Monday.
        var view = new GanttView
        {
            Width = 900,
            Height = 360,
            AutoSchedule = true,
            Tasks = new ObservableCollection<GanttTask>
            {
                new("a", "A", new DateTime(2026, 4, 30), May1), // finishes Friday
                new("b", "B", May1, May1.AddDays(1)),
            },
            Dependencies = new ObservableCollection<GanttDependency>
            {
                new("a", "b", lagDays: 1),
            },
        };
        var window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        try
        {
            var b = view.Tasks!.First(t => t.Id == "b");
            Assert.Equal(May2, b.Start); // raw calendar-day math first (Saturday)

            view.Calendar = new GanttCalendar();
            TestWindowHost.Pump();

            Assert.Equal(May4, b.Start); // re-scheduled in working days
            Assert.Contains(view.Timeline!.NonWorkingDays, d => d == May2);
        }
        finally
        {
            window.Close();
        }
    }
}
