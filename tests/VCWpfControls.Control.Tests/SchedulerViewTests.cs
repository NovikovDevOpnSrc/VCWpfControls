using System.Collections.ObjectModel;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The SchedulerView control: day/work-week/week/month views rendered from the headless
/// engine — day columns, block geometry (time → height), overlap lanes, the all-day band,
/// the hour window, overnight segmentation, month chips/overflow, navigation and selection.
/// </summary>
public class SchedulerViewTests
{
    private static readonly DateTime Nov9 = new(2026, 11, 9); // Monday
    private static readonly DateTime Nov10 = new(2026, 11, 10);
    private static readonly DateTime Nov12 = new(2026, 11, 12);

    private static SchedulerAppointment A(string id, string subject, DateTime start, DateTime end, bool allDay = false)
        => new(id, subject, start, end) { IsAllDay = allDay };

    private static SchedulerView Create(out Window window, params SchedulerAppointment[] appointments)
    {
        var view = new SchedulerView
        {
            Width = 800,
            Height = 420,
            AnchorDate = Nov9,
            Appointments = new ObservableCollection<SchedulerAppointment>(appointments),
        };
        window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        return view;
    }

    private static SchedulerOccurrence OccurrenceOf(SchedulerView view, string subject, DateTime? day = null)
        => view.VisibleOccurrences.First(o => o.Subject == subject && (day is null || o.Start.Date == day.Value.Date));

    [StaFact]
    public void WorkWeek_ShowsFiveColumnsFromMonday()
    {
        var view = Create(out var window);
        try
        {
            Assert.NotNull(view.DaysPanel);
            var days = view.CurrentDays;
            Assert.Equal(5, days.Count);
            Assert.Equal(Nov9, days[0]); // WorkWeek always starts Monday
            Assert.Equal(new DateTime(2026, 11, 13), days[4]);
            Assert.Equal(Visibility.Collapsed, view.MonthPanel!.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void BlockGeometry_TimeMapsToHeight()
    {
        var view = Create(out var window, A("m1", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11.5)));
        try
        {
            var panel = view.DaysPanel!;
            var meeting = OccurrenceOf(view, "Meeting");
            var block = panel.BlockOf(meeting, Nov9);
            Assert.NotNull(block);
            // 10:00 with the default 0..24 window: top = header + 10 hours; 1.5h tall.
            Assert.Equal(panel.ContentTop + 10 * panel.HourHeight, block!.Value.Top, 2);
            Assert.Equal(1.5 * panel.HourHeight, block.Value.Height, 2);
            // Inside the first day column.
            Assert.True(block.Value.X >= panel.ColumnX(0), "block must start inside its column");
            Assert.True(block.Value.Right <= panel.ColumnX(1) + 0.5, "block must not cross into the next column");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void OverlapsRenderSideBySide_SequentialStacksFullWidth()
    {
        var view = Create(out var window,
            A("a", "A", Nov9.AddHours(10), Nov9.AddHours(11)),
            A("b", "B", Nov9.AddHours(10), Nov9.AddHours(11)),
            A("c", "C", Nov9.AddHours(12), Nov9.AddHours(13)));
        try
        {
            var panel = view.DaysPanel!;
            var a = panel.BlockOf(OccurrenceOf(view, "A"), Nov9)!.Value;
            var b = panel.BlockOf(OccurrenceOf(view, "B"), Nov9)!.Value;
            var c = panel.BlockOf(OccurrenceOf(view, "C"), Nov9)!.Value;

            // The overlapping pair splits the column into two lanes.
            Assert.Equal(panel.DayWidth / 2 - 2, a.Width, 1);
            Assert.Equal(panel.DayWidth / 2 - 2, b.Width, 1);
            Assert.Equal(panel.DayWidth / 2, Math.Abs(b.X - a.X), 1);
            Assert.True(a.Top < c.Top, "the later meeting renders below");

            // The stand-alone one after a gap gets the full column width.
            Assert.Equal(panel.DayWidth - 2, c.Width, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RecurrenceExpandsAcrossDays()
    {
        var standup = A("s", "Standup", Nov9.AddHours(9), Nov9.AddHours(9.25));
        standup.Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Count = 5 };
        var view = Create(out var window, standup);
        try
        {
            Assert.Equal(5, view.VisibleOccurrences.Count(o => o.Subject == "Standup"));
            foreach (var day in view.CurrentDays)
            {
                var block = view.DaysPanel!.BlockOf(OccurrenceOf(view, "Standup", day), day);
                Assert.NotNull(block);
                Assert.Equal(0.25 * view.DaysPanel!.HourHeight, block!.Value.Height, 2);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AllDayBand_ChipsAboveTheHourGrid()
    {
        var view = Create(out var window,
            A("conf", "Conference", Nov10, Nov10.AddDays(1), allDay: true),
            A("m", "Meeting", Nov10.AddHours(10), Nov10.AddHours(11)));
        try
        {
            var panel = view.DaysPanel!;
            Assert.Equal(1, panel.AllDayBandRows);

            var conference = OccurrenceOf(view, "Conference");
            var chip = panel.AllDayChipOf(conference);
            Assert.NotNull(chip);
            Assert.True(chip!.Value.Y < panel.ContentTop, "the all-day chip must sit above the hour grid");
            Assert.Equal(panel.DayWidth - 4, chip.Value.Width, 1); // spans exactly one day column

            // All-day occurrences never render in the hour area.
            Assert.Null(panel.BlockOf(conference, Nov10));

            // Timed content starts below the band.
            var meeting = panel.BlockOf(OccurrenceOf(view, "Meeting"), Nov10);
            Assert.NotNull(meeting);
            Assert.True(meeting!.Value.Top >= panel.ContentTop);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void HourWindow_ClipsEarlyBlocks()
    {
        var view = Create(out var window,
            A("early", "Early", Nov9.AddHours(7), Nov9.AddHours(7.5)),
            A("mid", "Mid", Nov9.AddHours(10), Nov9.AddHours(11)));
        try
        {
            view.DayStartHour = 8;
            view.DayEndHour = 18;
            TestWindowHost.Pump();

            var panel = view.DaysPanel!;
            Assert.Equal(10, panel.VisibleHourCount);
            Assert.Null(panel.BlockOf(OccurrenceOf(view, "Early"), Nov9)); // fully outside the window

            var mid = panel.BlockOf(OccurrenceOf(view, "Mid"), Nov9)!.Value;
            Assert.Equal(panel.ContentTop + 2 * panel.HourHeight, mid.Top, 2); // 10:00 = window start + 2h
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void OvernightOccurrence_SegmentsBothDays()
    {
        var view = Create(out var window, A("on", "OnCall", Nov10.AddHours(22), Nov10.AddHours(26)));
        try
        {
            var panel = view.DaysPanel!;
            var onCall = OccurrenceOf(view, "OnCall");

            var firstNight = panel.BlockOf(onCall, Nov10)!.Value; // 22:00–24:00
            Assert.Equal(panel.ContentTop + 22 * panel.HourHeight, firstNight.Top, 2);
            Assert.Equal(2 * panel.HourHeight, firstNight.Height, 2);

            var morning = panel.BlockOf(onCall, Nov10.AddDays(1))!.Value; // 00:00–02:00
            Assert.Equal(panel.ContentTop, morning.Top, 2);
            Assert.Equal(2 * panel.HourHeight, morning.Height, 2);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MonthView_ShowsSixWeekGridWithChips()
    {
        var view = Create(out var window, A("m", "Review", Nov12.AddHours(10), Nov12.AddHours(11)));
        try
        {
            view.ViewType = SchedulerViewType.Month;
            TestWindowHost.Pump();

            var month = view.MonthPanel!;
            Assert.Equal(Visibility.Visible, month.Visibility);
            Assert.Equal(42, month.Days.Count);
            Assert.Empty(view.CurrentDays); // the days panel holds no columns in the month view

            var cell = month.CellOf(Nov12);
            Assert.NotNull(cell);
            var review = OccurrenceOf(view, "Review");
            var chip = month.ChipOf(review, Nov12);
            Assert.NotNull(chip);
            Assert.True(chip!.Value.X >= cell!.Value.X - 0.5 && chip.Value.Right <= cell.Value.Right + 0.5,
                "the chip must stay inside its day cell");
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MonthView_OverflowsBeyondTwoChipsPerCell()
    {
        var view = Create(out var window,
            A("a", "One", Nov12.AddHours(9), Nov12.AddHours(10)),
            A("b", "Two", Nov12.AddHours(11), Nov12.AddHours(12)),
            A("c", "Three", Nov12.AddHours(13), Nov12.AddHours(14)),
            A("d", "Four", Nov12.AddHours(15), Nov12.AddHours(16)));
        try
        {
            view.ViewType = SchedulerViewType.Month;
            TestWindowHost.Pump();

            var month = view.MonthPanel!;
            Assert.NotNull(month.ChipOf(OccurrenceOf(view, "One"), Nov12));
            Assert.NotNull(month.ChipOf(OccurrenceOf(view, "Two"), Nov12));
            Assert.Null(month.ChipOf(OccurrenceOf(view, "Three"), Nov12)); // overflow row
            Assert.Null(month.ChipOf(OccurrenceOf(view, "Four"), Nov12));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Navigate_StepsByViewUnit()
    {
        var view = Create(out var window);
        try
        {
            view.ViewType = SchedulerViewType.Day;
            view.Navigate(1);
            Assert.Equal(Nov10, view.AnchorDate);
            view.Navigate(-2);
            Assert.Equal(new DateTime(2026, 11, 8), view.AnchorDate);

            view.ViewType = SchedulerViewType.Week;
            view.AnchorDate = Nov9;
            view.Navigate(1);
            Assert.Equal(Nov9.AddDays(7), view.AnchorDate);

            view.ViewType = SchedulerViewType.Month;
            view.AnchorDate = Nov9;
            view.Navigate(1);
            Assert.Equal(new DateTime(2026, 12, 9), view.AnchorDate);
            Assert.Equal(42, view.MonthPanel!.Days.Count); // the month view rebuilt for December
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void GoToToday_ResetsAnchor()
    {
        var view = Create(out var window);
        try
        {
            view.GoToToday();
            Assert.Equal(DateTime.Today, view.AnchorDate);
            // WorkWeek always starts Monday: assert the ALIGNMENT, not "Start == Today"
            // (the old form only held while tests ran on a Monday).
            Assert.Equal(SchedulerEngine.AlignToWeek(DateTime.Today, DayOfWeek.Monday), view.CurrentRange.Start);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ClickSelectsOccurrence_DoubleClickRaises()
    {
        var view = Create(out var window, A("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11)));
        try
        {
            var meeting = OccurrenceOf(view, "Meeting");
            var raised = 0;
            view.OccurrenceDoubleClicked += (_, _) => raised++;

            view.DaysPanel!.ClickOccurrence(meeting);
            Assert.Same(meeting, view.SelectedOccurrence); // mirrored into the bindable DP

            view.DaysPanel!.ClickOccurrence(meeting);
            Assert.Equal(1, raised); // the quick second click = double-click
        }
        finally
        {
            window.Close();
        }
    }
}
