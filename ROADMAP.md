# Roadmap

## Release history

- **v0.1.0** (2026-11-02) — **GridControl** (columns with auto-generation/resize/reorder/
  hide/freeze, BestFit, virtualization with recycling, multi-column sorting, filter row +
  value-list filter menu, grouping with drag-to-group, footer summaries, editing with
  validation, cell/row selection, clipboard copy, `UseAsyncRefresh`, export CSV/Excel/HTML,
  Light/Dark themes, EN/RU/ZH localization, UI Automation peers, row details);
  **TreeListView** (hierarchy, per-level sorting, `FilterPredicate`, lazy loading);
  **PropertyGrid** (typed editors, categories, search, expandable objects);
  **Editors** (`MaskedTextBox`, `NumericUpDown`); **DockingManager** (zones with splitters,
  tabbed center, floating windows, drag-to-redock, XML layout persistence).
- **v0.2.x** (2026-11-03…05) — pilot-user issue rounds #1–#15 (UIA row/header/cell peers,
  `NullText`, `GroupHeaderStringFormat`, star-resize clamp, template-cell accessibility,
  scroll crash, format-string crash, exporter headers); add-row + clipboard paste;
  `SelectionUnit=CellOrRow` cell ranges; header menu (Filter…, Column Chooser dialog,
  Reset Layout); filter ranges; docking auto-hide + nested splitter groups; visual
  root-cause round (cell padding alignment, frozen-zone clipping, DPI peer bounds,
  `MinWidth 20`, tree indent); column separators; sort optimization (70 → 14 MB on 100k
  rows, `benchmarks/RESULTS.md`); dark-theme text/UX pass; agent skills + AGENTS.md.
- **v0.3.0** (2026-11-08) — PropertyGrid TypeConverter support + `CommitMode`; TreeList
  per-level summaries (`Subtree`/`Children` + `Level`); **AutoComplete** editor + grid
  column; **Wizard** control; **Pivot Grid** (UI-free `PivotEngine`, `PivotGrid`,
  drill-down, dimension filters, CSV export).
- **v0.3.1** (2026-11-08) — **condition expression editor** (engine + text-language
  parser, `ConditionBuilder` control, grid/pivot integration via `CustomFilterPredicate`);
  third-party integration recipes (LiveCharts2, Fluent.Ribbon — MIT, demo-only);
  nested docking splitter groups (`SplitGroup`).
- **v0.4.0** (2026-11-08) — **Gantt chart**: `GanttEngine` (auto-scheduling FS/SS/FF/SF +
  lag/lead, cycle detection, CPM critical path, culture-aware timescales), `GanttView`
  (task tree + timeline, bars/milestones/dependency arrows/today line, day-week-month
  zoom), drag interactions (move/resize bars, Alt+drag to link, live re-scheduling).

## Next

### Quality (next up)
- FlaUI e2e coverage for the post-v0.3 demo pages: Pivot, Conditions, Gantt,
  Charts & Ribbon (UIA-pattern-only flows — own reliability stats named UIA the most
  bug-prone zone, and these pages are covered by STA tests only).

### Flagship
- **Scheduler / Calendar** — the remaining LOB-ladder gap (DevExpress/Syncfusion ship it,
  OSS WPF doesn't). Same three-stage shape as Gantt: UI-free engine (occurrences,
  recurrence-rule subset, conflicts) → control (day/week/month views, appointment
  drag/resize) → integration + demo + docs ×3.

### Increments
- Gantt follow-ups: non-working days/weekends (rendering + scheduling awareness),
  baseline comparison, inline date editing in the task tree, timeline virtualization
  for large task lists.
- Grid: per-cell UIA polish, `GroupHeaderTemplate`.
- Benchmarks for the new engines (Pivot, Conditions, Gantt).

### On hold
- MDI — tabbed docking covers most scenarios; low community demand.
- First-party charts / ribbon — covered by the integration recipes (MIT third-party,
  demo-only packages).

## Done from the original research list (history)

- Sort engine optimization (v0.2.x: 70 MB → 14 MB on 100k rows).
- Editing: add-row entry (`CanUserAddRows`) + clipboard paste (v0.2.6).
- Cell selection unit (`SelectionUnit=CellOrRow`): rectangular ranges, item-based
  (survive sorting), bounding-box TSV copy (v0.2.7).
- Header menu extensions + filter ranges (v0.2.8); column separators (v0.2.5).
- Docking auto-hide (v0.2.9); nested splitter groups (v0.3.1); redock floating windows
  by dragging the window.
- TreeListView node check boxes with tri-state cascade; per-level summaries (v0.3.0).
- PropertyGrid: deferred commit + TypeConverters (v0.3.0).
- AutoComplete editor + column (v0.3.0, wpfui #15).
- Wizard control (v0.3.0).
- Pivot Grid complete (v0.3.0); condition editor complete (v0.3.1).
- DocFX API reference live; integration docs instead of building charts/ribbon (v0.3.1);
  FlaUI mouse-path/UIA coverage of the grid funnels (grows again for the new pages).
