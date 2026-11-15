using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Scheduler;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Scheduler accessibility round (ui-review acceptance finding: the calendar had NO
/// automation surface — occurrences were OnRender pixels, invisible to screen readers):
/// the view peer exposes the visible occurrences as DataItem children with SelectionItem
/// (the control's selection) and Invoke (the double-click channel). Plus the pinned-hour-
/// window hidden-content cues and keyboard-selection BringIntoView.
/// </summary>
public class SchedulerAccessibilityTests
{
    private static readonly DateTime Nov9 = new(2026, 11, 9); // Monday

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

    private static SchedulerOccurrencePeer PeerOf(SchedulerView view, string subject)
    {
        var peer = (SchedulerViewAutomationPeer)System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(view)!;
        var occurrence = view.VisibleOccurrences.First(o => o.Subject == subject);
        return peer.OccurrencePeers.First(p => ReferenceEquals(p.Occurrence, occurrence));
    }

    [StaFact]
    public void CalendarPeer_ExposesOccurrencesAsDataItems()
    {
        var view = Create(out var window,
            new SchedulerAppointment("a", "Standup", Nov9.AddHours(9), Nov9.AddHours(9.5)),
            new SchedulerAppointment("b", "Planning", Nov9.AddHours(14), Nov9.AddHours(15)));
        try
        {
            var peer = (SchedulerViewAutomationPeer)System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(view)!;
            Assert.NotNull(peer);

            var children = peer.GetChildren();
            Assert.Equal(view.VisibleOccurrences.Count, children.Count);

            var standup = children.Cast<SchedulerOccurrencePeer>().Single(p => p.Occurrence.Subject == "Standup");
            Assert.Contains("Standup", standup.GetName());
            Assert.Contains("9:", standup.GetName()); // the time range is part of the name
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void OccurrencePeer_Selects_AndInvokesTheDoubleClickChannel()
    {
        var view = Create(out var window,
            new SchedulerAppointment("a", "Standup", Nov9.AddHours(9), Nov9.AddHours(9.5)));
        try
        {
            SchedulerOccurrence? activated = null;
            view.OccurrenceDoubleClicked += (_, occurrence) => activated = occurrence;

            var peer = PeerOf(view, "Standup");
            Assert.False(peer.IsSelected);

            ((ISelectionItemProvider)peer).Select();
            Assert.True(peer.IsSelected);
            Assert.Same(peer.Occurrence, view.SelectedOccurrence);

            ((IInvokeProvider)peer).Invoke();
            Assert.Same(peer.Occurrence, activated);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void PinnedHourWindow_CountsHiddenOccurrences_AndCuesTheDay()
    {
        // A FIXED Wednesday (the lesson from the GoToToday date-dependence bug: a test
        // mixing DateTime.Today with a week-aligned range breaks when Today lands on a
        // weekend — Today must not appear in the WorkWeek days then).
        var wednesday = new DateTime(2026, 11, 11);
        var oncall = new SchedulerAppointment("oncall", "On-call handover",
            wednesday.AddHours(22), wednesday.AddHours(26)); // overnight, crosses BOTH edges
        var view = new SchedulerView
        {
            Width = 800,
            Height = 420,
            AnchorDate = wednesday,
            DayStartHour = 8,
            DayEndHour = 20,
            Appointments = new ObservableCollection<SchedulerAppointment> { oncall },
        };
        var window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        try
        {
            var panel = view.DaysPanel!;
            var dayIndex = -1;
            for (var i = 0; i < view.CurrentDays.Count; i++)
            {
                if (view.CurrentDays[i] == wednesday)
                {
                    dayIndex = i;
                    break;
                }
            }

            Assert.True(dayIndex >= 0, "the anchored Wednesday must be in the work week");

            var (above, below) = panel.HiddenCounts(dayIndex);
            Assert.True(below >= 1, "the 22:00 part hides below the 8-20 window");
            Assert.Equal(0, above); // the Wednesday segment starts INSIDE the day, after the window

            var nextIndex = dayIndex + 1 < view.CurrentDays.Count ? dayIndex + 1 : dayIndex;
            if (nextIndex != dayIndex)
            {
                var (nextAbove, _) = panel.HiddenCounts(nextIndex);
                Assert.True(nextAbove >= 1, "the 00:00-02:00 part hides above the next day's window");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void KeyboardSelection_BringsOffscreenOccurrencesIntoView()
    {
        var late = new SchedulerAppointment("late", "Late sync", Nov9.AddHours(19), Nov9.AddHours(20));
        var view = new SchedulerView
        {
            Width = 700,
            Height = 260, // small viewport; HourHeight 120 puts 19:00 far below it
            AnchorDate = Nov9,
            HourHeight = 120,
            DayStartHour = 8,
            DayEndHour = 20,
            Appointments = new ObservableCollection<SchedulerAppointment> { late },
        };
        var window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        try
        {
            Assert.Equal(0, view.DaysPanel!.VerticalOffset, 0); // starts at the top

            view.RaiseKeyDownForTest(Key.Down); // selects the only occurrence — 19:00 is off-screen
            TestWindowHost.Pump();

            Assert.True(view.DaysPanel!.VerticalOffset > 0,
                $"selection must scroll into view (offset {view.DaysPanel.VerticalOffset})");
        }
        finally
        {
            window.Close();
        }
    }
}

file static class SchedulerViewTestExtensions
{
    internal static void RaiseKeyDownForTest(this SchedulerView view, Key key)
    {
        var source = PresentationSource.FromVisual(view);
        view.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.KeyDownEvent,
            Source = view,
        });
    }
}
