---
title: Темы оформления
description: Тема GridControl по умолчанию, ключи ресурсов GridResources (ComponentResourceKey) и способы переопределения кистей (акцент, выделение, линии сетки) в приложении.
---

# Темы оформления

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
| `CurrentCellBorderBrushKey` | Рамка текущей ячейки | `#FF2563EB` |
| `GridLineKey` | Горизонтальные разделители строк | `#FFE5E7EB` |
| `AccentKey` | Индикатор сортировки, кромка закреплённых колонок | `#FF2563EB` |
| `HoverBackgroundKey` | Фон строки/заголовка при наведении | `#FFEDF2F9` |
| `AccentTextKey` | Акцентный текст (нажатые заголовки, ссылки) | `#FF1D4ED8` |

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

Встроенного переключателя Light/Dark пока нет (запланирован на этап 1.3), но перечисленных
выше десяти ключей достаточно, чтобы работать в тёмной палитре:

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
