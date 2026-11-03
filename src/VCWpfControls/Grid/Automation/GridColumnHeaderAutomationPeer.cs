using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid.Automation;

/// <summary>
/// Automation peer of a <see cref="GridColumnHeader"/>: name = header text, Invoke = sort
/// by the column (cycles asc/desc/none like a click).
/// </summary>
public sealed class GridColumnHeaderAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly GridColumnHeader _owner;

    /// <summary>Creates the peer.</summary>
    public GridColumnHeaderAutomationPeer(GridColumnHeader owner)
        : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GridColumnHeader";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;

    /// <summary>The header text as the name (text content is extracted, not ToString).</summary>
    protected override string GetNameCore()
    {
        if (_owner.Column is { } column)
        {
            var text = GridColumnBase.HeaderText(column.Header);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return column.FieldName;
        }

        return base.GetNameCore() ?? string.Empty;
    }

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    void IInvokeProvider.Invoke()
    {
        if (_owner.Column is { } column && _owner.Owner is { } grid)
        {
            var next = column.SortDirection switch
            {
                Data.GridSortDirection.None => Data.GridSortDirection.Ascending,
                Data.GridSortDirection.Ascending => Data.GridSortDirection.Descending,
                _ => Data.GridSortDirection.None,
            };
            grid.SortBy(column, next);
        }
    }
}
