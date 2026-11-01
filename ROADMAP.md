# Roadmap

## Stage 1 — GridControl

### 1.1 Core (working grid)
- ItemsSource binding (IEnumerable / ObservableCollection / live updates)
- Auto-generated columns + manual columns (Text, Numeric, CheckBox, ComboBox, Date, Template)
- Column resize / reorder / hide / freeze, star and auto sizing
- UI virtualization with recycling, deferred scrolling
- Sorting: single & multi-column, indicators, custom comparers
- Selection: row single/extended, keyboard, current cell, check-box selection
- Editing with validation (IDataErrorInfo, INotifyDataErrorInfo), add/delete rows
- Keyboard navigation, clipboard copy

### 1.2 Analytics
- Filter row + per-column filter menu (text, value list, numeric/date ranges)
- Grouping with group panel, expand/collapse
- Aggregate summaries (Sum, Min, Max, Avg, Count) — footer and group level
- Asynchronous sorting/filtering for large data sets

### 1.3 Completion of feature parity
- Export: CSV, Excel (xlsx), HTML; clipboard as HTML
- Themes: Light/Dark + accents, High-DPI
- Built-in localization (RU/EN/ZH)
- Accessibility: UI Automation peers, full keyboard support
- Row details, context menus

## Stage 2 (candidates)
- TreeListView
- PropertyGrid

## Stage 3 (candidates)
- Additional controls: editors (masked input, spin buttons), spinner, docking panels
