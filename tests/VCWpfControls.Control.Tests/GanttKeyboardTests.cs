using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Gantt;
using Xunit;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// The Gantt keyboard/a11y parity round: the timeline was mouse-only — Up/Down walk the
/// tasks (selection outlines the bar), Enter activates, Delete removes, Shift+arrows
/// move a task by a day, Ctrl+arrows move the finish edge, T scrolls to today; the
/// chart exposes an automation surface (task DataItems with SelectionItem/Invoke).
/// </summary>
public class GanttKeyboardTests
{
    private static readonly DateTime May1 = new(2026, 5, 1);

    private static GanttTask T(string id, string name, int start, int duration, bool milestone = false)
        => new(id, name, May1.AddDays(start), May1.AddDays(start + duration), 0, milestone);

    private static GanttView Create(out Window window, ObservableCollection<GanttTask>? tasks = null)
    {
        var view = new GanttView { Width = 900, Height = 360 };
        view.Tasks = tasks ?? new ObservableCollection<GanttTask>
        {
            T("a", "Analysis", 0, 3),
            T("b", "Build", 3, 5),
            T("m", "Kickoff done", 3, 0, milestone: true),
        };
        window = TestWindowHost.Show(view);
        TestWindowHost.Pump();
        return view;
    }

    private static void SendKey(UIElement target, Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        var source = PresentationSource.FromVisual(target);
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source!, 0, key)
        {
            RoutedEvent = UIElement.KeyDownEvent,
            Source = target,
        });
    }

    [StaFact]
    public void Arrows_WalkSelection_EnterActivates()
    {
        var view = Create(out var window);
        try
        {
            Assert.Null(view.SelectedTask);

            SendKey(view, Key.Down);
            Assert.Equal("Analysis", view.SelectedTask!.Name);

            SendKey(view, Key.Down);
            Assert.Equal("Build", view.SelectedTask!.Name);

            SendKey(view, Key.Up);
            Assert.Equal("Analysis", view.SelectedTask!.Name);

            GanttTask? activated = null;
            view.TaskActivated += (_, task) => activated = task;
            SendKey(view, Key.Enter);
            Assert.Same(view.SelectedTask, activated);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ShiftArrows_MoveTask_CtrlArrows_ResizeFinish()
    {
        var view = Create(out var window);
        try
        {
            SendKey(view, Key.Down); // Analysis: May 1 -> May 4
            // PLAIN arrows must NOT reschedule (looking around must not mutate the plan).
            SendKey(view, Key.Right);
            TestWindowHost.Pump();
            Assert.Equal(May1, view.SelectedTask!.Start);

            view.ModifiersForTest = () => ModifierKeys.Shift;
            SendKey(view, Key.Right);
            view.ModifiersForTest = null;
            TestWindowHost.Pump();
            Assert.Equal(May1.AddDays(1), view.SelectedTask!.Start);
            Assert.Equal(May1.AddDays(4), view.SelectedTask!.Finish);

            view.ModifiersForTest = () => ModifierKeys.Control;
            SendKey(view, Key.Right);
            view.ModifiersForTest = null;
            TestWindowHost.Pump();
            Assert.Equal(May1.AddDays(5), view.SelectedTask!.Finish); // duration +1
            Assert.Equal(May1.AddDays(1), view.SelectedTask!.Start); // start untouched
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Delete_RemovesTask_T_ScrollsToToday_NoCrash()
    {
        var tasks = new ObservableCollection<GanttTask> { T("a", "Analysis", 0, 3), T("b", "Build", 3, 5) };
        var view = Create(out var window, tasks);
        try
        {
            SendKey(view, Key.Down);
            var doomed = view.SelectedTask!;
            var removed = new List<GanttTask>();
            view.TaskRemoved += (_, task) => removed.Add(task);

            SendKey(view, Key.Delete);
            TestWindowHost.Pump();
            Assert.Single(tasks, task => task.Name == "Build");
            Assert.Null(view.SelectedTask);
            Assert.Same(doomed, Assert.Single(removed));
            Assert.DoesNotContain(view.Timeline!.Tasks, task => ReferenceEquals(task, doomed)); // no ghost bar

            SendKey(view, Key.T); // scrolls; no crash is the contract here
            TestWindowHost.Pump();
            Assert.False(window is null);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void AutomationSurface_ExposesTasks_SelectAndInvoke()
    {
        var view = Create(out var window);
        try
        {
            var peer = (GanttViewAutomationPeer)System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(view)!;
            var children = peer.GetChildren();
            // Base children (tree, timeline) + the task peers appended — count them.
            Assert.True(children.Count >= view.Tasks!.Count, $"expected >= {view.Tasks!.Count} children, got {children.Count}");

            var build = Assert.IsType<GanttTaskPeer>(children.OfType<GanttTaskPeer>().Single(p => p.Task.Name == "Build"));
            Assert.Contains("Build", build.GetName());
            Assert.False(build.IsSelected);

            ((ISelectionItemProvider)build).Select();
            TestWindowHost.Pump();
            Assert.True(build.IsSelected);
            Assert.Same(build.Task, view.SelectedTask);

            GanttTask? invoked = null;
            view.TaskActivated += (_, task) => invoked = task;
            ((IInvokeProvider)build).Invoke();
            Assert.Same(build.Task, invoked);
        }
        finally
        {
            window.Close();
        }
    }
}
