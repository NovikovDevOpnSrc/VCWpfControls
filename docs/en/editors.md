---
title: Editors
description: Masked text input and numeric spinners.
---

# Editors

The `VCWpfControls.Editors` namespace contains standalone value editors that share the
library theme (`GridResources.*`) and the UI-free engine philosophy.

## MaskedTextBox

```xml
<editors:MaskedTextBox Mask="+7 (000) 000-00-00" />
```

Mask language: `0` digit, `9` optional digit, `#` optional digit or sign, `L`/`?` required /
optional letter, `A`/`a` required / optional letter-or-digit, `C`/`c` any character,
`\` escapes the next character into a literal. Any other character is a literal.

- Literals are displayed and skipped while typing; `PromptChar` (default `_`) marks empty slots.
- `Value` holds the typed characters; `IncludeLiterals="True"` adds the literals.
- `IsMaskComplete` tells whether all required slots are filled.
- Backspace clears the previous slot, Delete the current one, paste is mask-validated.
- Inherits from `TextBox`, so selection, clipboard and styling all work.

## NumericUpDown

```xml
<editors:NumericUpDown MinValue="0" MaxValue="10" Increment="0.25" FormatString="C2" Value="2.5" />
```

- `Value` (decimal, two-way) is always coerced into `MinValue`/`MaxValue`.
- Step sources: repeat buttons (hold to repeat), Up/Down arrows, PageUp/PageDown (10 steps),
  mouse wheel; typed text is parsed with the current culture (invalid input reverts).
- `DecimalPlaces` (default: derived from the increment) or a `FormatString` control display;
  `IsReadOnly` disables editing; `ValueChanged` routed event reports changes.


## AutoComplete

A text input with a suggestion dropdown (`Editors.AutoComplete`), also usable as a grid
column (`GridAutoCompleteColumn`):

- `ItemsSource` — any IEnumerable; strings match directly, objects match by `ItemTextPath`;
- default matching is culture-invariant case-insensitive **contains**; replace with
  `ItemFilter` for custom logic;
- `MinimumPrefixLength` (default 1) and `MaxSuggestions` (default 8) gate the popup;
- **ArrowUp/Down** move the selection, **Enter/Tab** accept, **Esc** closes, a click
  accepts; `SuggestionAccepted` fires after acceptance; `Text` is two-way bindable and
  `SelectedItem` carries the accepted item.

```xml
<editors:AutoComplete Text="{Binding City}" ItemsSource="{Binding Cities}" />
<columns:GridAutoCompleteColumn FieldName="Name" ItemsSource="{Binding Names}" Width="190" />
```
