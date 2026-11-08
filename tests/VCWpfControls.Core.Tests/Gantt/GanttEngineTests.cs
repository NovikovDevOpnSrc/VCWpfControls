using System.Globalization;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Core.Tests.Gantt;

/// <summary>
/// The UI-free Gantt engine: auto-scheduling per dependency types (+lag, milestones),
/// cycle detection, critical-path floats, timescale math.
/// </summary>
public class GanttEngineTests
{
    private static readonly DateTime May1 = new(2026, 5, 1);

    private static GanttTask T(string id, int startDay, int durationDays, string? parent = null, bool milestone = false)
        => new(id, id, May1.AddDays(startDay), May1.AddDays(startDay + durationDays), 0, milestone, parent);

    // --- AutoSchedule ---

    [Fact]
    public void FinishToStart_ShiftsSuccessorAfterPredecessorFinish()
    {
        var a = T("a", 0, 3);              // May1..May4
        var b = T("b", 0, 2);              // violates: starts May1
        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b") });
        Assert.Equal(May1.AddDays(3), b.Start);   // starts at a's finish
        Assert.Equal(May1.AddDays(5), b.Finish);
        Assert.Equal(May1, a.Start);              // predecessor untouched
    }

    [Fact]
    public void SatisfiedLink_DoesNotMoveAnything()
    {
        var a = T("a", 0, 3);
        var b = T("b", 5, 2);              // starts after a finishes
        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b") });
        Assert.Equal(May1.AddDays(5), b.Start);   // kept
    }

    [Fact]
    public void StartToStart_ShiftsEarlySuccessor()
    {
        // SS forbids starting BEFORE the predecessor: an early successor is pushed,
        // a later one is never pulled back (forward-only scheduling).
        var a = T("a", 2, 5);
        var early = T("b", 0, 2);
        var late = T("c", 5, 2);
        GanttEngine.AutoSchedule(
            new[] { a, early, late },
            new[] { new GanttDependency("a", "b", GanttLinkType.StartToStart), new GanttDependency("a", "c", GanttLinkType.StartToStart) });
        Assert.Equal(May1.AddDays(2), early.Start);   // pushed to a's start
        Assert.Equal(May1.AddDays(5), late.Start);   // already satisfying: kept
    }

    [Fact]
    public void FinishToFinish_ConstrainsFinishes()
    {
        var a = T("a", 0, 5);              // finish May6
        var b = T("b", 0, 2);              // finish May3 — violates FF
        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b", GanttLinkType.FinishToFinish) });
        Assert.Equal(May1.AddDays(3), b.Start);   // start = finish(6) - duration(2) = May4
        Assert.Equal(May1.AddDays(5), b.Finish);
    }

    [Fact]
    public void Lag_ShiftsFurther_LeadOverlaps()
    {
        var a = T("a", 0, 3);
        var lagged = T("b", 0, 1);
        var lead = T("c", 0, 1);
        GanttEngine.AutoSchedule(
            new[] { a, lagged, lead },
            new[] { new GanttDependency("a", "b", lagDays: 2), new GanttDependency("a", "c", lagDays: -1) });
        Assert.Equal(May1.AddDays(5), lagged.Start);   // finish(3) + 2
        Assert.Equal(May1.AddDays(2), lead.Start);    // finish(3) - 1
    }

    [Fact]
    public void Chain_CascadesThroughPasses()
    {
        var a = T("a", 0, 2);
        var b = T("b", 0, 2);
        var c = T("c", 0, 2);
        GanttEngine.AutoSchedule(
            new[] { a, b, c },
            new[] { new GanttDependency("a", "b"), new GanttDependency("b", "c") });
        Assert.Equal(May1.AddDays(2), b.Start);
        Assert.Equal(May1.AddDays(4), c.Start);
    }

    [Fact]
    public void Milestone_ZeroDuration()
    {
        var a = T("a", 0, 4);
        var m = T("m", 0, 0, milestone: true);
        GanttEngine.AutoSchedule(new[] { a, m }, new[] { new GanttDependency("a", "m") });
        Assert.Equal(May1.AddDays(4), m.Start);
        Assert.Equal(m.Start, m.Finish);
        Assert.Equal(0, m.DurationDays);
    }

    [Fact]
    public void Cycle_ThrowsWithPath()
    {
        var a = T("a", 0, 1);
        var b = T("b", 0, 1);
        var exception = Assert.Throws<GanttCycleException>(() => GanttEngine.AutoSchedule(
            new[] { a, b },
            new[] { new GanttDependency("a", "b"), new GanttDependency("b", "a") }));
        Assert.Contains("a", exception.CyclePath);
        Assert.Contains("b", exception.CyclePath);
    }

    [Fact]
    public void SelfReference_IsACycle()
    {
        var a = T("a", 0, 1);
        Assert.Throws<GanttCycleException>(() => GanttEngine.AutoSchedule(
            new[] { a }, new[] { new GanttDependency("a", "a") }));
    }

    [Fact]
    public void UnknownTaskIds_AreIgnored()
    {
        var a = T("a", 0, 1);
        var result = GanttEngine.AutoSchedule(
            new[] { a },
            new[] { new GanttDependency("ghost", "a"), new GanttDependency("a", "phantom") });
        Assert.Equal(May1, result[0].Start);
    }

    // --- CriticalPath ---

    [Fact]
    public void CriticalPath_LongestChain_ZeroFloatTasks()
    {
        // a(3) -> b(2) -> d(finish): critical a,b,d. c(1) parallels a with slack.
        var a = T("a", 0, 3);
        var b = T("b", 3, 2);
        var c = T("c", 0, 1);
        var d = T("d", 5, 1);
        var critical = GanttEngine.CriticalPath(
            new[] { a, b, c, d },
            new[] { new GanttDependency("a", "b"), new GanttDependency("b", "d"), new GanttDependency("c", "d") });
        Assert.Contains("a", critical);
        Assert.Contains("b", critical);
        Assert.Contains("d", critical);
        Assert.DoesNotContain("c", critical); // 4 days of slack
    }

    [Fact]
    public void CriticalPath_TwoParallelChains_PicksLonger()
    {
        var start = T("s", 0, 1);
        var long1 = T("x", 1, 5);
        var long2 = T("y", 6, 5);
        var short1 = T("p", 1, 1);
        var end = T("e", 11, 1);
        var critical = GanttEngine.CriticalPath(
            new[] { start, long1, long2, short1, end },
            new[]
            {
                new GanttDependency("s", "x"),
                new GanttDependency("x", "y"),
                new GanttDependency("y", "e"),
                new GanttDependency("s", "p"),
                new GanttDependency("p", "e"),
            });
        Assert.Contains("s", critical);
        Assert.Contains("x", critical);
        Assert.Contains("y", critical);
        Assert.Contains("e", critical);
        Assert.DoesNotContain("p", critical);
    }

    [Fact]
    public void CriticalPath_Empty_NoTasks()
    {
        Assert.Empty(GanttEngine.CriticalPath(Array.Empty<GanttTask>(), Array.Empty<GanttDependency>()));
    }

    [Fact]
    public void CriticalPath_NoLinks_OnlyProjectFinishTaskCritical()
    {
        // Without constraints only the task ending at the project finish has zero float.
        var a = T("a", 0, 2);
        var b = T("b", 5, 2);
        var critical = GanttEngine.CriticalPath(new[] { a, b }, Array.Empty<GanttDependency>());
        Assert.Contains("b", critical);
        Assert.DoesNotContain("a", critical); // 4 days of float
    }

    // --- Timescale ---

    [Fact]
    public void Timescale_Day_OneCellPerDay()
    {
        var cells = GanttEngine.BuildTimescale(May1, May1.AddDays(2), GanttTimescaleUnit.Day, CultureInfo.InvariantCulture);
        Assert.Equal(3, cells.Count);
        Assert.Equal(May1, cells[0].Start);
        Assert.Equal(May1.AddDays(1), cells[0].End);
        Assert.Contains("01.05", cells[0].Caption, StringComparison.Ordinal);
    }

    [Fact]
    public void Timescale_Week_AlignsToFirstDayOfWeek()
    {
        // May 1 2026 is a Friday; de-DE weeks start Monday -> first cell Apr 27.
        var monday = CultureInfo.GetCultureInfo("de-DE");
        var cells = GanttEngine.BuildTimescale(May1, May1.AddDays(10), GanttTimescaleUnit.Week, monday);
        Assert.Equal(new DateTime(2026, 4, 27), cells[0].Start);
        Assert.Equal(7, (cells[0].End - cells[0].Start).Days);
        Assert.Equal(3, cells.Count); // Apr27, May4, May11 covers through May11

        // Invariant culture weeks start SUNDAY -> first cell Apr 26.
        var sunday = GanttEngine.BuildTimescale(May1, May1, GanttTimescaleUnit.Week, CultureInfo.InvariantCulture);
        Assert.Equal(new DateTime(2026, 4, 26), sunday[0].Start);
    }

    [Fact]
    public void Timescale_Month_CalendarBounds()
    {
        var cells = GanttEngine.BuildTimescale(May1, May1.AddDays(65), GanttTimescaleUnit.Month, CultureInfo.InvariantCulture);
        Assert.Equal(3, cells.Count); // May, June, July
        Assert.Equal(new DateTime(2026, 5, 1), cells[0].Start);
        Assert.Equal(new DateTime(2026, 6, 1), cells[0].End);
        Assert.Equal(new DateTime(2026, 6, 1), cells[1].Start);
    }

    [Fact]
    public void Timescale_EmptyRange_NoCells()
    {
        Assert.Empty(GanttEngine.BuildTimescale(May1, May1.AddDays(-1), GanttTimescaleUnit.Day, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AlignToWeek_MidWeek_BackToWeekStart()
    {
        Assert.Equal(new DateTime(2026, 4, 27), GanttEngine.AlignToWeek(new DateTime(2026, 4, 29), DayOfWeek.Monday)); // Wednesday
        Assert.Equal(new DateTime(2026, 4, 26), GanttEngine.AlignToWeek(new DateTime(2026, 4, 29), DayOfWeek.Sunday));  // US week
    }

    [Fact]
    public void EarliestStart_FF_KeepsSuccessorDuration()
    {
        var predecessor = T("a", 0, 5);                 // finish May6
        var successor = T("b", 0, 2);
        var earliest = GanttEngine.EarliestStart(predecessor, successor, new GanttDependency("a", "b", GanttLinkType.FinishToFinish));
        Assert.Equal(May1.AddDays(3), earliest);        // May6 - 2 days = May4
    }
}
