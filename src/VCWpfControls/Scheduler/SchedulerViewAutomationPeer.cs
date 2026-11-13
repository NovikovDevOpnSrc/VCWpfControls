using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace VCWpfControls.Scheduler;

/// <summary>
/// The automation surface of a <see cref="SchedulerView"/>: the visible occurrences are
/// exposed as virtual DataItem children (name = "Subject, start – end"), each supporting
/// SelectionItem (select = the control's <see cref="SchedulerView.SelectedOccurrence"/>)
/// and Invoke (the double-click activation channel). Without this peer the whole calendar
/// is OnRender pixels — invisible to screen readers (acceptance finding).
/// </summary>
public sealed class SchedulerViewAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly SchedulerView _owner;
    private readonly List<SchedulerOccurrencePeer> _occurrencePeers = new();

    /// <summary>Creates the calendar peer.</summary>
    public SchedulerViewAutomationPeer(SchedulerView owner)
        : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "SchedulerView";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Calendar;

    /// <summary>The occurrence peers (stable instances — UIA requires identity across queries).</summary>
    internal IList<SchedulerOccurrencePeer> OccurrencePeers
    {
        get
        {
            SyncOccurrences();
            return _occurrencePeers;
        }
    }

    /// <inheritdoc />
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        SyncOccurrences();
        return _occurrencePeers.Cast<AutomationPeer>().ToList();
    }

    private void SyncOccurrences()
    {
        var occurrences = _owner.VisibleOccurrences;
        if (_occurrencePeers.Count != occurrences.Count
            || occurrences.Count > 0
                && (!ReferenceEquals(_occurrencePeers[0].Occurrence, occurrences[0])
                    || !ReferenceEquals(_occurrencePeers[^1].Occurrence, occurrences[^1])))
        {
            _occurrencePeers.Clear();
            foreach (var occurrence in occurrences)
            {
                _occurrencePeers.Add(new SchedulerOccurrencePeer(this, _owner, occurrence));
            }
        }
    }

    /// <summary>Announces a selection change to automation clients (raised by the view).</summary>
    internal void RaiseSelectionChanged()
        => RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
}

/// <summary>
/// Virtual peer of one visible occurrence: DataItem + SelectionItem (the control's
/// single-selection) + Invoke (OccurrenceDoubleClicked — the activation channel).
/// </summary>
public sealed class SchedulerOccurrencePeer : AutomationPeer, ISelectionItemProvider, IInvokeProvider
{
    private readonly SchedulerViewAutomationPeer _parent;
    private readonly SchedulerView _owner;

    /// <summary>Creates the occurrence peer.</summary>
    internal SchedulerOccurrencePeer(SchedulerViewAutomationPeer parent, SchedulerView owner, SchedulerOccurrence occurrence)
    {
        _parent = parent;
        _owner = owner;
        Occurrence = occurrence;
    }

    /// <summary>The immutable occurrence this peer addresses (identity = per rebuild).</summary>
    internal SchedulerOccurrence Occurrence { get; }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "SchedulerOccurrence";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    /// <summary>"Subject, start – end" — what a screen reader announces.</summary>
    protected override string GetNameCore()
        => string.Create(CultureInfo.CurrentCulture, $"{Occurrence.Subject}, {Occurrence.Start:g} – {Occurrence.End:g}");

    /// <inheritdoc />
    protected override string GetAutomationIdCore()
        => string.Create(CultureInfo.InvariantCulture, $"Occurrence_{Occurrence.Appointment.Id}_{Occurrence.Start.Ticks}");

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.SelectionItem or PatternInterface.Invoke ? this : null;

    /// <inheritdoc />
    protected override bool IsPasswordCore() => false;

    /// <inheritdoc />
    public bool IsSelected => ReferenceEquals(_owner.SelectedOccurrence, Occurrence);

    /// <inheritdoc />
    public void Select() => _owner.SelectedOccurrence = Occurrence;

    /// <inheritdoc />
    public void AddToSelection() => Select();

    /// <inheritdoc />
    public void RemoveFromSelection()
    {
        // single-selection calendar: removal is not a concept
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

    /// <summary>Invoke = the double-click channel (open an editor).</summary>
    public void Invoke() => _owner.ActivateOccurrenceForAutomation(Occurrence);

    /// <summary>
    /// Best effort: the realized block (day views) or chip (month view) in screen
    /// coordinates; empty when the occurrence renders nowhere.
    /// </summary>
    protected override System.Windows.Rect GetBoundingRectangleCore()
    {
        if (_owner.DaysPanel is { } days)
        {
            foreach (var day in _owner.CurrentDays)
            {
                if (days.BlockOf(Occurrence, day) is { } block)
                {
                    return ToScreen(days, block);
                }
            }

            if (days.AllDayChipOf(Occurrence) is { } chip)
            {
                return ToScreen(days, chip);
            }
        }

        if (_owner.MonthPanel is { } month)
        {
            for (var day = Occurrence.Start.Date; day < Occurrence.End; day = day.AddDays(1))
            {
                if (month.ChipOf(Occurrence, day) is { } chip)
                {
                    return ToScreen(month, chip);
                }
            }
        }

        return default;
    }

    private static System.Windows.Rect ToScreen(System.Windows.Media.Visual visual, System.Windows.Rect rect)
    {
        if (rect.IsEmpty || (visual is System.Windows.UIElement { IsArrangeValid: false }))
        {
            return default;
        }

        var topLeft = visual.PointToScreen(rect.TopLeft);
        var bottomRight = visual.PointToScreen(rect.BottomRight);
        return new System.Windows.Rect(topLeft, bottomRight);
    }

    /// <summary>Stable runtime id for UIA identity.</summary>
    public int[] GetOccurrenceRuntimeId()
        => new[] { Occurrence.Appointment.Id.GetHashCode(StringComparison.Ordinal), Occurrence.Start.Ticks.GetHashCode() };

    /// <summary>Occurrences select through the owner (the virtual item has no HWND of its own).</summary>
    protected override void SetFocusCore()
    {
        _owner.SelectedOccurrence = Occurrence;
        _owner.Focus();
    }

    /// <summary>Keyboard focus lives on the scheduler view, not on virtual occurrences.</summary>
    protected override bool HasKeyboardFocusCore() => false;

    /// <summary>Virtual occurrences are reachable through the view's keyboard surface.</summary>
    protected override bool IsKeyboardFocusableCore() => false;

    /// <inheritdoc />
    protected override AutomationPeer? GetLabeledByCore() => null;

    /// <summary>On-screen when the occurrence renders a block or chip.</summary>
    protected override bool IsOffscreenCore() => GetBoundingRectangle() != default;

    /// <inheritdoc />
    protected override bool IsEnabledCore() => true;

    /// <inheritdoc />
    protected override bool IsRequiredForFormCore() => false;

    /// <summary>Hosted by the scheduler view; no own HWND.</summary>
    protected override HostedWindowWrapper? GetHostRawElementProviderCore() => null;

    // --- trivial overrides required by the abstract AutomationPeer contract ---

    /// <inheritdoc />
    protected override string GetAcceleratorKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetAccessKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetHelpTextCore()
        => string.Create(CultureInfo.CurrentCulture, $"{Occurrence.Start:g} – {Occurrence.End:g}");

    /// <inheritdoc />
    protected override string GetItemStatusCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetItemTypeCore() => "calendar occurrence";

    /// <inheritdoc />
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc />
    protected override bool IsControlElementCore() => true;

    /// <inheritdoc />
    protected override AutomationOrientation GetOrientationCore() => AutomationOrientation.None;

    /// <inheritdoc />
    protected override List<AutomationPeer>? GetChildrenCore() => null;

    /// <inheritdoc />
    protected override System.Windows.Point GetClickablePointCore() => default;
}
