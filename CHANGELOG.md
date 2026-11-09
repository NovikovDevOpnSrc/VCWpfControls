# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed — dark theme: invisible settings labels and white alternating rows (user report)

- New palette token `GridResources.AlternatingRowBackgroundKey` (#FAFAFA light / #2F2F2F
  dark); the demo's Basic/Selection pages used a hardcoded near-white alternation that
  stayed white in dark theme (white rows + white text). All hardcoded demo colors audited
  away: info callouts → SelectionBackground, the PropertyGrid splitter → HeaderBorder.
- The settings flyout labels/radio texts now follow TextBrush (default black-on-dark made
  them invisible).

### Added — demo application settings (theme, accent, language)

Settings live in a FLYOUT under the navigation (the gear button, VS Code/Fiori pattern),
not a page. New: the Light/Dark theme now applies APPLICATION-WIDE (the shell sidebar
follows the palette too — DynamicResource brushes), four accent presets override the
brand tokens live (Default/Purple/Green/Orange; the Fiori qualitative pattern through the
GridResources keys), and the library language switches en-US/ru-RU/zh-CN observably
(localized strings: menus, hints, wizard buttons). Settings persist to
`%AppData%/VCWpfControls.Demo/settings.json` and restore on the next launch (culture
applied at startup, before the UI). The Analytics page's per-grid theme toggle is removed
in favor of the single global source. Fixed: the nav ListBox's default white background
and system selection painted over the dark sidebar — it is now transparent with
theme-aware item states (hover/selected from the palette); the whole shell follows the
theme (gear button, content surface, header texts). E2E: theme/accent/language churn through the real
shell with an observable Russian «Далее» assertion (serialized "UiSerial" collection so
language persistence cannot race the wizard stress test; always restores defaults).

### Changed — default themes aligned with Fluent 2

The Light/Dark palettes now follow the Fluent 2 foundations: brand `#0F6CBD`/`#479EF5`,
the tinted neutral ramp, brand-tinted selection, neutral hovers. New foundation tokens on
`GridResources`: `FontFamilyKey` (Segoe UI Variable with fallback, applied to the grid),
the 12/14/16/20 type ramp (`FontSizeCaption/Body/Subtitle/Title`), and the 2px rounded
accent `FocusVisualKey` focus ring (grid + column headers). Corner radii follow the
Fluent 4/8 convention (blocks/chips 4, cards 8). Any token stays overridable through the
usual keys; chart internals keep deliberately denser sizes. 4 STA tests pin the tokens
and both palettes (accent injection-validated). One pixel test's darkness detector
learned to exclude accent-colored glyphs (the new brand blue is darker than the old one
and the sort arrow started reading as "text bleed"; BGRA channel order honored).

### Added — Gantt UX-guideline compliance round (Oracle FusionFX / SAP Fiori / Smartsheet)

Audited the chart against the three guidelines and closed every gap (the four misses plus
the four user-approved debatables):

- **Two-level time axis**: a major row (months, years at Month zoom —
  `GanttEngine.BuildMajorTimescale`) above the minor zoom-unit row; required by Oracle and
  Fiori.
- **Oracle summary bars**: parents render as thin bracket bands carrying the
  duration-weighted completion of ALL descendants (`GanttEngine.SummaryCompletion`);
  milestones weigh one day.
- **Legend** strip pinned to the timeline bottom (`ShowLegend`, default on): task /
  summary / milestone / critical / baseline / non-working samples, localized ×3
  (6 new strings).
- **Bar labels** after the bars (`ShowBarLabels`, default OFF per Oracle), clipped.
- **Discrete wheel zoom**: the wheel steps Day ↔ Week ↔ Month through the view's
  `TimescaleUnit` (bindings stay in sync).
- **Drag-to-pan** empty chart space scrolls the timeline; **cursor feedback**
  (SizeWE/SizeAll/Hand) over edges/bodies/empty rows.
- **`GanttTask.BarBrush`**: optional per-task color (the Fiori qualitative palette
  pattern); null = theme.

5 Core + 8 STA tests; the major-axis and the weighted-rollup regressions are
injection-validated. Docs gantt.md ×3 gained a compliance section; demo toggles +
a category brush.

### Fixed — Wizard gate ignored programmatic/UIA changes (found by the UI stress round)

Two broken links kept the Next button disabled forever when CanLeave changed without USER
INPUT (bindings, the UIA Toggle pattern — the same class as the earlier funnel a11y bug):
(1) page propagation relied on the page's logical `Parent` being the Wizard — null for
code-built pages; the Wizard now subscribes to every page's CanLeave through a dependency
property descriptor when pages enter/leave the collection. (2) the buttons carry commands
whose CanExecuteChanged rides `CommandManager.RequerySuggested` (input-driven only), so
`IsEnabledCore` kept the stale disabled verdict and overrode the direct IsEnabled set —
`RefreshNavigation` now ends with `CommandManager.InvalidateRequerySuggested()`.
STA regression (programmatic gate enablement) + UIA e2e (gate toggle → Next → Finish),
injection-validated (requery skipped → red).

### Added — UI stress round (pattern-only e2e through the real shell)

`UiStressTests`: sorting through header Invoke (ascending → descending → cycling every
column), cell validation through the Value pattern (valid input persists, garbage is
rejected keeping the previous value), the full Wizard gate/finish flow through Toggle and
Invoke, three rapid no-wait navigation sweeps and Scheduler/Gantt view-switch churn loops.

### Added — diverse testing campaign (adversarial, invariant, culture matrix, churn)

- `EngineEdgeCaseTests` (Core): degenerate/hostile inputs for Scheduler (inverted ranges,
  zero/negative recurrence intervals, Count 0, past Until, far-future dates), Gantt
  (unknown link ids, self-link cycles, empty charts, month timescale across a year
  boundary), Pivot (empty source, fully filtered-out fields), Condition parser garbage
  and clipboard-unconvertible inputs.
- `EngineInvariantTests` (Core): seeded randomized property tests — AutoSchedule on 50
  random acyclic graphs (every link satisfied afterwards, durations preserved, no
  inversions), LayoutOverlaps lane consistency vs an INDEPENDENT union-find clustering
  (exactly-once placement, lane bounds, overlapping pairs never share a lane, no wasted
  lanes per cluster), Pivot totals equal the source sum three ways, condition
  Format→Parse fixed-point after normalization.
- `CultureMatrixTests` (Core): the same weekly expansion yields byte-identical dates
  under ru-RU/en-US/zh-CN/de-DE/ar-SA/Invariant; clipboard numeric/date round-trips per
  culture; timescale captions non-empty everywhere; Week view always spans 7 days.
- `StressChurnTests` (STA): a 300-mutation storm under active sort+grouping converges to
  the exact live item set; a 20-wave sort/filter/group settings storm keeps the grid
  alive with a non-empty view.
- **Fixed (found by the campaign)**: `SchedulerEngine.OccurrencesOf` now guards the model
  contract — appointments with `End <= Start` (zero/negative length) contribute NO
  occurrences (previously they leaked garbage occurrences into Expand/LayoutOverlaps/
  FindConflicts). The regression test was observed red before the guard.

## [0.5.0] — 2026-11-09

The Scheduler/Calendar flagship (UI-free engine + control + interactions), Gantt
non-working-day scheduling, baselines, inline tree editing and timeline virtualization,
`GroupHeaderTemplate`, per-cell Value/GridItem UIA patterns, engine benchmarks, plus the
review fixes (tree/timeline scroll sync) and accessibility names for every unnamed button.

### Fixed — accessibility names for unnamed buttons (found by the demo tour)

Buttons whose content is a panel or a glyph read as UNNAMED to screen readers and UIA
clients. Named: the demo's icon toolbar buttons (Analytics/Tree list/Docking, 14
controls), the column-header filter funnels (`PART_FilterButton`, "Filter: <column>"),
the NumericUpDown spin buttons (new localized strings NumericIncrease/NumericDecrease ×3)
and the docking zone tab toggles + their close buttons ("Welcome"/"Close: Welcome").
New FlaUI regression: every page's interactive buttons must carry a name (scrollbar
system chrome excluded) — validated by injection (funnel name removed → red).

### Added — GroupHeaderTemplate, cell UIA patterns, engine benchmarks

- `GridControl.GroupHeaderTemplate` (DataTemplate): replaces the default caption on every
  group row; the context is a `GridGroupHeaderInfo` (Key / ItemCount / Level / ColumnName /
  Caption). Runtime template changes re-apply to rendered group rows. `PART_GroupHeaderContent`.
- Cell UIA polish: `GridCellAutomationPeer` now exposes the **Value pattern** (reads the
  formatted cell value; writes go through the paste conversion path `TrySetCellText`,
  read-only columns reject with the UIA contract exception) and the **GridItem pattern**
  (row/column coordinates, spans, containing grid) — assistive tech and UIA clients can
  read and edit cells directly.
- `EngineBenchmarks` (BenchmarkDotNet): Pivot 100k×5×8 with totals, an And/Or condition
  tree over 100k items, Gantt auto-schedule/critical path on a 1000-task chain, Scheduler
  100 daily series × 3 years expansion and a dense 1000-occurrence lane layout. Results in
  `benchmarks/RESULTS.md`.
- Docs: sorting-filtering-grouping ×3 (custom group headers). UIA e2e: the Basic page's
  cell Value pattern reads and writes through the real shell.
- 5 STA tests; template-wiring and value-write regressions injection-validated.

### Fixed — Gantt tree/timeline scroll sync (found in review)

The tree→timeline vertical mirror was dead on arrival: the hook searched the tree's
ScrollViewer at the owner's `OnApplyTemplate`, before the tree's own template (with
that viewer) applies — so nothing subscribed. Additionally the two halves scroll in
different units (the tree in items, the timeline in pixels), so both directions passed
raw offsets around (clamping to the end / never moving). Now the hook attaches deferred
at Loaded priority and both directions convert through the row height. Regression test
covers both directions on a 300-task chart (injection-validated).

### Added — Gantt baselines, inline editing, timeline virtualization

- **Baselines**: `GanttTask.BaselineStart`/`BaselineFinish` render as a thin gray band
  under the actual bar (`GanttView.ShowBaseline`, default on); the bar tooltip shows the
  baseline range and the start variance in days.
- **Inline editing**: the tree columns write THROUGH to the task (`GanttTaskNode`
  properties got setters); committing a Start/Finish/% edit re-runs auto-scheduling and
  the critical path and raises `TaskEdited` — the same semantics as a bar drag.
- **Timeline virtualization** (also fixes a latent extent bug): the timeline panel now
  reports its true size (`MeasureOverride` = header + rows × row height) so the vertical
  scroll reaches every row, the timeline ScrollViewer shows its scrollbar (Auto) and
  stays in lock-step with the tree, and rendering culls to the visible viewport band —
  300 tasks draw ~17 rows' worth of bars instead of all of them.
- Demo "Gantt": baseline data on build/test (slipped vs plan) + a "Baseline" toggle.
  Docs gantt.md ×3. 7 STA tests; regressions injection-validated (baseline reading the
  actual dates, edit-wiring removal, culling bypass).

### Added — Gantt non-working days

- `GanttCalendar` (UI-free): weekend weekdays (Sat/Sun by default) + explicit holiday
  dates; `IsWorkingDay`, `AddWorkingDays` (positive/negative, throws on a calendar with
  no working days), `NextWorkingDay`.
- Calendar-aware `GanttEngine.AutoSchedule(tasks, links, calendar)` and `EarliestStart`
  overload: durations and lags count in WORKING days and a moved task never starts on a
  non-working day. Manual satisfying dates stay untouched; `CriticalPath` remains
  calendar-day based; a null calendar keeps the historic math (the 2-argument overload
  is unchanged).
- `GanttView.Calendar` DP: the timeline tints non-working days under the grid and
  auto-scheduling (when on) switches to working-day math; toggling re-runs both.
- Demo "Gantt": a "Skip weekends" toggle (weekends + a sample holiday). Docs gantt.md ×3.
- 12 Core + 4 STA tests; regressions injection-validated (engine snap bypass → red,
  view wiring to null → red; the snap test needed a manual-Saturday-finish predecessor —
  lag-based cases alone never exercise the snap).

### Added — Scheduler stage 3 (interactions)

- Day/work-week/week views are editable (month stays read-only for now): drag a block to
  move (vertical = snapped time, horizontal = whole days; all-day band chips by days
  only), drag its top/bottom edge to resize (at least one snap of duration is kept), and
  drag out a range on empty space to create — a localized "New appointment" is added to
  `Appointments` and `AppointmentCreated` fires. Everything snaps to the new
  `SnapMinutes` DP (default 15). Live drag previews ghost the drop position; cursors
  (SizeNS/SizeAll/Cross) hint the zones. `AppointmentEdited` fires after a commit and the
  views rebuild. Recurring series are not drag-editable (the rule owns the times).
- 9 STA tests (`SchedulerInteractionsTests`), incl. one through the real
  down/move/up mouse path; the recurring guard and the minimum-duration clamp are
  injection-validated regressions. New `SchedulerNewAppointment` string ×3.
- Demo "Scheduler" page reports moves/resizes/creates in the action log.

### Added — Scheduler stage 2 (the control)

- `SchedulerView`: Day / WorkWeek / Week / Month views over the stage-1 engine
  (`ViewType`, `AnchorDate` — unset anchors to today, `Navigate`/`GoToToday`).
  Day-family views: pinned time ruler + day headers, one column per day, the
  `DayStartHour`/`DayEndHour` visible window (blocks outside clip away), side-by-side
  lanes for overlapping occurrences, an all-day band (chips span their days, max 3 rows),
  overnight occurrences as per-day clipped segments, today tint and a dashed now line.
  Month view: the 42-day grid with weekday headers, neighbor-month dimming, today
  highlight and 2 chips per cell + a "+N more" overflow row.
  Click selects (`SelectedOccurrence`, two-way); double-click raises
  `OccurrenceDoubleClicked` for editor integration. `Appointments` mutations refresh
  the views.
- Demo page "Scheduler" (recurring sample data anchored to today: standup, 1:1, planning,
  retro, a 3-day all-day conference, overlapping reviews and an overnight on-call).
- 12 STA tests (`SchedulerViewTests`); two regression injections validated (midnight
  segment mapping, lane split). UIA e2e: view switching + navigation on the demo page.

### Added — Scheduler stage 1 (headless engine)

- `VCWpfControls.Scheduler` engine + model (UI-free, no STA): `SchedulerAppointment`
  (all-day, resource, location), `SchedulerRecurrence` — an RRULE subset: Daily / Weekly
  (weekday set) / Monthly (day-of-month, clamped into short months — the 31st lands on the
  last day of February) / Yearly (month + day, Feb 29 → Feb 28 in non-leap years), interval
  (every N units), `Count`/`Until` limits with RFC-5545 semantics (counted from the SERIES
  start, not the queried range; deleted occurrences still consume their slot) and per-date
  `Exceptions`.
- `SchedulerEngine`: `Expand`/`OccurrencesOf` (half-open range overlap — touching a
  boundary is not inside), `LayoutOverlaps` (interval-graph lane clustering for day/week
  views: transitively overlapping occurrences share a cluster; touching intervals share a
  lane), `FindConflicts` (per-resource double bookings; touching ≠ conflict; no resource =
  never conflicts), `ViewRange` (Day / WorkWeek Mon-Fri / Week by culture / Month) and
  `MonthGrid` (42-day six-week grid aligned to the culture week start).
- 28 Core tests. The weekly walk's day offset is computed from the CULTURE week start, not
  the enum value — the naive `(int)day` produced Saturdays for Friday selections under
  Monday-first cultures (caught red by the weekday test, fixed to green).

### Added / Fixed — fit options (user request)

- New `GridControl.FillColumnsToViewport()` + header-menu "Fit all columns (fill width)":
  scales the visible columns proportionally to their current widths until they exactly
  fill the control (water-filling; `MinWidth` pinned and redistributed; minimums beyond
  the width keep the scrollbar). String `BestFitAllFill` x3.
- Fixed BestFit hiding short headers ("ID"): `MeasureHeaderWidth` now reserves the
  template's real furniture (margins 15 + always-laid-out sort slot 14 + funnel 24 when
  filterable = up to 53px) instead of a flat 34px — the text area of short headers was
  clipped to zero while the cell text stayed visible. Regression test asserts the
  header ContentPresenter keeps a visible width (injection-validated); the stale
  `< 90px` expectation in the content-fit test was updated to the header-visible bound.

### Changed — header menu "Filter..." now filters BY CONDITION (user request)

- The right-click "Filter..." entry opens the condition builder (`ConditionBuilder`)
  PRESEEDED with one Equals condition on the clicked column — filter by condition instead
  of the value-check multitude. The value list moved to a separate "Filter by values..."
  entry, and the old duplicate "Filter......" text (two near-identical entries —
  `FilterEllipsis + "..."` next to `FilterEllipsis`) was removed. `OpenConditionDialog`
  takes an optional column; tests drive it through `ShowConditionDialogModally = false`
  (modal/non-modal share one path). New string `FilterByValues` x3. Preseed
  injection-validated.

### Fixed — pivot automation names + clipboard copy (found by the new e2e round)

- `PivotTextColumn` overrides the new virtual `GridColumnBase.GetCellValue` to resolve its
  display `Path` (`Values[i].Caption` / `Cells[c]` / `Totals[m]`). The base read
  `FieldName`, which on pivot columns names a whole collection — row/cell automation names
  (and TSV copy from a pivot) carried `System.Object[]` instead of the cell text. Caught by
  the first run of the new Pivot e2e; regression test injection-validated.

### Added — FlaUI e2e for the post-v0.3 pages

- `PostV3PagesUiTests` (UIA-pattern-only): Pivot (totals column + grand row through the
  Table/DataItem patterns — this is the test that caught the GetCellValue bug), Gantt
  (tree headers/rows, critical-path toggle, month zoom, Reset — the full-rebuild path in
  the real shell), Conditions (builder add/remove condition row, empty-condition Apply
  keeps all rows), Charts & Ribbon (page renders with the third-party ribbon + our grid;
  Fluent exposes no UIA children — documented in the test, out of scope).
- `DemoSmokeTests` navigation sweep now asserts page automation ids for Wizard, Pivot,
  Gantt, Conditions and Charts & Ribbon too; the Gantt sample page gained its id.

## [0.4.0] — 2026-11-08

Gantt chart: headless scheduling engine, GanttView (task tree + timeline), drag interactions (move/resize/link with live re-scheduling).

### Added — Gantt control (stage 3: drag interactions)

- Timeline drags, all day-snapped: **move** a bar body (Start/Finish together, milestones
  fixed), **resize** via the edges (at least one day kept, clamp guarded), **link** by
  Alt+dragging from an edge onto another bar — a rubber band follows the pointer and the
  drop creates a `FinishToStart` dependency.
- `GanttTimelinePanel.TaskEdited` / `LinkCreated` events; `GanttView` surfaces them, adds
  drawn links to `Dependencies` and re-runs `AutoSchedule` + `CriticalPath` after every
  edit (a full rebuild — the tree columns read dates through one-way bindings without
  change notifications, so only fresh wrappers show new dates).
- Demo page "Gantt" (release-plan sample, toggles, zoom, reset, action log). 9 STA tests
  (hook semantics incl. clamps validated by bug injection; view wiring: edit → successor
  reschedule, drawn link → Dependencies + cascade). Docs x3 updated.


### Added — Gantt control (stage 2: rendering)

- `GanttView`: the task hierarchy in a TreeListView (left, splitter-resizable) plus the
  `GanttTimelinePanel` timeline (right) — bars with a completion fill, milestone diamonds,
  dependency arrows for FS/SS/FF/SF (critical links accented), a dashed today line, and
  day/week/month zoom. `TaskRowHeight` keeps both halves in lock-step; the timeline
  scrolls horizontally and mirrors the tree's vertical scroll. `AutoSchedule` runs the
  engine on load/change; `ShowCriticalPath` outlines zero-float bars/links; double-click
  on a bar raises `BarDoubleClicked`.
- `GanttTaskNode` wraps tasks for the tree grid (Name/Start/Finish/% columns); the row
  order maps the FLATTENED tree render order — the tree's Items is already flat, so the
  first version's recursive collection duplicated children (caught by an order test).
- Layout note: a TreeListView in an Auto grid column is measured with infinite width and
  its headers presenter returns PositiveInfinity — the template pins the tree width.
- Docs x3 (new "Gantt" page). 6 STA tests (tree+timeline from flat tasks, parent-first
  order, bars/milestones rendered, auto-schedule shift, critical-path toggle, zoom;
  offscreen pixel check green except the disconnected-RDP environment, CI verifies).


### Added — Gantt engine (stage 1: headless)

- UI-free scheduling core for a Gantt chart: `GanttTask` (id/name/start/finish/completion/
  milestone/parent — flat list keyed by id), `GanttDependency` with all four precedence
  relations (FinishToStart/StartToStart/FinishToFinish/StartToFinish) plus lag days
  (negative = lead/overlap).
- `GanttEngine.AutoSchedule`: forward-only relaxation — violating successors shift to the
  earliest satisfying date (duration captured before the move; the first version recomputed
  it after moving the start and collapsed cascaded tasks to zero days — caught by the
  chain test). `GanttCycleException` carries the cycle path (self-references included;
  links with unknown ids are ignored).
- `GanttEngine.CriticalPath`: CPM floats over a Kahn topological order (early/late passes);
  critical = zero total float; FF/SF constrain the successor's finish keeping its duration.
  With no links only the project-finish task is critical.
- `GanttEngine.BuildTimescale` (Day/Week/Month; weeks align to the culture's first day —
  invariant starts Sunday, de-DE Monday) + `AlignToWeek`. 20 Core tests; the critical-float
  comparison is injection-validated.


## [0.3.1] — 2026-11-08

Condition expression editor (engine → ConditionBuilder control → grid/pivot integration), third-party integration recipes (LiveCharts2 + Fluent.Ribbon), nested docking splitter groups. The v0.3 roadmap is complete.

### Added — docking: nested splitter groups

- `DockPane.SplitGroup` (int, default 0): panes of one zone sharing a group tab together;
  each distinct group gets its own host stacked INSIDE the zone with an independent
  GridSplitter (left/right zones stack vertically, top/bottom horizontally; center ignores
  groups). `DockZoneGroup` hosts the stack — the template's zone host becomes its primary
  entry, so plain layouts are unchanged.
- Zone visibility now considers nested hosts (a zone stays visible when only a split-group
  host has panes); hiding the last pane of a group prunes its host and splitter;
  `SplitGroup` round-trips through SaveLayout/LoadLayout (`sg` attribute).
- 4 STA tests (grouping/splitter presence, prune-on-close, zone visibility via nested
  hosts, XML round-trip); routing validated by injection. This closes the last v0.3
  roadmap item.


### Added — third-party integration recipes (LiveCharts2, Fluent.Ribbon)

- Demo page "Charts & Ribbon" (verified, runs): a Fluent.Ribbon toolbar (grouping
  commands) above the grid and a LiveCharts2 CartesianChart charting the salary totals
  per department. Demo-only packages (CPM): LiveChartsCore.SkiaSharpView.WPF 2.0.5,
  Fluent.Ribbon 11.0.2 — both MIT; the LIBRARY stays dependency-free.
- Docs ×3 gained an "Integration" page with the verified recipes: NU1701 suppression
  needed for LiveCharts2's SkiaSharp/OpenTK fallback assets; Fluent's namespace is a
  plain `clr-namespace:Fluent;assembly=Fluent` (the urn:/https: forms do not resolve);
  charting grid aggregates and pivot totals.


### Added — condition editor stage C: grid and pivot integration

- `GridControl.CustomFilterPredicate` (`Predicate<object?>`): ANDed with the engine
  filters; the RAW source is remembered, so a filter that empties the view keeps the item
  type resolvable (the condition dialog falls back to it) and the filter lifts cleanly.
- Header menu gained a **condition entry** (under the sorting commands) opening a modal
  `ConditionBuilder` over the item type — OK applies the tree through
  `ConditionEvaluator`, Clear removes the filter, Cancel keeps it.
- `PivotGrid.Condition`: a condition tree filtering the SOURCE rows before bucketing
  (composed with `FieldFilter`); null disables.
- Demo page "Conditions" (builder + text tab + Apply); docs ×3. Tests: 4 STA — the
  predicate path (combined with engine filters, lifting), the dialog OK/Clear/Cancel
  flows (via a non-modal test hook; ShowDialog blocks an STA test), and the pivot bridge
  (condition filters rows; totals follow; null restores).


### Added — condition editor stage B: the ConditionBuilder control

- `ConditionBuilder`: visual editing of condition trees — recursive And (all)/Or (any)
  groups, per-row field + operator combos (operators filtered by the field type) and
  typed value editors (TextBox/CheckBox/DatePicker/enum combo; `between` gets two,
  `in` a comma list); add condition/group and remove buttons; localized EN/RU/ZH.
- Text tab: edits the `ConditionParser` language; parse errors report their position
  (`ErrorText`) and keep the previous tree; valid text replaces it. Live `PreviewText`
  and `ConditionChanged` on every edit. Values parse through the shared clipboard
  converter (culture-aware), empty text means null.
- Hardened against WPF re-entrancy: row refresh guards the operator-combo rebuild and
  the mode radio buttons no-op when the mode is already set.
- 11 STA tests (auto group, add/remove condition/group, typed editors per field kind,
  between dual editors, operator set switch on field change, And/Or switch, text-mode
  error keeps tree, valid text replaces tree, preview). Stage C (grid/pivot
  integration) is next.


### Added — condition expression editor, stage A (UI-free core)

- `ConditionGroup`/`ConditionExpression`: a filter tree with nested And/Or groups;
  `ConditionSchema` (+ `FromType` reflection over booleans/enums/dates/numbers/strings,
  `[Display]` names, enum suggestions) describes the filterable fields.
- `ConditionEvaluator.Matches/MatchesAll/Where`: evaluation DELEGATES to
  `GridFilterDescription.Matches` — the exact semantics the grid view uses, so a condition
  tree and the grid can never disagree. Empty groups: And=true, Or=false.
- `ConditionParser.Parse/Format`: DevExpress-style text language with full round-trip —
  `[Field] Op value` (all 16 operators; Between takes a comma pair, In a comma list),
  `And` binds tighter than `Or`, parentheses nest, literals: 'strings' ('' escape),
  numbers (int→long→decimal), true/false/null, `#yyyy-MM-dd#` dates; `ConditionParseException`
  carries the error position for editor underlining. Zero new dependencies.
- 21 Core tests (schema, evaluator incl. And/Or discrimination, precedence, round-trips,
  literals, 8 error cases); And-semantics injection-validated. Stage B (the control) is next.


## [0.3.0] — 2026-11-08

The forum-driven milestone: PropertyGrid TypeConverters/commit modes, TreeList per-level summaries, AutoComplete editor, Wizard, and the flagship Pivot Grid (engine → control → drill-down/filters/CSV). Plus reliability fixes across PropertyGrid.

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
