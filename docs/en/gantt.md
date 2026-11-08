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

## The engine (headless)

`GanttTask`/`GanttDependency` (FS/SS/FF/SF + lag/lead days), `GanttEngine.AutoSchedule`
(forward-only; `GanttCycleException` carries the cycle path), `CriticalPath` (CPM floats),
`BuildTimescale` (Day/Week/Month, culture first-day). Unit-tested without STA.
