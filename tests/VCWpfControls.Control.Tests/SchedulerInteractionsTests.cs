using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Localization;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The scheduler stage-3 interactions: drag-move (timed and all-day), edge resize with the
/// minimum-duration clamp, snap quantization, empty-space create, the recurring-series guard
/// — through the internal hooks AND one real synthesized-mouse path.
/// </summary>
public class SchedulerInteractionsTests
{
    private static readonly DateTime Nov9 = new(2026, 11, 9); // Monday
    private static readonly DateTime Nov10 = new(2026, 11, 10);
    private static readonly DateTime Nov11 = new(2026, 11, 11);

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

    private static SchedulerOccurrence OccurrenceOf(SchedulerView view, string subject)
        => view.VisibleOccurrences.First(o => o.Subject == subject);

    [StaFact]
    public void MoveOccurrence_Timed_ShiftsStartAndEnd()
    {
        var appointment = new SchedulerAppointment("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11));
        var view = Create(out var window, appointment);
        try
        {
            var edited = 0;
            view.AppointmentEdited += (_, _) => edited++;

            var before = OccurrenceOf(view, "Meeting");
            view.DaysPanel!.MoveOccurrence(before, 0, TimeSpan.FromMinutes(90));

            Assert.Equal(Nov9.AddHours(11.5), appointment.Start);
            Assert.Equal(Nov9.AddHours(12.5), appointment.End);
            Assert.Equal(1, edited);

            // The view rebuilt: the fresh occurrence renders at the new time.
            var after = OccurrenceOf(view, "Meeting");
            var block = view.DaysPanel!.BlockOf(after, Nov9);
            Assert.NotNull(block);
            Assert.Equal(view.DaysPanel!.ContentTop + 11.5 * view.DaysPanel.HourHeight, block!.Value.Top, 2);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MoveOccurrence_CrossesIntoNextDay()
    {
        var appointment = new SchedulerAppointment("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11));
        var view = Create(out var window, appointment);
        try
        {
            view.DaysPanel!.MoveOccurrence(OccurrenceOf(view, "Meeting"), 1, TimeSpan.Zero);

            Assert.Equal(Nov10.AddHours(10), appointment.Start);
            var after = OccurrenceOf(view, "Meeting");
            Assert.Equal(Nov10, after.Start.Date);
            Assert.NotNull(view.DaysPanel!.BlockOf(after, Nov10));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MoveOccurrence_RecurringSeries_IsIgnored()
    {
        var appointment = new SchedulerAppointment("s", "Standup", Nov9.AddHours(9), Nov9.AddHours(9.5))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily, Count = 3 },
        };
        var view = Create(out var window, appointment);
        try
        {
            var edited = 0;
            view.AppointmentEdited += (_, _) => edited++;
            var standup = OccurrenceOf(view, "Standup");
            var originalStart = appointment.Start;

            view.DaysPanel!.MoveOccurrence(standup, 2, TimeSpan.FromHours(3));

            Assert.Equal(originalStart, appointment.Start); // series masters are not editable
            Assert.Equal(0, edited);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MoveOccurrence_AllDay_ShiftsDaysOnly()
    {
        var appointment = new SchedulerAppointment("conf", "Conference", Nov10, Nov11) { IsAllDay = true };
        var view = Create(out var window, appointment);
        try
        {
            view.DaysPanel!.MoveOccurrence(OccurrenceOf(view, "Conference"), 1, TimeSpan.FromHours(5));

            Assert.Equal(Nov11, appointment.Start); // whole days, the time delta is ignored
            Assert.Equal(new DateTime(2026, 11, 12), appointment.End);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ResizeEdges_KeepAtLeastOneSnapOfDuration()
    {
        var appointment = new SchedulerAppointment("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11));
        var view = Create(out var window, appointment);
        try
        {
            var meeting = OccurrenceOf(view, "Meeting");

            // Dragging the start FORWARD past the end clamps to one snap (15 min default);
            // dragging it backwards would legitimately lengthen the appointment.
            view.DaysPanel!.ResizeOccurrenceStart(meeting, TimeSpan.FromHours(3));
            Assert.Equal(Nov9.AddHours(10.75), appointment.Start);
            Assert.Equal(Nov9.AddHours(11), appointment.End);

            // Dragging the end before the start clamps too.
            view.DaysPanel!.ResizeOccurrenceEnd(meeting, TimeSpan.FromHours(-3));
            Assert.Equal(Nov9.AddHours(10.75), appointment.Start);
            Assert.Equal(Nov9.AddHours(10.75 + 0.25), appointment.End);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void MoveOccurrence_SnapsToSnapMinutes()
    {
        var appointment = new SchedulerAppointment("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11));
        var view = Create(out var window, appointment);
        try
        {
            // 10 minutes rounds UP to the 15-minute grid; 8 minutes rounds DOWN to no move.
            view.DaysPanel!.MoveOccurrence(OccurrenceOf(view, "Meeting"), 0, TimeSpan.FromMinutes(10));
            Assert.Equal(Nov9.AddHours(10.25), appointment.Start);

            view.DaysPanel!.MoveOccurrence(OccurrenceOf(view, "Meeting"), 0, TimeSpan.FromMinutes(-8));
            Assert.Equal(Nov9.AddHours(10.25 - 0.25), appointment.Start);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CreateFromRange_AddsLocalizedAppointment_RendersBlock()
    {
        var view = Create(out var window);
        try
        {
            var created = 0;
            SchedulerAppointment? createdAppointment = null;
            view.AppointmentCreated += (_, appointment) =>
            {
                created++;
                createdAppointment = appointment;
            };

            view.DaysPanel!.CreateFromRange(Nov9.AddHours(14), Nov9.AddHours(15.5));

            Assert.Equal(1, created);
            Assert.NotNull(createdAppointment);
            Assert.Equal(GridStrings.SchedulerNewAppointment, createdAppointment!.Subject);
            Assert.Equal(Nov9.AddHours(14), createdAppointment.Start);
            Assert.Equal(Nov9.AddHours(15.5), createdAppointment.End);
            Assert.Single(view.Appointments!); // added to the collection

            // The dragged-out range renders as a new block.
            var fresh = OccurrenceOf(view, GridStrings.SchedulerNewAppointment);
            Assert.NotNull(view.DaysPanel!.BlockOf(fresh, Nov9));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void CreateFromRange_ReversedRangeIsOrdered_MinimumIsOneSnap()
    {
        var view = Create(out var window);
        try
        {
            view.DaysPanel!.CreateFromRange(Nov9.AddHours(15), Nov9.AddHours(14)); // dragged upwards
            var appointment = view.Appointments!.Single();
            Assert.Equal(Nov9.AddHours(14), appointment.Start);
            Assert.Equal(Nov9.AddHours(15), appointment.End);

            view.DaysPanel!.CreateFromRange(Nov9.AddHours(16), Nov9.AddHours(16)); // a click, not a drag
            Assert.Equal(Nov9.AddHours(16.25), view.Appointments!.Last().End);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void RealMousePath_DragBlockDown_MovesOneSnappedHour()
    {
        var appointment = new SchedulerAppointment("m", "Meeting", Nov9.AddHours(10), Nov9.AddHours(11));
        var view = Create(out var window, appointment);
        try
        {
            var panel = view.DaysPanel!;
            var meeting = OccurrenceOf(view, "Meeting");
            var block = panel.BlockOf(meeting, Nov9)!.Value;
            var start = new Point(block.X + block.Width / 2, block.Y + block.Height / 2);
            var end = new Point(start.X, start.Y + panel.HourHeight); // exactly one hour down

            // Synthesized events carry no position — the panel's test seam supplies it while
            // the REAL OnMouseLeftButtonDown/Move/Up path runs (zones, capture, commit).
            panel.NextPositionForTest = () => start;
            panel.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = panel,
            });

            panel.NextPositionForTest = () => end;
            panel.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
            {
                RoutedEvent = UIElement.MouseMoveEvent,
                Source = panel,
            });

            panel.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = panel,
            });
            panel.NextPositionForTest = null;
            TestWindowHost.Pump();

            Assert.Equal(Nov9.AddHours(11), appointment.Start); // snapped 60 minutes
            Assert.Equal(Nov9.AddHours(12), appointment.End);
        }
        finally
        {
            window.Close();
        }
    }
}
