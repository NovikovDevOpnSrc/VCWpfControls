using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Core.Tests.Gantt;

/// <summary>
/// The working-time calendar (weekends + holidays) and the calendar-aware AutoSchedule:
/// durations and lags count in WORKING days, moved tasks never start on non-working days,
/// and a null calendar keeps the historic calendar-day math.
/// </summary>
public class GanttCalendarTests
{
    private static readonly DateTime May1 = new(2026, 5, 1); // Friday
    private static readonly DateTime May2 = new(2026, 5, 2); // Saturday
    private static readonly DateTime May4 = new(2026, 5, 4); // Monday
    private static readonly DateTime May6 = new(2026, 5, 6); // Wednesday

    [Fact]
    public void AddWorkingDays_SkipsWeekends()
    {
        var calendar = new GanttCalendar();

        Assert.Equal(May4, calendar.AddWorkingDays(May1, 1)); // Fri + 1 = Mon
        Assert.Equal(new DateTime(2026, 5, 8), calendar.AddWorkingDays(May1, 5)); // Fri + 5 = Fri
    }

    [Fact]
    public void AddWorkingDays_NegativeCountsBackwards()
    {
        var calendar = new GanttCalendar();

        Assert.Equal(May1, calendar.AddWorkingDays(May4, -1)); // Mon - 1 = Fri
        Assert.Equal(new DateTime(2026, 4, 29), calendar.AddWorkingDays(May4, -3)); // Mon - 3 = Wed
    }

    [Fact]
    public void AddWorkingDays_SkipsHolidays()
    {
        var calendar = new GanttCalendar();
        calendar.Holidays.Add(May6); // a Wednesday off

        Assert.Equal(new DateTime(2026, 5, 7), calendar.AddWorkingDays(May4, 2)); // Mon -> Tue -> Thu
    }

    [Fact]
    public void AddWorkingDays_ZeroReturnsAnchorUnchanged()
    {
        var calendar = new GanttCalendar();
        Assert.Equal(May2, calendar.AddWorkingDays(May2, 0)); // even a non-working anchor
    }

    [Fact]
    public void AddWorkingDays_CalendarWithoutWorkingDays_Throws()
    {
        var calendar = new GanttCalendar();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            calendar.WeekendDays.Add(day);
        }

        Assert.Throws<InvalidOperationException>(() => calendar.AddWorkingDays(May1, 1));
    }

    [Fact]
    public void NextWorkingDay_SkipsToMonday_WorkingUnchanged()
    {
        var calendar = new GanttCalendar();
        Assert.Equal(May4, calendar.NextWorkingDay(May2));
        Assert.Equal(May1, calendar.NextWorkingDay(May1));
    }

    [Fact]
    public void AutoSchedule_CalendarCountsDurationsInWorkingDays()
    {
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May4); // Thu, finishes Mon
        var b = new GanttTask("b", "B", May1, May1.AddDays(2)); // starts Fri — violates
        var calendar = new GanttCalendar();

        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b") }, calendar);

        Assert.Equal(May4, b.Start); // not Saturday: the Monday after a finishes
        Assert.Equal(May6, b.Finish); // 2 WORKING days: Mon -> Tue -> Wed
    }

    [Fact]
    public void AutoSchedule_CalendarLagCountsInWorkingDays()
    {
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May1); // finishes Friday
        var b = new GanttTask("b", "B", May1, May1.AddDays(1)); // violates with the lag
        var calendar = new GanttCalendar();

        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b", lagDays: 1) }, calendar);

        Assert.Equal(May4, b.Start); // Fri + 1 working day = Monday, not Saturday
    }

    [Fact]
    public void AutoSchedule_CalendarSnapsNonWorkingEarliestToMonday()
    {
        // A MANUAL predecessor finishing Saturday: the raw FS earliest is Saturday itself —
        // only the snap moves the successor to Monday.
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May2); // finishes Saturday
        var b = new GanttTask("b", "B", May1, May1.AddDays(1)); // violates
        var calendar = new GanttCalendar();

        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b") }, calendar);

        Assert.Equal(May4, b.Start); // snapped off Saturday onto the next working day
    }

    [Fact]
    public void AutoSchedule_CalendarMilestoneKeepsStartAsFinish()
    {
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May4);
        var m = new GanttTask("m", "M", May1, May1, isMilestone: true);
        var calendar = new GanttCalendar();

        GanttEngine.AutoSchedule(new[] { a, m }, new[] { new GanttDependency("a", "m") }, calendar);

        Assert.Equal(May4, m.Start);
        Assert.Equal(May4, m.Finish);
    }

    [Fact]
    public void AutoSchedule_NullCalendarKeepsCalendarDayMath()
    {
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May1); // finishes Friday
        var b = new GanttTask("b", "B", May1, May1.AddDays(1)); // violates with the lag
        var calendar = new GanttCalendar();

        // The calendar exists but is NOT passed: raw calendar-day math (historic behavior).
        GanttEngine.AutoSchedule(new[] { a, b }, new[] { new GanttDependency("a", "b", lagDays: 1) });

        Assert.Equal(May2, b.Start); // Fri + 1 calendar day = Saturday
    }

    [Fact]
    public void EarliestStart_CalendarOverloadSnapsToWorkingDay()
    {
        var a = new GanttTask("a", "A", new DateTime(2026, 4, 30), May1); // finishes Friday
        var b = new GanttTask("b", "B", May1, May1.AddDays(1));
        var calendar = new GanttCalendar();

        var raw = GanttEngine.EarliestStart(a, b, new GanttDependency("a", "b", lagDays: 1));
        var snapped = GanttEngine.EarliestStart(a, b, new GanttDependency("a", "b", lagDays: 1), calendar);

        Assert.Equal(May2, raw); // Saturday by raw math
        Assert.Equal(May4, snapped); // snapped/working = Monday
    }
}
