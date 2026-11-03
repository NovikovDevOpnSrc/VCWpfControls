using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Automation;

/// <summary>
/// Virtual automation peer of one cell: a (row, column) pair addressable through
/// <c>GridPattern.GetItem(row, column)</c> and as a child of the row's DataItem.
/// The name is the formatted cell value (StringFormat and NullText honored).
/// </summary>
public sealed class GridCellAutomationPeer : AutomationPeer
{
    private readonly GridRowAutomationPeer _rowPeer;
    private readonly GridControl _grid;
    private readonly GridColumnBase _column;

    /// <summary>Creates the cell peer for a (row, column) pair.</summary>
    public GridCellAutomationPeer(GridRowAutomationPeer rowPeer, GridControl grid, GridColumnBase column)
    {
        _rowPeer = rowPeer;
        _grid = grid;
        _column = column;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GridCell";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    /// <summary>The formatted cell value (StringFormat / NullText honored).</summary>
    protected override string GetNameCore()
    {
        if (_rowPeer.RowOwner.DataItem is not { } item)
        {
            return string.Empty;
        }

        return GridControl.FormatCellText(_column, _column.GetCellValue(item));
    }

    /// <summary>The cell rectangle: row bounds shifted to the column slot.</summary>
    protected override System.Windows.Rect GetBoundingRectangleCore()
    {
        var rowRect = _rowPeer.GetBoundingRectangle();
        if (rowRect.IsEmpty || _grid.Layout.GetSlot(_column) is not { } slot)
        {
            return rowRect;
        }

        var x = slot.IsFrozen ? slot.X : slot.X - _grid.HorizontalOffset;
        return new System.Windows.Rect(rowRect.X + x, rowRect.Y, slot.Width, rowRect.Height);
    }

    /// <summary>Stable per-cell id (row flat index + column hash) for UIA runtime identity.</summary>
    public int[] GetCellRuntimeId()
        => new[] { _rowPeer.RowOwner.RowInfo?.FlatIndex ?? 0, _column.GetHashCode() };

    /// <summary>Cells are not focusable; selection lives on the row.</summary>
    protected override void SetFocusCore()
    {
        // no-op
    }

    /// <summary>Cells are hosted by the row element; no own HWND.</summary>
    protected override HostedWindowWrapper? GetHostRawElementProviderCore() => null;

    // --- trivial overrides required by the abstract AutomationPeer contract ---

    /// <inheritdoc />
    protected override string GetAcceleratorKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetAccessKeyCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetAutomationIdCore()
        => $"Cell_{_rowPeer.RowOwner.RowInfo?.FlatIndex ?? 0}_{_column.FieldName}";

    /// <inheritdoc />
    protected override List<AutomationPeer>? GetChildrenCore() => null;

    /// <inheritdoc />
    protected override System.Windows.Point GetClickablePointCore()
        => new(_rowPeer.GetClickablePoint().X, _rowPeer.GetClickablePoint().Y);

    /// <inheritdoc />
    protected override string GetHelpTextCore()
        => Convert.ToString(_column.Header, System.Globalization.CultureInfo.CurrentCulture) ?? _column.FieldName;

    /// <inheritdoc />
    protected override string GetItemStatusCore() => string.Empty;

    /// <inheritdoc />
    protected override string GetItemTypeCore() => "Data cell";

    /// <inheritdoc />
    protected override AutomationPeer? GetLabeledByCore() => null;

    /// <inheritdoc />
    protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore()
        => System.Windows.Automation.Peers.AutomationOrientation.None;

    /// <inheritdoc />
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc />
    protected override bool IsControlElementCore() => true;

    /// <inheritdoc />
    protected override bool IsKeyboardFocusableCore() => false;

    /// <inheritdoc />
    protected override bool IsOffscreenCore() => _rowPeer.IsOffscreen();

    /// <inheritdoc />
    protected override bool IsPasswordCore() => false;

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface) => null;

    /// <inheritdoc />
    protected override bool HasKeyboardFocusCore() => false;

    /// <inheritdoc />
    protected override bool IsEnabledCore() => true;

    /// <inheritdoc />
    protected override bool IsRequiredForFormCore() => false;

}
