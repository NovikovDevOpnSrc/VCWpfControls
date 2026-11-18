using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace VCWpfControls.Gantt;

/// <summary>
/// The automation surface of a <see cref="GanttView"/>: the visible tasks are exposed as
/// virtual DataItem children (name = "Name, start – finish") supporting SelectionItem
/// (select = the view's <see cref="GanttView.SelectedTask"/>) and Invoke (the
/// <see cref="GanttView.TaskActivated"/> channel) — the timeline was OnRender pixels,
/// invisible to screen readers.
/// </summary>
public sealed class GanttViewAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly GanttView _owner;
    private readonly List<GanttTaskPeer> _taskPeers = new();

    /// <summary>Creates the chart peer.</summary>
    public GanttViewAutomationPeer(GanttView owner)
        : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GanttView";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane; // no Chart in UIA's control types — Pane + task children

    /// <summary>The task peers (stable instances — UIA requires identity across queries).</summary>
    internal IList<GanttTaskPeer> TaskPeers => _taskPeers;

    /// <inheritdoc />
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var tasks = _owner.Tasks;
        if (_taskPeers.Count != tasks?.Count
            || (tasks?.Count > 0
                && (!ReferenceEquals(_taskPeers[0].Task, tasks[0]) || !ReferenceEquals(_taskPeers[^1].Task, tasks[^1])))
            || (tasks?.Count > 1 && _taskPeers.Count == tasks.Count && tasks.Cast<GanttTask>().Zip(_taskPeers, (t, p) => (t, p)).Any(x => !ReferenceEquals(x.t, x.p.Task))))
        {
            _taskPeers.Clear();
            foreach (var task in tasks ?? (IEnumerable<GanttTask>)Array.Empty<GanttTask>())
            {
                _taskPeers.Add(new GanttTaskPeer(this, _owner, task));
            }
        }

        // Append the task peers AFTER the base children (the tree grid, the timeline):
        // returning ONLY them hides the interactive children from UIA — the Gantt's
        // tree grid disappeared from the FlaUI tree until the base list was kept.
        var children = base.GetChildrenCore() ?? new List<AutomationPeer>();
        children.AddRange(_taskPeers);
        return children;
    }

    /// <summary>
    /// Announces a selection change to automation clients: the event is raised on the
    /// SELECTED TASK peer (a Pane without the SelectionItem pattern is ignored by UIA
    /// clients — announcing on the view announced nothing).
    /// </summary>
    internal void RaiseSelectionChanged()
    {
        var selected = _owner.SelectedTask;
        if (selected is null)
        {
            return;
        }

        var peer = _taskPeers.FirstOrDefault(p => ReferenceEquals(p.Task, selected));
        peer?.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
    }
}

/// <summary>
/// Virtual peer of one task: DataItem + SelectionItem (the view's single selection) +
/// Invoke (the TaskActivated channel).
/// </summary>
public sealed class GanttTaskPeer : AutomationPeer, ISelectionItemProvider, IInvokeProvider
{
    private readonly GanttViewAutomationPeer _parent;
    private readonly GanttView _owner;

    /// <summary>Creates the task peer.</summary>
    internal GanttTaskPeer(GanttViewAutomationPeer parent, GanttView owner, GanttTask task)
    {
        _parent = parent;
        _owner = owner;
        Task = task;
    }

    /// <summary>The immutable task this peer addresses (identity = per rebuild).</summary>
    internal GanttTask Task { get; }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GanttTask";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    /// <summary>"Name, start – finish" — what a screen reader announces.</summary>
    protected override string GetNameCore()
        => string.Create(CultureInfo.CurrentCulture, $"{Task.Name}, {Task.Start:d} – {Task.Finish:d}");

    /// <inheritdoc />
    protected override string GetAutomationIdCore()
        => string.Create(CultureInfo.InvariantCulture, $"GanttTask_{Task.Id}");

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.SelectionItem or PatternInterface.Invoke ? this : null;

    /// <inheritdoc />
    public bool IsSelected => ReferenceEquals(_owner.SelectedTask, Task);

    /// <inheritdoc />
    public void Select() => _owner.SelectedTask = Task;

    /// <inheritdoc />
    public void AddToSelection() => Select();

    /// <inheritdoc />
    public void RemoveFromSelection()
    {
        // single-selection chart: removal is not a concept
    }

    /// <inheritdoc />
    public IRawElementProviderSimple SelectionContainer
    {
        get
        {
            var provider = ProviderFromPeer(_parent);
            return provider!;
        }
    }

    /// <summary>Invoke = the activation channel (Enter / double-click).</summary>
    public void Invoke() => _owner.ActivateTaskForAutomation(Task);

    /// <inheritdoc />
    protected override System.Windows.Rect GetBoundingRectangleCore() => default;

    /// <summary>Tasks select through the owner (the virtual item has no HWND of its own).</summary>
    protected override void SetFocusCore()
    {
        _owner.SelectedTask = Task;
        _owner.Focus();
    }

    /// <summary>Hosted by the chart view; no own HWND.</summary>
    protected override HostedWindowWrapper? GetHostRawElementProviderCore() => null;

    // --- trivial overrides required by the abstract AutomationPeer contract ---

    /// <inheritdoc />
    protected override string GetAcceleratorKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetAccessKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetHelpTextCore()
        => string.Create(CultureInfo.CurrentCulture, $"{Task.Start:d} – {Task.Finish:d}");

    /// <inheritdoc />
    protected override string GetItemStatusCore()
        => string.Create(CultureInfo.CurrentCulture, $"{Task.Completion}%");

    /// <inheritdoc />
    protected override string GetItemTypeCore() => "gantt task";

    /// <inheritdoc />
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc />
    protected override bool IsControlElementCore() => true;

    /// <inheritdoc />
    protected override AutomationOrientation GetOrientationCore()
        => AutomationOrientation.None;

    /// <inheritdoc />
    protected override bool HasKeyboardFocusCore() => false;

    /// <inheritdoc />
    protected override bool IsKeyboardFocusableCore() => false;

    /// <inheritdoc />
    protected override AutomationPeer? GetLabeledByCore() => null;

    /// <inheritdoc />
    protected override bool IsOffscreenCore() => false;

    /// <inheritdoc />
    protected override bool IsPasswordCore() => false;

    /// <inheritdoc />
    protected override bool IsEnabledCore() => true;

    /// <inheritdoc />
    protected override bool IsRequiredForFormCore() => false;

    /// <inheritdoc />
    protected override List<AutomationPeer>? GetChildrenCore() => null;

    /// <inheritdoc />
    protected override System.Windows.Point GetClickablePointCore() => default;
}
