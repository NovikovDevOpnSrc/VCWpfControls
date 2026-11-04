# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.4] — 2026-11-04

Issues round 4 (#11–#14) + deterministic async pipeline.

### Fixed — issues round 4 (#11–#14)

- **#13 (crash)**: horizontal scrolling crashed the app — `SetCurrentValue` was used on the
  read-only `HorizontalOffset` DP; now set through its `DependencyPropertyKey`.
- **#12 (crash)**: `StringFormat="{}{0:N0}"` (the documented XAML form) crashed
  `FormatCellText` (UIA cell names, BestFit, clipboard): the XAML `{}` escape is stripped,
  already-composite formats pass through, bare patterns get wrapped — all three forms tested.
- **#14**: after a user resize of star columns the extent updates and the horizontal scrollbar
  appears; star columns now clamp to their own `MinWidth` (not the internal 10px floor) when
  the viewport shrinks, overflowing into the scrollbar instead of collapsing.
- **#11**: template cells expose the whole visual subtree to UIA with hoisting — peerless
  panels are skipped and their descendants (TextBlocks, ItemsControl items) are lifted,
  so nested bars/lists in `CellTemplate` are readable by screen readers and UIA tests.

## [0.2.3] — 2026-11-04

Issues rounds 2–3 (#6–#10), async-pipeline race fix, review polish.

### Fixed — CI flake / async pipeline race

- `RefreshAsync` now marshals the view apply back to the SynchronizationContext captured at
  call time (the dispatcher thread in apps): the previous `ConfigureAwait(true)` relied on the
  ambient context, which is not always the dispatcher context under test hosts — the apply
  then ran on a threadpool thread and WPF CollectionView rejected the cross-thread change
  (`NotSupportedException`, swallowed by the fire-and-forget, view never updated).
  The pipeline error is no longer swallowed (`LastAsyncRefreshError` + Trace); the stale-drop
  engine test is deterministic via a gated synchronization context.

### Fixed — async pipeline race (earlier note, superseded)

- `UseAsyncRefresh` rebuilds are now serialized into a single in-flight pipeline with
  trailing-edge coalescing: rapid settings changes (e.g. `SortBy` raising Clear+Add) no
  longer run concurrent `RefreshAsync` loops whose interleaved view Resets crashed the
  virtualizing panel with stale indexes (`ArgumentOutOfRangeException` in
  `VirtualizingStackPanel.MeasureOverride`, seen once on a CI runner); the final view could
  also end up built from a stale snapshot depending on completion order.

### Fixed — issues round 3 (#9–#10)

Issues round 3: current-cell border cleanup, header text extraction shared across UI surfaces.

- **#9 (bug)**: the current-cell focus border stayed visible on the old row when
  the current cell moved to another row (borders accumulated across rows).
  `GridRow.IsCurrent` now invalidates its cells panel arrange, so the border is
  refreshed for both the old and the new row on any path that changes the flag.
- **#10 (bug)**: the column chooser of the header context menu labeled columns
  with element headers (e.g. `TextBlock`) as `System.Windows.Controls.TextBlock`.
  Header text extraction (string as-is; `TextBlock`/`AccessText`/`ContentControl`
  unwrap to their text) is now a shared `GridColumnBase.HeaderText` helper used
  by the header automation peer, the chooser menu, the cell peer's help text and
  clipboard header export.

## [0.2.2] — 2026-11-03

Issues round 2 (#6–#8) + review polish.

### Added

- `GridColumnBase.EditingElementStyle` — style for generated editing elements (matches the
  DataGrid naming; `ElementStyle` covers display elements).

### Fixed — issues round 2

### Fixed — issues round 2

- **#8 (bug)**: `GridColumnBase.ElementStyle` is now applied to generated cell
  elements (previously the DP was declared but never assigned, so e.g. a check
  box column could not be styled from the column). Changing `ElementStyle` at
  runtime re-styles already generated (cached) cells; setting it to null
  reverts to the default style.
- **#7 (accessibility)**: UIA names — group rows now expose the group caption
  (key + item count, respecting `HeaderStringFormat`) as their UIA `Name`
  instead of an empty string; column headers with `TextBlock`/`ContentControl`
  content unwrap to their text instead of the type name (`System.Windows.Controls.TextBlock`).
- **#6 (accessibility)**: `GridTemplateColumn` cells expose their
  `CellTemplate` content (buttons, links, custom controls) as UIA children of
  the cell peer, restoring the 0.2.0 behavior where interactive template
  content was reachable by UIA clients and screen readers (regression from
  the 0.2.1 cell-level peers). Covered by regression tests (template column
  with a named button → child peer with Invoke).

### Changed — review polish

- `ElementStyle` live change no longer triggers a redundant layout invalidation
  (direct re-style of cached cells only).

## [0.2.1] — 2026-11-03

Community issues round (#1–#5) + UIA cell-level peers.

### Added — UIA cell-level peers (issue #1 follow-up)

- `GridCellAutomationPeer`: `GridPattern.GetItem(row, column)` now returns the CELL DataItem
  (name = formatted value with `StringFormat`/`NullText` honored) instead of the row container;
  cells appear as children of the row DataItem, enabling exact per-column search from UIA
  clients; covered by FlaUI tests (cell names differ per column, contained in the row name).

### Fixed — issues round 1

- **#5 (bug)**: incremental Add/Remove/Replace in an `ObservableCollection` with an active
  grouping now rebuilds the view (group headers and counters cannot be patched incrementally);
  previously the view stayed flat until a manual refresh. Without grouping, adds remain
  incremental.
- **#1 (bug)**: UI Automation — `GridRow` and `GridColumnHeader` now have automation peers:
  rows are DataItems with `SelectionItem` support (select from UIA clients without input
  synthesis) and names built from visible cell values; headers expose their text and Invoke
  (sort cycling). `GridPattern.GetItem`, `TablePattern.ColumnHeaders` and
  `SelectionProvider.GetSelection` now return real elements. Covered by FlaUI UIA tests.
- **#4 (docs)**: fixed a broken callout in the RU sorting/filtering/grouping page.

### Added — issues round 1

- **#2**: `GridColumnBase.NullText` — marker (e.g. "—") shown instead of null values via the
  binding's `TargetNullValue` (text and numeric columns).
- **#3**: `GridGroupDescription.HeaderStringFormat` — custom group captions:
  `{0}` key, `{1}` count, `{2}` column header; localized default preserved.

### Added — TreeListView node check boxes

- `ShowNodeCheckBoxes` (tri-state): checking cascades to descendants (path mode loads collapsed
  subtrees; lazy mode records the node itself without loading), ancestors derive the
  indeterminate state from mixed children; API `IsNodeChecked`/`SetNodeChecked`/`CheckedItems`
  (all checked items, collapsed included) + routed `NodeCheckChanged` event.

## [0.2.0] — 2026-11-03

Post-v0.1.0 iterations: bug fixes, column content fit, header context menu, UX pass, sort performance.

### Changed — sort performance

- **Sort allocations −80%** (Sort_100k: 70.5 MB → 14.0 MB, mean 148 → 128 ms): the dominant
  allocator was `StringComparer.Create(culture)` built per string comparison in
  `GridValueCompare` (replaced with the allocation-free `string.Compare` overload;
  culture-aware ordering preserved); the sort itself is now an index-based stable sort
  (keys materialized once per level, index tie-breaker) instead of an LINQ
  `OrderBy`/`ThenBy` chain. Behavior is unchanged (multi-column order, stability on ties,
  nulls, culture) — covered by new 50k-row stress tests. FullPipeline_100k: 5.5 MB → 3.3 MB.

### Fixed

- Reported blank headers/... cells on the Editing demo page were a stale executable:
  verified structurally (STA model replica) and end-to-end (FlaUI dump of the Release build
  shows headers and values); added permanent content regressions to the FlaUI smoke
  (Basic + Editing pages must render header names and data values).
- Demo now pins the UI culture to en-US so library strings match the English demo texts
  (remove one line in App.xaml.cs to let the library follow the OS language).

### Added — UX pass (Fluent)

- **UX pass (Fluent-inspired)**: fixed the Dark theme showing dark text on dark surface —
  new `GridResources.TextBrushKey` carries the data foreground and switches with the theme
  (all controls re-bound, hardcoded colors removed); body typography 14 px; headers SemiBold
  with 32 px touch targets and hover/pressed states; localized empty state ("No data") shown
  when the view has no rows; `docs/ux-checklist.md` records the library UX standard.
  Note: RDP sessions return black frames on screen capture — verified structurally instead.

### Added — header context menu

- **Header context menu** (right-click, localized EN/RU/ZH): sort ascending/descending with
  state marks, clear sorting, fit column / fit all columns, group-by toggle, freeze-up-to-column
  toggle, hide column (guarded for the last visible column) and a column-visibility check-list
  submenu; replaceable via `ContextMenuOpening`.

### Added — column content fit

- **Column content fit**: auto-generated columns now default to `Width="Auto"` (content-based;
  horizontal scrollbar when content exceeds the viewport) instead of equal star shares;
  `BestFitColumn`/`BestFitAllColumns` measure the current view exactly (row sampling, header
  included); double-clicking a column splitter best-fits the column; the horizontal scrollbar
  now refreshes on every layout pass.

### Added — docking drag-to-redock

- **Docking drag-to-redock**: dragging a pane tab highlights the drop zone (quarter edges +
  center) via a theme-aware adorner; dropping moves the pane into that zone.

### Added — TreeListView lazy loading

- **TreeListView lazy loading**: `ChildrenProvider` (lazy children source taking precedence over
  `ChildMemberPath`), `HasChildrenPredicate` (expander visibility without loading), children
  cached across rebuilds, live provider collections invalidate the cache.

### Added — tree filtering and docking floating windows

- **TreeListView filtering**: `FilterPredicate` — a node is visible when it or any descendant
  matches; ancestors of matches are temporarily expanded, manual expansion state restored on clear.
- **Docking floating windows**: `FloatPane`/`DockPaneBack`, floating windows with cascade
  positioning above the app window, dock-back on window close; floating state and window
  bounds included in the persisted layout.

### Added (stage 3)

- **Docking** (`VCWpfControls.Docking`): `DockingManager` with five zones (left/top/right/
  bottom with live splitters, tabbed center), `DockPane` (zone switching, closable/hidable,
  `DockWidth`/`DockHeight`, bubbling `Closed`), `ActivePane` two-way, and XML layout
  persistence (`SaveLayout`/`LoadLayout` via the UI-free `DockLayoutSerializer`).
- **Editors** (`VCWpfControls.Editors`): `MaskedTextBox` (mask engine with required/optional
  digit/letter/any slots, literals, prompt char, paste validation, value with or without
  literals) and `NumericUpDown` (bounded decimal value, repeat buttons, arrows/PageUp/PageDown,
  mouse wheel, culture-aware parsing, format string/decimal places, ValueChanged event).

### Added (stage 2)

- **TreeListView** (`VCWpfControls.TreeList`): hierarchy with columns on top of GridControl
  (columns, virtualization, selection, editing, row details, themes); roots from `ItemsSource`,
  children from `ChildMemberPath`; expand/collapse (toggle, `ToggleNode`/`ExpandAll`/`CollapseAll`,
  state survives rebuilds), per-level sibling sorting, live updates via ObservableCollections.
- **PropertyGrid** control (`VCWpfControls.PropertyGrid`): typed editors (text, multi-line
  text, check box, enum combo, numeric, date, read-only), attribute-driven metadata
  (`[Display]`/`[Category]`/`[DisplayName]`/`[ReadOnly]`/`[Browsable]`), category expanders,
  search box, expandable nested objects, live refresh via `INotifyPropertyChanged`,
  `PropertyValueChanged` event; localized (EN/RU/ZH).
- **GridControl row details**: `RowDetailsTemplate` + `RowDetailsVisibilityMode`
  (`Collapsed`/`VisibleWhenSelected`/`Visible`) — stage 1 complete.

## [0.1.0] — 2026-11-02 (stage 1–3)

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

### Added (stage 1.2.x)

- **Incremental collection updates**: single-item `Add`/`Remove`/`Replace` in an
  `ObservableCollection` no longer rebuild the view — rows are inserted at the
  binary-searched sorted position (or skipped by active filters); grouped views still
  rebuild. Selection, current cell and summaries stay in sync automatically.
- **Background refresh**: `UseAsyncRefresh="True"` computes filter/sort/group on the thread
  pool and applies the result on the UI thread; stale results are dropped and recomputed.
- **Group panel with drag-to-group**: `ShowGroupPanel` (on by default) — drop a column
  header to group by it (`CanUserGroupColumns`), removable chips per grouping level.
- **Column filter menu**: funnel button in every header (`CanUserFilter`) opening a
  localized value-list menu (search, check list, select all, clear).
- Filter row input is debounced (300 ms, configurable per presenter).

### Known limitations (next releases)
- Docking: auto-hide and nested splitter groups.
