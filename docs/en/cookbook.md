---
title: Cookbook
description: Ready-to-use GridControl recipes — huge data sets, currency formatting, checkbox and enum combo columns, clipboard copy, intercepting Sorting and scrolling to a row.
---

# Cookbook

Short, complete recipes for common tasks. All snippets use these namespaces:

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## 1. Hundreds of thousands of rows

Virtualization with container recycling is always on. Get the most out of it by fixing the
row height and avoiding auto-width columns where possible:

```xml
<grid:GridControl x:Name="Grid"
                  ItemsSource="{Binding Trades}"
                  RowHeight="24"
                  HeaderHeight="28"
                  FrozenColumnCount="2"
                  CanUserSortColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Symbol" Header="Symbol" Width="90" />
        <columns:GridTextColumn FieldName="Side" Header="Side" Width="60" />
        <columns:GridNumericColumn FieldName="Price" Header="Price" Width="110" StringFormat="N4" />
        <columns:GridNumericColumn FieldName="Quantity" Header="Qty" Width="100" />
        <columns:GridDateColumn FieldName="Timestamp" Header="Time" Width="140" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

Notes for very large sources:

- At stage 1 the engine rebuilds the full pipeline (filter → sort → group) on every
  collection change; prefer bulk updates (replace the `ObservableCollection` contents,
  then call `Refresh()`) over per-item notifications. Incremental and asynchronous
  processing is on the roadmap for stage 1.2.
- Sorting 1M+ rows in code is `grid.SortBy(...)` — see recipe 6 for how to observe it.

## 2. Currency and number formats

`StringFormat` on any column, exactly like a `Binding.StringFormat`:

```xml
<columns:GridNumericColumn FieldName="Salary" Header="Salary" Width="120"
                           StringFormat="C" />          <!-- currency of the current culture -->
<columns:GridNumericColumn FieldName="Ratio" Header="Ratio" Width="90"
                           StringFormat="{}{0:P1}" />   <!-- percents, 1 digit -->
<columns:GridDateColumn FieldName="HireDate" Header="Hired" Width="120"
                        StringFormat="dd.MM.yyyy" />    <!-- date columns default to "d" -->
```

## 3. A checkbox column that commits on click

`GridCheckBoxColumn` is interactive without entering edit mode and writes through its
two-way binding immediately. `Space` toggles the current cell, `ReadOnly` turns it off:

```xml
<columns:GridCheckBoxColumn FieldName="IsActive" Header="Active" Width="70" />
<columns:GridCheckBoxColumn FieldName="IsArchived" Header="Archived" Width="80" ReadOnly="True" />
```

## 4. ComboBox column bound to an enum

Manual definition (works for any lookup list):

```xml
<columns:GridComboBoxColumn FieldName="Status" Header="Status" Width="140"
                            ItemsSource="{Binding Statuses}"
                            DisplayMemberPath="Label"
                            SelectedValuePath="Id" />
```

For enums you do not need to do anything: auto-generation maps an enum property to a
`GridComboBoxColumn` whose `ItemsSource` is `Enum.GetValues(type)` — the cell shows the
value name and the combo edits it out of the box:

```csharp
public enum OrderStatus { Draft, Confirmed, Shipped, Closed }

public class Order
{
    public OrderStatus Status { get; set; }   // auto-generated as a combo column
}
```

## 5. Copying to the clipboard (Excel-friendly)

`Ctrl+C` (or `GridCommands.Copy`, or `CopyToClipboard()`) copies the selected rows — or the
current row when nothing is selected — as tab-separated text. With the default
`ClipboardCopyMode="IncludeHeaders"` the first line contains the headers, so pasting into
Excel/Sheets produces a neat table:

```xml
<grid:GridControl ItemsSource="{Binding Orders}" ClipboardCopyMode="IncludeHeaders" />
```

```csharp
grid.SelectRange(firstOrder, lastOrder);
grid.CopyToClipboard();
```

Set `ClipboardCopyMode="None"` to disable copying entirely.

## 6. Intercepting sorting

Cancel it, constrain it or just observe it:

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Timestamp" && e.NewDirection == GridSortDirection.Descending)
    {
        e.Cancel = true;   // forbid descending sorts on the timestamp column
    }
};

grid.Sorted += (s, e) =>
{
    StatusText = $"Sorted by {string.Join(", ", grid.SortDescriptions.Select(d => d.FieldName))}";
};
```

Programmatic sorting is also available without touching the mouse:
`SortBy(...)` replaces all levels, `AddSortLevel(...)` extends them, `ClearSorting()`
resets, and `SortDescriptions` is data-bindable if you prefer driving it from a view model.

## 7. Scroll to and highlight a row

```csharp
// after loading or a search:
grid.Refresh();                                   // rebuild the view if data changed externally
grid.SortBy(nameof(Customer.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(customer);                    // bring the row into view
grid.UnselectAll();
grid.SelectItem(customer);
grid.CurrentItem = customer;                      // place the current cell on it
```

## 8. Filtering with a custom predicate

When the 17 built-in operators are not enough, the `Custom` operator takes a predicate over
the whole data item. Combine it with regular filters (they AND together):

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription(nameof(Trade.Symbol),
    GridFilterOperator.NotEquals, "CANCELLED"));

grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Trade t && t.Quantity * t.Price > 10_000m,
});
```

A mini search box in your UI can simply replace a `Contains` filter on every keystroke:

```csharp
_searchFilter = new GridFilterDescription(nameof(Customer.Name), GridFilterOperator.Contains, text);
// on text change:
grid.FilterDescriptions.Clear();
grid.FilterDescriptions.Add(_searchFilter);
```
