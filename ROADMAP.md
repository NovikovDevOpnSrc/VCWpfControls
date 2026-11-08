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

## Future features (from forum/market research, 2026-11)

Ranked by community demand (r/csharp, Xceed/MahApps/WPF-UI trackers, LOB vendor catalogs):

### Near (v0.3 candidates)
- TreeListView: built-in per-level summaries (also closes the existing backlog item).
- PropertyGrid: deferred value commit while typing + TypeConverter-based value display
  (Xceed ships these in the PAID Plus edition - a free differentiator).
- AutoComplete editor (grid column + standalone): explicitly requested in wpfui #15.

### Mid
- Wizard control (perennial "missing WPF controls" list item).
- Docking: nested splitter groups inside one zone (existing backlog item).

### Large / flagship
- Pivot Grid — SHIPPED (v0.3.0 candidate): UI-free PivotEngine, PivotGrid control,
  drill-down, dimension filters, CSV export (stages 5.1-5.3).

### Condition expression editor (stage A done)
- Stage A (SHIPPED, UI-free core): ConditionGroup/ConditionExpression tree (And/Or, nested),
  ConditionSchema.FromType, ConditionEvaluator (delegates to GridFilterDescription.Matches —
  the grid semantics), ConditionParser text language with round-trip
  (`[Salary] > 1000 And [Dept] In ('IT','HR')`).
- Stage B (next): the ConditionBuilder control — typed value editors, And/Or groups UI,
  text tab with parse-error highlighting, human-readable preview. Localized x3.
- Stage C: grid integration (CustomFilterPredicate + "Condition..." header-menu entry),
  pivot FieldFilter bridge, JSON persistence, demo page, docs x3.

### Integration docs instead of building
- Charts: LiveCharts2 (MIT) usage recipes with our theming.
- Ribbon: Fluent.Ribbon (MIT) alongside our DockingManager.

## v0.2 candidates (next)

### GridControl
- Header menu extensions: "Filter…" opens the column filter menu; "Column Chooser" as a
  separate reorderable dialog; "Reset layout" (restore original widths/order/visibility);
  copy/export from the menu.
- Filter menu: numeric/date range mode alongside the value list.
### Done (kept for history)
- Sort engine optimization (v0.2.x: 70 MB → 14 MB on 100k rows, `benchmarks/RESULTS.md`).
- Editing: add-row entry (`CanUserAddRows`) + clipboard paste (v0.2.6).
- Cell selection unit (`SelectionUnit=CellOrRow`): rectangular ranges, item-based
  (survive sorting), bounding-box TSV copy (v0.2.7).

### TreeListView
- Node check boxes with tri-state cascade (done).
- Built-in group/footer summaries per level.

### Docking
- Nested splitter groups inside one zone.

### Done (kept for history)
- Docking auto-hide: edge tab strips + hover flyout (v0.2.9).
- Redock floating windows by dragging the window (not just the tab).

### Infrastructure
- Coverage of the mouse-driven paths (filter menu popup, header drag) with FlaUI tests.
- DocFX: enable the commented-out API reference generation (metadata block in docfx.json).
