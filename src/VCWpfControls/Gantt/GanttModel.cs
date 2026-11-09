namespace VCWpfControls.Gantt;

/// <summary>Dependency type between two Gantt tasks (precedence relation).</summary>
public enum GanttLinkType
{
    /// <summary>Successor starts after the predecessor finishes (the default).</summary>
    FinishToStart = 0,

    /// <summary>Successor starts after the predecessor starts.</summary>
    StartToStart = 1,

    /// <summary>Successor finishes after the predecessor finishes.</summary>
    FinishToFinish = 2,

    /// <summary>Successor finishes after the predecessor starts.</summary>
    StartToFinish = 3,
}

/// <summary>
/// One task of a Gantt chart (UI-free): a time band on the chart or a milestone (zero
/// duration). Identity is <see cref="Id"/>; the hierarchy is expressed through
/// <see cref="ParentId"/> (null = root) so the schedule engine works on flat lists.
/// </summary>
public sealed class GanttTask
{
    /// <summary>Creates a task.</summary>
    public GanttTask(string id, string name, DateTime start, DateTime finish, int completion = 0, bool isMilestone = false, string? parentId = null)
    {
        Id = id;
        Name = name;
        Start = start;
        Finish = finish;
        Completion = completion;
        IsMilestone = isMilestone;
        ParentId = parentId;
    }

    /// <summary>The unique task id (referenced by dependencies).</summary>
    public string Id { get; set; }

    /// <summary>The display name.</summary>
    public string Name { get; set; }

    /// <summary>Start of the task (inclusive; date or datetime).</summary>
    public DateTime Start { get; set; }

    /// <summary>Finish of the task (exclusive band end; equal to Start for milestones).</summary>
    public DateTime Finish { get; set; }

    /// <summary>Completion percent 0..100 (drives the bar fill).</summary>
    public int Completion { get; set; }

    /// <summary>Whether the task is a milestone (rendered as a diamond, zero duration).</summary>
    public bool IsMilestone { get; set; }

    /// <summary>The parent task id (null = root) — left tree of the chart.</summary>
    public string? ParentId { get; set; }

    /// <summary>The baseline (planned) start; null = no baseline for the task.</summary>
    public DateTime? BaselineStart { get; set; }

    /// <summary>The baseline (planned) finish; null = no baseline for the task.</summary>
    public DateTime? BaselineFinish { get; set; }

    /// <summary>The task duration in whole days (0 for milestones).</summary>
    public int DurationDays => IsMilestone ? 0 : Math.Max(0, (int)Math.Ceiling((Finish.Date - Start.Date).TotalDays));
}

/// <summary>A precedence link between two tasks.</summary>
public sealed class GanttDependency
{
    /// <summary>Creates the dependency.</summary>
    public GanttDependency(string predecessorId, string successorId, GanttLinkType type = GanttLinkType.FinishToStart, int lagDays = 0)
    {
        PredecessorId = predecessorId;
        SuccessorId = successorId;
        Type = type;
        LagDays = lagDays;
    }

    /// <summary>The predecessor task id.</summary>
    public string PredecessorId { get; set; }

    /// <summary>The successor task id.</summary>
    public string SuccessorId { get; set; }

    /// <summary>The precedence relation.</summary>
    public GanttLinkType Type { get; set; }

    /// <summary>Lag in days added after the relation (negative = lead/overlap).</summary>
    public int LagDays { get; set; }
}

/// <summary>One cell of the chart timescale header.</summary>
public sealed class GanttTimescaleCell
{
    /// <summary>Creates the cell.</summary>
    public GanttTimescaleCell(DateTime start, DateTime end, string caption)
    {
        Start = start;
        End = end;
        Caption = caption;
    }

    /// <summary>Cell start (inclusive).</summary>
    public DateTime Start { get; }

    /// <summary>Cell end (exclusive).</summary>
    public DateTime End { get; }

    /// <summary>Header caption.</summary>
    public string Caption { get; }
}

/// <summary>The timescale zoom unit of the chart.</summary>
public enum GanttTimescaleUnit
{
    /// <summary>One cell per day.</summary>
    Day = 0,

    /// <summary>One cell per week (culture start-of-week).</summary>
    Week = 1,

    /// <summary>One cell per calendar month.</summary>
    Month = 2,
}
