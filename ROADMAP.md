# Roadmap

## Released

### v0.1.0 — Stage 1–3 (published on nuget.org, 2026-11-02)

- **GridControl**: columns (Text/Numeric/CheckBox/ComboBox/Date/Template, auto-generation,
  resize/reorder/hide/freeze, Auto/Absolute/Star widths, BestFit + header context menu),
  UI virtualization with recycling, sorting (multi-column, indicators, custom comparers),
  filter row + value-list filter menu, grouping (drag-to-group, expand/collapse), footer
  summaries, selection (single/extended, keyboard), editing with validation, row details,
  clipboard copy, incremental ObservableCollection updates, `UseAsyncRefresh`,
  export CSV/Excel/HTML, Light/Dark themes, localization (EN/RU/ZH), UI Automation peers.
- **TreeListView**: hierarchy with columns, per-level sorting, live updates,
  `FilterPredicate`, lazy loading (`ChildrenProvider`/`HasChildrenPredicate`).
- **PropertyGrid**: typed editors, attribute metadata, categories, search,
  expandable objects, INotifyDataErrorInfo/PropertyChanged refresh.
- **Editors**: `MaskedTextBox` (mask engine), `NumericUpDown` (bounded spinner).
- **Docking**: `DockingManager` — zones with splitters, tabbed center, floating windows,
  drag-to-redock, XML layout persistence.

## v0.2 candidates (next)

### GridControl
- Header menu extensions: "Filter…" opens the column filter menu; "Column Chooser" as a
  separate reorderable dialog; "Reset layout" (restore original widths/order/visibility);
  copy/export from the menu.
- Sort engine optimization: replace LINQ OrderBy chains (≈70 MB allocations on 100k rows,
  see `benchmarks/RESULTS.md`) with a stable in-place list sort.
- Editing: add-row entry (`CanUserAddRows`), paste from clipboard.
- Filter menu: numeric/date range mode alongside the value list.
- Cell selection unit (`SelectedCells`).

### TreeListView
- Node check boxes with tri-state cascade.
- Built-in group/footer summaries per level.

### Docking
- Auto-hide (pinned tabs with flyout).
- Nested splitter groups inside one zone.
- Redock floating windows by dragging the window (not just the tab).

### Infrastructure
- Coverage of the mouse-driven paths (filter menu popup, header drag) with FlaUI tests.
- DocFX: enable the commented-out API reference generation (metadata block in docfx.json).
