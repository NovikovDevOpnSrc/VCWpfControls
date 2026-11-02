# WPF UX checklist (library standard)

Applied when changing any template/theme/visual. Every item has a "why" — Fluent 2 / WinUI 3
conventions and WCAG. Keep this list in sync with `Themes/*.xaml`.

## Typography
- [ ] Body text 14 px (`FontSize` setter on the control style); captions/hints may be 12.
- [ ] Headers: SemiBold, at least 32 px tall (touch target).
- [ ] One type ramp per surface: 12 / 14 / 16 / 20 — no arbitrary sizes in templates.

## Layout & spacing
- [ ] 4 px grid: all paddings/margins are multiples of 4 (8 preferred for cell insets).
- [ ] Content inset from edges: 8 px (cells use `Margin="8,0,8,0"`).
- [ ] Row separators 1 px, `GridLineKey`; never rely on control borders for data separation.

## Color & theming
- [ ] **No hardcoded colors in styles** — every brush goes through a `GridResources.*` key and
  is defined in Light AND Dark palettes (bug fixed in `37afb7b`-era: dark theme had dark text
  on dark surface).
- [ ] Text contrast ≥ 4.5:1 (WCAG AA) in both themes; selection keeps text readable.
- [ ] Selection/hover/accent derive from one accent per theme.

## States
- [ ] Hover AND pressed states on every interactive element (headers, tab strips, spinners).
- [ ] Focus visible for keyboard users (default focus rect is acceptable; never remove it).
- [ ] Disabled state instead of hidden when an action is temporarily unavailable
  (e.g. "Hide column" for the last visible column).
- [ ] Empty state: centered localized message (`PART_EmptyState`, `GridStrings.NoData`).

## Interaction
- [ ] Double-click on column splitter = best fit (Excel convention).
- [ ] Context menus are localized and rebuilt per open (state marks are always current).
- [ ] Destructive actions guarded (hide-last-column disabled, close requires IsClosable).

## Localization (UX side)
- [ ] Strings do not rely on fixed widths (RU ≈ +20% length vs EN, ZH ≈ −30%).
- [ ] No text baked into images; all UI strings come from `Strings.*.resx`.

## Verification
- [ ] STA tests for state transitions (empty state, theme switch, hover-independent logic).
- [ ] Dark theme test asserts foreground switches (`GridUxTests.Theme_SwitchesTextBrush…`).
- [ ] Screenshot review: on RDP sessions captures return black frames — run the demo manually
  or use FlaUI-driven structural assertions instead of pixels.
