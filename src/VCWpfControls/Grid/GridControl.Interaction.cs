using System.Windows;
using System.Windows.Input;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid;

/// <summary>Interaction logic of <see cref="GridControl"/>: sorting, resizing, reordering and keyboard navigation.</summary>
public partial class GridControl
{
    private GridColumnBase? _dragColumn;

    /// <summary>Sorts by the given path, replacing existing sort levels.</summary>
    public void SortBy(string fieldName, GridSortDirection direction)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name must not be empty.", nameof(fieldName));
        }

        SortDescriptions.Clear();
        if (direction != GridSortDirection.None)
        {
            SortDescriptions.Add(new GridSortDescription(fieldName, direction));
        }
    }

    /// <summary>Sorts by the given column, replacing existing sort levels.</summary>
    public void SortBy(GridColumnBase column, GridSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(column);
        SortBy(column.EffectiveSortPath, direction);
    }

    /// <summary>Adds or updates one sort level, keeping other levels (multi-column sort).</summary>
    public void AddSortLevel(string fieldName, GridSortDirection direction)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name must not be empty.", nameof(fieldName));
        }

        var existing = SortDescriptions.FirstOrDefault(s => s.FieldName == fieldName);
        if (existing is not null)
        {
            if (direction == GridSortDirection.None)
            {
                SortDescriptions.Remove(existing);
            }
            else
            {
                existing.Direction = direction;
                Refresh();
            }

            return;
        }

        if (direction != GridSortDirection.None)
        {
            SortDescriptions.Add(new GridSortDescription(fieldName, direction));
        }
    }

    /// <summary>Removes all sort levels.</summary>
    public void ClearSorting() => SortDescriptions.Clear();

    /// <summary>Applies a new width to a column (clamped to min/max), used by header resize thumbs.</summary>
    internal static void ResizeColumn(GridColumnBase column, double newWidth)
    {
        var width = Math.Clamp(newWidth, column.MinWidth, column.MaxWidth);
        column.Width = new GridColumnWidth(width, GridUnitType.Pixel);
    }

    internal void NotifyHeaderClick(GridColumnBase column, ModifierKeys modifiers)
    {
        if (!CanUserSortColumns || !column.CanUserSort)
        {
            return;
        }

        var next = column.SortDirection switch
        {
            GridSortDirection.None => GridSortDirection.Ascending,
            GridSortDirection.Ascending => GridSortDirection.Descending,
            _ => GridSortDirection.None,
        };

        var args = new GridSortingEventArgs(column, next) { RoutedEvent = SortingEvent };
        RaiseEvent(args);
        if (args.Cancel)
        {
            return;
        }

        var extend = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (extend)
        {
            AddSortLevel(column.EffectiveSortPath, next);
        }
        else
        {
            SortBy(column, next);
        }

        RaiseEvent(new RoutedEventArgs(SortedEvent));
    }

    internal void NotifyColumnDragStarted(GridColumnBase column) => _dragColumn = column;

    internal void NotifyColumnDragOver(GridColumnBase column, Point pointInGrid)
    {
        if (_dragColumn is null)
        {
            return;
        }

        // The group panel wins over header reorder when the header is dragged above them.
        if (IsPointOverGroupPanel(pointInGrid))
        {
            foreach (var visible in VisibleColumns)
            {
                if (HeadersPresenter?.GetHeader(visible) is { } header)
                {
                    header.IsDropTarget = false;
                }
            }

            if (GroupPanel is not null)
            {
                GroupPanel.IsDropTarget = true;
            }

            return;
        }

        if (GroupPanel is not null)
        {
            GroupPanel.IsDropTarget = false;
        }

        var target = HitTestHeader(pointInGrid);
        foreach (var visible in VisibleColumns)
        {
            if (HeadersPresenter?.GetHeader(visible) is { } header)
            {
                header.IsDropTarget = visible == target && visible != column;
            }
        }
    }

    internal void NotifyColumnDragCompleted(GridColumnBase column, Point pointInGrid)
    {
        _dragColumn = null;
        foreach (var visible in VisibleColumns)
        {
            if (HeadersPresenter?.GetHeader(visible) is { } header)
            {
                header.IsDropTarget = false;
            }
        }

        if (GroupPanel is not null)
        {
            GroupPanel.IsDropTarget = false;
        }

        if (IsPointOverGroupPanel(pointInGrid))
        {
            GroupByColumn(column);
            return;
        }

        var target = HitTestHeader(pointInGrid);
        if (target is null || ReferenceEquals(target, column))
        {
            return;
        }

        var oldIndex = Columns.IndexOf(column);
        if (oldIndex < 0 || Columns.IndexOf(target) < 0)
        {
            return;
        }

        // Dropping onto the right half of the target inserts after it.
        var slot = Layout.GetSlot(target);
        var targetCenter = (slot?.X ?? 0) + (slot?.Width ?? 0) / 2;
        var insertAfter = pointInGrid.X + HorizontalOffset > targetCenter;

        Columns.RemoveAt(oldIndex);
        var newIndex = Math.Clamp(Columns.IndexOf(target) + (insertAfter ? 1 : 0), 0, Columns.Count);
        Columns.Insert(newIndex, column);
    }

    private GridColumnBase? HitTestHeader(Point pointInGrid)
    {
        var contentX = pointInGrid.X + HorizontalOffset;
        return Layout.FindSlotAt(contentX)?.Column;
    }

    /// <summary>Scrolls horizontally so the column is fully visible.</summary>
    internal void EnsureColumnVisible(GridColumnBase? column)
    {
        if (column is null)
        {
            return;
        }

        var slot = Layout.GetSlot(column);
        if (slot is null)
        {
            return;
        }

        if (slot.IsFrozen)
        {
            return;
        }

        var left = slot.X - HorizontalOffset;
        var right = left + slot.Width;
        if (left < 0)
        {
            SetHorizontalOffsetCore(slot.X);
        }
        else if (right > ViewportWidth)
        {
            SetHorizontalOffsetCore(slot.X + slot.Width - ViewportWidth);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }

        var key = e.Key;
        var modifiers = Keyboard.Modifiers;

        switch (key)
        {
            case Key.Down:
                MoveCurrentCell(0, 1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveCurrentCell(0, -1);
                e.Handled = true;
                break;
            case Key.Right:
                MoveCurrentCell(1, 0);
                e.Handled = true;
                break;
            case Key.Left:
                MoveCurrentCell(-1, 0);
                e.Handled = true;
                break;
            case Key.PageDown:
                MoveCurrentCell(0, Math.Max(1, VisibleRowCount() * 3 / 4));
                e.Handled = true;
                break;
            case Key.PageUp:
                MoveCurrentCell(0, -Math.Max(1, VisibleRowCount() * 3 / 4));
                e.Handled = true;
                break;
            case Key.Home when (modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                SetCurrentCell(FirstDataItem(), VisibleColumns.FirstOrDefault(), alignIntoView: true);
                e.Handled = true;
                break;
            case Key.Home:
                SetCurrentCell(CurrentItem, VisibleColumns.FirstOrDefault(), alignIntoView: true);
                e.Handled = true;
                break;
            case Key.End when (modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                SetCurrentCell(LastDataItem(), VisibleColumns.LastOrDefault(), alignIntoView: true);
                e.Handled = true;
                break;
            case Key.End:
                SetCurrentCell(CurrentItem, VisibleColumns.LastOrDefault(), alignIntoView: true);
                e.Handled = true;
                break;
            case Key.Enter:
                if (IsEditing)
                {
                    CommitEdit();
                }
                else
                {
                    MoveCurrentCell(0, 1);
                }

                e.Handled = true;
                break;
            case Key.Escape:
                if (IsEditing)
                {
                    CancelEdit();
                    e.Handled = true;
                }

                break;
            case Key.F2:
                if (BeginEdit())
                {
                    e.Handled = true;
                }

                break;
            case Key.Space:
                if (CurrentItem is not null && CurrentColumn is GridCheckBoxColumn && !IsReadOnly)
                {
                    ToggleCheckBox(CurrentItem, (GridCheckBoxColumn)CurrentColumn);
                    e.Handled = true;
                }

                break;
            case Key.A when (modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                if (SelectionMode == GridSelectionMode.Extended)
                {
                    SelectAll();
                    e.Handled = true;
                }

                break;
            case Key.C when (modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                CopyToClipboard();
                e.Handled = true;
                break;
            case Key.Delete:
                if (CanDeleteSelectedItems() && DeleteSelectedItems())
                {
                    e.Handled = true;
                }

                break;
        }
    }

    private static void ToggleCheckBox(object item, GridCheckBoxColumn column)
    {
        if (column.ReadOnly)
        {
            return;
        }

        var value = column.GetCellValue(item);
        var newValue = value is bool b ? !b : true;
        try
        {
            var property = item.GetType().GetProperty(column.FieldName);
            if (property is { CanWrite: true })
            {
                property.SetValue(item, newValue);
            }
        }
        catch (System.Reflection.TargetInvocationException)
        {
            // Property setter threw; keep the old value.
        }
    }

    /// <summary>Moves the current cell by the given delta (columns/rows), skipping group rows.</summary>
    internal void MoveCurrentCell(int columnDelta, int rowDelta)
    {
        var view = DataEngine.View;
        var currentIndex = -1;
        for (var i = 0; i < view.Count; i++)
        {
            if (view[i] is GridDataRow data && ReferenceEquals(data.Item, CurrentItem))
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
        {
            var firstItem = FirstDataItem();
            if (firstItem is not null)
            {
                SetCurrentCell(firstItem, VisibleColumns.FirstOrDefault(), alignIntoView: true);
            }

            return;
        }

        // Row movement across data rows only.
        int targetIndex = currentIndex;
        if (rowDelta != 0)
        {
            var step = Math.Sign(rowDelta);
            var remaining = Math.Abs(rowDelta);
            while (remaining > 0 && targetIndex + step >= 0 && targetIndex + step < view.Count)
            {
                targetIndex += step;
                if (view[targetIndex] is GridDataRow)
                {
                    remaining--;
                }
            }
        }

        // Column movement.
        GridColumnBase? targetColumn = CurrentColumn;
        if (columnDelta != 0)
        {
            var columns = VisibleColumns;
            var columnIndex = IndexOfColumn(columns, CurrentColumn);
            var targetColumnIndex = Math.Clamp(columnIndex + columnDelta, 0, columns.Count - 1);
            targetColumn = columns[targetColumnIndex];
        }

        if (targetIndex < view.Count && view[targetIndex] is GridDataRow targetRow)
        {
            SetCurrentCell(targetRow.Item, targetColumn, alignIntoView: true);
            if (SelectionMode == GridSelectionMode.SingleRow || (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                // Plain navigation moves the current cell; selection follows in Single mode.
                if (SelectionMode == GridSelectionMode.SingleRow)
                {
                    UnselectAll();
                    SelectItem(targetRow.Item);
                }
            }
        }
    }

    private static int IndexOfColumn(IReadOnlyList<Columns.GridColumnBase> columns, Columns.GridColumnBase? column)
    {
        if (column is null)
        {
            return -1;
        }

        for (var i = 0; i < columns.Count; i++)
        {
            if (ReferenceEquals(columns[i], column))
            {
                return i;
            }
        }

        return -1;
    }

    private object? FirstDataItem()
    {
        var view = DataEngine.View;
        for (var i = 0; i < view.Count; i++)
        {
            if (view[i] is GridDataRow data)
            {
                return data.Item;
            }
        }

        return null;
    }

    private object? LastDataItem()
    {
        var view = DataEngine.View;
        for (var i = view.Count - 1; i >= 0; i--)
        {
            if (view[i] is GridDataRow data)
            {
                return data.Item;
            }
        }

        return null;
    }

    private int VisibleRowCount()
        => ScrollHost is null ? 10 : Math.Max(1, (int)(ScrollHost.ViewportHeight / (double.IsNaN(RowHeight) ? 24d : RowHeight)));
}
