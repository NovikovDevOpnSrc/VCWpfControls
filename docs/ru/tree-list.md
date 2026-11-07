---
title: Дерево с колонками
description: Иерархические данные с колонками, сворачиванием и сортировкой на каждом уровне.
---

# TreeListView

`VCWpfControls.TreeList.TreeListView` отображает иерархию как таблицу: корни берутся из
`ItemsSource`, дочерние элементы — из `ChildMemberPath` (любое свойство-`IEnumerable`).
Наследуется всё от `GridControl`: ручные/авто-колонки, виртуализация, выделение,
редактирование, row details, темы, клавиатура.

## Быстрый старт

```xml
<Window xmlns:tree="clr-namespace:VCWpfControls.TreeList;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls">
    <tree:TreeListView ItemsSource="{Binding Units}" ChildMemberPath="Children">
        <tree:TreeListView.Columns>
            <columns:GridTextColumn FieldName="Name" Header="Подразделение" Width="2*" />
            <columns:GridNumericColumn FieldName="Headcount" Header="Люди" Width="110" />
        </tree:TreeListView.Columns>
    </tree:TreeListView>
</Window>
```

## Сворачивание

- Узлы стартуют свёрнутыми; `InitiallyExpanded="True"` раскрывает первый уровень.
- API: `ToggleNode(item, expanded)`, `ExpandAll()`, `CollapseAll()`, `IsExpanded(item)`;
  стрелка у первого столбца делает то же для отдельного узла.
- Состояние раскрытия переживает сортировку и изменения коллекций.

## Сортировка

Клики по заголовкам и `SortBy`/`AddSortLevel` упорядочивают детей **внутри каждого уровня**
(корни среди корней, их дети среди соседей; отображение — в DFS-порядке).

## Живые обновления

Изменения в корневой коллекции или в любой материализованной коллекции детей
(рекомендуется `ObservableCollection`) автоматически перестраивают представление.

## Пока нет (см. ROADMAP)

 панель группировки/строка фильтров
неприменимы.

## Чекбоксы узлов

`ShowNodeCheckBoxes="True"` добавляет каждому узлу tri-state чекбокс. Отметка узла каскадом применяется к потомкам; предки выводят неопределённое состояние из смешанных детей. API: `IsNodeChecked(item)`, `SetNodeChecked(item, bool)`, `CheckedItems` (все отмеченные, включая свёрнутые), routed-событие `NodeCheckChanged`. В lazy-режиме (`ChildrenProvider`) свёрнутый узел записывает своё состояние без загрузки поддерева.


## Суммы по уровням

```xml
<treeList:TreeListView ChildMemberPath="Children" ...>
    <treeList:TreeListView.TreeSummaryItems>
        <treeList:GridTreeSummaryItem FieldName="Amount" Function="Sum" StringFormat="N0"/>
        <treeList:GridTreeSummaryItem FieldName="Amount" Function="Count" Level="0"/>
    </treeList:TreeListView.TreeSummaryItems>
</treeList:TreeListView>
```

Строка каждого узла с детьми показывает агрегаты у правого края (Sum/Min/Max/Avg/Count).
`Mode="Subtree"` (по умолчанию) агрегирует всех потомков — включая свёрнутых и ленивых
(`ChildrenProvider`); `Mode="Children"` — только прямых детей. `Level` ограничивает
суммирование одним уровнем дерева (null = все уровни); сам узел в сумму не входит.
Значения считаются по полной структуре — раскрытие их не меняет; изменение коллекции
сразу перерисовывает строки.
