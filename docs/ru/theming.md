---
title: Темы оформления
description: Тема GridControl по умолчанию, ключи ресурсов GridResources (ComponentResourceKey) и способы переопределения кистей (акцент, выделение, линии сетки) в приложении.
---

# Темы оформления

## ThemeManager — централизованный переключатель

Библиотека несёт менеджер уровня приложения в стиле DevExpress/Fiori (по рекомендации
Microsoft — переопределять ресурсы темы на уровне приложения):

```csharp
using VCWpfControls.Themes;

ThemeManager.Apply(GridTheme.Dark);                          // свап палитры на всё приложение
ThemeManager.SetAccent(new("Purple", "#8B5CF6", "#B9A2F8")); // бренд-оверрайд для обеих тем
ThemeManager.SetAccent(null);                                // вернуть бренд темы
ThemeManager.ThemeChanged += (_, _) => RefreshIcons();       // перечитать закэшированные кисти
```

Все контролы резолвят кисти через ключи `GridResources` с `DynamicResource` — UI
перекрашивается мгновенно, без перезагрузок и пер-контрольной обвязки. Для режима
«System» (аналог `ThemeMode="System"` из .NET 9) читайте предпочтение ОС
(`AppsUseLightTheme`) — страница Settings демо показывает ровно это, включая живой
2-секундный watcher. `GridThemeManager.Apply(grid, theme)` остаётся для per-control
opt-in. Полнота палитр охраняется тестом-контрактом: каждый словарь темы определяет
каждый кистевой токен.

## Высокая контрастность (системные схемы Windows)

`GridTheme.HighContrast` — палитра доступности: **каждая** кисть токена алиасит живой
системный цвет (`SystemColors` через `DynamicResource` на Color), поэтому интерфейс следует
любой контрастной схеме Windows и мгновенно перекрашивается при её смене — без перезапуска
приложения. Настройки доступности пользователя не переопределяются (A11Y-003 UX-профиля):

- текст и поверхности → `Window`/`WindowText`; линии, полосы и разделители → `WindowFrame`;
- выделение — пара `Highlight`/`HighlightText`: текст выбранной строки инвертируется вместе
  с фоном (`GridResources.SelectionTextKey`), поэтому инвертированные схемы (белое
  выделение, чёрный текст) остаются читаемыми, а не исчезают;
- акцент следует `HotTrack`; override акцента (`SetAccent`) игнорируется, пока активна эта
  тема, и автоматически возвращается при переходе обратно на Light/Dark;
- декоративные состояния (подсветка наведения, чередование строк) схлопываются в цвет окна.

На странице Settings демо есть режим; в режиме `System` контрастная схема разрешается в
HighContrast (watcher опрашивает `SystemParameters.HighContrast` вместе с предпочтением
светлой/тёмной темы).

## Соответствие Fluent 2

Палитры Light/Dark по умолчанию следуют фундаменту **Fluent 2** (дизайн-система
Microsoft): бренд `#0F6CBD` (светлая) / `#479EF5` (тёмная), тонированная нейтральная
ramp'а (`#242424`/`#FFFFFF` текст, `#FAFAFA`/`#1F1F1F` поверхности, `#E0E0E0`/`#3D3D3D`
линии), выбор с брендовым подтоном (`#EBF3FC`/`#115EA3`), нейтральные hover-состояния.
Поверх палитры темы раскрывают токены фундамента:

- `GridResources.FontFamilyKey` — «Segoe UI Variable Text» с фолбэком на Segoe UI;
- типо-лесенка — `FontSizeCaptionKey` (12) / `FontSizeBodyKey` (14, дефолт контролов) /
  `FontSizeSubtitleKey` (16) / `FontSizeTitleKey` (20);
- `GridResources.FocusVisualKey` — фокус-ринг 2px со скруглением (грид и заголовки);
- радиусы — конвенция Fluent 4/8 (контролы 4, карточки 8).

Приложения переопределяют любой токен через те же ключи; плотные внутренности чартов
намеренно сохраняют меньшие размеры.

## Тема по умолчанию

Светлая тема по умолчанию лежит в `Themes/Generic.xaml` внутри библиотеки и применяется
автоматически — словари объединять не нужно. WPF подхватывает её, потому что `GridControl`
переопределяет свой `DefaultStyleKey`.

Все значения темы — ресурсы, адресованные **`ComponentResourceKey`** и выставленные как
статические поля класса `GridResources`; шаблон контрола потребляет их через
`DynamicResource`. Отсюда два полезных следствия:

- переопределения можно положить в любой словарь ресурсов, находящийся ближе к контролу,
  чем словарь темы (например, `Application.Resources`), и они применятся **без замены
  шаблона**;
- их же можно менять и на лету, во время выполнения.

## Ключи ресурсов

| Ключ (`GridResources.*`) | На что влияет | По умолчанию |
|---|---|---|
| `SurfaceBackgroundKey` | Фон всей поверхности грида | `#FFFFFFFF` |
| `HeaderBackgroundKey` | Фон заголовков колонок | `#FFF3F4F6` |
| `HeaderForegroundKey` | Текст заголовков колонок | `#FF374151` |
| `HeaderBorderKey` | Границы и разделители заголовков | `#FFD1D5DB` |
| `SelectionBackgroundKey` | Фон выделенной строки | `#FFCFE3F5` |
| `SelectionTextKey` | Текст выделенной строки (HighlightText в высокой контрастности) | `#FF1F2937` |
| `CurrentCellBorderBrushKey` | Рамка текущей ячейки | `#FF2563EB` |
| `GridLineKey` | Горизонтальные разделители строк | `#FFE5E7EB` |
| `AccentKey` | Индикатор сортировки, кромка закреплённых колонок | `#FF2563EB` |
| `HoverBackgroundKey` | Фон строки/заголовка при наведении | `#FFEDF2F9` |
| `AccentTextKey` | Акцентный текст (нажатые заголовки, ссылки) | `#FF1D4ED8` |

Помимо таблицы, каждая встроенная тема (Light/Dark/HighContrast) определяет и chart-токены
кастомно-рендерящихся контролов: `BarFillKey` (тела баров/чипов), `EventFillKey` (события
Scheduler), `BaselineFillKey` (базлайны Gantt), `NowLineKey` (линия сегодня/сейчас),
`SeparatorLineKey` (заметные разделители) и `MutedTextKey` (вторичный текст).

## Переопределение ключей

Переопределите ключ в `Application.Resources` (или в любом локальном словаре) — тот же ключ
просто разрешится раньше в цепочке поиска:

```xml
<Application xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Фирменные цвета, не трогая шаблон контрола -->
            <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.CurrentCellBorderBrushKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.SelectionBackgroundKey}" Color="#FFEDE9FE" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Поскольку ключи — обычные статические поля, в `x:Key` достаточно указать
`{x:Static grid:GridResources.AccentKey}` — разметку `ComponentResourceKey` писать не нужно.

Тема разрешает кисти через `DynamicResource`, поэтому переопределение на лету (например, из
сервиса тем) тоже работает.

## Кисти на уровне экземпляра

Для точечных настроек есть обычные свойства зависимости — ресурсы темы не нужны:

| Свойство | Смысл |
|---|---|
| `RowBackground` | Кисть чётных строк данных |
| `AlternatingRowBackground` | Кисть чередующихся (нечётных) строк |
| `GridLinesBrush` | Кисть горизонтальных линий сетки |
| `ShowColumnHeaders` | Полностью скрыть строку заголовков |
| `RowHeight` / `HeaderHeight` | Фиксированные высоты (`NaN` = по содержимому) |

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  RowBackground="White"
                  AlternatingRowBackground="#FFF9FAFB"
                  GridLinesBrush="#FFE5E7EB"
                  RowHeight="28" />
```

## Минимальный тёмный рецепт

`ThemeManager.Apply(GridTheme.Dark)` делает это автоматически; вручную — перечисленных выше
ключей достаточно, чтобы работать в тёмной палитре:

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

Подключайте этот словарь по условию (по умолчанию — светлый, при тёмном режиме приложения —
тёмный): запросы `DynamicResource` подхватят актуальные значения.

## Если нужно больше

Для глубокого изменения стиля контрол предоставляет стандартные части шаблона
(`PART_ScrollViewer`, `PART_HorizontalScrollBar`, `PART_ColumnHeadersPresenter`) — можно
заменить весь `ControlTemplate`. Но для подавляющего большинства приложений хватает
переопределения ключей ресурсов.

### Кисть текста

`GridResources.TextBrushKey` хранит цвет текста данных и переключается вместе с темой (Light `#1F2937`, Dark `#D8DAE3`). Все контролы библиотеки биндят `Foreground` к ней — в своих шаблонах делайте так же, без хардкода цветов.

### UX-стандарт

Библиотека следует чеклисту [ux-checklist.md](../ux-checklist.md): типографика Fluent (основной текст 14 px, заголовки SemiBold высотой 32 px), сетка отступов 4 px, состояния hover+pressed, локализованный empty-state, контраст WCAG AA в обеих темах.
