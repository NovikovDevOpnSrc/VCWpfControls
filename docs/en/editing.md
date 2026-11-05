---
title: Editing
description: GridControl cell editing lifecycle (F2/Enter/Esc, double-click), CellEdit* events, validation via IDataErrorInfo/INotifyDataErrorInfo and the GridCommands routed commands.
---

# Editing

## Enabling editing

Editing is enabled by default (`IsReadOnly = false`) for columns that provide an editing
element. Switch it off per grid or per column:

```xml
<grid:GridControl ItemsSource="{Binding Orders}" IsReadOnly="False">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Number" Header="Order" ReadOnly="True" />
        <columns:GridNumericColumn FieldName="Quantity" Header="Qty" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

`ReadOnly` on the column wins over the grid: a read-only column cannot enter edit mode
even when the grid is editable. `CanUserDeleteRows` (default `true`) separately controls
whether the Delete key may remove selected rows.

## Edit lifecycle

| Action | Result |
|---|---|
| Double-click a cell / `F2` | Begin edit on the current cell |
| `Enter` | Commit the value through the binding |
| `Esc` | Cancel the edit and restore the previous value |
| Click another cell / arrow keys while editing | The edit is committed first |

Equivalent API: `BeginEdit()`, `BeginEdit(item, column)`, `CommitEdit()`, `CancelEdit()`;
`IsEditing` tells whether a cell is currently in edit mode.

```csharp
grid.BeginEdit(selectedItem, salaryColumn);
// ...
if (!grid.CommitEdit())
{
    // validation failed — the cell stays in edit mode
}
```

CheckBox cells are different: they are interactive without entering edit mode and commit
immediately on click; `Space` toggles the check box of the current cell.

## Events

Three events cover the lifecycle. All use `GridCellEditEventArgs` with `Row` (the data
item), `Column`, `EditingElement` (may be `null` on `CellEditStarting`), `CancelEdit`
(true on `CellEditEnding`/`CellEditEnded` when the edit is being cancelled) and a
cancellable `Cancel` flag.

```csharp
// Veto editing for shipped orders:
grid.CellEditStarting += (s, e) =>
{
    if (e.Column.FieldName == "Quantity" && ((Order)e.Row).IsShipped)
    {
        e.Cancel = true;
    }
};

// Last chance to interfere with the commit:
grid.CellEditEnding += (s, e) =>
{
    if (e.CancelEdit) return;                 // Esc was pressed
    var box = e.EditingElement as TextBox;
    if (box?.Text.Contains("evil") == true)
    {
        e.Cancel = true;                      // keep editing
    }
};

// After the fact:
grid.CellEditEnded += (s, e) =>
{
    if (!e.CancelEdit)
    {
        Logger.LogInformation("Committed {Field}", e.Column.FieldName);
    }
};
```

## Validation

The two-way cell bindings created by the grid enable `ValidatesOnDataErrors`,
`ValidatesOnNotifyDataErrors` and `ValidatesOnExceptions`. That means your existing
`IDataErrorInfo` / `INotifyDataErrorInfo` models work unchanged:

```csharp
public class Order : IDataErrorInfo
{
    public int Quantity { get; set; }

    public string Error => this[nameof(Quantity)];

    public string this[string columnName]
        => columnName == nameof(Quantity) && Quantity <= 0
            ? "Quantity must be positive"
            : string.Empty;
}
```

On commit the grid calls `UpdateSource()` on the binding and checks
`Validation.GetHasError`. When the value is invalid, **the cell stays in edit mode** so the
user can fix it; `CommitEdit()` returns `false`. Errors are rendered by the standard WPF
validation template of the editing element.

With a `CellEditingTemplate`, the grid does not push values itself — your template's own
bindings (e.g. `UpdateSourceTrigger=PropertyChanged`) update the data item, and commit
reduces to ending the edit session.

## GridCommands

`GridCommands` exposes routed commands wired into the control (including default key
gestures), so toolbar buttons or menus integrate without code-behind:

| Command | Default gesture | Effect |
|---|---|---|
| `GridCommands.BeginEdit` | `F2` | Begin editing the current cell |
| `GridCommands.CancelEdit` | `Esc` | Cancel the current edit |
| `GridCommands.CommitEdit` | `Enter` | Commit the current edit |
| `GridCommands.Delete` | `Delete` | Delete the selected rows |
| `GridCommands.SelectAll` | `Ctrl+A` | Select all rows (Extended mode) |
| `GridCommands.Copy` | `Ctrl+C` | Copy the selection to the clipboard |

```xml
<StackPanel Orientation="Horizontal">
    <Button Content="Edit (F2)"
            Command="grid:GridCommands.BeginEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="Save row (Enter)"
            Command="grid:GridCommands.CommitEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="Delete"
            Command="grid:GridCommands.Delete"
            CommandTarget="{Binding ElementName=Grid}" />
</StackPanel>
```

`CommandTarget` must point at the grid because routed commands execute on the focused
element otherwise.

## Deleting rows

`DeleteSelectedItems()` (also the Delete key) removes the selected items **from the source
collection** — this works only when `ItemsSource` implements `IList` (e.g.
`ObservableCollection<T>`), `CanUserDeleteRows` is `true` and the grid is not `IsReadOnly`.
The selection is cleared first; removal then flows back to the grid through
`INotifyCollectionChanged`.


## Adding rows (CanUserAddRows)

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                   CanUserAddRows="True"
                   NewItemFactory="{Binding CreateEmployee}" />
```

With `CanUserAddRows="True"` an empty add-row appears at the end. Editing a cell in it:

1. raises `AddingNewItem` (set `e.NewItem` to supply the item, `e.Cancel` to abort);
2. otherwise `NewItemFactory` runs; otherwise a parameterless instance of the item type
   is created (`ItemsSource` must be a writable `IList`);
3. the item is added to the source and the edit continues in the new row.

Pressing **Esc** during that first edit removes the just-added item again (DataGrid
behavior); committing it keeps it. `CanUserAddRows` requires a writable `IList` source and
switches off automatically for read-only grids.

## Pasting from the clipboard

`PasteFromClipboard()` (also **Ctrl+V**) reads TSV text (the Excel clipboard format) and
writes it into the grid starting at the current cell:

- pasted columns align with the visual columns; cells destined for read-only columns are
  dropped, not shifted;
- values are converted to the property type using the current culture; unconvertible
  cells are skipped;
- extra rows/columns beyond the data are dropped; the add-row placeholder is never a
  paste target;
- `PastingFromClipboard` fires before pasting (inspect `e.Text`, cancel with `e.Cancel`).

Returns the number of cells written.
