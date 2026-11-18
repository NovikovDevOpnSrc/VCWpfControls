using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.TreeList;

namespace VCWpfControls.Gantt;

/// <summary>
/// A Gantt chart control: a <see cref="TreeListView"/> with the task hierarchy on the left
/// and a <see cref="GanttTimelinePanel"/> with bars/dependencies on the right, sharing row
/// height and the vertical scroll. Optionally auto-schedules and highlights the critical
/// path (both through the headless <see cref="GanttEngine"/>).
/// </summary>
public class GanttView : Control
{
    /// <summary>Identifies the <see cref="Tasks"/> dependency property.</summary>
    public static readonly DependencyProperty TasksProperty = DependencyProperty.Register(
        nameof(Tasks), typeof(ObservableCollection<GanttTask>), typeof(GanttView),
        new PropertyMetadata(null, OnDataChanged));

    /// <summary>Identifies the <see cref="Dependencies"/> dependency property.</summary>
    public static readonly DependencyProperty DependenciesProperty = DependencyProperty.Register(
        nameof(Dependencies), typeof(ObservableCollection<GanttDependency>), typeof(GanttView),
        new PropertyMetadata(null, OnDataChanged));

    /// <summary>Identifies the <see cref="AutoSchedule"/> dependency property.</summary>
    public static readonly DependencyProperty AutoScheduleProperty = DependencyProperty.Register(
        nameof(AutoSchedule), typeof(bool), typeof(GanttView),
        new PropertyMetadata(Boxes.False, OnDataChanged));

    /// <summary>Identifies the <see cref="ShowCriticalPath"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCriticalPathProperty = DependencyProperty.Register(
        nameof(ShowCriticalPath), typeof(bool), typeof(GanttView),
        new PropertyMetadata(Boxes.False, OnDataChanged));

    /// <summary>Identifies the <see cref="Calendar"/> dependency property.</summary>
    public static readonly DependencyProperty CalendarProperty = DependencyProperty.Register(
        nameof(Calendar), typeof(GanttCalendar), typeof(GanttView),
        new PropertyMetadata(null, OnDataChanged));

    /// <summary>Identifies the <see cref="ShowBaseline"/> dependency property.</summary>
    public static readonly DependencyProperty ShowBaselineProperty = DependencyProperty.Register(
        nameof(ShowBaseline), typeof(bool), typeof(GanttView),
        new PropertyMetadata(Boxes.True, OnDataChanged));

    /// <summary>Identifies the <see cref="ShowLegend"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(
        nameof(ShowLegend), typeof(bool), typeof(GanttView),
        new PropertyMetadata(Boxes.True, OnDataChanged));

    /// <summary>Identifies the <see cref="ShowBarLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowBarLabelsProperty = DependencyProperty.Register(
        nameof(ShowBarLabels), typeof(bool), typeof(GanttView),
        new PropertyMetadata(Boxes.False, OnDataChanged));

    /// <summary>Identifies the <see cref="TimescaleUnit"/> dependency property.</summary>
    public static readonly DependencyProperty TimescaleUnitProperty = DependencyProperty.Register(
        nameof(TimescaleUnit), typeof(GanttTimescaleUnit), typeof(GanttView),
        new PropertyMetadata(GanttTimescaleUnit.Day, OnUnitChanged));

    /// <summary>Identifies the <see cref="TaskRowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty TaskRowHeightProperty = DependencyProperty.Register(
        nameof(TaskRowHeight), typeof(double), typeof(GanttView),
        new PropertyMetadata(26d, OnRowHeightChanged));

    /// <summary>Identifies the <see cref="SelectedTask"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTaskProperty = DependencyProperty.Register(
        nameof(SelectedTask), typeof(GanttTask), typeof(GanttView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTaskChanged));

    private TreeListView? _tree;
    private GanttTimelinePanel? _timeline;
    private ScrollViewer? _treeScroll;
    private ScrollViewer? _timelineScroll;
    private IReadOnlyList<string> _criticalIds = Array.Empty<string>();

    private static class Boxes
    {
        public static readonly object False = false;
        public static readonly object True = true;
    }

    static GanttView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GanttView), new FrameworkPropertyMetadata(typeof(GanttView)));
    }

    /// <inheritdoc />
    protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new GanttViewAutomationPeer(this);

    /// <summary>Creates the view.</summary>
    public GanttView()
    {
        Tasks = new ObservableCollection<GanttTask>();
        Dependencies = new ObservableCollection<GanttDependency>();
    }

    /// <summary>The tasks (hierarchy through <see cref="GanttTask.ParentId"/>).</summary>
    public ObservableCollection<GanttTask>? Tasks
    {
        get => (ObservableCollection<GanttTask>?)GetValue(TasksProperty);
        set => SetValue(TasksProperty, value);
    }

    /// <summary>The dependency arrows.</summary>
    public ObservableCollection<GanttDependency>? Dependencies
    {
        get => (ObservableCollection<GanttDependency>?)GetValue(DependenciesProperty);
        set => SetValue(DependenciesProperty, value);
    }

    /// <summary>Whether the engine shifts violating successors on load/change.</summary>
    public bool AutoSchedule
    {
        get => (bool)GetValue(AutoScheduleProperty);
        set => SetValue(AutoScheduleProperty, value);
    }

    /// <summary>Whether critical-path tasks (and their links) render accented.</summary>
    public bool ShowCriticalPath
    {
        get => (bool)GetValue(ShowCriticalPathProperty);
        set => SetValue(ShowCriticalPathProperty, value);
    }

    /// <summary>
    /// The working-time calendar: weekends/holidays tint on the timeline and, with
    /// <see cref="AutoSchedule"/>, durations and lags count in working days (null = none).
    /// </summary>
    public GanttCalendar? Calendar
    {
        get => (GanttCalendar?)GetValue(CalendarProperty);
        set => SetValue(CalendarProperty, value);
    }

    /// <summary>Whether baseline bars render under the actual bars (tasks with a baseline).</summary>
    public bool ShowBaseline
    {
        get => (bool)GetValue(ShowBaselineProperty);
        set => SetValue(ShowBaselineProperty, value);
    }

    /// <summary>Whether the legend strip renders pinned to the bottom of the timeline.</summary>
    public bool ShowLegend
    {
        get => (bool)GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    /// <summary>Whether task names render as labels after the bars (default off, Oracle).</summary>
    public bool ShowBarLabels
    {
        get => (bool)GetValue(ShowBarLabelsProperty);
        set => SetValue(ShowBarLabelsProperty, value);
    }

    /// <summary>The timeline zoom unit.</summary>
    public GanttTimescaleUnit TimescaleUnit
    {
        get => (GanttTimescaleUnit)GetValue(TimescaleUnitProperty);
        set => SetValue(TimescaleUnitProperty, value);
    }

    /// <summary>The task row height (both halves stay in lock-step).</summary>
    public double TaskRowHeight
    {
        get => (double)GetValue(TaskRowHeightProperty);
        set => SetValue(TaskRowHeightProperty, value);
    }

    /// <summary>The left tree grid (test hook).</summary>
    internal TreeListView? Tree => _tree;

    /// <summary>The right timeline (test hook).</summary>
    internal GanttTimelinePanel? Timeline => _timeline;

    /// <summary>The timeline's scroll viewer (test hook).</summary>
    internal ScrollViewer? TimelineScroll => _timelineScroll;

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((GanttView)d).Rebuild();

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (GanttView)d;
        if (view._timeline is not null)
        {
            view._timeline.Unit = view.TimescaleUnit;
        }
    }

    private static void OnRowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (GanttView)d;
        if (view._tree is not null)
        {
            view._tree.RowHeight = view.TaskRowHeight;
        }

        if (view._timeline is not null)
        {
            view._timeline.RowHeight = view.TaskRowHeight;
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _tree = GetTemplateChild("PART_Tree") as TreeListView;
        _timeline = GetTemplateChild("PART_Timeline") as GanttTimelinePanel;
        _timelineScroll = GetTemplateChild("PART_TimelineScroll") as ScrollViewer;
        if (_timelineScroll is not null && _timeline is not null)
        {
            _timelineScroll.ScrollChanged += (_, e) =>
            {
                if (_timeline is not null)
                {
                    _timeline.HorizontalOffset = e.HorizontalOffset;
                    _timeline.VerticalSyncOffset = e.VerticalOffset;
                    _timeline.ViewportHeight = e.ViewportHeight;
                }

                if (_treeScroll is not null && _timeline is not null)
                {
                    // The tree scrolls in ITEM units (virtualizing panel), the timeline in
                    // pixels — convert, or a pixel offset clamps the tree to its last item.
                    var itemOffset = Math.Round(e.VerticalOffset / _timeline.RowHeight);
                    if (Math.Abs(_treeScroll.VerticalOffset - itemOffset) > 0.5)
                    {
                        _treeScroll.ScrollToVerticalOffset(itemOffset);
                    }
                }
            };
        }

        if (_tree is not null)
        {
            _tree.RowHeight = TaskRowHeight;
            _tree.CellEditEnded += OnTreeCellEditEnded;
            _treesCrollHook();
        }

        if (_timeline is not null)
        {
            _timeline.RowHeight = TaskRowHeight;
            _timeline.Unit = TimescaleUnit;
            _timeline.TaskEdited += OnTimelineTaskEdited;
            _timeline.Owner = this;
            _timeline.RequestSelectTask += task => SelectedTask = task;
            _timeline.RequestScrollTo += offset =>
            {
                _timelineScroll?.ScrollToHorizontalOffset(offset);
            };
            _timeline.LinkCreated += OnTimelineLinkCreated;
            _timeline.ZoomStepRequested += OnTimelineZoomStep;
            _timeline.PanRequested += OnTimelinePan;
        }

        Rebuild();
    }

    /// <summary>The selected task (bindable two-way; the timeline outlines its bar).</summary>
    public GanttTask? SelectedTask
    {
        get => (GanttTask?)GetValue(SelectedTaskProperty);
        set => SetValue(SelectedTaskProperty, value);
    }

    /// <summary>Raised after a bar was moved/resized (dates already on the task).</summary>
    public event EventHandler<GanttTask>? TaskEdited;

    /// <summary>Raised when the user activates a task (Enter or double-click channel).</summary>
    public event EventHandler<GanttTask>? TaskActivated;

    /// <summary>Raised after the Delete key removed a task from <see cref="Tasks"/>.</summary>
    public event EventHandler<GanttTask>? TaskRemoved;

    /// <summary>Raised after the user drew a link (already added to <see cref="Dependencies"/>).</summary>
    public event EventHandler<GanttDependency>? LinkCreated;

    /// <summary>
    /// The keyboard surface: Up/Down walk the tasks, Enter activates the selection,
    /// Delete removes it, Shift+Left/Right move the task by a day, Ctrl+Left/Right move
    /// the finish edge (duration), T scrolls to today — the timeline was mouse-only
    /// before. Keys the focused tree/editors already consumed never arrive here.
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var tasks = Tasks;
        if (tasks is null || tasks.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case System.Windows.Input.Key.Down or System.Windows.Input.Key.Up:
            {
                var delta = e.Key == System.Windows.Input.Key.Down ? 1 : -1;
                var index = IndexOfTask(SelectedTask, tasks);
                SelectedTask = tasks[Math.Clamp(index < 0 ? (delta > 0 ? 0 : tasks.Count - 1) : index + delta, 0, tasks.Count - 1)];
                e.Handled = true;
                break;
            }

            case System.Windows.Input.Key.Enter when SelectedTask is { } activated:
                TaskActivated?.Invoke(this, activated);
                e.Handled = true;
                break;

            case System.Windows.Input.Key.Delete when SelectedTask is { } doomed:
                RemoveTask(doomed);
                e.Handled = true;
                break;

            case System.Windows.Input.Key.Left or System.Windows.Input.Key.Right
                when SelectedTask is { } shifted && SelectedTask.IsMilestone == false
                && (EffectiveModifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)
                    || EffectiveModifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)):
            {
                var delta = e.Key == System.Windows.Input.Key.Right ? 1 : -1;
                if (EffectiveModifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                {
                    _timeline?.ResizeTaskFinish(shifted, delta);
                }
                else
                {
                    _timeline?.MoveTask(shifted, delta);
                }

                RefreshAfterEdit();
                e.Handled = true;
                break;
            }

            case System.Windows.Input.Key.T:
                ScrollToToday();
                e.Handled = true;
                break;
        }
    }

    /// <summary>Test seam for the keyboard modifiers (null = the real device state).</summary>
    internal Func<System.Windows.Input.ModifierKeys>? ModifiersForTest { get; set; }

    private System.Windows.Input.ModifierKeys EffectiveModifiers
        => ModifiersForTest?.Invoke() ?? System.Windows.Input.Keyboard.Modifiers;

    /// <summary>The automation activation channel (Invoke on a task peer).</summary>
    internal void ActivateTaskForAutomation(GanttTask task)
        => TaskActivated?.Invoke(this, task);

    private static int IndexOfTask(GanttTask? task, IReadOnlyList<GanttTask> tasks)
    {
        if (task is null)
        {
            return -1;
        }

        for (var i = 0; i < tasks.Count; i++)
        {
            if (ReferenceEquals(tasks[i], task))
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveTask(GanttTask task)
    {
        if (Tasks is not System.Collections.IList mutable || !mutable.Contains(task))
        {
            return;
        }

        SelectedTask = null;
        mutable.Remove(task);
        RefreshAfterEdit(); // the DP does NOT change on collection mutations — rebuild explicitly (ghost row otherwise)
        TaskRemoved?.Invoke(this, task);
    }

    /// <summary>Scrolls the timeline so today's line is visible.</summary>
    public void ScrollToToday()
    {
        if (_timeline is null || _timelineScroll is null)
        {
            return;
        }

        _timeline.BringDayIntoView(DateTime.Today, _timelineScroll.ViewportWidth);
    }

    private static void OnSelectedTaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (GanttView)d;
        if (view._timeline is not null)
        {
            view._timeline.SelectedTask = view.SelectedTask;
        }

        // The tree mirrors the selection (its own row highlight) and BOTH halves scroll
        // the row into view — walking past the viewport had zero visible feedback before.
        view.SyncTreeSelection();
        view.BringTaskIntoView(view.SelectedTask);

        if (System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(view) is GanttViewAutomationPeer peer)
        {
            peer.RaiseSelectionChanged();
        }
    }

    private void SyncTreeSelection()
    {
        if (_tree is null || SelectedTask is not { } task)
        {
            return;
        }

        for (var i = 0; i < _tree.DataEngine.View.Count; i++)
        {
            if (_tree.DataEngine.View[i] is Grid.Data.GridDataRow row && ReferenceEquals(row.Item, task))
            {
                _tree.SelectedItem = row.Item;
                _tree.ScrollIntoView(row.Item);
                break;
            }
        }
    }

    private void BringTaskIntoView(GanttTask? task)
    {
        if (task is null || _timeline is null || _timelineScroll is null || Tasks is not { } tasks)
        {
            return;
        }

        for (var i = 0; i < tasks.Count; i++)
        {
            if (!ReferenceEquals(tasks[i], task))
            {
                continue;
            }

            var rowTop = _timeline.HeaderHeight + i * _timeline.RowHeight;
            var viewportTop = _timeline.VerticalSyncOffset;
            var viewportHeight = _timelineScroll.ViewportHeight;
            if (rowTop < viewportTop)
            {
                _timelineScroll.ScrollToVerticalOffset(Math.Max(0, rowTop - 8));
            }
            else if (rowTop + _timeline.RowHeight > viewportTop + viewportHeight)
            {
                _timelineScroll.ScrollToVerticalOffset(rowTop + _timeline.RowHeight - viewportHeight + 8);
            }

            break;
        }
    }

    private void OnTimelineTaskEdited(object? sender, GanttTask task)
    {
        RefreshAfterEdit();
        TaskEdited?.Invoke(this, task);
    }

    private void OnTreeCellEditEnded(object? sender, GridCellEditEventArgs e)
    {
        if (e.CancelEdit || e.Row is not GanttTaskNode node)
        {
            return;
        }

        if (e.Column.FieldName is not ("Start" or "Finish" or "Completion"))
        {
            return; // name edits don't touch the schedule
        }

        // The write-through node already updated the task; rebuild so the timeline,
        // auto-schedule and the critical path see the new dates.
        RefreshAfterEdit();
        TaskEdited?.Invoke(this, node.Task);
    }

    private void OnTimelineZoomStep(object? sender, int steps)
    {
        var current = (int)TimescaleUnit;
        var target = Math.Clamp(current + steps, 0, Enum.GetValues<GanttTimescaleUnit>().Length - 1);
        if (target != current)
        {
            TimescaleUnit = (GanttTimescaleUnit)target; // the DP drives the panel and app bindings
        }
    }

    private void OnTimelinePan(object? sender, double pixelIncrement)
    {
        if (_timelineScroll is not null)
        {
            _timelineScroll.ScrollToHorizontalOffset(_timelineScroll.HorizontalOffset + pixelIncrement);
        }
    }

    private void OnTimelineLinkCreated(object? sender, (GanttTask Predecessor, GanttTask Successor, GanttLinkType Type) link)
    {
        var dependency = new GanttDependency(link.Predecessor.Id, link.Successor.Id, link.Type);
        Dependencies?.Add(dependency);
        RefreshAfterEdit();
        LinkCreated?.Invoke(this, dependency);
    }

    /// <summary>
    /// Re-runs auto-schedule/critical and refreshes both halves. A full rebuild (not just
    /// a timeline repaint): the tree rows show the task dates through one-way bindings on
    /// <see cref="GanttTaskNode"/>, which have no change notification — only fresh
    /// wrappers pick up the new dates.
    /// </summary>
    internal void RefreshAfterEdit() => Rebuild();

    private bool _treeScrollHooked;

    private void _treesCrollHook()
    {
        // The tree's OWN template (with its ScrollViewer) applies during its measure pass —
        // NOT yet at the owner's OnApplyTemplate. Find the viewer deferred, after layout.
        if (_treeScrollHooked)
        {
            return;
        }

        _treeScrollHooked = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(HookTreeScroll));
    }

    private void HookTreeScroll()
    {
        _treeScrollHooked = false;
        // Find the tree's internal ScrollViewer to mirror the vertical offset.
        _treeScroll = _tree is null ? null : FindScroll(_tree);
        if (_treeScroll is not null)
        {
            _treeScroll.ScrollChanged += (_, e) =>
            {
                if (_timeline is null || TaskRowHeight <= 0)
                {
                    return;
                }

                // The tree scrolls in ITEM units — convert to the timeline's pixels and
                // scroll its own viewer: the culling band alone would draw the scrolled-to
                // rows while the content stays at offset 0 (a blank timeline).
                var pixelOffset = e.VerticalOffset * TaskRowHeight;
                if (_timelineScroll is not null && Math.Abs(_timelineScroll.VerticalOffset - pixelOffset) > 0.5)
                {
                    _timelineScroll.ScrollToVerticalOffset(pixelOffset);
                }

                _timeline.VerticalSyncOffset = pixelOffset;
            };
        }
    }

    private static ScrollViewer? FindScroll(DependencyObject root)
    {
        if (root is ScrollViewer viewer)
        {
            return viewer;
        }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (FindScroll(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>Rebuilds the tree grid rows and the timeline from the task list.</summary>
    public void Rebuild()
    {
        if (_tree is null || _timeline is null)
        {
            return;
        }

        var tasks = Tasks ?? new ObservableCollection<GanttTask>();
        var links = Dependencies ?? new ObservableCollection<GanttDependency>();
        if (AutoSchedule)
        {
            GanttEngine.AutoSchedule(tasks, links, Calendar);
        }

        _criticalIds = ShowCriticalPath ? GanttEngine.CriticalPath(tasks, links) : Array.Empty<string>();

        _tree.ChildMemberPath = nameof(GanttTaskNode.Children);
        _tree.Columns.Clear();
        _tree.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Task", Width = new GridColumnWidth(220, GridUnitType.Pixel) });
        _tree.Columns.Add(new GridDateColumn { FieldName = "Start", Header = "Start", Width = new GridColumnWidth(100, GridUnitType.Pixel) });
        _tree.Columns.Add(new GridDateColumn { FieldName = "Finish", Header = "Finish", Width = new GridColumnWidth(100, GridUnitType.Pixel) });
        _tree.Columns.Add(new GridNumericColumn { FieldName = "Completion", Header = "%", Width = new GridColumnWidth(50, GridUnitType.Pixel) });
        _tree.ItemsSource = BuildHierarchy(tasks);
        _tree.ExpandAll();

        _timeline.Tasks = VisibleRowsOf(_tree, tasks);
        _timeline.Dependencies = links;
        _timeline.CriticalTaskIds = _criticalIds;
        _timeline.Calendar = Calendar;
        _timeline.ShowBaseline = ShowBaseline;
        _timeline.ShowLegend = ShowLegend;
        _timeline.ShowBarLabels = ShowBarLabels;
    }

    /// <summary>Wraps the flat tasks into hierarchy view-models for the tree grid.</summary>
    private static ObservableCollection<GanttTaskNode> BuildHierarchy(ObservableCollection<GanttTask> tasks)
    {
        var nodes = tasks.ToDictionary(t => t.Id, t => new GanttTaskNode(t));
        var roots = new ObservableCollection<GanttTaskNode>();
        foreach (var task in tasks)
        {
            if (task.ParentId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(nodes[task.Id]);
            }
            else
            {
                roots.Add(nodes[task.Id]);
            }
        }

        return roots;
    }

    /// <summary>The tasks in the order the tree grid renders them (expanded), so the bars align row-for-row.</summary>
    private static IReadOnlyList<GanttTask> VisibleRowsOf(TreeListView tree, ObservableCollection<GanttTask> tasks)
    {
        // Collect the tasks in tree-render order via the materialized containers.
        // The tree grid's Items are ALREADY the flattened render order (expanded tree,
        // parent first) — one row per visible node, children not duplicated.
        var ordered = new List<GanttTask>();
        foreach (var wrapper in tree.Items)
        {
            if (wrapper is Grid.Data.GridDataRow data && data.Item is GanttTaskNode node)
            {
                ordered.Add(node.Task);
            }
        }

        return ordered;
    }
}

/// <summary>The tree-grid row of one task: the task itself plus child rows. The bound
/// columns write THROUGH to the wrapped task, so inline tree editing reschedules the chart.</summary>
public sealed class GanttTaskNode
{
    /// <summary>Creates the node.</summary>
    public GanttTaskNode(GanttTask task)
    {
        Task = task;
    }

    /// <summary>The wrapped task.</summary>
    public GanttTask Task { get; }

    /// <summary>The display name (bound column; writes through).</summary>
    public string Name
    {
        get => Task.Name;
        set => Task.Name = value;
    }

    /// <summary>The start (bound column; writes through).</summary>
    public DateTime Start
    {
        get => Task.Start;
        set => Task.Start = value;
    }

    /// <summary>The finish (bound column; writes through).</summary>
    public DateTime Finish
    {
        get => Task.Finish;
        set => Task.Finish = value;
    }

    /// <summary>The completion percent (bound column; writes through).</summary>
    public int Completion
    {
        get => Task.Completion;
        set => Task.Completion = value;
    }

    /// <summary>The child rows.</summary>
    public List<GanttTaskNode> Children { get; } = new();
}
