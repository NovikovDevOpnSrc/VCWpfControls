using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid.Automation;

/// <summary>
/// Automation peer of a <see cref="GridRow"/>: DataItem with SelectionItem support, so UIA
/// clients (FlaUI, screen readers) can find, read and select rows.
/// </summary>
public sealed class GridRowAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
{
    private readonly GridRow _owner;

    /// <summary>Creates the peer.</summary>
    public GridRowAutomationPeer(GridRow owner)
        : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GridRow";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    /// <summary>The row's name: values of the visible columns joined with spaces.</summary>
    protected override string GetNameCore()
    {
        if (_owner.DataItem is not { } item || _owner.Owner is not { } grid)
        {
            return base.GetNameCore() ?? string.Empty;
        }

        var parts = new List<string>();
        foreach (var column in grid.VisibleColumns)
        {
            var text = Convert.ToString(column.GetCellValue(item), System.Globalization.CultureInfo.CurrentCulture);
            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(text);
            }
        }

        return parts.Count > 0 ? string.Join(" ", parts) : base.GetNameCore() ?? string.Empty;
    }

    bool ISelectionItemProvider.IsSelected => _owner.IsSelected;

    void ISelectionItemProvider.Select()
    {
        if (_owner.DataItem is { } item && _owner.Owner is { } grid)
        {
            if (grid.SelectionMode == GridSelectionMode.Extended)
            {
                grid.UnselectAll();
            }

            grid.SelectItem(item);
        }
    }

    void ISelectionItemProvider.AddToSelection()
    {
        if (_owner.DataItem is { } item
            && _owner.Owner is { } grid
            && grid.SelectionMode == GridSelectionMode.Extended)
        {
            grid.SelectItem(item);
        }
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        if (_owner.DataItem is { } item && _owner.Owner is { } grid)
        {
            grid.UnselectItem(item);
        }
    }

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.SelectionItem ? this : base.GetPattern(patternInterface);

    IRawElementProviderSimple? ISelectionItemProvider.SelectionContainer
        => ProviderFromPeer(new GridControlAutomationPeer(_owner.Owner!));
}
