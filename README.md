# Vibe Coded WPF Controls

[![CI](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml/badge.svg)](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A free, open-source library of WPF controls. **Stage 1** delivers **GridControl** — a fast, MVVM-friendly data grid with a feature set that matches or beats freely available WPF grids.

**Documentation:** [English](README.md) | [Русский](README.ru.md) | [中文](README.zh-CN.md)

## Features (GridControl, stage 1)

- Data binding to any `IEnumerable` (including `ObservableCollection` and live updates)
- Automatic column generation (reflection + `[Display]`/`[ReadOnly]`/`[Browsable]` attributes) and manual columns
- Column types: Text, Numeric, CheckBox, ComboBox, Date, Template
- Column resizing, reordering (drag & drop), hiding, freezing (`FrozenColumnCount`)
- UI virtualization with container recycling — smooth scrolling with 1,000,000+ rows
- Sorting: single- and multi-column, sort indicators, custom comparers
- Selection: single/extended, keyboard (Ctrl/Shift), row check-box selection, current cell
- Editing: F2/Enter/Esc, per-column read-only, validation via `IDataErrorInfo` / `INotifyDataErrorInfo`
- Filtering, grouping, aggregate summaries (stage 1.2)
- Export to CSV / Excel (.xlsx) / HTML, clipboard support (stage 1.3)
- Light/Dark themes, High-DPI, accessibility (UI Automation), built-in RU/EN/ZH localization (stage 1.3)

See [ROADMAP.md](ROADMAP.md) for the full plan.

## Installation

```
dotnet add package VCWpfControls
```

Supports **.NET 6** and **.NET 8** on Windows (`net6.0-windows`, `net8.0-windows`).

## Quick start

```xml
<Window xmlns:vc="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <vc:GridControl ItemsSource="{Binding Employees}"
                    AutoGenerateColumns="True"
                    CanUserSortColumns="True"
                    SelectionMode="Extended" />
</Window>
```

Manual columns:

```xml
<vc:GridControl ItemsSource="{Binding Employees}">
    <vc:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="Name" Width="2*" />
        <columns:GridNumericColumn FieldName="Salary" Header="Salary" Width="100" StringFormat="C" />
        <columns:GridDateColumn FieldName="HireDate" Header="Hired" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="Active" Width="70" />
    </vc:GridControl.Columns>
</vc:GridControl>
```

## Documentation

- [Documentation site (EN/RU/ZH)](https://novikovdevopnsrc.github.io/VCWpfControls/)
- [Getting started (EN)](https://novikovdevopnsrc.github.io/VCWpfControls/en/getting-started.html)
- [Начало работы (RU)](https://novikovdevopnsrc.github.io/VCWpfControls/ru/index.html)
- [入门指南 (ZH)](https://novikovdevopnsrc.github.io/VCWpfControls/zh/index.html)

The `src/VCWpfControls.Demo` application contains a live example for every feature.

## Testing

The library is developed test-first where practical:

- `tests/VCWpfControls.Core.Tests` — unit tests for the UI-free data engine
- `tests/VCWpfControls.Control.Tests` — STA tests for the control itself
- `tests/VCWpfControls.UiTests` — FlaUI UI-automation tests against the demo app
- `benchmarks/VCWpfControls.Benchmarks` — BenchmarkDotNet performance suite

## License

[MIT](LICENSE) — free for any use, commercial included, with warranty disclaimer.
