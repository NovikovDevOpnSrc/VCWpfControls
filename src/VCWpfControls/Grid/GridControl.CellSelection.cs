using System.Windows;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid;

/// <summary>Cell-level selection of <see cref="GridControl"/> (the <c>SelectionUnit=CellOrRow</c> mode).</summary>
public partial class GridControl
{
    private readonly List<GridCellRange> _selectedCellRanges = new();
    private GridCellInfo _cellSelectionAnchor;

    /// <summary>The unit selected with the mouse and keyboard (rows by default).</summary>
    public GridSelectionUnit SelectionUnit
    {
        get => (GridSelectionUnit)GetValue(SelectionUnitProperty);
        set => SetValue(SelectionUnitProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectionUnit"/> dependency property.</summary>
    public static readonly DependencyProperty SelectionUnitProperty = DependencyProperty.Register(
        nameof(SelectionUnit), typeof(GridSelectionUnit), typeof(GridControl),
        new PropertyMetadata(GridSelectionUnit.Row, OnSelectionUnitChanged));

    /// <summary>The selected cells of all ranges, in view order (row-major). Empty in row mode.</summary>
    public IReadOnlyList<GridCellInfo> SelectedCells => MaterializeSelectedCells();

    /// <summary>Replaces the cell selection with a single range between the two cells.</summary>
    public void SelectCellRange(GridCellInfo start, GridCellInfo end)
    {
        if (!start.IsValid || !end.IsValid)
        {
            return;
        }

        _selectedCellRanges.Clear();
        _selectedCellRanges.Add(new GridCellRange(start, end));
        _cellSelectionAnchor = start;
        OnCellSelectionChanged();
    }

    /// <summary>Adds a range to the cell selection without clearing it.</summary>
    public void AddCellRange(GridCellInfo start, GridCellInfo end)
    {
        if (!start.IsValid || !end.IsValid)
        {
            return;
        }

        _selectedCellRanges.Add(new GridCellRange(start, end));
        if (!_cellSelectionAnchor.IsValid)
        {
            _cellSelectionAnchor = start;
        }

        OnCellSelectionChanged();
    }

    /// <summary>Clears the cell selection (rows are unaffected).</summary>
    public void UnselectAllCells()
    {
        if (_selectedCellRanges.Count == 0)
        {
            return;
        }

        _selectedCellRanges.Clear();
        OnCellSelectionChanged();
    }

    /// <summary>Whether the given cell is inside any selected range.</summary>
    public bool IsCellSelected(GridCellInfo cell)
    {
        if (!cell.IsValid)
        {
            return false;
        }

        var map = BuildRowIndexMap();
        var columns = VisibleColumns;
        var (row, col) = ResolveCell(cell, map, columns);
        if (row < 0 || col < 0)
        {
            return false;
        }

        foreach (var range in _selectedCellRanges)
        {
            var (r1, c1) = ResolveCell(range.Start, map, columns);
            var (r2, c2) = ResolveCell(range.End, map, columns);
            if (r1 < 0 || r2 < 0)
            {
                continue;
            }

            var (rowMin, rowMax) = (Math.Min(r1, r2), Math.Max(r1, r2));
            var (colMin, colMax) = (Math.Min(c1, c2), Math.Max(c1, c2));
            if (row >= rowMin && row <= rowMax && col >= colMin && col <= colMax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The selected column spans of one view row (merged, sorted) — used by the cells
    /// panel to paint the selection background.
    /// </summary>
    internal IReadOnlyList<(int ColStart, int ColEnd)> GetSelectedCellSpans(int flatRowIndex)
    {
        var spans = new List<(int ColStart, int ColEnd)>();
        var columns = VisibleColumns;
        foreach (var range in _selectedCellRanges)
        {
            var map = BuildRowIndexMap();
            var (r1, c1) = ResolveCell(range.Start, map, columns);
            var (r2, c2) = ResolveCell(range.End, map, columns);
            if (r1 < 0 || r2 < 0 || c1 < 0 || c2 < 0)
            {
                continue;
            }

            var (rowMin, rowMax) = (Math.Min(r1, r2), Math.Max(r1, r2));
            if (flatRowIndex < rowMin || flatRowIndex > rowMax)
            {
                continue;
            }

            spans.Add((Math.Min(c1, c2), Math.Max(c1, c2)));
        }

        spans.Sort((a, b) => a.ColStart.CompareTo(b.ColStart));
        var merged = new List<(int ColStart, int ColEnd)>();
        foreach (var span in spans)
        {
            if (merged.Count > 0 && span.ColStart <= merged[^1].ColEnd + 1)
            {
                merged[^1] = (merged[^1].ColStart, Math.Max(merged[^1].ColEnd, span.ColEnd));
            }
            else
            {
                merged.Add(span);
            }
        }

        return merged;
    }

    /// <summary>Drops cell ranges whose rows are no longer in the view (sorting/filtering).</summary>
    internal void PruneCellSelectionToSource()
    {
        if (_selectedCellRanges.Count == 0)
        {
            return;
        }

        var map = BuildRowIndexMap();
        var kept = _selectedCellRanges.Where(r => ResolveCell(r.Start, map, VisibleColumns).Row >= 0
            && ResolveCell(r.End, map, VisibleColumns).Row >= 0).ToList();
        if (kept.Count == _selectedCellRanges.Count)
        {
            return;
        }

        _selectedCellRanges.Clear();
        _selectedCellRanges.AddRange(kept);
        OnCellSelectionChanged();
    }

    internal void HandleCellMousePress(object item, GridColumnBase? column, bool isCtrl, bool isShift)
    {
        var cell = new GridCellInfo(item, column);
        if (!cell.IsValid)
        {
            return;
        }

        if (isShift && _cellSelectionAnchor.IsValid)
        {
            if (!isCtrl)
            {
                _selectedCellRanges.Clear();
            }

            _selectedCellRanges.Add(new GridCellRange(_cellSelectionAnchor, cell));
        }
        else if (isCtrl && !isShift)
        {
            // Toggle: an exactly-single-cell range for this cell is removed; a cell inside a
            // larger range is kept selected; otherwise the cell is added.
            var removedExact = _selectedCellRanges.RemoveAll(r =>
                ReferenceEquals(r.Start.Item, cell.Item) && ReferenceEquals(r.End.Item, cell.Item)
                && r.Start.Column == cell.Column && r.End.Column == cell.Column);
            if (removedExact == 0 && !IsCellSelected(cell))
            {
                _selectedCellRanges.Add(new GridCellRange(cell, cell));
            }
        }
        else
        {
            _selectedCellRanges.Clear();
            _selectedCellRanges.Add(new GridCellRange(cell, cell));
            _cellSelectionAnchor = cell;
        }

        OnCellSelectionChanged();
    }

    /// <summary>Extends the cell selection from the anchor to the current cell (Shift+arrows).</summary>
    internal void ExtendCellSelectionToCurrent()
    {
        var current = new GridCellInfo(CurrentItem, CurrentColumn);
        if (!current.IsValid)
        {
            return;
        }

        var anchor = _cellSelectionAnchor.IsValid ? _cellSelectionAnchor : current;
        _cellSelectionAnchor = anchor;
        _selectedCellRanges.Clear();
        _selectedCellRanges.Add(new GridCellRange(anchor, current));
        OnCellSelectionChanged();
    }

    private List<GridCellInfo> MaterializeSelectedCells()
    {
        var cells = new List<GridCellInfo>();
        if (_selectedCellRanges.Count == 0)
        {
            return cells;
        }

        var map = BuildRowIndexMap();
        var columns = VisibleColumns;
        var seen = new HashSet<GridCellInfo>();
        foreach (var range in _selectedCellRanges)
        {
            var (r1, c1) = ResolveCell(range.Start, map, columns);
            var (r2, c2) = ResolveCell(range.End, map, columns);
            if (r1 < 0 || r2 < 0 || c1 < 0 || c2 < 0)
            {
                continue;
            }

            var items = DataItemsInOrder();
            var (rowMin, rowMax) = (Math.Min(r1, r2), Math.Max(r1, r2));
            var (colMin, colMax) = (Math.Min(c1, c2), Math.Max(c1, c2));
            for (var row = rowMin; row <= rowMax; row++)
            {
                for (var col = colMin; col <= colMax; col++)
                {
                    var cell = new GridCellInfo(items[row], columns[col]);
                    if (seen.Add(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        return cells;
    }

    private List<object> DataItemsInOrder()
    {
        var items = new List<object>();
        foreach (var row in DataEngine.View)
        {
            if (row is GridDataRow data && !IsNewItemPlaceholder(data.Item))
            {
                items.Add(data.Item);
            }
        }

        return items;
    }

    private Dictionary<object, int> BuildRowIndexMap()
    {
        var map = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        var index = 0;
        foreach (var item in DataItemsInOrder())
        {
            map[item] = index++;
        }

        return map;
    }

    private static (int Row, int Col) ResolveCell(GridCellInfo cell, Dictionary<object, int> rows, IReadOnlyList<GridColumnBase> columns)
    {
        var row = cell.Item is not null && rows.TryGetValue(cell.Item, out var r) ? r : -1;
        var col = -1;
        for (var i = 0; i < columns.Count; i++)
        {
            if (ReferenceEquals(columns[i], cell.Column))
            {
                col = i;
                break;
            }
        }

        return (row, col);
    }

    private void OnCellSelectionChanged()
    {
        // The selection background is painted by the row panels' OnRender.
        foreach (var row in _rowRegistry)
        {
            row.CellsPanel?.InvalidateVisual();
        }

        RaiseEvent(new RoutedEventArgs(CellSelectionChangedEvent));
    }

    /// <summary>Raised when the selected cells change.</summary>
    public event RoutedEventHandler CellSelectionChanged
    {
        add => AddHandler(CellSelectionChangedEvent, value);
        remove => RemoveHandler(CellSelectionChangedEvent, value);
    }

    /// <summary>Identifies the <see cref="CellSelectionChanged"/> routed event.</summary>
    public static readonly RoutedEvent CellSelectionChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(CellSelectionChanged), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(GridControl));

    /// <summary>
    /// Copies the bounding box of the selected cell ranges as TSV (Excel semantics):
    /// every column of the box is emitted; cells outside the ranges come out empty.
    /// </summary>
    private void CopySelectedCellsToClipboard()
    {
        var map = BuildRowIndexMap();
        var columns = VisibleColumns;
        var items = DataItemsInOrder();
        var rowMin = int.MaxValue;
        var rowMax = int.MinValue;
        var colMin = int.MaxValue;
        var colMax = int.MinValue;
        foreach (var range in _selectedCellRanges)
        {
            var (r1, c1) = ResolveCell(range.Start, map, columns);
            var (r2, c2) = ResolveCell(range.End, map, columns);
            if (r1 < 0 || r2 < 0 || c1 < 0 || c2 < 0)
            {
                continue;
            }

            rowMin = Math.Min(rowMin, Math.Min(r1, r2));
            rowMax = Math.Max(rowMax, Math.Max(r1, r2));
            colMin = Math.Min(colMin, Math.Min(c1, c2));
            colMax = Math.Max(colMax, Math.Max(c1, c2));
        }

        if (rowMin > rowMax)
        {
            return;
        }

        var lines = new List<string>();
        if (ClipboardCopyMode == GridClipboardCopyMode.IncludeHeaders)
        {
            var headers = new List<string>();
            for (var col = colMin; col <= colMax; col++)
            {
                headers.Add(GridColumnBase.HeaderText(columns[col].Header) ?? string.Empty);
            }

            lines.Add(string.Join("	", headers));
        }

        for (var row = rowMin; row <= rowMax; row++)
        {
            var cells = new List<string>();
            for (var col = colMin; col <= colMax; col++)
            {
                cells.Add(IsCellSelected(new GridCellInfo(items[row], columns[col]))
                    ? Convert.ToString(columns[col].GetCellValue(items[row]), System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty
                    : string.Empty);
            }

            lines.Add(string.Join("	", cells));
        }

        try
        {
            Clipboard.SetDataObject(string.Join(Environment.NewLine, lines), true);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // Clipboard can be locked by another process; copying is best-effort.
        }
    }

    private static void OnSelectionUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        if ((GridSelectionUnit)e.NewValue == GridSelectionUnit.Row)
        {
            grid.UnselectAllCells();
        }
        else
        {
            grid.UnselectAll();
        }
    }
}
