using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid.Automation;

/// <summary>Automation peer exposing the grid as a data grid: Grid/Table/Selection patterns.</summary>
public sealed class GridControlAutomationPeer : FrameworkElementAutomationPeer, IGridProvider, ITableProvider, ISelectionProvider
{
    private readonly GridControl _owner;

    /// <summary>Creates the peer.</summary>
    public GridControlAutomationPeer(GridControl owner)
        : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    protected override string GetClassNameCore() => "GridControl";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var baseName = base.GetNameCore();
        if (!string.IsNullOrEmpty(baseName))
        {
            return baseName!;
        }

        return AutomationProperties.GetName(_owner) is { Length: > 0 } name ? name : "Data grid";
    }

    /// <summary>Row count of the current view.</summary>
    public int RowCount => _owner.DataEngine.DataRows.Count;

    /// <summary>Visible column count.</summary>
    public int ColumnCount => _owner.VisibleColumns.Count;

    /// <summary>
    /// Returns the UIA provider of the row container at (row, column) — the row peer's name
    /// contains the cell values, and the column index selects which value leads the name
    /// via cell peers when they exist. Rows are DataItems with SelectionItem support.
    /// </summary>
    IRawElementProviderSimple? IGridProvider.GetItem(int row, int column)
    {
        if (row < 0 || column < 0 || row >= RowCount || column >= ColumnCount)
        {
            return null;
        }

        var dataRow = _owner.DataEngine.DataRows[row];
        if (TryFindRowContainer(dataRow.Item) is not { } container
            || CreatePeerForElement(container) is not GridRowAutomationPeer rowPeer)
        {
            return null;
        }

        return rowPeer.GetCellPeer(column) is { } cell
            ? ProviderFromPeer(cell)
            : null;
    }

    /// <inheritdoc />
    IRawElementProviderSimple[]? ITableProvider.GetRowHeaders() => null;

    /// <inheritdoc />
    IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
    {
        if (_owner.HeadersPresenter is null)
        {
            return Array.Empty<IRawElementProviderSimple>();
        }

        var providers = new List<IRawElementProviderSimple>();
        foreach (var header in _owner.HeadersPresenter.Children.OfType<GridColumnHeader>())
        {
            var provider = ProviderFromPeer(CreatePeerForElement(header));
            if (provider is not null)
            {
                providers.Add(provider);
            }
        }

        return providers.ToArray();
    }

    /// <inheritdoc />
    RowOrColumnMajor ITableProvider.RowOrColumnMajor => RowOrColumnMajor.RowMajor;

    /// <inheritdoc />
    bool ISelectionProvider.CanSelectMultiple => _owner.SelectionMode == GridSelectionMode.Extended;

    /// <inheritdoc />
    bool ISelectionProvider.IsSelectionRequired => false;

    /// <inheritdoc />
    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var providers = new List<IRawElementProviderSimple>();
        foreach (var item in _owner.SelectedItems.Cast<object>())
        {
            if (TryFindRowContainer(item) is { } container
                && ProviderFromPeer(CreatePeerForElement(container)) is { } provider)
            {
                providers.Add(provider);
            }
        }

        return providers.ToArray();
    }

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.Grid or PatternInterface.Table or PatternInterface.Selection
            ? this
            : base.GetPattern(patternInterface);

    private GridRow? TryFindRowContainer(object item)
    {
        for (var i = 0; i < _owner.Items.Count; i++)
        {
            if (_owner.ItemContainerGenerator.ContainerFromIndex(i) is GridRow row
                && ReferenceEquals(row.DataItem, item))
            {
                return row;
            }
        }

        return null;
    }
}
