---
title: Status bar
description: Themed StatusBar/StatusBarItem implicit styles in every palette — caption-size text, a 1px top separator, themed item separators; the demo shell docks a live bar (page + theme).
---

# Status bar

The library themes the standard WPF `StatusBar` and `StatusBarItem` through implicit
styles merged application-wide by `ThemeManager` — write plain WPF status bars and they
follow Light/Dark/HighContrast.

```xml
<StatusBar>
    <StatusBarItem>
        <TextBlock Text="{Binding Status}" />
    </StatusBarItem>
    <StatusBarItem>
        <!-- item separators are themed 1px Rectangles -->
        <Rectangle Width="1" Margin="4,6,4,6"
                   Fill="{DynamicResource {x:Static grid:GridResources.SeparatorLineKey}}" />
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <TextBlock Text="{Binding ItemCount}" />
    </StatusBarItem>
</StatusBar>
```

- **Metrics** (the UI guidelines): caption-size text (12) on a 28px strip, the surface
  background, a 1px `SeparatorLineKey` top separator, 8px item rhythm.
- **Item separators**: `StatusBarItem` has NO `SeparatorStyleKey` in WPF (unlike
  `MenuItem`) — a plain `<Separator/>` renders a horizontal line. Use a themed 1px
  `Rectangle` as above.
- **High contrast**: every brush goes through the palette tokens, which alias the live
  system colors.
- The demo shell docks a **live status bar** (bottom, spanning the window): the current
  page title and the current theme name, updating on navigation and on
  `ThemeManager.ThemeChanged`.

## Announcing changes

Status bars report state that changes on its own — mark the text region live so assistive
tech announces updates: `AutomationProperties.LiveSetting="Polite"` (the demo's theme
item does).
