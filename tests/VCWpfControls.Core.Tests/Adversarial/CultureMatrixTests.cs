using System.Globalization;
using VCWpfControls.Gantt;
using VCWpfControls.Grid.Data;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Core.Tests.Adversarial;

/// <summary>
/// The culture matrix: the same input must produce equivalent results under representative
/// cultures — Monday-first (ru-RU), Sunday-first (en-US), CJK (zh-CN), comma-decimal
/// (de-DE), non-Latin digits (ar-SA with Arabic numerals off via standard digit shapes)
/// and InvariantCulture. Culture may change CAPTIONS and string conversion, never
/// occurrence dates or numeric truth.
/// </summary>
public class CultureMatrixTests
{
    private static readonly string[] CultureNames = { "ru-RU", "en-US", "zh-CN", "de-DE", "ar-SA", "" };

    private static IEnumerable<CultureInfo> Cultures
        => CultureNames.Select(CultureInfo.GetCultureInfo);

    [Fact]
    public void Scheduler_WeeklyExpansion_OccurrenceDatesAreCultureIndependent()
    {
        // The SAME weekday set must yield the SAME dates regardless of the week's first day.
        var master = new SchedulerAppointment("w", "Weekly", new DateTime(2026, 5, 4, 10, 0, 0), new DateTime(2026, 5, 4, 11, 0, 0))
        {
            Recurrence = new SchedulerRecurrence
            {
                Pattern = SchedulerRecurrencePattern.Weekly,
                DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
            },
        };

        var reference = default(IReadOnlyList<DateTime>);
        foreach (var culture in Cultures)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
            try
            {
                var appointment = new SchedulerAppointment("w", "Weekly", master.Start, master.End)
                {
                    Recurrence = new SchedulerRecurrence
                    {
                        Pattern = SchedulerRecurrencePattern.Weekly,
                        DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                    },
                };
                var dates = SchedulerEngine.Expand(new[] { appointment }, new DateTime(2026, 5, 1), new DateTime(2026, 5, 29))
                    .Select(o => o.Start)
                    .ToList();

                Assert.NotEmpty(dates);
                Assert.All(dates, d => Assert.True(d.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday, $"{culture}: wrong weekday {d.DayOfWeek}"));

                if (reference is null)
                {
                    reference = dates;
                }
                else
                {
                    Assert.Equal(reference, dates); // byte-identical across cultures
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }

    [Fact]
    public void Clipboard_NumericRoundTrip_PerCulture()
    {
        foreach (var culture in Cultures)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
            try
            {
                const decimal value = 1234.56m;
                var text = value.ToString("0.00", CultureInfo.CurrentCulture);
                Assert.True(GridClipboard.TryConvert(text, typeof(decimal), out var parsed), $"{culture}: parse failed for '{text}'");
                Assert.Equal(value, (decimal)parsed!);

                var date = new DateTime(2026, 5, 4);
                Assert.True(GridClipboard.TryConvert(date.ToString("d", CultureInfo.CurrentCulture), typeof(DateTime), out var parsedDate), $"{culture}: date parse failed");
                Assert.Equal(date, parsedDate);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }

    [Fact]
    public void Gantt_TimescaleCaptions_NonEmptyUnderEveryCulture()
    {
        foreach (var culture in Cultures)
        {
            foreach (var unit in Enum.GetValues<GanttTimescaleUnit>())
            {
                var cells = GanttEngine.BuildTimescale(new DateTime(2026, 5, 4), new DateTime(2026, 6, 15), unit, culture);
                Assert.NotEmpty(cells);
                Assert.All(cells, c => Assert.False(string.IsNullOrWhiteSpace(c.Caption), $"{culture}/{unit}: empty caption"));
            }
        }
    }

    [Fact]
    public void Scheduler_ViewRange_WeekAlwaysSpansSevenDays()
    {
        foreach (var culture in Cultures)
        {
            var (start, end) = SchedulerEngine.ViewRange(SchedulerViewType.Week, new DateTime(2026, 5, 6), culture);
            Assert.Equal(7, (end - start).Days); // the window is 7 days whatever the first day is
            Assert.Equal(start, SchedulerEngine.AlignToWeek(new DateTime(2026, 5, 6), culture.DateTimeFormat.FirstDayOfWeek));
        }
    }
}
