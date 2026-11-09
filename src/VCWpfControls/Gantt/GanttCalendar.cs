namespace VCWpfControls.Gantt;

/// <summary>
/// The working-time calendar of a Gantt chart (UI-free): weekend weekdays plus explicit
/// holiday dates. A <c>null</c> calendar means every day works (the historic behavior);
/// with a calendar, the calendar-aware <see cref="GanttEngine.AutoSchedule(IEnumerable{GanttTask}, IEnumerable{GanttDependency}, GanttCalendar?)"/>
/// counts durations and lags in WORKING days and never starts a task on a non-working day.
/// </summary>
public sealed class GanttCalendar
{
    /// <summary>The weekend weekdays (default Saturday and Sunday).</summary>
    public HashSet<DayOfWeek> WeekendDays { get; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

    /// <summary>The explicit non-working dates (holidays), by date at midnight.</summary>
    public HashSet<DateTime> Holidays { get; } = new();

    /// <summary>Whether the date is a working day (not a weekend, not a holiday).</summary>
    public bool IsWorkingDay(DateTime date)
        => !WeekendDays.Contains(date.DayOfWeek) && !Holidays.Contains(date.Date);

    /// <summary>
    /// The date <paramref name="days"/> WORKING days away from <paramref name="start"/>
    /// (the anchor itself is not counted; 0 returns the anchor unchanged — possibly
    /// non-working). Negative counts backwards. Throws when the calendar has no working
    /// days at all.
    /// </summary>
    public DateTime AddWorkingDays(DateTime start, int days)
    {
        var date = start.Date;
        var remaining = Math.Abs(days);
        var step = days >= 0 ? 1 : -1;
        var iterations = 0;
        while (remaining > 0)
        {
            if (iterations++ > 10_000)
            {
                throw new InvalidOperationException("The calendar has no working days.");
            }

            date = date.AddDays(step);
            if (IsWorkingDay(date))
            {
                remaining--;
            }
        }

        return date;
    }

    /// <summary>The date itself when it works, otherwise the next working day.</summary>
    public DateTime NextWorkingDay(DateTime date)
    {
        var day = date.Date;
        var iterations = 0;
        while (!IsWorkingDay(day))
        {
            if (iterations++ > 10_000)
            {
                throw new InvalidOperationException("The calendar has no working days.");
            }

            day = day.AddDays(1);
        }

        return day;
    }
}
