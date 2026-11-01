---
title: Getting started
description: Install VCWpfControls from NuGet, check the system requirements (.NET 6/8 on Windows) and build your first data grid in minutes.
---

# Getting started

## System requirements

| Requirement | Details |
|---|---|
| OS | Windows (WPF is Windows-only) |
| Framework | .NET 6 or .NET 8 (`net6.0-windows` / `net8.0-windows`) |
| Package | `VCWpfControls` (NuGet) |

The library is a plain WPF control library — no native dependencies, no external packages.

## Installation

Install the NuGet package:

```
dotnet add package VCWpfControls
```

Or add the reference to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="VCWpfControls" Version="*" />
</ItemGroup>
```

The default control theme (`Themes/Generic.xaml`) ships inside the package and is picked up
automatically — no `MergeDictionaries` calls are required.

## Your first grid

Define a model. Note the data annotations: they drive auto-generated columns (see
[Columns](columns.md) for the full list).

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Employee
{
    [Display(Name = "Name", Order = 0)]
    public string Name { get; set; } = "";

    [Display(Name = "Salary", Order = 1)]
    public decimal Salary { get; set; }

    [Display(Name = "Hired", Order = 2)]
    public DateTime HireDate { get; set; }

    [Display(Name = "Active", Order = 3)]
    public bool IsActive { get; set; }
}
```

Expose the data as `ObservableCollection<Employee>` so the grid sees additions and removals:

```csharp
public ObservableCollection<Employee> Employees { get; } = new()
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob",   Salary = 87000m, HireDate = new DateTime(2022, 7, 15), IsActive = false },
};
```

Drop the grid into your window and bind `ItemsSource`:

```xml
<Window xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <grid:GridControl ItemsSource="{Binding Employees}" />
</Window>
```

That is all: columns are generated from `Employee`, clicking a header sorts,
double-clicking a cell starts editing (F2 also works), Ctrl+C copies the selection.

## Useful properties on the first day

| Property | Default | Purpose |
|---|---|---|
| `AutoGenerateColumns` | `true` | Generate columns when `Columns` is empty |
| `IsReadOnly` | `false` | Disable editing for the whole grid |
| `SelectionMode` | `Extended` | `SingleRow` or `Extended` (Ctrl/Shift) |
| `RowHeight` | `NaN` (auto) | Fixed row height squeezes the most out of virtualization |
| `FrozenColumnCount` | `0` | Keep the first N columns visible while scrolling horizontally |
| `ClipboardCopyMode` | `IncludeHeaders` | `None` disables Ctrl+C |
| `CanUserDeleteRows` | `true` | Delete key removes selected rows (source must be an `IList`) |

## Live updates

When `ItemsSource` implements `INotifyCollectionChanged` (e.g. `ObservableCollection<T>`),
the grid rebuilds its view on every change. Note that at stage 1 each change rebuilds the
whole filter → sort → group pipeline; incremental and asynchronous updates for very large
collections are planned for stage 1.2.

Call `Refresh()` to force a full rebuild manually (for example after bulk edits that do not
raise collection change notifications):

```csharp
grid.Refresh();
```

## Next steps

- Define columns explicitly — see [Columns](columns.md)
- Sort, filter and group from code — see [Sorting, filtering & grouping](sorting-filtering-grouping.md)
- Restyle the grid for your application — see [Theming](theming.md)
