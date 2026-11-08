using System.Globalization;

namespace VCWpfControls.Gantt;

/// <summary>Thrown when the dependency graph contains a cycle.</summary>
public sealed class GanttCycleException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GanttCycleException(string message, IReadOnlyList<string> cyclePath)
        : base(message)
    {
        CyclePath = cyclePath;
    }

    /// <summary>The task ids forming the cycle (in order).</summary>
    public IReadOnlyList<string> CyclePath { get; }
}

/// <summary>
/// The UI-free Gantt scheduling engine: auto-scheduling (shifting successors per
/// dependencies + lag), critical-path computation (CPM floats) and timescale math.
/// Works on flat task lists keyed by <see cref="GanttTask.Id"/>; dates are treated as
/// whole days (the band end is exclusive, matching <see cref="GanttTask.DurationDays"/>).
/// </summary>
public static class GanttEngine
{
    /// <summary>
    /// Shifts tasks forward so every dependency is satisfied: a successor violating a link
    /// is moved to the earliest satisfying date (auto-scheduling, forward pass). Manual
    /// (already satisfying) dates are kept. Throws <see cref="GanttCycleException"/> on
    /// circular dependencies.
    /// </summary>
    public static IReadOnlyList<GanttTask> AutoSchedule(IEnumerable<GanttTask> tasks, IEnumerable<GanttDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(dependencies);

        var list = tasks.ToList();
        var byId = list.ToDictionary(t => t.Id, t => t);
        var links = dependencies.Where(d => byId.ContainsKey(d.PredecessorId) && byId.ContainsKey(d.SuccessorId)).ToList();
        EnsureAcyclic(byId, links);

        // Relaxation: keep pushing until nothing moves (the graph is acyclic, so this
        // terminates in at most N passes).
        for (var pass = 0; pass <= list.Count; pass++)
        {
            var moved = false;
            foreach (var link in links)
            {
                var predecessor = byId[link.PredecessorId];
                var successor = byId[link.SuccessorId];
                var earliest = EarliestStart(predecessor, successor, link);
                if (earliest > successor.Start.Date)
                {
                    // Capture the duration BEFORE moving the start: Finish-Start would
                    // already be negative, collapsing the task to zero days.
                    var duration = successor.DurationDays;
                    successor.Start = earliest;
                    successor.Finish = successor.IsMilestone || duration == 0
                        ? successor.Start
                        : successor.Start.AddDays(duration);
                    moved = true;
                }
            }

            if (!moved)
            {
                break;
            }
        }

        return list;
    }

    /// <summary>
    /// The earliest start a successor may have given the link: FS/SS constrain the start
    /// directly; FF/SF constrain the successor's finish, so the implied start keeps the
    /// successor's duration.
    /// </summary>
    public static DateTime EarliestStart(GanttTask predecessor, GanttTask successor, GanttDependency link)
        => link.Type switch
        {
            GanttLinkType.FinishToStart => predecessor.Finish.Date.AddDays(link.LagDays),
            GanttLinkType.StartToStart => predecessor.Start.Date.AddDays(link.LagDays),
            GanttLinkType.FinishToFinish => predecessor.Finish.Date.AddDays(link.LagDays).AddDays(-successor.DurationDays),
            _ => predecessor.Start.Date.AddDays(link.LagDays).AddDays(-successor.DurationDays),
        };

    /// <summary>
    /// The ids of the tasks on the critical path: the longest chain (by duration) from the
    /// project start to the project finish; critical tasks have zero total float. The last
    /// task (project finish) is included; milestones can be critical.
    /// </summary>
    public static IReadOnlyList<string> CriticalPath(IEnumerable<GanttTask> tasks, IEnumerable<GanttDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(dependencies);

        var list = tasks.ToList();
        var byId = list.ToDictionary(t => t.Id, t => t);
        var links = dependencies.Where(d => byId.ContainsKey(d.PredecessorId) && byId.ContainsKey(d.SuccessorId)).ToList();
        EnsureAcyclic(byId, links);

        var successorsOf = links.ToLookup(l => l.PredecessorId, l => l.SuccessorId);
        var predecessorsOf = links.ToLookup(l => l.SuccessorId, l => l.PredecessorId);

        // Topological order (Kahn) over the dependency graph.
        var incoming = links.GroupBy(l => l.SuccessorId).ToDictionary(g => g.Key, g => g.Count());
        var queue = new Queue<string>(list.Select(t => t.Id).Where(id => !incoming.ContainsKey(id)));
        var order = new List<string>();
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            order.Add(id);
            foreach (var next in successorsOf[id])
            {
                incoming[next]--;
                if (incoming[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        // Early finish (forward pass): EF = start + duration, then pushed by links.
        var earlyFinish = list.ToDictionary(t => t.Id, t => t.Finish.Date);
        var earlyStart = list.ToDictionary(t => t.Id, t => t.Start.Date);
        foreach (var id in order)
        {
            var task = byId[id];
            var duration = task.DurationDays;
            var earliest = task.Start.Date;
            foreach (var predecessorId in predecessorsOf[id])
            {
                var link = links.First(l => l.SuccessorId == id && l.PredecessorId == predecessorId);
                var candidate = link.Type switch
                {
                    GanttLinkType.FinishToStart => earlyFinish[predecessorId].AddDays(link.LagDays),
                    GanttLinkType.StartToStart => earlyStart[predecessorId].AddDays(link.LagDays),
                    GanttLinkType.FinishToFinish => earlyFinish[predecessorId].AddDays(link.LagDays).AddDays(-duration),
                    _ => earlyStart[predecessorId].AddDays(link.LagDays).AddDays(-duration),
                };
                if (candidate > earliest)
                {
                    earliest = candidate;
                }
            }

            earlyStart[id] = earliest;
            earlyFinish[id] = earliest.AddDays(duration);
        }

        var projectFinish = list.Count == 0 ? DateTime.MinValue : list.Max(t => earlyFinish[t.Id]);

        // Late finish (backward pass): LF = min(constraints), float = LS - ES.
        var lateFinish = list.ToDictionary(t => t.Id, _ => projectFinish);
        var lateStart = new Dictionary<string, DateTime>();
        foreach (var id in order.OrderByDescending(x => order.IndexOf(x)))
        {
            var task = byId[id];
            var duration = task.DurationDays;
            var latest = lateFinish[id];
            foreach (var successorId in successorsOf[id])
            {
                var link = links.First(l => l.SuccessorId == successorId && l.PredecessorId == id);
                var candidate = link.Type switch
                {
                    GanttLinkType.FinishToStart => lateStart.GetValueOrDefault(successorId, projectFinish).AddDays(-link.LagDays),
                    GanttLinkType.StartToStart => lateStart.GetValueOrDefault(successorId, projectFinish).AddDays(-link.LagDays).AddDays(duration),
                    GanttLinkType.FinishToFinish => lateFinish[successorId].AddDays(-link.LagDays),
                    _ => lateFinish[successorId].AddDays(-link.LagDays).AddDays(duration),
                };
                if (candidate < latest)
                {
                    latest = candidate;
                }
            }

            lateFinish[id] = latest;
            lateStart[id] = latest.AddDays(-duration);
        }

        // Critical = zero total float (late start equals early start).
        var critical = new List<string>();
        foreach (var task in list)
        {
            if ((lateStart[task.Id] - earlyStart[task.Id]).Days <= 0)
            {
                critical.Add(task.Id);
            }
        }

        return critical;
    }

    /// <summary>
    /// Timescale cells covering [rangeStart, rangeEnd]: one cell per unit (Day/Week with
    /// the culture's first day of week/Month). Weeks and months align to calendar bounds,
    /// so the first and last cells may extend beyond the range.
    /// </summary>
    public static List<GanttTimescaleCell> BuildTimescale(DateTime rangeStart, DateTime rangeEnd, GanttTimescaleUnit unit, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var cells = new List<GanttTimescaleCell>();
        var start = rangeStart.Date;
        var end = rangeEnd.Date;
        if (end < start)
        {
            return cells;
        }

        if (unit == GanttTimescaleUnit.Day)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                cells.Add(new GanttTimescaleCell(day, day.AddDays(1), day.ToString("ddd dd.MM", culture)));
            }

            return cells;
        }

        if (unit == GanttTimescaleUnit.Week)
        {
            var firstDay = culture.DateTimeFormat.FirstDayOfWeek;
            var weekStart = AlignToWeek(start, firstDay);
            for (var week = weekStart; week <= end; week = week.AddDays(7))
            {
                var caption = $"{week.ToString("dd.MM", culture)} \u2013 {week.AddDays(6).ToString("dd.MM", culture)}";
                cells.Add(new GanttTimescaleCell(week, week.AddDays(7), caption));
            }

            return cells;
        }

        var month = new DateTime(start.Year, start.Month, 1);
        while (month <= end)
        {
            var next = month.AddMonths(1);
            cells.Add(new GanttTimescaleCell(month, next, month.ToString("MMMM yyyy", culture)));
            month = next;
        }

        return cells;
    }

    /// <summary>Aligns the date down to the week start of the culture.</summary>
    public static DateTime AlignToWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        var diff = ((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        return date.Date.AddDays(-diff);
    }

    private static void EnsureAcyclic(Dictionary<string, GanttTask> byId, List<GanttDependency> links)
    {
        // Depth-first search with the current path for the cycle report.
        var state = byId.Keys.ToDictionary(id => id, _ => 0); // 0=white 1=gray 2=black
        var stack = new List<string>();
        void Visit(string id)
        {
            if (state[id] == 2)
            {
                return;
            }

            if (state[id] == 1)
            {
                var cycleStart = stack.IndexOf(id);
                var path = stack.Skip(cycleStart).Concat(new[] { id }).ToList();
                throw new GanttCycleException(
                    "The dependency graph contains a cycle: " + string.Join(" -> ", path),
                    path);
            }

            state[id] = 1;
            stack.Add(id);
            foreach (var link in links.Where(l => l.PredecessorId == id))
            {
                Visit(link.SuccessorId);
            }

            stack.RemoveAt(stack.Count - 1);
            state[id] = 2;
        }

        foreach (var id in byId.Keys)
        {
            Visit(id);
        }
    }
}
