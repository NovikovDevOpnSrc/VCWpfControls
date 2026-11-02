---
title: Редактор свойств (PropertyGrid)
description: Просмотр и редактирование свойств любого объекта с типизированными редакторами, категориями и поиском.
---

# Редактор свойств (PropertyGrid)

`VCWpfControls.PropertyGrid.PropertyGrid` показывает и редактирует публичные свойства любого
объекта: типизированные редакторы, группировка по категориям, поиск, раскрываемые вложенные
объекты и живое обновление через `INotifyPropertyChanged`.

## Быстрый старт

```xml
<Window xmlns:pg="clr-namespace:VCWpfControls.PropertyGrid;assembly=VCWpfControls">
    <pg:PropertyGrid SelectedObject="{Binding SelectedEmployee}" />
</Window>
```

Типичный сценарий — связка с `GridControl`:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="380" />
    </Grid.ColumnDefinitions>
    <grid:GridControl x:Name="Grid" ItemsSource="{Binding Employees}" />
    <pg:PropertyGrid Grid.Column="1" SelectedObject="{Binding SelectedItem, ElementName=Grid}" />
</Grid>
```

## Возможности

- **Типизированные редакторы** по типу свойства: текстовое поле, многострочный текст
  (`[DataType(DataType.MultilineText)]`), чекбокс, комбобокс для перечислений, числовое поле
  с выравниванием вправо, выбор даты, read-only текст для коллекций и неподдерживаемых типов.
- **Метаданные** через атрибуты: `[Display]` (`Name`, `Description`, `Order`, `GroupName`),
  `[Category]`, `[DisplayName]`, `[ReadOnly]`, `[Browsable(false)]`.
- **Категории** — раскрывающиеся группы (`ShowCategories="False"` — плоский список).
- **Поиск** (`ShowSearchBox`) по отображаемому или CLR-имени свойства.
- **Раскрываемые свойства**: у сложных объектов появляется переключатель «+» с вложенными
  свойствами (лениво, с отступом).
- **Живое обновление**: если объект реализует `INotifyPropertyChanged`, изменения значения
  обновляют соответствующий редактор.
- **Событие** `PropertyValueChanged` — после каждого зафиксированного изменения (значение уже
  записано в объект).
- **Настройки**: `LabelWidth` — ширина колонки меток.

## Семантика редактирования

- Текстовые редакторы фиксируются по **Enter** и при потере фокуса; некорректный ввод (буквы в
  числовом поле) молча откатывается к текущему значению.
- Комбобокс не позволяет выбрать несуществующее значение; чекбоксы и даты фиксируются сразу.
- Свойства только для чтения и коллекции отображаются, но не редактируются.

## Темы и локализация

Контрол использует те же ресурсные ключи `GridResources.*`, что и грид (см.
[Темы](theming.md)); подсказка поиска и имя категории «Прочее» локализованы (в пакете уже
есть EN/RU/ZH).
