using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using VCWpfControls.Conditions;
using VCWpfControls.Gantt;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using VCWpfControls.Scheduler;

namespace VCWpfControls.Benchmarks;

/// <summary>A pivot/condition scenario row: low-cardinality dimensions (realistic pivot shape).</summary>
public sealed class Sale
{
    /// <summary>The region (5 values).</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>The category (8 values).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The sale amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>The item quantity.</summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Benchmarks of the post-v0.3 engines: <see cref="PivotEngine"/> (100k rows × 5×8 grid),
/// <see cref="ConditionEvaluator"/> (a mixed And/Or tree over 100k items),
/// <see cref="GanttEngine"/> (a 1000-task FS chain; auto-schedule rebuilds the violating
/// chain per invocation — task construction is part of the measured allocation) and
/// <see cref="SchedulerEngine"/> (100 endless daily series expanded over 3 years; a dense
/// 1000-occurrence overlap layout).
/// </summary>
[MemoryDiagnoser]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Benchmark names follow the underscore size-suffix convention.")]
[SuppressMessage("Usage", "CA1515:Consider making types internal", Justification = "BenchmarkDotNet discovers benchmark classes via the public API surface.")]
public class EngineBenchmarks
{
    private List<Sale> _sales = new();
    private List<PivotMeasure> _measures = new();
    private ConditionNode? _condition;
    private List<GanttTask> _settledChain = new();
    private List<GanttDependency> _chainLinks = new();
    private List<SchedulerAppointment> _series = new();
    private List<SchedulerOccurrence> _dense = new();
    private DateTime _chainStart = new(2026, 1, 1);

    /// <summary>One-time data generation (not measured).</summary>
    [GlobalSetup]
    public void Setup()
    {
        var regions = new[] { "East", "West", "North", "South", "Center" };
        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "VIP" };
        var random = new Random(42);
        _sales = new List<Sale>(100_000);
        for (var i = 0; i < 100_000; i++)
        {
            _sales.Add(new Sale
            {
                Region = regions[random.Next(regions.Length)],
                Category = categories[random.Next(categories.Length)],
                Amount = (decimal)(random.NextDouble() * 1000),
                Quantity = random.Next(1, 50),
            });
        }

        _measures = new List<PivotMeasure>
        {
            new(nameof(Sale.Amount), GridSummaryFunction.Sum, "Sum"),
            new(nameof(Sale.Quantity), GridSummaryFunction.Avg, "Avg"),
        };

        // (Region = East AND Amount > 500) OR Category = VIP.
        _condition = new ConditionGroup(ConditionGroupOperator.Or)
        {
            Children =
            {
                new ConditionGroup(ConditionGroupOperator.And)
                {
                    Children =
                    {
                        new ConditionExpression(nameof(Sale.Region), GridFilterOperator.Equals, "East"),
                        new ConditionExpression(nameof(Sale.Amount), GridFilterOperator.GreaterThan, 500m),
                    },
                },
                new ConditionExpression(nameof(Sale.Category), GridFilterOperator.Equals, "VIP"),
            },
        };

        // A settled 1000-task FS chain (for CriticalPath; auto-schedule rebuilds its own).
        for (var i = 0; i < 1000; i++)
        {
            _settledChain.Add(new GanttTask($"t{i}", $"Task {i}", new DateTime(2026, 1, 1).AddDays(i), new DateTime(2026, 1, 1).AddDays(i + 1)));
            if (i > 0)
            {
                _chainLinks.Add(new GanttDependency($"t{i - 1}", $"t{i}"));
            }
        }

        _series = new List<SchedulerAppointment>();
        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        for (var i = 0; i < 100; i++)
        {
            _series.Add(new SchedulerAppointment($"s{i}", $"Series {i}", start.AddMinutes(i), start.AddMinutes(i + 30))
            {
                Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily },
            });
        }

        _dense = new List<SchedulerOccurrence>();
        var day = new DateTime(2026, 1, 1, 9, 0, 0);
        for (var i = 0; i < 1000; i++)
        {
            _dense.Add(new SchedulerOccurrence(_series[0], day.AddSeconds(i * 30), day.AddSeconds(i * 30 + 600)));
        }
    }

    /// <summary>Pivot of 100k rows into a 5×8 grid with 2 measures and totals.</summary>
    [Benchmark]
    public PivotResult Pivot_100k()
        => PivotEngine.Compute(_sales, new[] { nameof(Sale.Region) }, new[] { nameof(Sale.Category) }, _measures, includeTotals: true);

    /// <summary>The And/Or condition tree over 100k items.</summary>
    [Benchmark]
    public int Condition_MatchesAll_100k()
    {
        var matched = 0;
        foreach (var sale in _sales)
        {
            if (ConditionEvaluator.Matches(sale, _condition))
            {
                matched++;
            }
        }

        return matched;
    }

    /// <summary>Auto-scheduling a fresh violating 1000-task FS chain (tasks rebuilt per run).</summary>
    [Benchmark]
    public IReadOnlyList<GanttTask> Gantt_AutoSchedule_1000chain()
    {
        var day = _chainStart;
        var tasks = new List<GanttTask>(1000);
        var links = new List<GanttDependency>(999);
        for (var i = 0; i < 1000; i++)
        {
            tasks.Add(new GanttTask($"t{i}", $"Task {i}", day, day.AddDays(1))); // all violating
            if (i > 0)
            {
                links.Add(new GanttDependency($"t{i - 1}", $"t{i}"));
            }
        }

        return GanttEngine.AutoSchedule(tasks, links);
    }

    /// <summary>Critical path (CPM) over the settled 1000-task chain.</summary>
    [Benchmark]
    public IReadOnlyList<string> Gantt_CriticalPath_1000chain()
        => GanttEngine.CriticalPath(_settledChain, _chainLinks);

    /// <summary>100 endless daily series expanded over a 3-year window (~110k occurrences).</summary>
    [Benchmark]
    public IReadOnlyList<SchedulerOccurrence> Scheduler_Expand_3years()
        => SchedulerEngine.Expand(_series, new DateTime(2026, 1, 1), new DateTime(2028, 12, 31));

    /// <summary>Lane layout of 1000 transitively overlapping occurrences.</summary>
    [Benchmark]
    public IReadOnlyList<SchedulerLaneSlot> Scheduler_LayoutOverlaps_1000dense()
        => SchedulerEngine.LayoutOverlaps(_dense);
}
