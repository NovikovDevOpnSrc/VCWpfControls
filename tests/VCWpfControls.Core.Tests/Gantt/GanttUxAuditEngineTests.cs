using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Core.Tests.Gantt;

/// <summary>
/// The UX-audit engine additions: the MAJOR timescale row (Oracle/Fiori two-level axes)
/// and the duration-weighted summary completion rollup.
/// </summary>
public class GanttUxAuditEngineTests
{
    [Fact]
    public void MajorTimescale_DayUnit_YieldsMonths()
    {
        var cells = GanttEngine.BuildMajorTimescale(new DateTime(2026, 11, 9), new DateTime(2026, 12, 15), GanttTimescaleUnit.Day, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(2, cells.Count); // November 2026, December 2026
        Assert.Equal(new DateTime(2026, 11, 1), cells[0].Start);
        Assert.Equal(new DateTime(2026, 12, 1), cells[1].Start);
        Assert.All(cells, c => Assert.False(string.IsNullOrWhiteSpace(c.Caption)));
    }

    [Fact]
    public void MajorTimescale_MonthUnit_YieldsYears()
    {
        var cells = GanttEngine.BuildMajorTimescale(new DateTime(2025, 11, 1), new DateTime(2026, 2, 1), GanttTimescaleUnit.Month, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(2, cells.Count); // 2025, 2026
        Assert.Equal("2026", cells[1].Caption);
    }

    [Fact]
    public void MajorTimescale_AlwaysCoversTheMinorRange()
    {
        foreach (var unit in Enum.GetValues<GanttTimescaleUnit>())
        {
            var minor = GanttEngine.BuildTimescale(new DateTime(2026, 3, 15), new DateTime(2026, 8, 20), unit, System.Globalization.CultureInfo.InvariantCulture);
            var major = GanttEngine.BuildMajorTimescale(new DateTime(2026, 3, 15), new DateTime(2026, 8, 20), unit, System.Globalization.CultureInfo.InvariantCulture);
            Assert.NotEmpty(major);
            Assert.True(major[0].Start <= minor[0].Start, $"{unit}: major must not start after the minor");
            Assert.True(major[^1].End >= minor[^1].End, $"{unit}: major must not end before the minor");
        }
    }

    [Fact]
    public void SummaryCompletion_WeightedByDescendantDuration()
    {
        var day = new DateTime(2026, 1, 1);
        var tasks = new[]
        {
            new GanttTask("sum", "Summary", day, day.AddDays(10)),                     // the parent
            new GanttTask("a", "A", day, day.AddDays(2), 100, parentId: "sum"),         // 2d * 100%
            new GanttTask("b", "B", day, day.AddDays(6), 50, parentId: "sum"),          // 6d * 50%
            new GanttTask("b1", "B1", day, day.AddDays(4), 25, parentId: "b"),          // nested: 4d * 25%
        };

        // (2*100 + 6*50 + 4*25) / 12 = 600 / 12 = 50
        Assert.Equal(50, GanttEngine.SummaryCompletion(tasks[0], tasks));

        // A parentless task asked as a summary yields zero (nothing to roll up).
        Assert.Equal(0, GanttEngine.SummaryCompletion(new GanttTask("leaf", "Leaf", day, day), tasks));
    }

    [Fact]
    public void SummaryCompletion_NoChildren_Zero()
    {
        var leaf = new GanttTask("l", "L", new DateTime(2026, 1, 1), new DateTime(2026, 1, 3), 90);
        Assert.Equal(0, GanttEngine.SummaryCompletion(leaf, new[] { leaf }));
    }
}
