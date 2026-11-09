---
title: Theming
description: GridControl default theme, the GridResources ComponentResourceKey resource keys and how to override brushes (accent, selection, grid lines) in your application.
---

# Theming

## Fluent 2 alignment

The default Light/Dark palettes follow the **Fluent 2** foundations (Microsoft's design
system): brand primary `#0F6CBD` (light) / `#479EF5` (dark), the tinted neutral ramp
(`#242424`/`#FFFFFF` text, `#FAFAFA`/`#1F1F1F` surfaces, `#E0E0E0`/`#3D3D3D` strokes),
brand-tinted selection (`#EBF3FC`/`#115EA3`) and neutral hovers. On top of the palette
brushes, themes expose Fluent foundation tokens:

- `GridResources.FontFamilyKey` — "Segoe UI Variable Text" with a Segoe UI fallback;
- the type ramp — `FontSizeCaptionKey` (12) / `FontSizeBodyKey` (14, the control default) /
  `FontSizeSubtitleKey` (16) / `FontSizeTitleKey` (20);
- `GridResources.FocusVisualKey` — the 2px rounded accent focus ring (grid + headers);
- corner radii follow the Fluent 4/8 convention (controls 4, cards 8).

Applications override any token through the same keys; data-dense chart internals keep
deliberately smaller sizes.

## Default theme

The default (light) theme lives in `Themes/Generic.xaml` inside the library and is applied
automatically — you do not need to merge any dictionaries. WPF picks it up because
`GridControl` overrides its `DefaultStyleKey`.

All theme values are resources addressed by **`ComponentResourceKey`s** exposed as static
fields on `GridResources`, and the control's template consumes them with `DynamicResource`.
Two useful consequences:

- Overrides can be placed in any resource dictionary that is closer to the control than
  the theme dictionary (e.g. `Application.Resources`), and they apply **without retemplating**;
- they can also be swapped at runtime.

## Resource keys

| Key (`GridResources.*`) | Affects | Default |
|---|---|---|
| `SurfaceBackgroundKey` | Background of the whole grid surface | `#FFFFFFFF` |
| `HeaderBackgroundKey` | Column header background | `#FFF3F4F6` |
| `HeaderForegroundKey` | Column header text | `#FF374151` |
| `HeaderBorderKey` | Header borders and separators | `#FFD1D5DB` |
| `SelectionBackgroundKey` | Selected row background | `#FFCFE3F5` |
| `CurrentCellBorderBrushKey` | Current cell border | `#FF2563EB` |
| `GridLineKey` | Horizontal row separators | `#FFE5E7EB` |
| `AccentKey` | Sort glyph, frozen-column edge | `#FF2563EB` |
| `HoverBackgroundKey` | Row/header hover background | `#FFEDF2F9` |
| `AccentTextKey` | Accent text (pressed headers, links) | `#FF1D4ED8` |

## Overriding keys

Redefine a key in `Application.Resources` (or in any scoped dictionary) — the same key
simply resolves earlier in the lookup chain:

```xml
<Application xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Brand the grid without touching its template -->
            <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.CurrentCellBorderBrushKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.SelectionBackgroundKey}" Color="#FFEDE9FE" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Because the keys are plain static fields, use `{x:Static grid:GridResources.AccentKey}` as
the `x:Key` — no need to restate the `ComponentResourceKey` markup yourself.

The theme resolves brushes with `DynamicResource`, so overriding at runtime (for example
from a theme service) also works.

## Per-instance brushes

For one-off tweaks, several looks are plain dependency properties on the control and do
not require theme resources at all:

| Property | Meaning |
|---|---|
| `RowBackground` | Brush of even data rows |
| `AlternatingRowBackground` | Brush of alternating (odd) data rows |
| `GridLinesBrush` | Brush of the horizontal grid lines |
| `ShowColumnHeaders` | Hide the header row entirely |
| `RowHeight` / `HeaderHeight` | Fixed heights (`NaN` = size to content) |

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  RowBackground="White"
                  AlternatingRowBackground="#FFF9FAFB"
                  GridLinesBrush="#FFE5E7EB"
                  RowHeight="28" />
```

## A minimal dark recipe

There is no built-in Light/Dark theme switch yet (planned for stage 1.3), but the ten keys
above are all it takes to run the grid dark:

```xml
<ResourceDictionary xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <SolidColorBrush x:Key="{x:Static grid:GridResources.SurfaceBackgroundKey}" Color="#FF111827" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderBackgroundKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderForegroundKey}" Color="#FFE5E7EB" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderBorderKey}" Color="#FF374151" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.SelectionBackgroundKey}" Color="#FF1E3A5F" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.CurrentCellBorderBrushKey}" Color="#FF60A5FA" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.GridLineKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentKey}" Color="#FF60A5FA" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HoverBackgroundKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentTextKey}" Color="#FF93C5FD" />
</ResourceDictionary>
```

Merge this dictionary conditionally (light by default, dark when your app is in dark mode)
— the `DynamicResource` lookups pick the currently merged values.

## Going further

For deeper restyling, the control exposes the usual template parts
(`PART_ScrollViewer`, `PART_HorizontalScrollBar`, `PART_ColumnHeadersPresenter`) if you
replace the whole `ControlTemplate`. Overriding the resource keys above is enough for the
vast majority of applications.

### Text brush

`GridResources.TextBrushKey` carries the data foreground color and switches with the theme (Light `#1F2937`, Dark `#D8DAE3`). All library controls bind their `Foreground` to it — custom templates should do the same instead of hardcoding colors.

### UX standard

The library follows the checklist in [ux-checklist.md](../ux-checklist.md): Fluent-style typography (body 14 px, SemiBold 32 px headers), 4 px spacing grid, hover+pressed states, localized empty state, WCAG AA contrast in both themes.
