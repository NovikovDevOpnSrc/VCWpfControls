using System.Globalization;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Core.Tests.Scheduler;

/// <summary>
/// The UI-free scheduler engine: recurrence expansion (all patterns, count/until,
/// exceptions), overlap-lane layout, resource conflicts and view-period math.
/// </summary>
public class SchedulerEngineTests
{
    private static readonly DateTime May1 = new(2026, 5, 1); // Friday

    private static SchedulerAppointment A(
        string id, string subject, int day, int startHour, int hours, string? resource = null)
        => new(id, subject, May1.AddDays(day).AddHours(startHour), May1.AddDays(day).AddHours(startHour + hours)) { ResourceId = resource };

    // --- Expansion: plain appointments ---

    [Fact]
    public void PlainAppointment_InRange_SingleOccurrence()
    {
        var appointment = A("a", "Standup", 0, 9, 1);
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(1)).ToList();
        Assert.Single(occurrences);
        Assert.Equal(appointment.Start, occurrences[0].Start);
        Assert.Equal(appointment.End, occurrences[0].End);
    }

    [Fact]
    public void PlainAppointment_TouchingBoundary_NotIncluded()
    {
        // Starts exactly at rangeEnd / ends exactly at rangeStart: half-open semantics.
        var startsAtEnd = A("a", "Late", 1, 0, 1);
        Assert.Empty(SchedulerEngine.OccurrencesOf(startsAtEnd, May1, May1.AddDays(1)));

        var endsAtStart = A("b", "Early", -1, 23, 1); // Apr30 23:00..May1 00:00
        Assert.Empty(SchedulerEngine.OccurrencesOf(endsAtStart, May1, May1.AddDays(1)));
    }

    [Fact]
    public void PlainAppointment_SpanningRange_Included()
    {
        var spanning = new SchedulerAppointment("a", "Trip", May1.AddDays(-1), May1.AddDays(2));
        var occurrences = SchedulerEngine.OccurrencesOf(spanning, May1, May1.AddDays(1)).ToList();
        Assert.Single(occurrences);
    }

    [Fact]
    public void Expand_SortsByStart_AndFiltersByResource()
    {
        var appointments = new[]
        {
            A("late", "Late", 0, 15, 1, "room2"),
            A("early", "Early", 0, 8, 1, "room1"),
            A("mid", "Mid", 0, 12, 1, "room2"),
        };
        var all = SchedulerEngine.Expand(appointments, May1, May1.AddDays(1));
        Assert.Equal(new[] { "early", "mid", "late" }, all.Select(o => o.Appointment.Id));

        var room2 = SchedulerEngine.Expand(appointments, May1, May1.AddDays(1), "ROOM2"); // case-insensitive
        Assert.Equal(new[] { "mid", "late" }, room2.Select(o => o.Appointment.Id));
    }

    // --- Expansion: recurrence ---

    [Fact]
    public void Daily_EveryTwoDays_TimeAndDurationPreserved()
    {
        var appointment = A("a", "Gym", 0, 7, 2);
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Interval = 2 };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(10)).ToList();
        Assert.Equal(5, occurrences.Count); // May 1, 3, 5, 7, 9
        Assert.All(occurrences, o => Assert.Equal(2, (o.End - o.Start).TotalHours));
        Assert.All(occurrences, o => Assert.Equal(7, o.Start.Hour));
        Assert.Equal(May1.AddDays(8).AddHours(7), occurrences[^1].Start); // time-of-day kept
    }

    [Fact]
    public void Daily_Count_CountsFromSeriesStart_NotFromRange()
    {
        var appointment = A("a", "Pills", 0, 8, 1);
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Count = 3 };
        var lateWindow = SchedulerEngine.OccurrencesOf(appointment, May1.AddDays(5), May1.AddDays(10)).ToList();
        Assert.Empty(lateWindow); // all 3 occurrences happened before the window

        var wholeSeries = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(10)).ToList();
        Assert.Equal(3, wholeSeries.Count);
        Assert.Equal(May1.AddDays(2).AddHours(8), wholeSeries[^1].Start); // time-of-day kept
    }

    [Fact]
    public void Daily_Until_IsInclusiveByDate()
    {
        var appointment = A("a", "Gym", 0, 7, 1);
        appointment.Recurrence = new SchedulerRecurrence
        {
            Pattern = SchedulerRecurrencePattern.Daily,
            Until = May1.AddDays(2), // inclusive: May 1,2,3
        };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(10)).ToList();
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(May1.AddDays(2).AddHours(7), occurrences[^1].Start);
    }

    [Fact]
    public void Weekly_SelectedDays_EveryMatchingWeekday()
    {
        var appointment = A("a", "Yoga", 0, 18, 1); // starts Friday May 1
        appointment.Recurrence = new SchedulerRecurrence
        {
            Pattern = SchedulerRecurrencePattern.Weekly,
            DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
        };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(14)).ToList();
        // Week 1: Fri May1 (Mon/Wed precede the series start); week 2: Mon4, Wed6, Fri8;
        // week 3: Mon11, Wed13 (Fri15 is the range end, exclusive).
        Assert.Equal(new[] { 1, 4, 6, 8, 11, 13 }, occurrences.Select(o => o.Start.Day));
    }

    [Fact]
    public void Weekly_NoDays_FallsBackToMasterWeekday()
    {
        var appointment = A("a", "Weekly", 0, 9, 1); // Friday
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Weekly };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(21)).ToList();
        Assert.Equal(3, occurrences.Count); // Fridays: 1, 8, 15
        Assert.All(occurrences, o => Assert.Equal(DayOfWeek.Friday, o.Start.DayOfWeek));
    }

    [Fact]
    public void Weekly_Biweekly_SkipsAlternateWeeks()
    {
        var appointment = A("a", "Biweek", 0, 9, 1); // Friday May 1
        appointment.Recurrence = new SchedulerRecurrence
        {
            Pattern = SchedulerRecurrencePattern.Weekly,
            Interval = 2,
        };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(28)).ToList();
        Assert.Equal(new[] { 1, 15 }, occurrences.Select(o => o.Start.Day)); // weeks of May1, May15
    }

    [Fact]
    public void Monthly_Day31_ClampsIntoShortMonths()
    {
        var appointment = A("a", "Report", 0, 10, 1); // May 1st... use May 31 below
        appointment.Start = new DateTime(2026, 1, 31, 10, 0, 0);
        appointment.End = appointment.Start.AddHours(1);
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Monthly };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1)).ToList();
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 1, 31, 10, 0, 0), occurrences[0].Start);
        Assert.Equal(new DateTime(2026, 2, 28, 10, 0, 0), occurrences[1].Start); // clamped (2026 not leap)
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0), occurrences[2].Start);
    }

    [Fact]
    public void Monthly_EveryOtherMonth_DefaultsToMasterDay()
    {
        var appointment = A("a", "Retro", 0, 15, 2); // May 1
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Monthly, Interval = 2 };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddMonths(6)).ToList();
        Assert.Equal(new[] { 5, 7, 9 }, occurrences.Select(o => o.Start.Month)); // May, Jul, Sep
    }

    [Fact]
    public void Yearly_Feb29_ClampsToFeb28InNonLeapYears()
    {
        var appointment = new SchedulerAppointment("a", "Leap", new DateTime(2024, 2, 29, 9, 0, 0), new DateTime(2024, 2, 29, 10, 0, 0));
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Yearly };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, new DateTime(2024, 1, 1), new DateTime(2027, 1, 1)).ToList();
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2024, 2, 29, 9, 0, 0), occurrences[0].Start);
        Assert.Equal(new DateTime(2025, 2, 28, 9, 0, 0), occurrences[1].Start); // clamped
        Assert.Equal(new DateTime(2026, 2, 28, 9, 0, 0), occurrences[2].Start); // clamped
    }

    [Fact]
    public void Yearly_MonthAndDay_Explicit()
    {
        var appointment = A("a", "Review", 0, 9, 1); // May 1, 2026
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Yearly, Month = 11, DayOfMonth = 9, Interval = 2 };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, new DateTime(2032, 1, 1)).ToList();
        Assert.Equal(3, occurrences.Count);
        Assert.All(occurrences, o => Assert.Equal(11, o.Start.Month));
        Assert.Equal(new DateTime(2026, 11, 9, 9, 0, 0), occurrences[0].Start);
        Assert.Equal(new DateTime(2028, 11, 9, 9, 0, 0), occurrences[1].Start);
    }

    [Fact]
    public void Exceptions_RemoveSingleOccurrences_WithoutFreeingCountSlots()
    {
        var appointment = A("a", "Gym", 0, 7, 1);
        appointment.Recurrence = new SchedulerRecurrence
        {
            Pattern = SchedulerRecurrencePattern.Daily,
            Count = 4, // generates May 1..4
        };
        appointment.Recurrence.Exceptions.Add(May1.AddDays(1)); // May 2 deleted
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(10)).ToList();
        Assert.Equal(new[] { 1, 3, 4 }, occurrences.Select(o => o.Start.Day)); // count NOT extended to May 5
    }

    [Fact]
    public void AllDay_Recurrence_SpansWholeDaysFromMidnight()
    {
        var appointment = new SchedulerAppointment("a", "Conference", May1, May1.AddDays(2))
        {
            IsAllDay = true,
        };
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Interval = 7 };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddDays(8)).ToList();
        Assert.Equal(2, occurrences.Count);
        Assert.Equal(May1.Date, occurrences[0].Start); // midnight, not start-time-based
        Assert.Equal(May1.AddDays(2), occurrences[0].End); // 2-day duration kept
    }

    [Fact]
    public void Daily_ThreeYearRange_ExpandsWithoutHanging()
    {
        var appointment = A("a", "Daily", 0, 8, 1);
        appointment.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily };
        var occurrences = SchedulerEngine.OccurrencesOf(appointment, May1, May1.AddYears(3)).ToList();
        Assert.Equal(1096, occurrences.Count); // 2026-05-01..2029-04-30 inclusive
    }

    // --- Lane layout ---

    private static SchedulerOccurrence O(int day, int startHour, int endHour, string id = "x")
        => new(new SchedulerAppointment(id, id, May1.AddDays(day).AddHours(startHour), May1.AddDays(day).AddHours(endHour)),
            May1.AddDays(day).AddHours(startHour), May1.AddDays(day).AddHours(endHour));

    [Fact]
    public void Layout_NonOverlapping_EachItsOwnCluster()
    {
        var slots = SchedulerEngine.LayoutOverlaps(new[] { O(0, 8, 9), O(0, 10, 11), O(0, 13, 14) });
        Assert.All(slots, s => Assert.Equal(1, s.LaneCount));
        Assert.All(slots, s => Assert.Equal(0, s.Lane));
    }

    [Fact]
    public void Layout_TwoOverlapping_SideBySideLanes()
    {
        var slots = SchedulerEngine.LayoutOverlaps(new[] { O(0, 9, 11), O(0, 10, 12) });
        Assert.Equal(2, slots.Count);
        Assert.All(slots, s => Assert.Equal(2, s.LaneCount));
        Assert.Equal(0, slots[0].Lane);
        Assert.Equal(1, slots[1].Lane);
    }

    [Fact]
    public void Layout_TouchingIntervals_ShareALane()
    {
        // Slots come back sorted by start; find each by its start hour.
        var slots = SchedulerEngine.LayoutOverlaps(new[] { O(0, 9, 10), O(0, 10, 11), O(0, 9, 11) });
        Assert.Equal(2, slots[0].LaneCount); // one cluster of two lanes
        Assert.All(slots, s => Assert.Equal(2, s.LaneCount));
        Assert.Equal(0, SlotAt(slots, 9, 10).Lane); // 9-10 on lane 0
        Assert.Equal(1, SlotAt(slots, 9, 11).Lane); // the wide 9-11 got its own lane
        Assert.Equal(0, SlotAt(slots, 10).Lane);  // 10:00 touches the 9-10 end -> reuses lane 0
    }

    private static SchedulerLaneSlot SlotAt(IReadOnlyList<SchedulerLaneSlot> slots, int startHour, int? endHour = null)
        => slots.Single(s => s.Occurrence.Start.Hour == startHour && (endHour is null || s.Occurrence.End.Hour == endHour));

    [Fact]
    public void Layout_ClustersAfterGap_RestartLanes()
    {
        var slots = SchedulerEngine.LayoutOverlaps(new[] { O(0, 9, 10), O(0, 9, 10), O(0, 15, 16) });
        Assert.Equal(2, slots[0].LaneCount);
        Assert.Equal(2, slots[1].LaneCount);
        Assert.Equal(1, slots[2].LaneCount); // new cluster after the gap
        Assert.Equal(0, slots[2].Lane);
    }

    [Fact]
    public void Layout_TransitiveOverlap_IsOneCluster()
    {
        // A(9-10) + B(9-10) + C(10-11): C touches A's end but overlaps nothing while B
        // runs... transitivity says cluster = all three (B ends 10, C starts 10 -> touches).
        // Use a real chain instead: A(9-11), B(10-12), C(11-13) — B overlaps both.
        var slots = SchedulerEngine.LayoutOverlaps(new[] { O(0, 9, 11), O(0, 10, 12), O(0, 11, 13) });
        Assert.All(slots, s => Assert.Equal(2, s.LaneCount));
        Assert.Equal(0, slots[0].Lane);
        Assert.Equal(1, slots[1].Lane);
        Assert.Equal(0, slots[2].Lane); // C reuses A's lane (A ended at 11)
    }

    // --- Conflicts ---

    [Fact]
    public void Conflicts_OverlappingSameResource_Reported()
    {
        var occurrences = new[]
        {
            O(0, 9, 11, "a"),
            O(0, 10, 12, "b"),
        };
        occurrences[0].Appointment.ResourceId = "room";
        occurrences[1].Appointment.ResourceId = "room";
        var conflicts = SchedulerEngine.FindConflicts(occurrences);
        Assert.Single(conflicts);
        Assert.Equal(("a", "b"), (conflicts[0].A.Appointment.Id, conflicts[0].B.Appointment.Id));
    }

    [Fact]
    public void Conflicts_DifferentResources_OrNoResource_NotReported()
    {
        var occurrences = new[]
        {
            O(0, 9, 11, "a"),
            O(0, 10, 12, "b"),
            O(0, 10, 12, "none"),
        };
        occurrences[0].Appointment.ResourceId = "room1";
        occurrences[1].Appointment.ResourceId = "room2";
        // occurrences[2] has no resource
        Assert.Empty(SchedulerEngine.FindConflicts(occurrences));
    }

    [Fact]
    public void Conflicts_TouchingIntervals_DoNotConflict()
    {
        var occurrences = new[]
        {
            O(0, 9, 10, "a"),
            O(0, 10, 11, "b"),
        };
        occurrences[0].Appointment.ResourceId = "room";
        occurrences[1].Appointment.ResourceId = "room";
        Assert.Empty(SchedulerEngine.FindConflicts(occurrences));
    }

    // --- View periods ---

    [Fact]
    public void ViewRange_DayWorkWeekWeekMonth()
    {
        var wednesday = new DateTime(2026, 5, 6);

        var day = SchedulerEngine.ViewRange(SchedulerViewType.Day, wednesday, CultureInfo.InvariantCulture);
        Assert.Equal((new DateTime(2026, 5, 6), new DateTime(2026, 5, 7)), day);

        var workWeek = SchedulerEngine.ViewRange(SchedulerViewType.WorkWeek, wednesday, CultureInfo.InvariantCulture);
        Assert.Equal((new DateTime(2026, 5, 4), new DateTime(2026, 5, 9)), workWeek); // Mon..Fri

        var week = SchedulerEngine.ViewRange(SchedulerViewType.Week, wednesday, CultureInfo.InvariantCulture);
        Assert.Equal((new DateTime(2026, 5, 3), new DateTime(2026, 5, 10)), week); // Sunday start

        var mondayCulture = CultureInfo.GetCultureInfo("de-DE");
        var weekDe = SchedulerEngine.ViewRange(SchedulerViewType.Week, wednesday, mondayCulture);
        Assert.Equal((new DateTime(2026, 5, 4), new DateTime(2026, 5, 11)), weekDe); // Monday start

        var month = SchedulerEngine.ViewRange(SchedulerViewType.Month, wednesday, CultureInfo.InvariantCulture);
        Assert.Equal((new DateTime(2026, 5, 1), new DateTime(2026, 6, 1)), month);
    }

    [Fact]
    public void MonthGrid_SixWeeks_CoversTheWholeMonth()
    {
        var grid = SchedulerEngine.MonthGrid(new DateTime(2026, 5, 15), CultureInfo.InvariantCulture);
        Assert.Equal(42, grid.Count);
        Assert.Equal(new DateTime(2026, 4, 26), grid[0]); // Sunday before May 1
        Assert.Contains(new DateTime(2026, 5, 1), grid);
        Assert.Contains(new DateTime(2026, 5, 31), grid);
        Assert.Equal(new DateTime(2026, 6, 6), grid[^1]);
        Assert.Equal(6, grid.Count(d => d.DayOfWeek == DayOfWeek.Sunday)); // 42/7
    }

    [Fact]
    public void AlignToWeek_BackToWeekStart()
    {
        Assert.Equal(new DateTime(2026, 5, 4), SchedulerEngine.AlignToWeek(new DateTime(2026, 5, 7), DayOfWeek.Monday)); // Thu -> Mon
        Assert.Equal(new DateTime(2026, 5, 4), SchedulerEngine.AlignToWeek(new DateTime(2026, 5, 4), DayOfWeek.Monday)); // already aligned
    }
}
