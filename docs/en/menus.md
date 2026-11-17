---
title: Menus
description: Themed main menu bar and context menus — implicit Menu/MenuItem/ContextMenu styles in every palette with Win11 metrics, access keys, shortcuts, checkable items and separators.
---

# Menus

The library themes the standard WPF menu types — `Menu`, `MenuItem` and `ContextMenu` —
through implicit styles merged application-wide by `ThemeManager` (the same mechanism as
the themed Button/ComboBox/TextBox). No new types to learn: write plain WPF menus and
they follow Light/Dark/HighContrast.

```xml
<Menu>
    <MenuItem Header="_File">
        <MenuItem Header="_Open…" InputGestureText="Ctrl+O" Click="Open" />
        <Separator />
        <MenuItem Header="_Exit" InputGestureText="Alt+F4" Click="Exit" />
    </MenuItem>
    <MenuItem Header="_Edit">
        <MenuItem Header="Autosave" IsCheckable="True" IsChecked="True" />
    </MenuItem>
</Menu>
```

- **Metrics** (the UI guidelines): menu items 32px minimum, surface cards with 8px
  corners and 4px inner padding, hover through `HoverBackgroundKey`, disabled at 45%
  opacity.
- **Roles**: one template serves top-level headers/items and submenu items/headers —
  top-level items collapse the check/gesture/arrow columns; an open top-level shows the
  accent underline (Win11). Submenu items show the check glyph (when `IsCheckable`),
  the `InputGestureText` shortcut (muted, right-aligned) and the submenu arrow.
- **Separators** theme through `MenuItem.SeparatorStyleKey` (`<Separator/>` inside a
  menu picks it up automatically) — a 1px `SeparatorLineKey` rule.
- **Keyboard**: stock WPF behavior — access keys via `_` in headers (Alt+F opens File),
  arrow navigation, first-letter jumps; the shared focus ring shows on focused items.
- **High contrast**: every brush goes through the palette tokens, which alias the live
  system colors — the menus follow the user's contrast scheme.
- The **grid's header context menu** (right-click a column header) follows the theme
  automatically — it builds plain `ContextMenu`/`MenuItem` instances.

The demo's Menus page shows the bar (access keys, shortcuts, a checkable item, a
disabled item, separators) and a context menu area.
