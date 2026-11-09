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

    /// <summary>Identifies the <see cref="TimescaleUnit"/> dependency property.</summary>
    public static readonly DependencyProperty TimescaleUnitProperty = DependencyProperty.Register(
        nameof(TimescaleUnit), typeof(GanttTimescaleUnit), typeof(GanttView),
        new PropertyMetadata(GanttTimescaleUnit.Day, OnUnitChanged));

    /// <summary>Identifies the <see cref="TaskRowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty TaskRowHeightProperty = DependencyProperty.Register(
        nameof(TaskRowHeight), typeof(double), typeof(GanttView),
        new PropertyMetadata(26d, OnRowHeightChanged));

    private TreeListView? _tree;
    private GanttTimelinePanel? _timeline;
    private ScrollViewer? _treeScroll;
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
        if (GetTemplateChild("PART_TimelineScroll") is ScrollViewer timelineScroll && _timeline is not null)
        {
            timelineScroll.ScrollChanged += (_, e) =>
            {
                if (_timeline is not null)
                {
                    _timeline.HorizontalOffset = e.HorizontalOffset;
                    _timeline.VerticalSyncOffset = e.VerticalOffset;
                    _timeline.ViewportHeight = e.ViewportHeight;
                }

                if (_treeScroll is not null && Math.Abs(_treeScroll.VerticalOffset - e.VerticalOffset) > 0.5)
                {
                    _treeScroll.ScrollToVerticalOffset(e.VerticalOffset);
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
            _timeline.LinkCreated += OnTimelineLinkCreated;
        }

        Rebuild();
    }

    /// <summary>Raised after a bar was moved/resized (dates already on the task).</summary>
    public event EventHandler<GanttTask>? TaskEdited;

    /// <summary>Raised after the user drew a link (already added to <see cref="Dependencies"/>).</summary>
    public event EventHandler<GanttDependency>? LinkCreated;

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

    private void _treesCrollHook()
    {
        // Find the tree's internal ScrollViewer to mirror the vertical offset.
        _treeScroll = _tree is null ? null : FindScroll(_tree);
        if (_treeScroll is not null)
        {
            _treeScroll.ScrollChanged += (_, e) =>
            {
                if (_timeline is not null && _treeScroll is not null
                    && Math.Abs(_timeline.VerticalSyncOffset - e.VerticalOffset) > 0.5)
                {
                    _timeline.VerticalSyncOffset = e.VerticalOffset;
                }
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
