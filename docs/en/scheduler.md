---
title: Scheduler
description: SchedulerView — day/work-week/week/month calendar views over recurring appointments, with a time ruler, overlap lanes, an all-day band and a month grid; powered by the UI-free SchedulerEngine.
---

# Scheduler

The scheduler (Outlook-style calendar) ships in two layers: a UI-free engine (stage 1)
and the control (stage 2). Drag/resize interactions are on the roadmap (stage 3).

## The control

```xml
<scheduler:SchedulerView Appointments="{Binding Appointments}"
                          ViewType="WorkWeek"
                          DayStartHour="8"
                          DayEndHour="20"
                          OccurrenceDoubleClicked="OpenEditor" />
```

- **Day / WorkWeek / Week / Month** views (`ViewType`); `AnchorDate` picks the shown
  day/week/month (the default anchors to today).
- **Day-family views**: a pinned time ruler and day headers, one column per day.
  `DayStartHour`/`DayEndHour` window the day (default 0–24); blocks outside clip away.
  Overlapping occurrences split the column into side-by-side **lanes** (interval-graph
  clustering per day); occurrences crossing midnight draw one clipped segment per day.
- **All-day band**: `IsAllDay` appointments render as chips above the hour grid, one band
  row per overlap lane (max 3, extra lanes clip away); a chip spans each of its days.
- **Month view**: the six-week grid (42 cells, culture week start), weekday headers, day
  numbers with neighbor-month dimming, today highlighted, up to 2 chips per cell plus a
  "+N more" overflow row.
- Today's column/month cell is tinted and a dashed **now line** crosses it.
- Click selects (`SelectedOccurrence`, two-way bindable); double-click raises
  `OccurrenceDoubleClicked` for editor integration.

## Interactions

Day/work-week/week views are editable (the month view is read-only for now):

- **Move** — drag a block: time shifts vertically (snapped), days shift horizontally.
  All-day band chips drag by whole days only.
- **Resize** — drag a block's top/bottom edge; at least one snap of duration is kept.
- **Create** — drag out a range on empty space: an appointment is added to `Appointments`
  with the localized "New appointment" subject; `AppointmentCreated` fires so an editor
  can open (double-click is the natural follow-up).
- Everything snaps to `SnapMinutes` (default 15; set 1 for minute precision).
- Recurring series are **not** drag-editable (the recurrence rule owns the times) —
  drags on series occurrences are ignored; edit the rule itself instead.
- **Keyboard** — arrows walk the selection through the visible occurrences by start time
  (no wrap at the ends), `Enter` activates the selection (the double-click channel),
  `Delete` removes the appointment from `Appointments`, `T` anchors to today, `Esc`
  clears the selection. Clicking the view focuses it, so the keys work right after
  pointing; the focus ring follows the shared `FocusVisualKey` token.

```csharp
view.AppointmentEdited += (_, appointment) => Persist(appointment);   // drag/resize
view.AppointmentCreated += (_, appointment) => OpenEditor(appointment);
view.AppointmentDeleted += (_, appointment) => PersistRemoval(appointment); // Delete key
```

## Accessibility and hidden-content cues

The control exposes a full automation surface: every visible occurrence is a DataItem
child of the calendar peer (name = "Subject, start – end") supporting SelectionItem
(select = `SelectedOccurrence`) and Invoke (the `OccurrenceDoubleClicked` channel) —
screen readers can list, select and activate appointments, and selection changes raise
UIA events. Give the control an `AutomationProperties.Name` ("Appointment calendar" in
the demo) so it announces meaningfully.

When `DayStartHour`/`DayEndHour` pin a narrower window than the data, each day column
shows a small "▲ N / ▼ N" cue at its window edges counting occurrences clipped above
and below — a pinned window never silently hides events. Keyboard selection
automatically scrolls the selected occurrence into view.

## Navigation

```csharp
view.Navigate(1);     // forward by one view step: day / week / month
view.Navigate(-2);    // two steps back
view.GoToToday();     // anchor back to today
view.AnchorDate = new DateTime(2026, 11, 9);  // or bind it
```

`Appointments` mutations (`ObservableCollection` add/remove/replace) refresh the views.

## The appointments

Plain appointments are single bands. Recurring ones carry a `SchedulerRecurrence` rule —
daily/weekly/monthly/yearly with `Interval`, `Count`/`Until` limits (RFC 5545 semantics:
counted from the series start, deleted occurrences consume their slot) and per-date
`Exceptions`:

```csharp
var standup = new SchedulerAppointment("s", "Daily standup",
    today.AddHours(9.25), today.AddHours(9.5))
{
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Daily,
        Count = 50,
    },
};

var oneOnOne = new SchedulerAppointment("1", "1:1 with Alice",
    monday.AddHours(11), monday.AddHours(12))
{
    ResourceId = "alice",
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Weekly,
        DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday },
    },
};
```

## The engine (headless)

`SchedulerEngine` is testable without STA: `Expand` materializes the occurrences of a range
(resource-filterable), `LayoutOverlaps` assigns the side-by-side lanes, `FindConflicts`
returns double bookings per resource, `ViewRange`/`MonthGrid` compute the view periods.
See [the Gantt engine](gantt.md) for the sibling pattern.
