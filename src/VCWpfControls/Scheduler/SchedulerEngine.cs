using System.Globalization;

namespace VCWpfControls.Scheduler;

/// <summary>
/// The UI-free scheduler engine: recurrence expansion (daily/weekly/monthly/yearly with
/// count/until limits and per-date exceptions), overlap-lane layout for day/week views,
/// resource-conflict detection and view-period math (day/work-week/week/month grids).
/// All dates are local and naive (whole-day arithmetic, no DST transitions), matching
/// <see cref="Gantt.GanttEngine"/>.
/// </summary>
public static class SchedulerEngine
{
    /// <summary>
    /// Materializes every occurrence overlapping [rangeStart, rangeEnd) — an occurrence
    /// that merely touches a boundary (starts at rangeEnd / ends at rangeStart) is NOT
    /// included. Optionally restricted to one resource. Sorted by start.
    /// </summary>
    public static IReadOnlyList<SchedulerOccurrence> Expand(
        IEnumerable<SchedulerAppointment> appointments,
        DateTime rangeStart,
        DateTime rangeEnd,
        string? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(appointments);
        var result = new List<SchedulerOccurrence>();
        foreach (var appointment in appointments)
        {
            if (resourceId is not null && !string.Equals(appointment.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.AddRange(OccurrencesOf(appointment, rangeStart, rangeEnd));
        }

        result.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : string.Compare(a.Appointment.Id, b.Appointment.Id, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    /// <summary>
    /// The occurrences of ONE appointment overlapping [rangeStart, rangeEnd): a plain
    /// appointment yields itself (when in range); a recurring one expands its rule from
    /// the SERIES start — <see cref="SchedulerRecurrence.Count"/> and
    /// <see cref="SchedulerRecurrence.Until"/> count from there (RFC 5545 semantics:
    /// deleted occurrences still consume their count slot), not from the queried range.
    /// </summary>
    public static IEnumerable<SchedulerOccurrence> OccurrencesOf(SchedulerAppointment appointment, DateTime rangeStart, DateTime rangeEnd)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        if (appointment.Recurrence is not { } rule)
        {
            if (Overlaps(appointment.Start, appointment.End, rangeStart, rangeEnd))
            {
                yield return new SchedulerOccurrence(appointment, appointment.Start, appointment.End);
            }

            yield break;
        }

        var duration = appointment.Duration;
        var emitted = 0;
        foreach (var date in SeriesDates(appointment, rule, rangeEnd))
        {
            if (rule.Until is { } until && date > until.Date)
            {
                yield break;
            }

            if (rule.Count is { } count && emitted >= count)
            {
                yield break;
            }

            emitted++; // the date IS an occurrence of the series (deleted ones included)
            if (rule.Exceptions.Contains(date))
            {
                continue;
            }

            var start = appointment.IsAllDay ? date : date + appointment.Start.TimeOfDay;
            var end = start + duration;
            if (Overlaps(start, end, rangeStart, rangeEnd))
            {
                yield return new SchedulerOccurrence(appointment, start, end);
            }
        }
    }

    /// <summary>
    /// The candidate occurrence DATES (time-of-day excluded) of a series, walked from the
    /// series start and capped at <paramref name="rangeEnd"/> — dates before the queried
    /// range are still yielded (they consume count slots), just without occurrences.
    /// </summary>
    private static IEnumerable<DateTime> SeriesDates(SchedulerAppointment appointment, SchedulerRecurrence rule, DateTime rangeEnd)
    {
        var start = appointment.Start.Date;
        var interval = Math.Max(1, rule.Interval);
        const int maxIterations = 100_000;
        var iterations = 0;

        switch (rule.Pattern)
        {
            case SchedulerRecurrencePattern.Daily:
                for (var date = start; iterations++ < maxIterations; date = date.AddDays(interval))
                {
                    if (date > rangeEnd.Date)
                    {
                        yield break;
                    }

                    yield return date;
                }

                yield break;

            case SchedulerRecurrencePattern.Weekly:
            {
                var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                var weekdays = rule.DaysOfWeek.Length == 0 ? new[] { appointment.Start.DayOfWeek } : rule.DaysOfWeek;
                var weekStart = AlignToWeek(start, firstDay);
                for (var week = weekStart; iterations++ < maxIterations; week = week.AddDays(7 * interval))
                {
                    foreach (var day in SortedWeekDays(weekdays, firstDay))
                    {
                        var offset = ((int)day - (int)firstDay + 7) % 7; // day's offset FROM the week start
                        var date = week.AddDays(offset);
                        if (date < start)
                        {
                            continue; // before the series start
                        }

                        if (date > rangeEnd.Date)
                        {
                            yield break;
                        }

                        yield return date;
                    }
                }

                yield break;
            }

            case SchedulerRecurrencePattern.Monthly:
            {
                var dayOfMonth = rule.DayOfMonth > 0 ? rule.DayOfMonth : appointment.Start.Day;
                var month = new DateTime(start.Year, start.Month, 1);
                for (; iterations++ < maxIterations; month = month.AddMonths(interval))
                {
                    var date = WithDayClamped(month, dayOfMonth);
                    if (date < start)
                    {
                        continue; // the first month's clamped day precedes the series start
                    }

                    if (date > rangeEnd.Date)
                    {
                        yield break;
                    }

                    yield return date;
                }

                yield break;
            }

            default: // Yearly
            {
                var monthOfYear = rule.Month is >= 1 and <= 12 ? rule.Month : start.Month;
                var dayOfMonth = rule.DayOfMonth > 0 ? rule.DayOfMonth : start.Day;
                var year = start.Year;
                for (; iterations++ < maxIterations; year += interval)
                {
                    var date = WithDayClamped(new DateTime(year, monthOfYear, 1), dayOfMonth);
                    if (date < start)
                    {
                        continue; // the first candidate precedes the series start
                    }

                    if (date > rangeEnd.Date)
                    {
                        yield break;
                    }

                    yield return date;
                }

                yield break;
            }
        }
    }

    private static IEnumerable<DayOfWeek> SortedWeekDays(DayOfWeek[] weekdays, DayOfWeek firstDay)
    {
        // Week order (culture first day first) so occurrences come out chronologically.
        var present = new HashSet<DayOfWeek>(weekdays);
        for (var offset = 0; offset < 7; offset++)
        {
            var day = (DayOfWeek)(((int)firstDay + offset) % 7);
            if (present.Contains(day))
            {
                yield return day;
            }
        }
    }

    private static DateTime WithDayClamped(DateTime month, int dayOfMonth)
        => month.AddDays(Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)) - 1);

    private static bool Overlaps(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
        => start < rangeEnd && end > rangeStart;

    /// <summary>
    /// Assigns side-by-side lanes to overlapping occurrences (interval-graph clustering):
    /// transitively overlapping occurrences form one cluster sharing
    /// <see cref="SchedulerLaneSlot.LaneCount"/> lanes; within a cluster each occurrence
    /// takes the first lane whose last end touches-or-precedes its start. Touching
    /// intervals (end == start) share a lane; clusters after a gap restart at lane 0.
    /// </summary>
    public static IReadOnlyList<SchedulerLaneSlot> LayoutOverlaps(IEnumerable<SchedulerOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        var sorted = occurrences.OrderBy(static o => o.Start).ThenBy(static o => o.End).ToList();
        var placed = new List<(SchedulerOccurrence Occurrence, int Lane)>(sorted.Count);
        var slots = new List<SchedulerLaneSlot>(sorted.Count);
        var laneEnds = new List<DateTime>();
        var clusterStart = 0;

        void FlushCluster()
        {
            var lanes = Math.Max(1, laneEnds.Count);
            for (var s = clusterStart; s < placed.Count; s++)
            {
                slots.Add(new SchedulerLaneSlot(placed[s].Occurrence, placed[s].Lane, lanes));
            }

            clusterStart = placed.Count;
            laneEnds.Clear();
        }

        for (var i = 0; i < sorted.Count; i++)
        {
            var occurrence = sorted[i];
            var clusterEnd = laneEnds.Count == 0 ? DateTime.MinValue : laneEnds.Max();
            if (i > clusterStart && occurrence.Start >= clusterEnd)
            {
                FlushCluster(); // gap: everything before is a closed cluster
            }

            var lane = laneEnds.Count;
            for (var l = 0; l < laneEnds.Count; l++)
            {
                if (occurrence.Start >= laneEnds[l])
                {
                    lane = l; // first fitting lane (touching = fitting)
                    break;
                }
            }

            if (lane == laneEnds.Count)
            {
                laneEnds.Add(occurrence.End);
            }
            else
            {
                laneEnds[lane] = occurrence.End;
            }

            placed.Add((occurrence, lane));
        }

        FlushCluster();
        return slots;
    }

    /// <summary>
    /// The overlapping occurrence pairs sharing one resource (double bookings). Touching
    /// intervals do not conflict; occurrences without a resource never conflict.
    /// </summary>
    public static IReadOnlyList<(SchedulerOccurrence A, SchedulerOccurrence B)> FindConflicts(IEnumerable<SchedulerOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        var conflicts = new List<(SchedulerOccurrence, SchedulerOccurrence)>();
        var byResource = occurrences
            .Where(static o => o.ResourceId is not null)
            .GroupBy(static o => o.ResourceId, StringComparer.OrdinalIgnoreCase)
            .Select(static g => g.OrderBy(static o => o.Start).ThenBy(static o => o.End).ToList());
        foreach (var group in byResource)
        {
            for (var i = 0; i < group.Count; i++)
            {
                for (var j = i + 1; j < group.Count; j++)
                {
                    if (group[j].Start < group[i].End)
                    {
                        conflicts.Add((group[i], group[j]));
                    }
                    else
                    {
                        break; // sorted by start: no later j overlaps i either
                    }
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// The date range a view shows for an anchor date: Day = the day; WorkWeek = Mon-Fri;
    /// Week = the culture week; Month = the calendar month. End is exclusive.
    /// </summary>
    public static (DateTime Start, DateTime End) ViewRange(SchedulerViewType view, DateTime anchor, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        switch (view)
        {
            case SchedulerViewType.Day:
                return (anchor.Date, anchor.Date.AddDays(1));
            case SchedulerViewType.WorkWeek:
            {
                var weekStart = AlignToWeek(anchor.Date, DayOfWeek.Monday);
                return (weekStart, weekStart.AddDays(5));
            }

            case SchedulerViewType.Week:
            {
                var weekStart = AlignToWeek(anchor.Date, culture.DateTimeFormat.FirstDayOfWeek);
                return (weekStart, weekStart.AddDays(7));
            }

            default:
            {
                var monthStart = new DateTime(anchor.Year, anchor.Month, 1);
                return (monthStart, monthStart.AddMonths(1));
            }
        }
    }

    /// <summary>
    /// The six-week month grid (42 days) starting at the culture's first day of the week
    /// that contains the anchor month's first day — always covers the whole month.
    /// </summary>
    public static IReadOnlyList<DateTime> MonthGrid(DateTime monthAnchor, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var monthStart = new DateTime(monthAnchor.Year, monthAnchor.Month, 1);
        var gridStart = AlignToWeek(monthStart, culture.DateTimeFormat.FirstDayOfWeek);
        var days = new List<DateTime>(42);
        for (var i = 0; i < 42; i++)
        {
            days.Add(gridStart.AddDays(i));
        }

        return days;
    }

    /// <summary>Aligns the date down to the week start of the given first day.</summary>
    public static DateTime AlignToWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        var diff = ((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        return date.Date.AddDays(-diff);
    }
}
