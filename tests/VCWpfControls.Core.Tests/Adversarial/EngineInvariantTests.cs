using VCWpfControls.Conditions;
using VCWpfControls.Gantt;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Core.Tests.Adversarial;

/// <summary>
/// Randomized invariant (property-based) tests — seeded, deterministic. The invariants are
/// stated independently of the implementation algorithms (union-find for overlap clusters
/// vs the engine's two-pass lanes; link satisfaction vs the relaxation loop).
/// </summary>
public class EngineInvariantTests
{
    private const int Seed = 20261109;

    // ---------- AutoSchedule: satisfaction + duration preservation ----------

    [Fact]
    public void AutoSchedule_RandomAcyclicGraphs_AllLinksSatisfiedAndDurationsPreserved()
    {
        var random = new Random(Seed);
        for (var graph = 0; graph < 50; graph++)
        {
            var count = 5 + random.Next(30);
            var day = new DateTime(2026, 1, 1);
            var tasks = new List<GanttTask>();
            for (var i = 0; i < count; i++)
            {
                var duration = random.Next(0, 8); // 0-duration non-milestones possible
                var start = day.AddDays(random.Next(52));
                tasks.Add(new GanttTask($"t{i}", $"T{i}", start, start.AddDays(duration), random.Next(101), duration == 0, null));
            }

            var links = new List<GanttDependency>();
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < 2 && i + 1 < count; j++)
                {
                    var successor = i + 1 + random.Next(count - i - 1); // forward-only = acyclic
                    links.Add(new GanttDependency($"t{i}", $"t{successor}", (GanttLinkType)random.Next(4), random.Next(-2, 3)));
                }
            }

            var durations = tasks.ToDictionary(t => t.Id, t => t.DurationDays);
            GanttEngine.AutoSchedule(tasks, links);

            foreach (var link in links)
            {
                var predecessor = tasks.First(t => t.Id == link.PredecessorId);
                var successor = tasks.First(t => t.Id == link.SuccessorId);
                var earliest = GanttEngine.EarliestStart(predecessor, successor, link);
                Assert.True(earliest <= successor.Start.Date,
                    $"graph {graph}: link {link.PredecessorId}->{link.SuccessorId} ({link.Type}, lag {link.LagDays}) violated: earliest {earliest:d} > start {successor.Start:d}");
            }

            foreach (var task in tasks)
            {
                Assert.Equal(durations[task.Id], task.DurationDays); // moving never resizes
                Assert.True(task.Finish >= task.Start, $"graph {graph}: {task.Id} inverted after scheduling");
            }
        }
    }

    // ---------- LayoutOverlaps: lanes never conflict inside a cluster ----------

    [Fact]
    public void LayoutOverlaps_RandomOccurrences_LanesConsistent()
    {
        var random = new Random(Seed + 1);
        for (var round = 0; round < 50; round++)
        {
            var count = 5 + random.Next(60);
            var day = new DateTime(2026, 6, 1, 8, 0, 0);
            var appointments = new List<SchedulerAppointment>();
            for (var i = 0; i < count; i++)
            {
                appointments.Add(new SchedulerAppointment($"a{i}", $"A{i}", day.AddMinutes(random.Next(600)), day.AddMinutes(random.Next(600)).AddMinutes(15)));
            }

            var occurrences = SchedulerEngine.Expand(appointments, day.Date, day.Date.AddDays(1));
            var slots = SchedulerEngine.LayoutOverlaps(occurrences);

            // 1. Every occurrence placed exactly once.
            Assert.Equal(occurrences.Count, slots.Count);
            Assert.Equal(occurrences.Count, slots.Select(s => s.Occurrence).Distinct().Count());

            // 2. Lanes bounded.
            foreach (var slot in slots)
            {
                Assert.InRange(slot.Lane, 0, slot.LaneCount - 1);
            }

            // 3. Overlapping occurrences never share a lane (independent union-find clustering).
            for (var i = 0; i < slots.Count; i++)
            {
                for (var j = i + 1; j < slots.Count; j++)
                {
                    var a = slots[i].Occurrence;
                    var b = slots[j].Occurrence;
                    var overlaps = a.Start < b.End && b.Start < a.End;
                    if (overlaps)
                    {
                        Assert.NotEqual(slots[i].Lane, slots[j].Lane);
                        Assert.Equal(slots[i].LaneCount, slots[j].LaneCount); // same cluster
                    }
                }
            }

            // 4. Within each transitive cluster, the lane count equals distinct lanes used.
            var components = UnionFindClusters(slots);
            foreach (var component in components)
            {
                var distinctLanes = component.Select(s => s.Lane).Distinct().Count();
                Assert.Equal(distinctLanes, component[0].LaneCount);
                Assert.Equal(distinctLanes, component.Max(s => s.Lane) + 1); // no wasted lanes
            }
        }
    }

    private static List<List<SchedulerLaneSlot>> UnionFindClusters(IReadOnlyList<SchedulerLaneSlot> slots)
    {
        var parent = Enumerable.Range(0, slots.Count).ToArray();
        int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
        void Union(int a, int b) => parent[Find(a)] = Find(b);

        for (var i = 0; i < slots.Count; i++)
        {
            for (var j = i + 1; j < slots.Count; j++)
            {
                var a = slots[i].Occurrence;
                var b = slots[j].Occurrence;
                if (a.Start < b.End && b.Start < a.End)
                {
                    Union(i, j);
                }
            }
        }

        return slots.Select((_, i) => (Index: i, Root: Find(i)))
            .GroupBy(t => t.Root)
            .Select(g => g.Select(t => slots[t.Index]).ToList())
            .ToList();
    }

    // ---------- Pivot: totals add up three ways ----------

    [Fact]
    public void Pivot_RandomData_GrandTotalEqualsRowAndColumnTotals()
    {
        var random = new Random(Seed + 2);
        for (var round = 0; round < 20; round++)
        {
            var rows = new List<PivotRow>();
            decimal sourceSum = 0;
            for (var i = 0; i < 200; i++)
            {
                var amount = random.Next(1000);
                sourceSum += amount;
                rows.Add(new PivotRow
                {
                    Region = $"R{random.Next(3)}",
                    Category = $"C{random.Next(4)}",
                    Amount = amount,
                });
            }

            var result = PivotEngine.Compute(
                rows,
                new[] { nameof(PivotRow.Region) },
                new[] { nameof(PivotRow.Category) },
                new[] { new PivotMeasure(nameof(PivotRow.Amount), GridSummaryFunction.Sum, "Sum") },
                includeTotals: true);

            var sumOfRowTotals = result.RowTotals.Sum(r => (decimal)r[0]!);
            var sumOfColumnTotals = result.ColumnTotals.Sum(c => (decimal)c[0]!);
            Assert.Equal(sourceSum, (decimal)result.GrandTotals[0]!);
            Assert.Equal(sourceSum, sumOfRowTotals);
            Assert.Equal(sourceSum, sumOfColumnTotals);
        }
    }

    private sealed class PivotRow
    {
        public string Region { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }

    // ---------- ConditionParser: Format -> Parse -> Format is a fixed point ----------

    [Fact]
    public void Condition_RandomTrees_RoundTripThroughText()
    {
        var random = new Random(Seed + 3);
        for (var round = 0; round < 30; round++)
        {
            var tree = RandomTree(random, depth: 0);
            // One cycle may NORMALIZE (a redundant single-child root group loses its
            // parentheses in the tree) — from the normalized text on it must be a fixed point.
            var normalized = ConditionParser.Format(ConditionParser.Parse(ConditionParser.Format(tree)));
            var again = ConditionParser.Format(ConditionParser.Parse(normalized));
            Assert.Equal(normalized, again);
        }
    }

    private static ConditionNode RandomTree(Random random, int depth)
    {
        if (depth >= 2 || random.Next(3) == 0)
        {
            var field = random.Next(2) == 0 ? "Salary" : "Name";
            return field == "Salary"
                ? new ConditionExpression("Salary", GridFilterOperator.GreaterThan, random.Next(1000))
                : new ConditionExpression("Name", GridFilterOperator.StartsWith, $"\"{new string((char)('a' + random.Next(26)), 1 + random.Next(5))}\"");
        }

        var group = new ConditionGroup(random.Next(2) == 0 ? ConditionGroupOperator.And : ConditionGroupOperator.Or);
        var children = 1 + random.Next(3);
        for (var i = 0; i < children; i++)
        {
            group.Children.Add(RandomTree(random, depth + 1));
        }

        return group;
    }
}
