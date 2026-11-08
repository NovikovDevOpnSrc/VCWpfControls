namespace VCWpfControls.Scheduler;

/// <summary>The scheduler view granularity.</summary>
public enum SchedulerViewType
{
    /// <summary>One day with a time ruler.</summary>
    Day = 0,

    /// <summary>Monday through Friday.</summary>
    WorkWeek = 1,

    /// <summary>The full culture week.</summary>
    Week = 2,

    /// <summary>A calendar month as a six-week grid.</summary>
    Month = 3,
}

/// <summary>The recurrence pattern of an appointment (an RRULE subset).</summary>
public enum SchedulerRecurrencePattern
{
    /// <summary>Every N days.</summary>
    Daily = 0,

    /// <summary>Every N weeks on the selected weekdays.</summary>
    Weekly = 1,

    /// <summary>Every N months on a day of the month.</summary>
    Monthly = 2,

    /// <summary>Every N years on a month/day.</summary>
    Yearly = 3,
}

/// <summary>
/// The recurrence rule of an appointment (UI-free): pattern + interval + pattern-specific
/// fields, limited by <see cref="Count"/> or <see cref="Until"/> (either, or endless), with
/// per-date <see cref="Exceptions"/> (deleted occurrences).
/// </summary>
public sealed class SchedulerRecurrence
{
    /// <summary>The pattern kind.</summary>
    public SchedulerRecurrencePattern Pattern { get; set; } = SchedulerRecurrencePattern.Daily;

    /// <summary>Every N units (days/weeks/months/years; 1 = every).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>The weekdays of a weekly pattern (empty = the master start's weekday).</summary>
    public DayOfWeek[] DaysOfWeek { get; set; } = Array.Empty<DayOfWeek>();

    /// <summary>
    /// The day of the month for monthly/yearly patterns (0 = the master start's day).
    /// Clamped into short months: the 31st falls on the last day of February.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>The month (1-12) of a yearly pattern (0 = the master start's month).</summary>
    public int Month { get; set; }

    /// <summary>The total number of occurrences counted from the series start (null = endless).</summary>
    public int? Count { get; set; }

    /// <summary>The inclusive last occurrence date (null = endless; date part only).</summary>
    public DateTime? Until { get; set; }

    /// <summary>Deleted occurrences by their local date (the date the occurrence WOULD start on).</summary>
    public HashSet<DateTime> Exceptions { get; } = new();
}

/// <summary>
/// One appointment of a scheduler (UI-free). A non-recurring appointment is a single band;
/// a recurring one expands into occurrences through <see cref="SchedulerEngine"/>. Identity
/// is <see cref="Id"/>; grouping by resource keeps side-by-side views honest.
/// </summary>
public sealed class SchedulerAppointment
{
    /// <summary>Creates an appointment.</summary>
    public SchedulerAppointment(string id, string subject, DateTime start, DateTime end)
    {
        Id = id;
        Subject = subject;
        Start = start;
        End = end;
    }

    /// <summary>The unique appointment id.</summary>
    public string Id { get; set; }

    /// <summary>The display subject.</summary>
    public string Subject { get; set; }

    /// <summary>The local start (naive; the time-of-day seeds every occurrence).</summary>
    public DateTime Start { get; set; }

    /// <summary>The local end (must be later than <see cref="Start"/>; touching intervals do not overlap).</summary>
    public DateTime End { get; set; }

    /// <summary>The optional location.</summary>
    public string? Location { get; set; }

    /// <summary>The optional resource (room/person) this appointment belongs to.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Spans whole days (rendered in the all-day band; the time-of-day is ignored).</summary>
    public bool IsAllDay { get; set; }

    /// <summary>The recurrence rule (null = a single occurrence).</summary>
    public SchedulerRecurrence? Recurrence { get; set; }

    /// <summary>The band length (kept on every occurrence of a series).</summary>
    public TimeSpan Duration => End - Start;
}

/// <summary>One materialized instance of an appointment inside a queried range.</summary>
public sealed class SchedulerOccurrence
{
    /// <summary>Creates the occurrence.</summary>
    public SchedulerOccurrence(SchedulerAppointment appointment, DateTime start, DateTime end)
    {
        Appointment = appointment;
        Start = start;
        End = end;
    }

    /// <summary>The master appointment.</summary>
    public SchedulerAppointment Appointment { get; }

    /// <summary>The occurrence start (the master's time-of-day on the occurrence date).</summary>
    public DateTime Start { get; }

    /// <summary>The occurrence end (<see cref="SchedulerAppointment.Duration"/> after the start).</summary>
    public DateTime End { get; }

    /// <summary>The occurrence's subject (from the master).</summary>
    public string Subject => Appointment.Subject;

    /// <summary>The master's resource id.</summary>
    public string? ResourceId => Appointment.ResourceId;
}

/// <summary>An occurrence with its computed overlap lane (for day/week views).</summary>
public sealed class SchedulerLaneSlot
{
    /// <summary>Creates the slot.</summary>
    public SchedulerLaneSlot(SchedulerOccurrence occurrence, int lane, int laneCount)
    {
        Occurrence = occurrence;
        Lane = lane;
        LaneCount = laneCount;
    }

    /// <summary>The occurrence being placed.</summary>
    public SchedulerOccurrence Occurrence { get; }

    /// <summary>The lane index within the overlap cluster (0-based).</summary>
    public int Lane { get; }

    /// <summary>How many side-by-side lanes the cluster uses (the x-share denominator).</summary>
    public int LaneCount { get; }
}
