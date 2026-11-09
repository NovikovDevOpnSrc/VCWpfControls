using VCWpfControls.Conditions;
using VCWpfControls.Gantt;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;
using VCWpfControls.Pivot;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Core.Tests.Adversarial;

/// <summary>
/// Adversarial inputs for the UI-free engines: degenerate ranges, hostile recurrence
/// rules, unknown links/fields, parse garbage, empty sources. The engines must either
/// produce well-defined results or throw their CONTRACTED exception — never hang, never
/// return garbage.
/// </summary>
public class EngineEdgeCaseTests
{
    // ---------- SchedulerEngine ----------

    [Fact]
    public void Scheduler_EmptyInputs_EmptyOutputs()
    {
        Assert.Empty(SchedulerEngine.Expand(Array.Empty<SchedulerAppointment>(), new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)));
        Assert.Empty(SchedulerEngine.Expand(new[] { new SchedulerAppointment("a", "A", new DateTime(2026, 1, 5), new DateTime(2026, 1, 6)) }, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1))); // inverted range
        Assert.Empty(SchedulerEngine.LayoutOverlaps(Array.Empty<SchedulerOccurrence>()));
        Assert.Empty(SchedulerEngine.FindConflicts(Array.Empty<SchedulerOccurrence>()));
    }

    [Fact]
    public void Scheduler_HostileRecurrenceRules_DoNotHang()
    {
        var anchor = new DateTime(2026, 1, 1, 9, 0, 0);
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);

        // Zero and negative intervals are clamped to 1, not treated as "never advance".
        foreach (var interval in new[] { 0, -1, -100 })
        {
            var zero = new SchedulerAppointment("z", "Z", anchor, anchor.AddHours(1))
            {
                Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Interval = interval },
            };
            Assert.NotEmpty(SchedulerEngine.Expand(new[] { zero }, start, end));
        }

        // Count 0 emits nothing; Until before the series start emits nothing.
        var zeroCount = new SchedulerAppointment("c", "C", anchor, anchor.AddHours(1))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Count = 0 },
        };
        Assert.Empty(SchedulerEngine.Expand(new[] { zeroCount }, start, end));

        var pastUntil = new SchedulerAppointment("u", "U", anchor, anchor.AddHours(1))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Until = new DateTime(2020, 1, 1) },
        };
        Assert.Empty(SchedulerEngine.Expand(new[] { pastUntil }, start, end));
    }

    [Fact]
    public void Scheduler_InvertedAppointment_Empty()
    {
        // End <= Start can never overlap a half-open range.
        var inverted = new SchedulerAppointment("i", "I", new DateTime(2026, 1, 10), new DateTime(2026, 1, 10));
        var negative = new SchedulerAppointment("n", "N", new DateTime(2026, 1, 10), new DateTime(2026, 1, 9));
        var range = SchedulerEngine.Expand(new[] { inverted, negative }, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        Assert.Empty(range);
    }

    [Fact]
    public void Scheduler_FarFutureDates_DoNotOverflow()
    {
        var far = new SchedulerAppointment("f", "F", new DateTime(9998, 1, 1, 9, 0, 0), new DateTime(9998, 1, 1, 10, 0, 0))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Monthly, DayOfMonth = 31 },
        };
        var occurrences = SchedulerEngine.Expand(new[] { far }, new DateTime(9998, 1, 1), new DateTime(9998, 3, 1));
        Assert.Equal(2, occurrences.Count); // Jan 31 and Feb 28 (clamped), no overflow
    }

    // ---------- GanttEngine ----------

    [Fact]
    public void Gantt_UnknownLinkIds_AreIgnored()
    {
        var task = new GanttTask("a", "A", new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));
        var links = new[]
        {
            new GanttDependency("ghost", "a"),
            new GanttDependency("a", "phantom"),
        };
        var result = GanttEngine.AutoSchedule(new[] { task }, links);
        Assert.Same(task, result[0]);
        Assert.Equal(new DateTime(2026, 1, 1), task.Start); // untouched
    }

    [Fact]
    public void Gantt_SelfLink_ThrowsCycle()
    {
        var task = new GanttTask("a", "A", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        var ex = Assert.Throws<GanttCycleException>(
            () => GanttEngine.AutoSchedule(new[] { task }, new[] { new GanttDependency("a", "a") }));
        Assert.Contains("a", ex.CyclePath);
    }

    [Fact]
    public void Gantt_EmptyChart_NoCycleProbeCrash()
    {
        Assert.Empty(GanttEngine.AutoSchedule(Array.Empty<GanttTask>(), Array.Empty<GanttDependency>()));
        Assert.Empty(GanttEngine.CriticalPath(Array.Empty<GanttTask>(), Array.Empty<GanttDependency>()));
    }

    [Fact]
    public void Gantt_MonthlyTimescaleAcrossYearBoundary()
    {
        var cells = GanttEngine.BuildTimescale(new DateTime(2026, 11, 15), new DateTime(2027, 1, 20), GanttTimescaleUnit.Month, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(3, cells.Count); // Nov 2026, Dec 2026, Jan 2027
        Assert.Equal(new DateTime(2026, 11, 1), cells[0].Start);
        Assert.Equal(new DateTime(2027, 2, 1), cells[2].End);
    }

    // ---------- PivotEngine ----------

    [Fact]
    public void Pivot_EmptySource_ProducesEmptyAxesWithZeroTotals()
    {
        var result = PivotEngine.Compute(
            Array.Empty<SaleRow>(),
            new[] { nameof(SaleRow.Region) },
            Array.Empty<string>(),
            new[] { new PivotMeasure(nameof(SaleRow.Amount), GridSummaryFunction.Sum, "Sum") },
            includeTotals: true);

        Assert.Empty(result.Rows);
        Assert.Null(result.GrandTotals[0]); // Sum of nothing is null — the engine's documented contract
    }

    [Fact]
    public void Pivot_AllFieldsFilteredOut_EmptyResult()
    {
        var rows = new[]
        {
            new SaleRow { Region = "East", Amount = 10m },
            new SaleRow { Region = "West", Amount = 20m },
        };
        var result = PivotEngine.Compute(
            rows,
            new[] { nameof(SaleRow.Region) },
            Array.Empty<string>(),
            new[] { new PivotMeasure(nameof(SaleRow.Amount), GridSummaryFunction.Sum, "Sum") },
            includeTotals: true,
            fieldFilter: (_, _) => false);

        Assert.Empty(result.Rows);
        Assert.Null(result.GrandTotals[0]); // totals follow the filter (5.3 regression class)
    }

    // ---------- ConditionParser / Evaluator ----------

    [Theory]
    [InlineData("[Name] = 'unclosed")]           // unterminated string
    [InlineData("[Name] > ")]                    // missing value
    [InlineData("[Unknown] = 1")]                // field not validated here — parses, evaluates to false
    [InlineData("([Name] = 'a'")]                // unbalanced paren
    [InlineData("[Name] Betweem 1")]             // unknown operator
    public void Condition_GarbageText_ThrowsOrIsBounded(string text)
    {
        try
        {
            var node = ConditionParser.Parse(text);
            // If it parses, evaluating it must not throw for a matching schema row.
            ConditionEvaluator.Matches(new SaleRow { Region = "East", Amount = 5m }, node);
        }
        catch (ConditionParseException)
        {
            // the contracted failure — fine
        }
    }

    [Fact]
    public void Condition_UnknownField_MatchesNothingWithoutThrowing()
    {
        var node = ConditionParser.Parse("[NoSuchField] = 1");
        Assert.False(ConditionEvaluator.Matches(new SaleRow { Region = "East" }, node));
        Assert.True(ConditionEvaluator.Matches(new SaleRow { Region = "East" }, null)); // null tree = pass-through
    }

    // ---------- GridClipboard ----------

    [Theory]
    [InlineData("not-a-number", typeof(int))]
    [InlineData("31.02.2020", typeof(DateTime))]
    [InlineData("XXX", typeof(Guid))]
    public void Clipboard_UnconvertibleText_ReturnsFalse(string text, Type target)
    {
        Assert.False(GridClipboard.TryConvert(text, target, out _));
    }

    [Fact]
    public void Clipboard_EmptyTextToNullable_ConvertsToNull()
    {
        Assert.True(GridClipboard.TryConvert("", typeof(string), out var value));
        Assert.Null(value);
    }

    /// <summary>A tiny scenario row.</summary>
    public sealed class SaleRow
    {
        /// <summary>Region.</summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>Amount.</summary>
        public decimal Amount { get; set; }
    }
}
