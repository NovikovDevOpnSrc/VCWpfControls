---
title: GridControl overview
description: Vibe Coded WPF Controls — a free, MIT-licensed WPF control library. Stage 1 delivers GridControl, a fast, virtualized, MVVM-friendly data grid for .NET 6/8 on Windows.
---

# GridControl overview

**Vibe Coded WPF Controls** is a free, open-source library of WPF controls licensed under
[MIT](https://github.com/vibe-coded/VCWpfControls/blob/main/LICENSE). Stage 1 delivers
**GridControl** — a fast, virtualized, MVVM-friendly data grid.

The grid binds to any `IEnumerable`, generates columns from your item type (or takes
manual definitions), virtualizes row containers with recycling, and keeps its data engine
(`GridDataManager`) UI-free and unit-testable.

## Features

- **Data binding** — any `IEnumerable`, live updates via `ObservableCollection<T>`
- **Columns** — Text, Numeric, CheckBox, ComboBox, Date, Template; auto-generation with
  `[Display]`/`[ReadOnly]`/`[Browsable]` attributes or fully manual definitions
- **Layout** — column resize, drag & drop reorder, hide, freeze (`FrozenColumnCount`),
  widths in pixels, stars (`2*`) or `Auto`
- **Virtualization** — UI virtualization with container recycling; 1,000,000+ rows scroll smoothly
- **Sorting** — single- and multi-column, click and Shift+click, sort indicators, custom comparers
- **Filtering** — engine-level `FilterDescriptions` with operators
  Equals/Contains/Between/In/IsNull/Custom, plus a built-in filter row (`ShowFilterRow`)
- **Grouping** — engine-level `GroupDescriptions` with expand/collapse that survives refreshes
  (group rows with expand/collapse; footer aggregate summaries via `SummaryItems`)
- **Editing** — F2/Enter/Esc, per-column read-only, validation through
  `IDataErrorInfo` / `INotifyDataErrorInfo`
- **Selection** — SingleRow/Extended modes, full keyboard support, current cell
- **Clipboard** — Ctrl+C copies the selection as tab-separated text
- **Theming** — the default theme is defined with `ComponentResourceKey` resources that you
  can override per application

See the [roadmap](https://github.com/vibe-coded/VCWpfControls/blob/main/ROADMAP.md) for
also included: filter row, summaries, export (CSV/Excel/HTML), dark theme and localization.

## Quick start (XAML)

```xml
<Window x:Class="Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
        Title="Employees">

    <grid:GridControl ItemsSource="{Binding Employees}"
                      AutoGenerateColumns="True"
                      CanUserSortColumns="True"
                      SelectionMode="Extended" />
</Window>
```

With `AutoGenerateColumns="True"` (the default) and an empty `Columns` collection, the grid
creates a typed column for every public readable property of the item type.

## Quick start (C#)

```csharp
using System.Collections.ObjectModel;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;

public class Employee
{
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

// In your view model / window code-behind:
var employees = new ObservableCollection<Employee>
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob", Salary = 87000m, HireDate = new DateTime(2022, 7, 15), IsActive = true },
};

var grid = new GridControl { ItemsSource = employees };

// Programmatic sorting and filtering work through description collections:
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.FilterDescriptions.Add(
    new GridFilterDescription(nameof(Employee.Salary), GridFilterOperator.GreaterThanOrEqual, 90000m));
```

## Where to go next

- [Getting started](getting-started.md) — installation, requirements, your first grid
- [Columns](columns.md) — column types, auto-generation, widths, templates
- [Sorting, filtering & grouping](sorting-filtering-grouping.md) — the data engine APIs
- [Editing](editing.md) — edit lifecycle, validation, `GridCommands`
- [Selection](selection.md) — selection modes and the current cell
- [Theming](theming.md) — resource keys and overrides
- [Cookbook](cookbook.md) — ready-to-use recipes
