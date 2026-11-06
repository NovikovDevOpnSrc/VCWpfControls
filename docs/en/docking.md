---
title: Docking
description: Dockable panes with splitters, close/show and XML layout persistence.
---

# Docking

`VCWpfControls.Docking.DockingManager` hosts `DockPane` items in five zones: left, top, right
and bottom (resizable via splitters, collapsed when empty) and a tabbed center.

## Quick start

```xml
<dock:DockingManager xmlns:dock="clr-namespace:VCWpfControls.Docking;assembly=VCWpfControls">
    <dock:DockPane Header="Editor" Zone="Center">...</dock:DockPane>
    <dock:DockPane Header="Properties" Zone="Right" DockWidth="300">...</dock:DockPane>
    <dock:DockPane Header="Output" Zone="Bottom" DockHeight="140">...</dock:DockPane>
    <dock:DockPane Header="Toolbox" Zone="Left" IsClosable="False">...</dock:DockPane>
</dock:DockingManager>
```

## Panes

- `Zone` (`Left`/`Top`/`Right`/`Bottom`/`Center`) — change it any time; the pane moves.
- `DockWidth`/`DockHeight` size the side zones; splitters resize them live.
- `IsClosable` controls the tab's close button; `Close()` hides the pane and raises the
  bubbling `Closed` event (`PaneClosed` on the manager). `ShowPane`/`HidePane` manage
  visibility; panes stay in the layout while hidden.
- All panes of a zone share one tab strip; the selected pane of the active zone is exposed
  as the two-way `ActivePane` property.

## Layout persistence

```csharp
string xml = manager.SaveLayout();   // compact XML: zones, sizes, hidden state
manager.LoadLayout(xml);             // matched by pane Name (fallback: header text)
```

The serializer is UI-free (`DockLayoutSerializer`) — layouts can be stored in settings files
and validated in unit tests.

## Floating windows

`FloatPane(pane)` moves a pane into a floating window above the application window; closing the window or `DockPaneBack(pane)` returns it to its zone. Floating state and window bounds are part of the saved layout.

## Drag-to-redock

Drag a pane's tab and drop it over a zone: the left/top/right/bottom quarters and the center of the manager light up while dragging; dropping moves the pane. The drop hint is drawn with an adorner using the library theme brushes.

## Auto-hide

```xml
<docking:DockPane Header="Tools" Zone="Left" IsAutoHidden="True" ... />
```

An auto-hidden pane collapses to a thin tab at its edge (left/right tabs are rotated);
hovering the tab for ~250 ms or clicking it flies the pane out as an overlay — it does
NOT push the layout. Moving the mouse away for ~400 ms collapses it again; the 📌 button
in the flyout header docks the pane back into its zone. Center panes ignore auto-hide
(setting it reverts). `IsAutoHidden` participates in `SaveLayout`/`LoadLayout`; the
`AutoHiddenLeftPanes`/`Top`/`Right`/`Bottom` collections expose the collapsed panes.

## Not yet (see ROADMAP)

Nested splitter groups.
