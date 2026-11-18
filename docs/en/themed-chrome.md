---
title: Themed chrome
description: The remaining stock WPF surfaces — tabs, expander, group box, slider, progress bar, lists, trees, tooltips — themed in every palette through implicit styles.
---

# Themed chrome

Beyond the controls the library defines, every common stock WPF surface follows
Light/Dark/HighContrast through implicit styles merged application-wide by
`ThemeManager`: `Button`, `TextBox`, `ComboBox`/`ComboBoxItem`, `CheckBox`,
`RadioButton`, `Hyperlink`, `ToggleButton`, `Menu`/`MenuItem`/`ContextMenu`,
`StatusBar`/`StatusBarItem` — and the "chrome completion" set:

- **TabControl / TabItem** — a transparent strip with a 1px underline tying it to the
  page; the selected tab carries the accent underline (matching the menu open state);
  items are 32px with keyboard focus visuals;
- **Expander** — a header toggle with a rotating arrow; content below (Space toggles —
  the toggle is the stock channel);
- **GroupBox** — a 1px rounded frame with the header knocking out the top border;
- **Slider** — the track on `BarFillKey`, an accent round thumb with a surface stroke
  (the required `PART_Track` keeps arrows/keyboard native);
- **ProgressBar** — `BarFill` track + accent indicator (`PART_Track`/`PART_Indicator`;
  the control sizes the indicator itself);
- **ListBox / ListBoxItem** and **TreeView / TreeViewItem** — surface + border, 32px
  items, hover and the SelectionBackground/SelectionText pairing (the grid row pattern);
  tree items indent 16px per level with a themed expander;
- **ToolTip** — a surface card (4px radius, caption-size text).

A palette-parity regression guards the whole family: the implicit style type set must be
identical across Light/Dark/HighContrast. High contrast rides the token aliases — the
surfaces follow the user's contrast scheme.

The demo's **Chrome** page shows the full set live.
