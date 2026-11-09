---
title: Gantt chart
description: GanttView — task tree + timeline bars, dependency arrows, milestones, today line, critical path; powered by the UI-free GanttEngine.
---

# Gantt chart

The forum research called Gantt the biggest open-source gap in WPF; VCWpfControls ships it in three layers.

## The control

```xml
<gantt:GanttView Tasks="{Binding Tasks}"
                 Dependencies="{Binding Links}"
                 AutoSchedule="True"
                 ShowCriticalPath="True"
                 TimescaleUnit="Week" />
```

- **Left**: the task hierarchy in a TreeListView (Name/Start/Finish/% columns; hierarchy
  through `GanttTask.ParentId`); a splitter adjusts its width.
- **Right**: the timeline — bars with a completion fill, milestone diamonds, dependency
  arrows for FS/SS/FF/SF, a dashed today line, day/week/month zoom (`TimescaleUnit`).
- `TaskRowHeight` keeps both halves in lock-step; the timeline scrolls horizontally and
  mirrors the tree's vertical scroll.
- `AutoSchedule` runs the engine on load/change (violating successors shift); `ShowCriticalPath`
  outlines critical bars/links in the accent brush. Double-click a bar → `BarDoubleClicked`
  (on the `GanttTimelinePanel`) for editor integration.

## Non-working days

`Calendar` (a `GanttCalendar`: weekend weekdays + explicit holiday dates) switches the chart to working time:

- The timeline tints weekends/holidays under the grid.
- With `AutoSchedule`, durations and lags count in **working** days and a moved task never
  starts on a non-working day (`Fri + 1` is Monday, not Saturday). Manual dates that
  already satisfy their links are kept as they are.
- `CriticalPath` stays calendar-day based (relative floats).
- `null` (the default) keeps plain calendar-day math — nothing changes for existing charts.

```csharp
var calendar = new GanttCalendar();                 // Sat/Sun weekends by default
calendar.Holidays.Add(new DateTime(2026, 11, 20));  // a company day off
view.Calendar = calendar;
```

## Drag interactions

All edits are day-snapped and fire events after the dates land on the task:

- **Move** — drag a bar body; Start and Finish shift together (milestones are fixed).
- **Resize** — drag a bar's left/right edge; the duration changes, at least one day is kept.
- **Link** — hold **Alt** and drag from a bar's edge onto another bar: a rubber band follows
  the pointer and dropping creates a `FinishToStart` dependency (added to `Dependencies`,
  immediately re-scheduled when `AutoSchedule` is on).

```csharp
view.TaskEdited += (_, task) => Persist(task);                       // move/resize
view.LinkCreated += (_, dependency) => Persist(dependency);          // drawn link
```

Both halves refresh after an edit: the tree columns pick up the new dates and the engine
re-runs `AutoSchedule`/`CriticalPath`, so successors jump and the critical chain updates live.

## The engine (headless)

`GanttTask`/`GanttDependency` (FS/SS/FF/SF + lag/lead days), `GanttEngine.AutoSchedule`
(forward-only; `GanttCycleException` carries the cycle path), `CriticalPath` (CPM floats),
`BuildTimescale` (Day/Week/Month, culture first-day). Unit-tested without STA.
