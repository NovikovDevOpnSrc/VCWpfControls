using System.Collections;
using System.Windows;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Grid;

/// <summary>Add-row logic of <see cref="GridControl"/> (the <c>CanUserAddRows</c> placeholder row).</summary>
public partial class GridControl
{
    /// <summary>
    /// The sentinel data item of the add-row placeholder shown when
    /// <see cref="CanUserAddRows"/> is on (the row itself renders empty cells; typing in it
    /// creates a real item and adds it to <see cref="ItemsSource"/>).
    /// </summary>
    public static readonly object NewItemPlaceholder = new();

    private GridDataRow? _placeholderRow;
    private object? _pendingNewItem;

    /// <summary>
    /// Creates the new item for the add-row. When null, the <see cref="AddingNewItem"/> event
    /// may supply one; otherwise a parameterless instance of the resolved item type is created.
    /// </summary>
    public Func<object?>? NewItemFactory { get; set; }

    /// <summary>Raised before the add-row creates a new item; lets handlers supply or cancel it.</summary>
    public event EventHandler<GridAddingNewItemEventArgs>? AddingNewItem
    {
        add => AddHandler(AddingNewItemEvent, value);
        remove => RemoveHandler(AddingNewItemEvent, value);
    }

    /// <summary>Identifies the <see cref="AddingNewItem"/> routed event.</summary>
    public static readonly RoutedEvent AddingNewItemEvent = EventManager.RegisterRoutedEvent(
        nameof(AddingNewItem), RoutingStrategy.Direct,
        typeof(EventHandler<GridAddingNewItemEventArgs>), typeof(GridControl));

    /// <summary>Whether the given item is the add-row sentinel.</summary>
    internal static bool IsNewItemPlaceholder(object? item) => ReferenceEquals(item, NewItemPlaceholder);

    /// <summary>Shows or hides the placeholder as the last row of the presented view.</summary>
    internal void MaintainNewItemPlaceholder()
    {
        var want = CanUserAddRows && !IsReadOnly && CanAddToSource();
        if (want && _placeholderRow is null)
        {
            _placeholderRow = new GridDataRow(NewItemPlaceholder);
        }

        if (!want)
        {
            _placeholderRow = null;
        }

        _itemsView.SetPlaceholder(want ? _placeholderRow : null);
    }

    private bool CanAddToSource()
    {
        if (ItemsSource is not IList { IsReadOnly: false })
        {
            return false;
        }

        if (NewItemFactory is not null)
        {
            return true;
        }

        return DataEngine.ResolvedItemType is { } type
            && (type.IsValueType || Nullable.GetUnderlyingType(type) is not null || type.GetConstructor(Type.EmptyTypes) is not null);
    }

    /// <summary>
    /// Creates the new item for the placeholder row (raising <see cref="AddingNewItem"/>),
    /// inserts it into the source collection and remembers it as pending — Esc on the
    /// first edit removes it again.
    /// </summary>
    private bool TryBeginNewRowEdit(GridColumnBase column, out object? newItem)
    {
        newItem = null;
        if (!CanAddToSource())
        {
            return false;
        }

        var item = NewItemFactory?.Invoke();
        var args = new GridAddingNewItemEventArgs(item)
        {
            RoutedEvent = AddingNewItemEvent,
        };
        RaiseEvent(args);
        if (args.Cancel)
        {
            return false;
        }

        item = args.NewItem ?? item ?? CreateDefaultItem();
        if (item is null)
        {
            return false;
        }

        if (ItemsSource is not IList { IsReadOnly: false } list)
        {
            return false;
        }

        list.Add(item);
        _pendingNewItem = item;
        newItem = item;
        return true;
    }

    private object? CreateDefaultItem()
    {
        var type = DataEngine.ResolvedItemType;
        if (type is null || type == typeof(object))
        {
            return null;
        }

        var nonNullable = Nullable.GetUnderlyingType(type);
        if (nonNullable is not null)
        {
            type = nonNullable;
        }

        return type.IsValueType ? Activator.CreateInstance(type) : Activator.CreateInstance(type);
    }

    /// <summary>Removes the pending new item after a cancelled first edit (DataGrid behavior).</summary>
    private void CancelPendingNewItem()
    {
        if (_pendingNewItem is null)
        {
            return;
        }

        var item = _pendingNewItem;
        _pendingNewItem = null;
        if (ItemsSource is IList { IsReadOnly: false } list && list.Contains(item))
        {
            list.Remove(item);
        }
    }

    private static void OnCanUserAddRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((GridControl)d).MaintainNewItemPlaceholder();
    }

    /// <summary>
    /// Pastes TSV clipboard text into the grid starting at the current cell (Ctrl+V).
    /// Read-only columns and unconvertible cells are skipped; extra rows/columns beyond the
    /// data are dropped. Returns the number of cells written.
    /// </summary>
    public int PasteFromClipboard()
    {
        if (IsReadOnly)
        {
            return 0;
        }

        var text = TryGetClipboardText();
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var pastingArgs = new GridPastingClipboardEventArgs(text)
        {
            RoutedEvent = PastingFromClipboardEvent,
        };
        RaiseEvent(pastingArgs);
        if (pastingArgs.Cancel)
        {
            return 0;
        }

        var rows = GridClipboard.ParseTsv(pastingArgs.Text);
        if (rows.Count == 0)
        {
            return 0;
        }

        // Anchor: the current cell, or the first data row / first editable column.
        var items = DataEngine.View
            .Where(r => r is GridDataRow d && !IsNewItemPlaceholder(d.Item))
            .Select(r => ((GridDataRow)r).Item)
            .ToList();
        if (items.Count == 0)
        {
            return 0;
        }

        // Pasted columns align with VISUAL columns (Excel semantics): the cell destined for
        // a read-only column is dropped, not shifted onto the next editable column.
        var columns = VisibleColumns.Where(c => !string.IsNullOrEmpty(c.FieldName)).ToList();
        if (columns.Count == 0)
        {
            return 0;
        }

        var anchorRow = CurrentItem is null ? -1 : items.IndexOf(CurrentItem);
        if (anchorRow < 0)
        {
            anchorRow = 0;
        }

        var anchorColumn = CurrentColumn is null ? -1 : columns.IndexOf(CurrentColumn);
        if (anchorColumn < 0)
        {
            anchorColumn = 0;
        }

        var written = 0;
        for (var r = 0; r < rows.Count && anchorRow + r < items.Count; r++)
        {
            var cells = rows[r];
            var pasted = 0;
            for (var v = anchorColumn; v < columns.Count && pasted < cells.Length; v++, pasted++)
            {
                if (columns[v].ReadOnly)
                {
                    continue; // drop the cell, keep the alignment
                }

                if (columns[v].TrySetCellText(items[anchorRow + r], cells[pasted]))
                {
                    written++;
                }
            }
        }

        if (written > 0)
        {
            // Push the changed values through the bindings (cells bind one-way).
            InvalidateVisualChildren();
            foreach (var row in _rowRegistry)
            {
                row.CellsPanel?.PrepareCells();
            }
        }

        return written;
    }

    /// <summary>
    /// Reads the clipboard text; retries briefly on clipboard contention (clipboard
    /// managers and concurrent apps hold it open transiently) and gives up with null.
    /// </summary>
    private static string? TryGetClipboardText()
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 8)
            {
                System.Threading.Thread.Sleep(30);
            }
        }
    }

    /// <summary>Raised before pasting; lets handlers inspect the text or cancel.</summary>
    public event EventHandler<GridPastingClipboardEventArgs>? PastingFromClipboard
    {
        add => AddHandler(PastingFromClipboardEvent, value);
        remove => RemoveHandler(PastingFromClipboardEvent, value);
    }

    /// <summary>Identifies the <see cref="PastingFromClipboard"/> routed event.</summary>
    public static readonly RoutedEvent PastingFromClipboardEvent = EventManager.RegisterRoutedEvent(
        nameof(PastingFromClipboard), RoutingStrategy.Direct,
        typeof(EventHandler<GridPastingClipboardEventArgs>), typeof(GridControl));
}
