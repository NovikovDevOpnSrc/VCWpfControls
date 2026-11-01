using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid;

/// <summary>Selection, current cell and clipboard handling of <see cref="GridControl"/>.</summary>
public partial class GridControl
{
    private readonly List<object> _selectedItems = new();
    private object? _selectionAnchor;

    /// <summary>The selected data items (live collection).</summary>
    public IList SelectedItems => SelectedItemsCollection;

    /// <summary>The selected data items as an observable collection.</summary>
    public ObservableCollection<object> SelectedItemsCollection { get; } = new();

    /// <summary>Whether a data item is currently selected.</summary>
    internal bool IsItemSelected(object item) => _selectedItems.Contains(item);

    /// <summary>Selects all data rows (Extended mode only).</summary>
    public void SelectAll()
    {
        if (SelectionMode != GridSelectionMode.Extended)
        {
            return;
        }

        var added = new List<object>();
        foreach (var rowInfo in DataEngine.View)
        {
            if (rowInfo is GridDataRow data && !_selectedItems.Contains(data.Item))
            {
                _selectedItems.Add(data.Item);
                added.Add(data.Item);
            }
        }

        if (added.Count > 0)
        {
            OnSelectionChanged(added, Array.Empty<object>());
        }
    }

    /// <summary>Clears the selection.</summary>
    public void UnselectAll()
    {
        var removed = _selectedItems.ToList();
        _selectedItems.Clear();
        if (removed.Count > 0)
        {
            OnSelectionChanged(Array.Empty<object>(), removed);
        }
    }

    /// <summary>Programmatically selects a data item.</summary>
    public void SelectItem(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_selectedItems.Contains(item))
        {
            return;
        }

        if (SelectionMode == GridSelectionMode.SingleRow && _selectedItems.Count > 0)
        {
            UnselectAll();
        }

        _selectedItems.Add(item);
        OnSelectionChanged(new[] { item }, Array.Empty<object>());
    }

    /// <summary>Programmatically deselects a data item.</summary>
    public void UnselectItem(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_selectedItems.Remove(item))
        {
            OnSelectionChanged(Array.Empty<object>(), new[] { item });
        }
    }

    /// <summary>Selects the range of data rows between two items (view order).</summary>
    public void SelectRange(object startItem, object endItem)
    {
        ArgumentNullException.ThrowIfNull(startItem);
        ArgumentNullException.ThrowIfNull(endItem);

        var view = DataEngine.View;
        var start = -1;
        var end = -1;
        for (var i = 0; i < view.Count; i++)
        {
            if (view[i] is not GridDataRow data)
            {
                continue;
            }

            if (ReferenceEquals(data.Item, startItem))
            {
                start = i;
            }

            if (ReferenceEquals(data.Item, endItem))
            {
                end = i;
            }
        }

        if (start < 0 || end < 0)
        {
            return;
        }

        var added = new List<object>();
        for (var i = Math.Min(start, end); i <= Math.Max(start, end); i++)
        {
            if (view[i] is GridDataRow data && !_selectedItems.Contains(data.Item))
            {
                _selectedItems.Add(data.Item);
                added.Add(data.Item);
            }
        }

        if (added.Count > 0)
        {
            OnSelectionChanged(added, Array.Empty<object>());
        }
    }

    internal void HandleRowMousePress(GridRow row, MouseButtonEventArgs e)
    {
        Focus();

        var item = row.DataItem;
        if (item is null)
        {
            return;
        }

        // Update the current cell first (position within the row from the click point).
        var column = ResolveColumnAt(row, e);
        SetCurrentCell(item, column, alignIntoView: false);

        var modifiers = Keyboard.Modifiers;
        var isCtrl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var isShift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (SelectionMode == GridSelectionMode.SingleRow)
        {
            if (!isCtrl || !IsItemSelected(item))
            {
                UnselectAll();
                SelectItem(item);
                _selectionAnchor = item;
            }
            else
            {
                UnselectItem(item);
                _selectionAnchor = null;
            }

            return;
        }

        if (isCtrl && !isShift)
        {
            if (IsItemSelected(item))
            {
                UnselectItem(item);
            }
            else
            {
                SelectItem(item);
                _selectionAnchor = item;
            }
        }
        else if (isShift)
        {
            UnselectAll();
            var anchor = _selectionAnchor ?? item;
            SelectRange(anchor, item);
        }
        else
        {
            UnselectAll();
            SelectItem(item);
            _selectionAnchor = item;
        }
    }

    internal static void HandleRowMouseRelease(GridRow row, MouseButtonEventArgs e)
    {
        // Reserved for drag-select and column reorder finishing.
    }

    internal void HandleRowDoubleClick(GridRow row, MouseButtonEventArgs e)
    {
        var item = row.DataItem;
        if (item is null)
        {
            return;
        }

        var column = ResolveColumnAt(row, e) ?? CurrentColumn ?? VisibleColumns.FirstOrDefault();
        SetCurrentCell(item, column, alignIntoView: false);
        if (column is not null)
        {
            BeginEdit(item, column);
        }
    }

    private GridColumnBase? ResolveColumnAt(GridRow row, MouseButtonEventArgs e)
    {
        if (row.CellsPanel is not { } panel)
        {
            return null;
        }

        var point = e.GetPosition(panel);
        return panel.GetColumnAt(point.X + HorizontalOffset);
    }

    /// <summary>Moves the current cell to the given item/column.</summary>
    internal void SetCurrentCell(object? item, GridColumnBase? column, bool alignIntoView)
    {
        var oldItem = CurrentItem;
        var oldColumn = CurrentColumn;

        SetCurrentValue(CurrentItemProperty, item);
        CurrentColumn = item is null ? null : column;

        if (ReferenceEquals(oldItem, item) && !ReferenceEquals(oldColumn, CurrentColumn))
        {
            // Column-only move: the item property callback did not run.
            if (item is not null && TryFindRow(item) is { } row)
            {
                row.CellsPanel?.InvalidateArrange();
            }

            CurrentCellChanged?.Invoke(this, EventArgs.Empty);
        }

        if (alignIntoView && item is not null)
        {
            ScrollIntoView(item);
            EnsureColumnVisible(CurrentColumn);
        }
    }

    private static void OnCurrentItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        if (e.NewValue is not null)
        {
            grid.CurrentColumn ??= grid.VisibleColumns.FirstOrDefault();
        }
        else
        {
            grid.CurrentColumn = null;
        }

        if (e.OldValue is { } oldItem && grid.TryFindRow(oldItem) is { } oldRow)
        {
            oldRow.IsCurrent = false;
        }

        if (e.NewValue is { } newItem && grid.TryFindRow(newItem) is { } newRow)
        {
            newRow.IsCurrent = true;
        }

        grid.CurrentCellChanged?.Invoke(grid, EventArgs.Empty);
    }

    private GridRow? TryFindRow(object item)
    {
        foreach (var row in _rowRegistry)
        {
            if (ReferenceEquals(row.DataItem, item))
            {
                return row;
            }
        }

        return null;
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        if (ReferenceEquals(e.NewValue, grid._selectedItems.FirstOrDefault()))
        {
            return;
        }

        grid.UnselectAll();
        if (e.NewValue is not null)
        {
            grid.SelectItem(e.NewValue);
        }
    }

    private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (GridControl)d;
        if ((GridSelectionMode)e.NewValue == GridSelectionMode.SingleRow && grid._selectedItems.Count > 1)
        {
            var first = grid._selectedItems[0];
            grid.UnselectAll();
            grid.SelectItem(first);
        }
    }

    private void OnSelectionChanged(IList added, IList removed)
    {
        foreach (var addedItem in added.Cast<object>())
        {
            SelectedItemsCollection.Add(addedItem);
        }

        foreach (var removedItem in removed.Cast<object>())
        {
            SelectedItemsCollection.Remove(removedItem);
        }

        if (_selectedItems.Count > 0)
        {
            SetCurrentValue(SelectedItemProperty, _selectedItems[0]);
        }
        else
        {
            SetCurrentValue(SelectedItemProperty, null);
        }

        foreach (var item in added.Cast<object>().Concat(removed.Cast<object>()))
        {
            if (TryFindRow(item) is { } row)
            {
                row.IsSelected = IsItemSelected(item);
            }
        }

        SelectionChanged?.Invoke(this, new GridSelectionChangedEventArgs(added, removed)
        {
            Source = this,
        });
    }

    private void PruneSelectionToSource()
    {
        // Selection survives filtering; it is only pruned when the source itself
        // no longer contains the items.
        var allItems = MaterializeAllItems();
        var removed = new List<object>();
        foreach (var item in _selectedItems)
        {
            if (!allItems.Contains(item, ReferenceEqualityComparer.Instance))
            {
                removed.Add(item);
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        foreach (var item in removed)
        {
            _selectedItems.Remove(item);
        }

        OnSelectionChanged(Array.Empty<object>(), removed);
    }

    private List<object> MaterializeAllItems()
    {
        var result = new List<object>();
        if (ItemsSource is null)
        {
            return result;
        }

        foreach (var item in ItemsSource)
        {
            if (item is not null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private void RestoreCurrentItem()
    {
        if (CurrentItem is null)
        {
            return;
        }

        var stillInView = DataEngine.View.OfType<GridDataRow>().Any(r => ReferenceEquals(r.Item, CurrentItem));
        if (!stillInView)
        {
            SetCurrentCell(null, null, alignIntoView: false);
        }
    }

    /// <summary>Copies the selection (or the current row) to the clipboard as tab-separated text.</summary>
    public void CopyToClipboard()
    {
        if (ClipboardCopyMode == GridClipboardCopyMode.None)
        {
            return;
        }

        var items = _selectedItems.Count > 0
            ? _selectedItems
            : CurrentItem is not null ? new List<object> { CurrentItem } : new List<object>();
        if (items.Count == 0)
        {
            return;
        }

        var lines = new List<string>();
        var columns = VisibleColumns;

        if (ClipboardCopyMode == GridClipboardCopyMode.IncludeHeaders)
        {
            lines.Add(string.Join("\t", columns.Select(c => Convert.ToString(c.Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)));
        }

        foreach (var item in items)
        {
            lines.Add(string.Join("\t", columns.Select(c => Convert.ToString(c.GetCellValue(item), System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)));
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

    /// <summary>Deletes the selected rows from the source collection when possible.</summary>
    public bool DeleteSelectedItems()
    {
        if (!CanDeleteSelectedItems() || _selectedItems.Count == 0)
        {
            return false;
        }

        if (ItemsSource is not IList editableList)
        {
            return false;
        }

        var deleted = new List<object>(_selectedItems);
        UnselectAll();
        foreach (var item in deleted)
        {
            editableList.Remove(item);
        }

        return true;
    }

    private bool CanDeleteSelectedItems()
        => CanUserDeleteRows && !IsReadOnly && ItemsSource is IList;
}
