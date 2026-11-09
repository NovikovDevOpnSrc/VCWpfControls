using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Automation.Provider;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid.Automation;

/// <summary>
/// Virtual automation peer of one cell: a (row, column) pair addressable through
/// <c>GridPattern.GetItem(row, column)</c> and as a child of the row's DataItem.
/// The name is the formatted cell value (StringFormat and NullText honored).
/// </summary>
public sealed class GridCellAutomationPeer : AutomationPeer, IValueProvider, IGridItemProvider
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

    /// <summary>
    /// The cell rectangle. Preferred from the REALIZED cell element (screen-accurate at any
    /// DPI); falls back to the row rectangle shifted by the column slot — converted with the
    /// grid's DPI scale, because <see cref="AutomationPeer.GetBoundingRectangle"/> returns
    /// physical screen pixels while slot coordinates are DIPs.
    /// </summary>
    protected override System.Windows.Rect GetBoundingRectangleCore()
    {
        var rowRect = _rowPeer.GetBoundingRectangle();
        if (rowRect.IsEmpty)
        {
            return rowRect;
        }

        var row = _rowPeer.RowOwner;
        if (row.CellsPanel?.GetCellElement(_column) is { IsVisible: true } element
            && element.ActualWidth > 0)
        {
            var topLeft = element.PointToScreen(new System.Windows.Point(0, 0));
            var bottomRight = element.PointToScreen(
                new System.Windows.Point(element.ActualWidth, element.ActualHeight));
            return new System.Windows.Rect(topLeft, bottomRight);
        }

        if (_grid.Layout.GetSlot(_column) is not { } slot)
        {
            return rowRect;
        }

        var x = slot.IsFrozen ? slot.X : slot.X - _grid.HorizontalOffset;
        var scale = System.Windows.Media.VisualTreeHelper.GetDpi(row).PixelsPerDip;
        return new System.Windows.Rect(rowRect.X + (x * scale), rowRect.Y, slot.Width * scale, rowRect.Height);
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

    /// <summary>
    /// Template cells expose their template content (buttons, check boxes, links) as children,
    /// so interactive <see cref="GridColumnBase.CellTemplate"/> content stays reachable for
    /// UIA clients and assistive technologies.
    /// </summary>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        if (_column.CellTemplate is null)
        {
            return null;
        }

        if (_rowPeer.RowOwner.CellsPanel?.GetCellElement(_column) is not { } element)
        {
            return null;
        }

        // Walk the whole visual subtree with hoisting: peerless nodes (panels) are
        // skipped and their descendants are lifted to the nearest collected ancestor,
        // mirroring how the standard WPF automation tree exposes template content.
        var peers = new List<AutomationPeer>();
        CollectPeers(element, peers, depth: 0);
        return peers.Count > 0 ? peers : null;
    }

    private const int MaxTemplateDepth = 12;

    private static void CollectPeers(System.Windows.Media.Visual visual, List<AutomationPeer> peers, int depth)
    {
        if (depth > MaxTemplateDepth)
        {
            return;
        }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(visual);
        for (var i = 0; i < count; i++)
        {
            if (System.Windows.Media.VisualTreeHelper.GetChild(visual, i) is not System.Windows.UIElement child)
            {
                continue;
            }

            if (child is System.Windows.Controls.TextBlock textBlock)
            {
                // TextBlocks auto-generate peers only when focused; always expose their text.
                if (!string.IsNullOrEmpty(textBlock.Text))
                {
                    peers.Add(new TextBlockAutomationPeerShim(textBlock));
                }

                continue;
            }

            if (FrameworkElementAutomationPeer.CreatePeerForElement(child) is { } peer)
            {
                peers.Add(peer);
                continue; // WPF will descend into this peer's own children itself
            }

            // Peerless node (panel etc.): hoist its children here.
            CollectPeers(child, peers, depth + 1);
        }
    }

    /// <summary>Minimal peer exposing a TextBlock's text in the UIA tree.</summary>
    private sealed class TextBlockAutomationPeerShim : AutomationPeer
    {
        private readonly TextBlock _owner;

        public TextBlockAutomationPeerShim(TextBlock owner) => _owner = owner;

        protected override void SetFocusCore()
        {
            // no-op: template text is not focusable
        }

        protected override string GetClassNameCore() => "Text";

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetNameCore() => _owner.Text;

        protected override System.Windows.Rect GetBoundingRectangleCore() => default;

        protected override HostedWindowWrapper? GetHostRawElementProviderCore() => null;

        protected override System.Windows.Point GetClickablePointCore() => default;

        public int[] GetTextRuntimeId() => new[] { _owner.GetHashCode() };

        protected override List<AutomationPeer>? GetChildrenCore() => null;

        protected override string GetAcceleratorKeyCore() => string.Empty;

        protected override string GetAccessKeyCore() => string.Empty;

        protected override string GetAutomationIdCore() => _owner.Name ?? string.Empty;

        protected override string GetHelpTextCore() => string.Empty;

        protected override string GetItemStatusCore() => string.Empty;

        protected override string GetItemTypeCore() => string.Empty;

        protected override AutomationPeer? GetLabeledByCore() => null;

        protected override System.Windows.Automation.Peers.AutomationOrientation GetOrientationCore()
            => System.Windows.Automation.Peers.AutomationOrientation.None;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        protected override bool IsKeyboardFocusableCore() => false;

        protected override bool IsOffscreenCore() => false;

        protected override bool IsPasswordCore() => false;

        protected override bool IsRequiredForFormCore() => false;

        protected override bool HasKeyboardFocusCore() => false;

        protected override bool IsEnabledCore() => true;

        public override object? GetPattern(PatternInterface patternInterface) => null;
    }

    /// <inheritdoc />
    protected override System.Windows.Point GetClickablePointCore()
    {
        var point = _rowPeer.GetClickablePoint();
        return new System.Windows.Point(point.X, point.Y);
    }

    /// <inheritdoc />
    protected override string GetHelpTextCore()
        => GridColumnBase.HeaderText(_column.Header) is { Length: > 0 } header ? header : _column.FieldName;

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

    /// <summary>Exposes the Value pattern (read/write the cell value) and the GridItem pattern (row/column coordinates).</summary>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.Value or PatternInterface.GridItem ? this : null;

    /// <summary>The formatted cell value (read through the UIA Value pattern).</summary>
    string IValueProvider.Value => GetNameCore();

    /// <summary>Whether the cell rejects edits (the column or the whole grid is read-only).</summary>
    bool IValueProvider.IsReadOnly => _column.ReadOnly || _grid.IsReadOnly;

    /// <summary>
    /// Writes a value through the paste conversion path (<c>TrySetCellText</c>: culture-aware
    /// parsing, read-only columns reject).
    /// </summary>
    void IValueProvider.SetValue(string? value)
    {
        if (((IValueProvider)this).IsReadOnly)
        {
            throw new InvalidOperationException("The cell is read-only.");
        }

        if (_rowPeer.RowOwner.DataItem is not { } item)
        {
            return;
        }

        _column.TrySetCellText(item, value);
    }

    /// <summary>The row's flat index in the grid view.</summary>
    int IGridItemProvider.Row => _rowPeer.RowOwner.RowInfo?.FlatIndex ?? -1;

    /// <summary>The column's visible index.</summary>
    int IGridItemProvider.Column
    {
        get
        {
            for (var i = 0; i < _grid.VisibleColumns.Count; i++)
            {
                if (ReferenceEquals(_grid.VisibleColumns[i], _column))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>The row span (always 1 — cells never merge).</summary>
    int IGridItemProvider.RowSpan => 1;

    /// <summary>The column span (always 1 — cells never merge).</summary>
    int IGridItemProvider.ColumnSpan => 1;

    /// <summary>The containing grid, as a raw UIA provider.</summary>
    System.Windows.Automation.Provider.IRawElementProviderSimple IGridItemProvider.ContainingGrid
        => ProviderFromPeer(UIElementAutomationPeer.FromElement(_grid) ?? new FrameworkElementAutomationPeer(_grid));

    /// <inheritdoc />
    protected override bool HasKeyboardFocusCore() => false;

    /// <inheritdoc />
    protected override bool IsEnabledCore() => true;

    /// <inheritdoc />
    protected override bool IsRequiredForFormCore() => false;

}
