using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Grid;

/// <summary>Event arguments for the <c>AutoGeneratingColumn</c> event.</summary>
public sealed class GridAutoGeneratingColumnEventArgs : RoutedEventArgs
{
    internal GridAutoGeneratingColumnEventArgs(GridColumnBase column)
    {
        Column = column;
    }

    /// <summary>The generated column; can be replaced with a customized instance.</summary>
    public GridColumnBase Column { get; set; }

    /// <summary>Set to <see langword="true"/> to prevent the column from being added.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Event arguments for the <c>Sorting</c> event.</summary>
public sealed class GridSortingEventArgs : RoutedEventArgs
{
    internal GridSortingEventArgs(GridColumnBase column, Data.GridSortDirection newDirection)
    {
        Column = column;
        NewDirection = newDirection;
    }

    /// <summary>The column the user is sorting by.</summary>
    public GridColumnBase Column { get; }

    /// <summary>The direction that would be applied.</summary>
    public Data.GridSortDirection NewDirection { get; }

    /// <summary>Set to <see langword="true"/> to cancel sorting.</summary>
    public bool Cancel { get; set; }
}

/// <summary>Event arguments for the <c>CellEditStarting</c>/<c>CellEditEnding</c> events.</summary>
public sealed class GridCellEditEventArgs : RoutedEventArgs
{
    internal GridCellEditEventArgs(object row, GridColumnBase column, FrameworkElement? editingElement, bool cancelEdit)
    {
        Row = row;
        Column = column;
        EditingElement = editingElement;
        CancelEdit = cancelEdit;
    }

    /// <summary>The row's data item.</summary>
    public object Row { get; }

    /// <summary>The column being edited.</summary>
    public GridColumnBase Column { get; }

    /// <summary>The editing element (may be <see langword="null"/> on <c>CellEditStarting</c> replacements).</summary>
    public FrameworkElement? EditingElement { get; }

    /// <summary>On <c>CellEditEnding</c>: <see langword="true"/> when the edit is being cancelled.</summary>
    public bool CancelEdit { get; }

    /// <summary>Set to <see langword="true"/> to cancel the action (start or commit).</summary>
    public bool Cancel { get; set; }
}

/// <summary>Event arguments for the <c>SelectionChanged</c> event.</summary>
public sealed class GridSelectionChangedEventArgs : RoutedEventArgs
{
    internal GridSelectionChangedEventArgs(IList added, IList removed)
    {
        AddedItems = added;
        RemovedItems = removed;
    }

    /// <summary>Items added to the selection.</summary>
    public IList AddedItems { get; }

    /// <summary>Items removed from the selection.</summary>
    public IList RemovedItems { get; }
}

/// <summary>Defines routed commands of <see cref="GridControl"/>.</summary>
public static class GridCommands
{
    private static readonly RoutedUICommand BeginEditCommand = new(
        "Begin Edit", "BeginEdit", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.F2) });

    private static readonly RoutedUICommand CancelEditCommand = new(
        "Cancel Edit", "CancelEdit", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.Escape) });

    private static readonly RoutedUICommand CommitEditCommand = new(
        "Commit Edit", "CommitEdit", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.Enter) });

    private static readonly RoutedUICommand DeleteCommand = new(
        "Delete Selected Rows", "Delete", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.Delete) });

    private static readonly RoutedUICommand SelectAllCommand = new(
        "Select All", "SelectAll", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control) });

    private static readonly RoutedUICommand CopyCommand = new(
        "Copy Selection", "Copy", typeof(GridCommands), new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control) });

    /// <summary>Begins editing the current cell (F2).</summary>
    public static RoutedUICommand BeginEdit => BeginEditCommand;

    /// <summary>Cancels the current edit and restores the old value (Esc).</summary>
    public static RoutedUICommand CancelEdit => CancelEditCommand;

    /// <summary>Commits the current edit (Enter).</summary>
    public static RoutedUICommand CommitEdit => CommitEditCommand;

    /// <summary>Deletes the selected rows (Delete).</summary>
    public static RoutedUICommand Delete => DeleteCommand;

    /// <summary>Selects all rows (Ctrl+A).</summary>
    public static RoutedUICommand SelectAll => SelectAllCommand;

    /// <summary>Copies the selection to the clipboard (Ctrl+C).</summary>
    public static RoutedUICommand Copy => CopyCommand;
}
