---
title: Sorting, filtering & grouping
description: How to sort (click, Shift+click, SortBy/AddSortLevel), filter (FilterDescriptions with 17 operators) and group (GroupDescriptions, GridGroup, SetGroupExpanded) GridControl from code.
---

# Sorting, filtering & grouping

All three features are implemented by the UI-free data engine (`GridDataManager`), driven by
three observable collections on the grid: `SortDescriptions`, `FilterDescriptions` and
`GroupDescriptions`. Changing a collection rebuilds the view immediately (pipeline:
filter → sort → group → flatten).

> **Built-in UI.** Set `ShowFilterRow="True"` to get a filter row under the headers
> (contains for text columns, typed equality for numeric/date columns; applied with a
> 300 ms debounce). Every column header carries a funnel button opening a value-list filter
> menu (search box + check list of distinct values, `(Select all)`/`Clear`/`OK`). Numeric
and date columns also get **From/To** boxes above the list: both bounds apply a `Between`
filter, one bound applies `>=` / `<=` (culture-aware parsing; a parsed range takes
precedence over the check list, and reopening the menu preselects the active range).
> Drag a column header into the **group panel** above the headers to group by it
> (`CanUserGroupColumns="True"` by default); chips in the panel remove grouping levels.
> Group rows render with an expand/collapse toggle; add aggregates to `SummaryItems` to show
> footer summaries (`Sum`, `Min`, `Max`, `Avg`, `Count`). All engine APIs below work
> regardless of UI.

## Performance modes

- **Incremental updates.** Single-item `Add`/`Remove`/`Replace` in an
  `ObservableCollection` are applied without rebuilding the view: the engine inserts the row
  at the binary-searched position under the active sorts and skips items filtered out
  (grouped views still rebuild). Selection, current cell and summaries are updated to match.
- **Background refresh.** `UseAsyncRefresh="True"` moves filtering/sorting/grouping to the
  thread pool and applies the result on the UI thread. Stale results (state changed while
  computing) are dropped and recomputed, so rapid keystrokes or header clicks always settle
  on the latest state. The demo page “Virtualization (100k rows)” has a toggle to feel the
  difference.

## Sorting

### By mouse and keyboard

Clicking a header cycles the sort direction: none → ascending → descending → none.
**Shift+click** adds the column as a further sort level instead of replacing the current
ones (multi-column sort). The direction is shown by a sort indicator in the header.

Both interactions can be observed or cancelled through the `Sorting` event:

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Total")
    {
        e.Cancel = true; // protect the server-side order for this column
    }
};

grid.Sorted += (s, e) => { /* order applied, e.g. update status bar */ };
```

### From code

```csharp
// Replace all sort levels:
grid.SortBy(nameof(Employee.Name), GridSortDirection.Descending);

// Or via a column instance (uses SortMemberPath ?? FieldName):
grid.SortBy(nameColumn, GridSortDirection.Ascending);

// Add/update one level, keeping the others (multi-column sort):
grid.AddSortLevel(nameof(Employee.Department), GridSortDirection.Ascending);
grid.AddSortLevel(nameof(Employee.Salary), GridSortDirection.Descending);

// Remove all levels:
grid.ClearSorting();
```

`SortDescriptions` is an `ObservableCollection<GridSortDescription>`, so you can also edit
it directly. Each `GridSortDescription` has:

- `FieldName` — property path (`"Address.City"` is valid);
- `Direction` — `Ascending` / `Descending` / `None`;
- `Comparer` — optional custom `IComparer` for cell values (replaces the culture-aware
  default comparison; the direction is still applied by the engine);
- `Culture` — culture for string comparisons (`CurrentCulture` by default).

```csharp
grid.SortDescriptions.Add(new GridSortDescription("Salary", GridSortDirection.Descending)
{
    Comparer = Comparer<object>.Create((a, b) => ((decimal)a!).CompareTo((decimal)b!)),
});
```

A column can sort by another member with `SortMemberPath`; per-column `CanUserSort = false`
disables header-click sorting for that column only.

## Filtering

`FilterDescriptions` is a collection of `GridFilterDescription`s; multiple filters combine
with a **logical AND**.

```csharp
using VCWpfControls.Grid.Data;

// Salary between 50,000 and 90,000:
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Salary), GridFilterOperator.Between, 50000m, 90000m));

// Name contains "al" (culture-aware, case-insensitive by default):
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Name), GridFilterOperator.Contains, "al"));

// Remove one / all filters:
grid.FilterDescriptions.RemoveAt(0);
grid.FilterDescriptions.Clear();
```

### Operators

| Operator | Meaning | Operands used |
|---|---|---|
| `None` | Filter disabled | — |
| `Equals`, `NotEquals` | (Not) equal to `Value` | `Value` |
| `Contains`, `NotContains`, `StartsWith`, `EndsWith` | Text matching (case-insensitive by default) | `Value` as text |
| `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` | Comparisons | `Value` |
| `Between`, `NotBetween` | Inside / outside an inclusive range | `Value` and `Value2` |
| `In`, `NotIn` | Value is (not) contained in a list (`Value` is an `IEnumerable`) | `Value` |
| `IsNull`, `IsNotNull` | Null (or `DBNull`) check | — |
| `Custom` | Your predicate over the whole data item | `CustomPredicate` |

Additional knobs on `GridFilterDescription`:

- `TextComparison` — `StringComparison` for text operators (default
  `CurrentCultureIgnoreCase`);
- `CustomPredicate` — a `Predicate<object>` used by the `Custom` operator:

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Employee e && e.IsActive && e.Salary > 50000m,
});
```

Null cells match only the explicit `IsNull` operator (and `In` lists that contain `null`),
so combined filters behave predictably.

## Grouping

`GroupDescriptions` is a collection of `GridGroupDescription`s, outermost level first:

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Department)));
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Office)));
```

When grouping is active, the grid's view interleaves **group header rows** with data rows.
The flattened view is available on the engine: `grid.DataEngine.View` is a
`IReadOnlyList<GridRowInfo>` of `GridDataRow` (`Kind == GridRowKind.Data`, exposes `Item`)
and `GridGroupRow` (`Kind == GridRowKind.Group`, exposes `Group`).

Each `GridGroup` provides:

- `Key` — the group key (the cell value, possibly transformed);
- `Level` and `Parent` — position in the group tree;
- `ChildGroups` — sub-groups of the next level;
- `Items` — data items directly in this group (deepest level only);
- `TotalItems` — total number of data rows under the group;
- `IsExpanded` — expansion state, **persisted across data refreshes**.

### Expand and collapse

Collapse or expand a group programmatically through the engine — the state survives
subsequent `Refresh()` calls:

```csharp
foreach (var row in grid.DataEngine.View.OfType<GridGroupRow>())
{
    if (row.Group.Level == 0 && row.Group.TotalItems < 5)
    {
        grid.DataEngine.SetGroupExpanded(row.Group, expanded: false);
    }
}
```

### Custom keys

`KeyConverter` transforms the cell value into the group key (e.g. bucketing dates by
month); `KeysComparer` controls the order of group keys:

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Order.CreatedAt))
{
    KeyConverter = value => ((DateTime)value!).ToString("yyyy-MM"),
});
```

Group rows are skipped by keyboard navigation, and `ScrollIntoView`, `SelectRange` and
friends operate on data items, not group rows.

> **Footer summaries** are configured via `grid.SummaryItems.Add(new GridSummaryItem("Salary", GridSummaryFunction.Sum, "C"))`.
> For group-level totals, compute them from `GridGroup.Items` / `TotalItems`.

### Custom group captions

`GridGroupDescription.HeaderStringFormat` customizes group rows: `{0}` = key, `{1}` = item count, `{2}` = column header. Default is the localized "key: N item(s)".

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription("Asset")
{
    HeaderStringFormat = "{2}: {0} ({1} items)",
});
```
