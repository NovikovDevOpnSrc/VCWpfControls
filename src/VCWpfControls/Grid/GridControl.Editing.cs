using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Primitives;

namespace VCWpfControls.Grid;

/// <summary>Editing logic of <see cref="GridControl"/>.</summary>
public partial class GridControl
{
    private object? _editingItem;
    private GridColumnBase? _editingColumn;

    /// <summary>Whether a cell is currently in edit mode.</summary>
    public bool IsEditing => _editingColumn is not null;

    internal bool CanBeginEdit()
        => CurrentItem is not null && CurrentColumn is not null && !IsReadOnly && !CurrentColumn.ReadOnly;

    /// <summary>Begins editing the current cell.</summary>
    public bool BeginEdit() => BeginEdit(CurrentItem, CurrentColumn);

    /// <summary>Begins editing the given cell; raises <c>CellEditStarting</c> (cancellable).</summary>
    public bool BeginEdit(object? item, GridColumnBase? column)
    {
        if (IsEditing)
        {
            CommitEdit();
        }

        if (item is null || column is null || IsReadOnly || column.ReadOnly)
        {
            return false;
        }

        if (IsNewItemPlaceholder(item))
        {
            // The add-row placeholder: create the item, add it to the source and edit THAT
            // row. Its container realizes a dispatcher pass later, so defer the edit.
            if (!TryBeginNewRowEdit(column, out var newItem) || newItem is null)
            {
                return false;
            }

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                () => BeginEdit(newItem, column));
            return true;
        }

        var args = new GridCellEditEventArgs(item, column, null, cancelEdit: false)
        {
            RoutedEvent = CellEditStartingEvent,
        };
        RaiseEvent(args);
        if (args.Cancel)
        {
            return false;
        }

        var row = TryFindRow(item);
        if (row?.CellsPanel is not { } panel)
        {
            return false;
        }

        panel.BeginCellEdit(column);
        if (panel.EditingElement is null)
        {
            return false;
        }

        _editingItem = item;
        _editingColumn = column;
        row.IsEditing = true;
        SetCurrentCell(item, column, alignIntoView: true);
        return true;
    }

    /// <summary>Commits the current edit; raises <c>CellEditEnding</c>/<c>CellEditEnded</c>.</summary>
    public bool CommitEdit() => EndEditCore(commit: true);

    /// <summary>Cancels the current edit and restores the previous value.</summary>
    public bool CancelEdit() => EndEditCore(commit: false);

    private bool EndEditCore(bool commit)
    {
        if (!IsEditing || _editingItem is null || _editingColumn is null)
        {
            return false;
        }

        var item = _editingItem;
        var column = _editingColumn;
        var row = TryFindRow(item);
        var editingElement = row?.CellsPanel?.EditingElement;

        var endingArgs = new GridCellEditEventArgs(item, column, editingElement, cancelEdit: !commit)
        {
            RoutedEvent = CellEditEndingEvent,
        };
        RaiseEvent(endingArgs);

        var committed = commit && !endingArgs.Cancel;
        if (committed && editingElement is not null && column.GetEditingCommitProperty() is { } property)
        {
            committed = TryPushValue(editingElement, property);
        }

        if (!committed && commit && !endingArgs.Cancel)
        {
            // Validation error: stay in edit mode so the user can fix the value.
            return false;
        }

        _editingItem = null;
        _editingColumn = null;

        if (row is not null)
        {
            row.CellsPanel?.EndCellEdit(committed);
            row.IsEditing = false;
        }

        var endedArgs = new GridCellEditEventArgs(item, column, editingElement, cancelEdit: !committed)
        {
            RoutedEvent = CellEditEndedEvent,
        };
        RaiseEvent(endedArgs);
        Focus();

        // The add-row's first edit: cancel removes the just-added item again (DataGrid
        // behavior); a successful commit ends its "pending" status.
        if (ReferenceEquals(_pendingNewItem, item))
        {
            if (committed)
            {
                _pendingNewItem = null;
            }
            else
            {
                CancelPendingNewItem();
            }
        }

        return committed || !commit;
    }

    /// <summary>Pushes the edited value through the binding and reports whether it is valid.</summary>
    private static bool TryPushValue(FrameworkElement element, DependencyProperty property)
    {
        if (BindingOperations.GetBindingExpression(element, property) is not { } expression)
        {
            return true;
        }

        expression.UpdateSource();
        return !Validation.GetHasError(element);
    }
}
