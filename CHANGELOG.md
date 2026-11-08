# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added — pivot stage 3: drill-down, dimension filters, CSV export

- **Drill-down**: `PivotEngine` optionally keeps per-intersection source buckets
  (`includeSourceRows`); `PivotResult.SourceRows[row][column]` powers
  `PivotGrid.GetDrillDownRows(r, c)`, and double-clicking a data cell raises
  `DrillDownRequested` (Row/Column/SourceRows/Aggregates — open the rows in a details
  grid; the grand-total row does not drill).
- **Dimension filters**: `PivotEngine.Compute(..., fieldFilter)` and the
  `PivotGrid.FieldFilter` DP — `Func<fieldName, value>` applied to every row/column field
  before bucketing; totals aggregate the INCLUDED rows only (a filtered grand total over
  unfiltered rows was caught by the tests); changing the filter recomputes.
- **CSV**: `PivotExporter.ToCsv(result, culture?, rowFieldNames?)` — culture list
  separator, quoting like the grid exporter, grand-total row when totals are present.
- Tests: 5 Core (filter/totals consistency, null filter, source buckets, CSV layout,
  ru-RU separator + empty cells) + 3 STA (filter recompute, drill-down rows, event args).
  Full pivot suite: 15 Core + 8 STA. Local pixel tests hit the disconnected-RDP empty
  render (environment); CI verifies.


### Added — PivotGrid control (stage 2)

- `PivotGrid : GridControl`: renders the `PivotEngine` result — one leading column per
  row field, one star data column per column-field combination (caption `A / B · Sum`
  with several measures), a localized Total column and a grand-total row
  (`IncludeTotals`). Themes, headers, sorting and virtualization are inherited from the
  grid; changing the field/measure collections or the source recomputes and rebuilds
  immediately (`Result` exposes the last PivotResult).
- `PivotRowItem` (Values/Cells/Totals/IsTotal) is the rendered row; `PivotTextColumn`
  binds cells through arbitrary paths (`Cells[2]`, `Values[0].Caption`) —
  `GridColumnBase.CreateCellBinding` became `protected internal virtual` for that.
- Demo page (sales by region x product); docs ×3 gained the control section; 5 STA tests
  (columns/rows/totals, path-bound display elements, multi-measure captions, no-totals
  mode, field-change recompute) — recompute wiring validated by injection. 188/188.


### Added — pivot engine (stage 1: headless)

- `PivotEngine.Compute(items, rowFields, columnFields, measures, includeTotals)`:
  groups any IEnumerable by row/column field values and aggregates every intersection
  through the shared footer calculator (Sum/Min/Max/Avg/Count; aggregates round back to
  the source-like type). UI-free — Core-tested without STA.
- `PivotMeasure` (field + function + caption; several per cell), `PivotAxisValue`
  (value + culture caption), `PivotResult` (sorted `Rows`/`Columns` with composite keys,
  `Cells[row][column][measure]`, `RowTotals`/`ColumnTotals`/`GrandTotals`).
- Semantics: null field values form their own group; empty intersections aggregate to
  null (Count to 0); axis values sort ascending with nulls first; totals optional.
- 10 Core tests (grouping/aggregation, totals, all functions, composite keys, null
  groups, empty intersections, no-totals mode, empty source, measure validation,
  axis-value equality) — bucket aggregation validated by injection. The rendering
  PivotGrid control is stage 2.


### Added — Wizard control

- `Wizard` + `WizardPage`: ordered pages with Back/Next/Finish/Cancel (localized EN/RU/ZH
  captions; Next becomes Finish on the last page). `CurrentPageIndex` coerces into range;
  `CanGoBack`/`CanGoNext`/`CanFinish` reflect the state.
- Page validation: `WizardPage.CanLeave` (bindable) disables forward navigation — Back is
  always allowed.
- Events: `PageChanging` (cancelable; a canceled move restores the index synchronously),
  `PageChanged`, `Finished` (Next on the last page), `Cancelled`.
- Template parts `PART_BackButton/NextButton/CancelButton` (commands refresh on state
  changes); custom templates just provide the three buttons.
- Demo page (blocked first page until a checkbox, live status line); docs ×3; 7 STA tests
  (navigation + button states, finish flow, CanLeave gate, cancelable changing, events,
  coercion), navigation injection-validated. 183/183.


### Added — AutoComplete editor + GridAutoCompleteColumn

- `Editors.AutoComplete`: text input with a suggestion popup — any IEnumerable source
  (strings or objects via `ItemTextPath`), culture-invariant contains matching or a custom
  `ItemFilter`, `MinimumPrefixLength`/`MaxSuggestions` gates, ArrowUp/Down + Enter/Tab/Esc
  and click handling, `SuggestionAccepted` event, two-way `Text`, `SelectedItem`.
  The matching logic is UI-free (`AutoCompleteFilter`, Core-tested).
- `GridAutoCompleteColumn`: grid column editing through the AutoComplete editor (display
  element identical to text columns; editing commits via the Text property).
- Demo: name suggestions on the Column types page; docs ×3; tests: 6 Core (filter) +
  6 STA (popup open/close, Enter accepts incl. ArrowDown navigation, Esc, prefix gate,
  column wiring). Control suite 171/171.


### Added — TreeListView per-level summaries

- `TreeListView.TreeSummaryItems` (`GridTreeSummaryItem`): aggregates next to every node
  row with children — Sum/Min/Max/Avg/Count through the shared footer calculator.
  `Mode` selects subtree (all descendants, resolved through the lazy providers — collapsed
  nodes included) vs direct children; `Level` restricts rows; the node itself never counts.
  Values are computed on the FULL structure (expansion-independent), cached per node, and
  drawn right-aligned in the row (OnRender, italic); collection changes repaint rows.
- Demo: org-tree page sums Headcount per unit; docs ×3; 6 STA tests (subtree/children/
  levels/expansion-independence/invalidation/all-functions), lazy resolution validated by
  bug injection. Control suite 170/170.


### Added/Fixed — PropertyGrid: TypeConverters, commit modes, reliability pass

- **TypeConverter support**: properties with a registered string-convertible converter
  (Guid, CultureInfo, custom structs) display via `ConvertToString(culture)` and parse
  input via `ConvertFromString(culture)` — previously read-only type names. Editor-kind
  resolution computes the converter in place (constructing a PropertyItem there recursed
  through its own ctor — StackOverflow).
- `PropertyGrid.CommitMode` (`OnCommit` default: Enter/focus-loss, what the editors
  already did — now explicit + documented; `Immediate`: per keystroke). **Esc** cancels a
  pending text edit and restores the value.
- **Two latent bugs found by the new tests**: expandable rows double-parented their grid
  (logical-tree exception); INPC refresh searched editors through the VISUAL tree, which
  misses not-yet-rendered/collapsed rows — now a logical walk.
- Coverage 4 → 14 tests (attributes, search, null display, enum commit, single
  ValueChanged per commit, INPC refresh, Guid roundtrip, commit timing, Esc). Immediate
  mode and converter parsing validated by bug injection. Control suite 164/164.


## [0.2.10] — 2026-11-07

API reference on the docs site, exporter header fix (#15), UIA end-to-end tests + funnel accessibility fix.

### Added — UIA-driven end-to-end tests (and a funnel a11y fix)

- `MousePathsUiTests`: real-application flows through UIA patterns only (Invoke/Toggle/
  Select — no raw SendInput mouse/keyboard, which disconnected RDP sessions deny):
  the funnel filter popup end-to-end (open → uncheck a value → OK → the excluded rows
  vanish from the UIA tree), the cell-selection toggle (live counter + row selection
  never leaking into SelectedCells), and Table.ColumnHeaders exposing header names.
- Navigation matches nav items by their caption Text child (the ListItem name is the
  view-model type); WPF filter popups live in a top-level `Popup` HWND — searched from
  the desktop; assertions are data-based (row-name composition) because GridPattern
  RowCount reflects the virtualized viewport, not the data size.
- **A11y fix found by these tests**: the header funnel `ToggleButton` opened the filter
  menu only on `Click` — the UIA Toggle pattern flips `IsChecked` without raising Click,
  so assistive tech/keyboards could not open the menu. It now opens on `Checked` too
  (and resets the state before opening).
- Raw-input paths (header drag, typing) stay covered by the STA suites. Local pixel
  tests went empty (user RDP session disconnected) — CI is the verification source.


### Fixed — #15: element headers export as type names

- `GridExporter.ToCsv`/`ToHtml` wrote headers set to ELEMENTS (e.g. `TextBlock` — the
  standard trick for header tooltips) as `System.Windows.Controls.TextBlock`. All header
  paths now unwrap elements through the shared `GridColumnBase.HeaderText` extractor —
  including two more sites of the same family found by audit: BestFit's header width
  measure and the group-panel/group-caption fallbacks. Regression-tested (fails with the
  old conversion, verified by injection).


### Added — API reference on the docs site

- The documentation site now includes an auto-generated API reference (97 types: Grid,
  TreeList, PropertyGrid, Editors, Docking) built from the library's XML doc comments.
- `apigen.docfx.json` at the repo root extracts the metadata (docfx globs cannot escape
  the config directory, hence the root placement); `docs/docfx.json` consumes
  `api/**/*.yml`. Each language TOC gained an "API Reference" entry.
- The docs workflow builds the Release library and regenerates the metadata on every
  change under `docs/` or `src/VCWpfControls/` and no longer tolerates build failures
  (`continue-on-error` removed). Local: `dotnet build src/VCWpfControls -c Release`,
  `docfx metadata apigen.docfx.json`, `docfx docs/docfx.json`. Site builds with
  0 warnings; `docs/api/` is generated (gitignored).


## [0.2.9] — 2026-11-07

Docking auto-hide (edge tabs + hover flyout) + clipboard-write retry fix.

### Added — docking auto-hide

- `DockPane.IsAutoHidden`: the pane collapses to a thin edge tab (rotated text on
  left/right); hovering ~250 ms or clicking flies it out as an OVERLAY (the layout is not
  pushed aside); leaving ~400 ms collapses it back; the pin button in the flyout header
  docks it into its zone again. Center panes ignore auto-hide. Auto-hide wins over
  floating (the window closes, the pane pins to its zone).
- `AutoHiddenLeftPanes`/`Top`/`Right`/`Bottom` collections expose the collapsed panes;
  `OpenAutoHideFlyout`/`CloseAutoHideFlyout` drive the flyout programmatically;
  `IsAutoHideFlyoutOpen`/`FlyoutPane` reflect its state.
- `SaveLayout`/`LoadLayout` round-trip the flag (`ah` attribute).
- Fixed a clipboard flake in the test suite: `CopyToClipboard` caught only
  ExternalException while contention raises COMException 0x800401D0 — writes now retry
  like reads, and clipboard-using test classes are serialized into one xunit collection
  (three consecutive full runs green).


## [0.2.8] — 2026-11-07

Header menu extensions (Filter..., Column Chooser, Reset layout) + numeric/date filter ranges.

### Added — filter ranges

- Numeric and date columns get **From/To** boxes in the column filter menu (above the
  value list): both bounds apply a `Between` filter, one bound applies `>=` / `<=`.
  Culture-aware parsing (reuses the clipboard converter); a parsed range takes precedence
  over the check list; reopening the menu preselects the active range. The engine already
  supported the operators — this is pure UI. String/bool columns show no boxes.
- Localized EN/RU/ZH (From/To); docs ×3; 5 STA tests (range priority validated by bug
  injection).


### Added — header menu extensions

- **Filter...** entry in the header context menu — opens the column's value-list filter
  menu (enabled when `CanUserFilter` is on).
- **Column chooser...** dialog (`OpenColumnChooser()`): visibility checkboxes in the
  current column order plus Move up/Move down reordering; reachable from the Columns
  submenu.
- **Reset layout** (`ResetLayout()`): restores the layout captured once after column
  generation settles — the column order, every width, visibility and the frozen column
  count.
- Localized EN/RU/ZH (7 new strings); docs ×3 updated.


## [0.2.7] — 2026-11-07

Cell selection (`SelectionUnit=CellOrRow`): rectangular ranges, bounding-box TSV copy.

### Added — cell selection (SelectedCells)

- `GridControl.SelectionUnit` (`Row` default / `CellOrRow`): clicks select cells; Shift+click
  selects the rectangle to the anchor, Ctrl+click toggles a cell, Ctrl+Shift+click adds a
  rectangle, Shift+arrows extend from the anchor, `SelectAll` selects every cell.
- `SelectedCells` (view order), `SelectCellRange`/`AddCellRange`/`UnselectAllCells`,
  `IsCellSelected`, `CellSelectionChanged`; selected spans painted with the theme
  selection background (row panels' OnRender, under the cell content).
- Ranges are stored item-based and survive sorting/filtering (always covering both corner
  cells); ranges of removed rows are pruned with the row selection.
- `Ctrl+C` in cell mode copies the bounding box of the ranges as TSV with empty holes
  (Excel semantics) — pairs with the v0.2.6 clipboard paste.
- Demo: Selection page toggle with a live selected-cells counter; docs ×3.


## [0.2.6] — 2026-11-05

Add-row (`CanUserAddRows`) + clipboard paste, column-frame alignment fixes, narrow-viewport floor, TreeListView indent.

### Added — add-row and clipboard paste

- `GridControl.CanUserAddRows` — an empty add-row placeholder at the end of the view
  (Excel/DataGrid style). Editing a cell in it raises `AddingNewItem` (supply or cancel),
  falls back to `NewItemFactory` or a parameterless instance of the item type, adds the
  item to a writable `IList` source and continues the edit in the new row; Esc on that
  first edit removes the item again. The placeholder lives in a new `GridItemsView`
  wrapper around the engine view — inside the engine itself it would desynchronize the
  incremental index bookkeeping (ListCollectionView cross-checks).
- `GridControl.PasteFromClipboard()` / **Ctrl+V** — pastes TSV clipboard text starting at
  the current cell: pasted columns align with visual columns (read-only cells dropped, not
  shifted), culture-aware conversion to property types (UI-free `GridClipboard` parser,
  unit-tested), extra rows/columns dropped, `PastingFromClipboard` cancelable event.
- Demo: the Editing page gained an add-row toggle (on by default) and a paste button;
  docs ×3 updated.

### Fixed — TreeListView

- The node text of the first column rendered straight under the expander toggle and the node
  check box (they draw on top of the cells panel, and the indent was `level*16` only — no
  space reserved for them; level-0 rows got no indent at all). The indent now clears the
  toggle (`16 + gap`) and the check box (`+16 + gap` when `ShowNodeCheckBoxes`) — the check
  box no longer climbs onto the node text. Regression-tested per level via element
  positions (fails with the old indent).

### Fixed — narrow viewport

- In a viewport too narrow for the columns, star columns collapsed into ~10px slivers (the
  internal star floor was the only limit). `GridColumnBase.MinWidth` now defaults to **20**
  (DataGrid convention): every column keeps a readable minimum and the horizontal scrollbar
  takes the overflow. Explicit `MinWidth`/`Width` values are unaffected (your own `MinWidth`
  still wins; an explicit `Width` below 20 is clamped up, exactly like the built-in DataGrid).

### Fixed — cell columns now truly align with headers (root cause of "values climb into the neighbor column")

- The row template's `PART_Cells` carried `Margin="8,0,8,0"` while the headers presenter has
  no margin: EVERY cell column sat 8px right of its header — with content-sized Auto columns
  each value ended past the next column's border. The margin is gone; cell padding now comes
  from the generated display elements (`8,0,6,0`; right-aligned numerics `6,0,8,0`) and the
  editing text boxes match, so text no longer jumps when editing starts.
- Every cell (and header) element is hard-clipped to its own slot — no content can render
  into a neighboring column whatever its template measures (unbounded panels, explicit
  widths). The TreeListView indent keeps its deliberate overflow.
- `GridCellAutomationPeer.GetBoundingRectangleCore` mixed physical screen pixels with DIP
  slot coordinates — cell bounds were wrong at any DPI above 100% (and took the slot width
  unscaled). Now derived from the realized cell element via `PointToScreen`, with a
  DPI-scaled fallback.
- Regression tests: cells-panel/headers-presenter origin equality (fails with the old
  8px margin), per-cell clip presence, and a pixel check that the separator margin zone
  stays clean.

### Fixed — frozen-zone clipping

- With `FrozenColumnCount > 0` and horizontal scrolling, scrollable cells (and headers, and
  the current-cell focus border) rendered on top of the frozen columns: both panels are a
  single clip region, so content shifted by the offset slid under the frozen zone. Scrollable
  children are now clipped to the part of their slot right of the frozen boundary (and to
  their own right edge, so text cannot bleed into the next column either). Pixel regression
  tests render a scrolled frozen grid and assert no dark pixels inside the frozen territory
  (verified to fail with the clip disabled).

## [0.2.5] — 2026-11-05

Column separators + deterministic async pipeline hardening.

### Added — column separators

- `GridControl.ShowColumnSeparators` — vertical lines between the columns in every row
  (off by default); `ColumnSeparatorBrush` overrides the color (defaults to `GridLinesBrush`);
  respects frozen columns and horizontal scrolling; demo toggle on the Basic page.
- Demo toolbars now use MahApps.Metro.IconPacks.Material (MIT) instead of text/emoji buttons.

### Fixed

- Async pipeline: the computed view is applied through the grid's `Dispatcher` explicitly
  (`ComputeAsync` + `ApplyView` split) — no more reliance on an ambient
  `SynchronizationContext`, which test hosts don't install.
- Demo: the column-separator checkbox targeted the restructured page panel instead of the
  grid, so toggling did nothing.

- `GridControl.ShowColumnSeparators` — vertical lines between the columns in every row
  (off by default); `ColumnSeparatorBrush` overrides the color (defaults to `GridLinesBrush`);
  respects frozen columns and horizontal scrolling; demo toggle on the Basic page.

### Fixed — separator alignment

- Separator lines sat ~8px right of the header column borders: the cells panel template
  margin (`8,0,8,0`) was not discounted when converting slot boundaries to panel-local
  coordinates, and the 1px pen was centered on the boundary instead of occupying the same
  pixel column as the header's inside right border. Lines now compensate the panel inset
  and center at `boundary − 0.5` — pixel-identical to the header borders, verified by a
  96-DPI offscreen-render regression test (fails with the old formula, passes with the fix).
- Separators stayed at the old column boundaries after moving a header (and would lag on
  resize and horizontal scrolling): reordering goes through `InvalidateLayout`, which only
  invalidated the row panels' measure — `OnRender` output is cached until `InvalidateVisual`,
  and the panel's `RenderSize` does not change. `InvalidateLayout` now invalidates the panels'
  visuals; the scroll path invalidates visuals when separators are on (arrange otherwise).
  Regression-tested by reordering via the drag-drop code path and asserting every line
  coincides with a header border.

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

### Fixed — async apply marshaled via the grid Dispatcher

- The async pipeline no longer relies on the ambient `SynchronizationContext` (which test
  hosts may not install as the dispatcher context — the apply then ran on a threadpool
  thread and WPF CollectionView rejected the cross-thread change). The engine exposes
  `ComputeAsync`/`ApplyView`, and `GridControl` posts the apply onto its own `Dispatcher`
  explicitly — deterministic on any host.

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
