---
title: Columns
description: GridControl column types (Text, Numeric, CheckBox, ComboBox, Date, Template), auto-generation with data annotations, widths, freezing and cell templates.
---

# Columns

All column types live in `VCWpfControls.Grid.Columns` and derive from `GridColumnBase`.

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## Column types

| Type | Displays | Edits with | Notes |
|---|---|---|---|
| `GridTextColumn` | `TextBlock` | `TextBox` | The general-purpose column |
| `GridNumericColumn` | right-aligned text | right-aligned `TextBox` | For numeric fields |
| `GridCheckBoxColumn` | interactive `CheckBox` | — | Commits immediately on click; `Space` toggles the current cell |
| `GridComboBoxColumn` | display text of the selected item | `ComboBox` | `ItemsSource`, `DisplayMemberPath`, `SelectedValuePath` |
| `GridDateColumn` | date text (`"d"` format by default) | `DatePicker` | `StringFormat` overrides the display format |
| `GridTemplateColumn` | your `CellTemplate` | your `CellEditingTemplate` | Fully custom cells; `CellTemplate` is required |

## Common properties (GridColumnBase)

| Property | Default | Purpose |
|---|---|---|
| `Header` | `""` | Header content (text or any object) |
| `FieldName` | `""` | Binding path inside the row item; dotted paths like `Address.City` work |
| `Width` | `1*` | `"120"`, `"Auto"`, `"*"`, `"2*"` — see below |
| `MinWidth` / `MaxWidth` | `0` / `∞` | Pixel limits applied to every sizing mode |
| `Visible` | `true` | Hide a column without removing it |
| `ReadOnly` | `false` | Cells of this column cannot enter edit mode |
| `CanUserSort` | `true` | Per-column switch for header-click sorting |
| `SortMemberPath` | `null` | Sort by a different path than `FieldName` |
| `StringFormat` | `null` | Display format, e.g. `"C"` or `"{}{0:N2}"` |
| `HorizontalContentAlignment` | `Left` | Alignment of cell content |
| `ElementStyle` | `null` | Style applied to generated cell elements (display mode only; editing elements are not affected) |
| `HeaderStyle` | `null` | Style applied to the column header |
| `CellTemplate` / `CellEditingTemplate` | `null` | Override display/edit elements with your own `DataTemplate` |

The `GetCellValue(item)` method reads the cell value of an item using the column's field path.

## Widths

`Width` accepts three unit kinds (XAML strings are converted automatically):

- **Pixels** — `Width="120"`: fixed device-independent pixels.
- **Star** — `Width="*"` or `Width="2*"`: proportional share of the remaining viewport
  (the default is `1*`).
- **Auto** — `Width="Auto"`: fits the widest content.

`MinWidth` and `MaxWidth` apply in all modes — including to user resizing.

## Auto-generation and attributes

With `AutoGenerateColumns="True"` (the default) and an **empty** `Columns` collection, the
grid reflects over the item type and creates one column per public readable property.
You can steer that with attributes:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Display(Name = "Order number", Order = 0)]   // header text and position
    public int Number { get; set; }

    [Display(Name = "Total", Order = 2), DisplayFormat(DataFormatString = "C")]
    public decimal Total { get; set; }

    [ReadOnly(true)]                              // column becomes read-only
    public DateTime CreatedAt { get; set; }

    [Browsable(false)]                            // no column at all
    public byte[] Blob { get; set; } = Array.Empty<byte>();
}
```

Rules the generator follows:

- `[Display]` — `ShortName`/`Name` becomes the header, `Order` sorts the columns
  (`[DisplayName]` is honored as a fallback for the header text).
- `[ReadOnly(true)]` — the generated column gets `ReadOnly = true`.
- `[Browsable(false)]` — the property is skipped.
- Column type mapping: `bool` → CheckBox, `DateTime`/`DateTimeOffset`/`DateOnly` → Date,
  enums → ComboBox (its `ItemsSource` is filled with `Enum.GetValues`), numeric types →
  Numeric, everything else → Text.

### AutoGeneratingColumn event

Raised for every generated column; replace the column instance or cancel it:

```csharp
grid.AutoGeneratingColumn += (s, e) =>
{
    if (e.Column.FieldName == "Blob")
    {
        e.Cancel = true;                      // skip this column
        return;
    }

    if (e.Column is GridNumericColumn { FieldName: "Salary" })
    {
        e.Column.StringFormat = "C";          // tweak the generated column
    }
};
```

## Manual columns

Set columns explicitly in XAML — auto-generation stays off while `Columns` is not empty:

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  FrozenColumnCount="1"
                  CanUserResizeColumns="True"
                  CanUserReorderColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="Name" Width="2*" MinWidth="120" />
        <columns:GridNumericColumn FieldName="Salary" Header="Salary" Width="110"
                                   StringFormat="C" HorizontalContentAlignment="Right" />
        <columns:GridDateColumn FieldName="HireDate" Header="Hired" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="Active" Width="70" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

## Freezing

`FrozenColumnCount` (a property of the grid, not of a column) keeps the first N columns
pinned while the user scrolls horizontally. Values below 0 are rejected; hidden columns do
not count. Combined with `CanUserReorderColumns`, users can drag a column to the front and
pin it by adjusting the count.

## Templates

Use `GridTemplateColumn` (or `CellTemplate`/`CellEditingTemplate` on any column) when the
built-in elements are not enough. The template's `DataContext` is the row's data item:

```xml
<columns:GridTemplateColumn FieldName="Progress" Header="Progress" Width="160">
    <columns:GridTemplateColumn.CellTemplate>
        <DataTemplate>
            <ProgressBar Value="{Binding Progress}" Maximum="100" Height="14" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellTemplate>
    <columns:GridTemplateColumn.CellEditingTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Progress, UpdateSourceTrigger=PropertyChanged}" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellEditingTemplate>
</columns:GridTemplateColumn>
```

Note: with `CellEditingTemplate` the grid does not push values itself — your template's own
bindings update the data item (see [Editing](editing.md)).

## ComboBox columns

`GridComboBoxColumn` chooses its binding mode from `SelectedValuePath`:

- **Without `SelectedValuePath`** — the cell value is the whole list item; the combo binds
  `SelectedItem`.
- **With `SelectedValuePath`** — the cell value is a key; the combo binds `SelectedValue`
  and the display cell resolves the key to the display text via `DisplayMemberPath`.

```xml
<columns:GridComboBoxColumn FieldName="DepartmentId" Header="Department"
                            ItemsSource="{Binding Departments}"
                            DisplayMemberPath="Name"
                            SelectedValuePath="Id" Width="180" />
```

### Content fit (BestFit)

Auto-generated columns are sized to their content by default (`Width="Auto"`: header width
plus the widths of the currently visible rows, growing as rows realize). When the content is
wider than the viewport, a horizontal scrollbar appears; `FrozenColumnCount` keeps the leading
columns in place while scrolling.

Exact one-shot sizing (measures up to 1000 rows of the current view by default, all rows with `0`):

```csharp
grid.BestFitColumn(column);        // one column (header included)
grid.BestFitAllColumns();          // every visible column
grid.FillColumnsToViewport();      // stretch all columns to fill the control width
```

`FillColumnsToViewport` scales the visible columns proportionally to their current widths
until they exactly fill the control (run `BestFitAllColumns` first for a content-proportional
result); `MinWidth` is always honored — when the minimums alone exceed the width, the
columns keep their minimums and the horizontal scrollbar stays. The header measurement
reserves the template's real furniture (funnel + sort slot + margins), so short headers
like "ID" stay visible after a fit.

Double-clicking a column's resize splitter best-fits that column (Excel-style).

## Header context menu

Right-click any column header for the built-in (localized EN/RU/ZH) menu:

- **Sort Ascending / Sort Descending** — with a check mark on the active sort;
- **Clear Sorting** — enabled while any sort is active;
- **Filter...** — opens the condition builder (`ConditionBuilder`) preseeded with one
  condition on THIS column — filter by condition; enabled when `CanUserFilter` is on;
- **Filter by values...** — the column's value-check list (the classic value filter);
- **Fit column (content) / Fit all columns** — one-shot best fit;
- **Fit all columns (fill width)** — stretches the columns to fill the control;
- **Group by this column** — toggle, adds/removes a grouping level;
- **Freeze up to this column** — toggles `FrozenColumnCount`;
- **Hide column** — disabled for the last visible column;
- **Columns** — check-list submenu toggling each column's visibility, led by the
  **Column chooser...** dialog entry;
- **Reset layout** — restores the layout captured when the columns were first built:
  the column order, every width, visibility and the frozen column count.

The **Column chooser** dialog (`OpenColumnChooser`) lists all columns with visibility
checkboxes in the current order plus Move up/Move down buttons for reordering.
`ResetLayout()` is also public — bind it to your own UI if you like.

The menu is built by `BuildColumnHeaderMenu` and can be replaced by handling
`ContextMenuOpening` on the grid and assigning your own `ContextMenu`.

### Null display

`NullText="—"` on a column shows the marker instead of empty null cells (applied via the binding's `TargetNullValue`; works for text and numeric columns).

### Column separators

`ShowColumnSeparators="True"` draws vertical lines between the columns inside every row (in addition to the horizontal row separators). `ColumnSeparatorBrush` overrides the color; it defaults to `GridLinesBrush`. Separators respect frozen columns and horizontal scrolling.
