# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] — 0.1.0 (stage 1)

### Added

**Infrastructure**
- Project skeleton: solution, library (`net6.0-windows`/`net8.0-windows`), demo app,
  unit / control-STA / FlaUI UI test projects, BenchmarkDotNet project, CI pipeline
  (build + tests + coverage + pack), docs workflow, release workflow.
- MIT license, README in English, Russian and Chinese; DocFX documentation (EN/RU/ZH);
  CONTRIBUTING, SECURITY, ROADMAP, issue/PR templates.

**GridControl — stage 1.1 (core)**
- Data binding to any `IEnumerable` incl. `ObservableCollection` live updates.
- Auto-generated columns (reflection, `[Display]`/`[ReadOnly]`/`[Browsable]`) and manual columns:
  Text, Numeric, CheckBox, ComboBox, Date, Template.
- Column widths: absolute / star / auto (content), resize, drag reorder, hide, freeze
  (`FrozenColumnCount`).
- UI virtualization with container recycling; horizontal scrolling with frozen columns.
- Sorting: single/multi-column (Shift+click), indicators, custom comparers, API
  (`SortBy`/`AddSortLevel`/`ClearSorting`), routed `Sorting`/`Sorted` events.
- Selection: `SingleRow`/`Extended`, Ctrl/Shift, `SelectItem`/`SelectRange`/`SelectAll`,
  two-way `SelectedItem`/`CurrentItem`.
- Editing: F2/Enter/Esc, double-click, per-column read-only, validation via
  `IDataErrorInfo`/`INotifyDataErrorInfo` (invalid values keep the cell in edit mode),
  `CellEditStarting`/`CellEditEnding`/`CellEditEnded` routed events, `GridCommands`.
- Keyboard navigation (arrows/PgUp/PgDn/Home/End/Ctrl+Home/End), clipboard copy (Ctrl+C),
  row deletion (Delete) for `IList` sources.

**GridControl — stage 1.2 (analytics)**
- Grouping: `GroupDescriptions`, group rows with expand/collapse (state survives refresh),
  localization-aware group captions.
- Filter row: `ShowFilterRow="True"` — contains for text columns, typed equality otherwise;
  programmatic `FilterDescriptions` (Equals/NotEquals/Contains/StartsWith/EndsWith/
  GreaterThan/LessThan/Between/In/NotIn/IsNull/IsNotNull/Custom).
- Footer summaries: `SummaryItems` (`Sum`/`Min`/`Max`/`Avg`/`Count`) with format strings,
  recomputed on every view change.

**GridControl — stage 1.3 (completion)**
- Export: CSV (culture-aware separator, quoting, BOM), Excel `.xlsx` (typed cells, frozen
  header, auto width) and HTML (styled, escaped); to strings, files or `XLWorkbook`.
- Theming: Light (default) and Dark themes, `GridThemeManager.Apply`, all brushes exposed
  as `ComponentResourceKey` resources for custom themes.
- Built-in localization: English, Russian, Chinese (`Strings.*.resx`, extendable).
- Accessibility: UI Automation peer with Grid/Table/Selection patterns.

### Known limitations (next releases)
- Collection changes rebuild the whole pipeline (incremental updates and asynchronous
  sort/filter for very large sets are planned for 1.2.x).
- Drag-to-group panel and per-column filter menu UI are not built-in yet (engine APIs work).
- Row details templates are planned.
