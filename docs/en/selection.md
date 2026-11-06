---
title: Selection
description: GridControl selection modes (SingleRow/Extended), SelectedItem/SelectedItems, the current cell (CurrentItem/CurrentColumn), the Select* API and keyboard navigation.
---

# Selection

## Selection modes

`SelectionMode` offers two modes:

- **`GridSelectionMode.SingleRow`** — one row at a time; clicking or navigating selects the row.
- **`GridSelectionMode.Extended`** (the default) — Ctrl+click toggles single rows,
  Shift+click selects a range, `Ctrl+A` selects everything.

Switching to `SingleRow` while several rows are selected keeps only the first one.

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  SelectionMode="Extended"
                  SelectedItem="{Binding SelectedEmployee}" />
```

## Selected items

| Member | Kind | Meaning |
|---|---|---|
| `SelectedItem` | DP (two-way by default) | First selected item; bind it to your view model |
| `SelectedItems` | `IList` | Snapshot-friendly live view of the selection |
| `SelectedItemsCollection` | `ObservableCollection<object>` | The same selection as an observable collection |

`SelectedItem` is a dependency property with `BindsTwoWayByDefault` — no
`Mode=TwoWay` needed in bindings.

## Programmatic selection

```csharp
grid.SelectItem(alice);            // add to the selection
grid.UnselectItem(bob);            // remove one item
grid.SelectRange(alice, dave);     // select all rows between two items (view order)
grid.SelectAll();                  // Extended mode only
grid.UnselectAll();                // clear
```

`SelectAll` is a no-op in `SingleRow` mode. When grouping is active, `SelectRange` and
`SelectAll` operate on data rows and skip group header rows.

Selection **survives filtering and sorting**: items stay selected even when they are
temporarily hidden by a filter; only removal from the source collection deselects them.

## The current cell

The current cell is the anchor for keyboard navigation and editing:

- `CurrentItem` — the data item of the current row (two-way bindable);
- `CurrentColumn` — the column of the current cell, or `null`;
- `CurrentCellChanged` — a CLR event fired whenever the current cell moves.

```csharp
grid.CurrentItem = employees[4];
grid.ScrollIntoView(employees[4]);
```

## SelectionChanged

```csharp
grid.SelectionChanged += (s, e) =>
{
    foreach (var added in e.AddedItems) Console.WriteLine($"selected: {added}");
    foreach (var removed in e.RemovedItems) Console.WriteLine($"deselected: {removed}");
};
```

## Keyboard reference

| Key | Action |
|---|---|
| Arrow keys | Move the current cell (commits an open edit first) |
| `PageUp` / `PageDown` | Move the current cell by ~3/4 of a page |
| `Home` / `End` | First/last column of the current row |
| `Ctrl+Home` / `Ctrl+End` | First/last cell of the view |
| `Ctrl+A` | Select all rows (Extended only) |
| `Ctrl+C` | Copy the selection as tab-separated text |
| `Delete` | Delete selected rows (see [Editing](editing.md)) |
| `Enter` / `Esc` / `F2` | Commit / cancel / begin editing |

In `SingleRow` mode plain navigation also moves the selection; in `Extended` mode arrow
keys move only the current cell — hold Shift (or Shift+click) to extend the selection.

## Scrolling to an item

`ScrollIntoView(item)` scrolls vertically until the data item's row is visible:

```csharp
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(viewModel.EmployeeToHighlight);
grid.SelectItem(viewModel.EmployeeToHighlight);
```

Group header rows never become the current row — navigation skips them automatically.


## Cell selection (SelectedCells)

```xml
<grid:GridControl SelectionUnit="CellOrRow" ... />
```

In `CellOrRow` mode clicks select CELLS instead of rows:

- click selects a cell; **Shift+click** selects the rectangle to the anchor;
  **Ctrl+click** toggles a cell; **Ctrl+Shift+click** adds a rectangle;
- **Shift+arrows** extend the rectangle from the anchor; `SelectAll` selects every cell;
- ranges are stored ITEM-based: they survive sorting/filtering (the rectangle always
  covers its two corner cells); ranges whose rows leave the source are pruned;
- the selected cells are painted with the theme selection background;
- `SelectedCells` returns the cells in view order; `SelectCellRange`/`AddCellRange`/
  `UnselectAllCells` drive it programmatically; `CellSelectionChanged` fires on changes;
- **Ctrl+C** copies the bounding box of the selected ranges as TSV — cells outside the
  ranges inside the box come out empty (Excel semantics), pairs with Ctrl+V paste.

Row selection keeps working through its API (`SelectItem`, `SelectedItems`) in both modes.
