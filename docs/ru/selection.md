---
title: Выделение
description: Режимы выделения GridControl (SingleRow/Extended), SelectedItem/SelectedItems, текущая ячейка (CurrentItem/CurrentColumn), API Select* и навигация с клавиатуры.
---

# Выделение

## Режимы выделения

`SelectionMode` предлагает два режима:

- **`GridSelectionMode.SingleRow`** — одна строка за раз; клик или навигация выделяет строку.
- **`GridSelectionMode.Extended`** (по умолчанию) — Ctrl+клик переключает отдельные строки,
  Shift+клик выделяет диапазон, `Ctrl+A` выделяет всё.

Переключение на `SingleRow` при нескольких выделенных строках оставляет только первую.

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  SelectionMode="Extended"
                  SelectedItem="{Binding SelectedEmployee}" />
```

## Выделенные элементы

| Член | Тип | Смысл |
|---|---|---|
| `SelectedItem` | DP (двусторонняя по умолчанию) | Первый выделенный элемент; привязывайте к вью-модели |
| `SelectedItems` | `IList` | Живое представление выделения |
| `SelectedItemsCollection` | `ObservableCollection<object>` | То же выделение в виде наблюдаемой коллекции |

`SelectedItem` — свойство зависимости с `BindsTwoWayByDefault`: в привязках не нужно
указывать `Mode=TwoWay`.

## Программное выделение

```csharp
grid.SelectItem(alice);            // добавить к выделению
grid.UnselectItem(bob);            // убрать один элемент
grid.SelectRange(alice, dave);     // выделить все строки между двумя элементами (в порядке представления)
grid.SelectAll();                  // только в режиме Extended
grid.UnselectAll();                // очистить
```

`SelectAll` в режиме `SingleRow` ничего не делает. При активной группировке `SelectRange`
и `SelectAll` работают только со строками данных и пропускают строки заголовков групп.

Выделение **переживает фильтрацию и сортировку**: элементы остаются выделенными, даже
когда фильтр временно их скрывает; снимается выделение только при удалении элемента из
источника.

## Текущая ячейка

Текущая ячейка — якорь для навигации с клавиатуры и редактирования:

- `CurrentItem` — элемент данных текущей строки (двусторонняя привязка);
- `CurrentColumn` — колонка текущей ячейки или `null`;
- `CurrentCellChanged` — CLR-событие, возникающее при любом смещении текущей ячейки.

```csharp
grid.CurrentItem = employees[4];
grid.ScrollIntoView(employees[4]);
```

## SelectionChanged

```csharp
grid.SelectionChanged += (s, e) =>
{
    foreach (var added in e.AddedItems) Console.WriteLine($"выделено: {added}");
    foreach (var removed in e.RemovedItems) Console.WriteLine($"снято выделение: {removed}");
};
```

## Клавиатура

| Клавиша | Действие |
|---|---|
| Стрелки | Сдвиг текущей ячейки (открытая правка сначала фиксируется) |
| `PageUp` / `PageDown` | Сдвиг текущей ячейки на ~3/4 страницы |
| `Home` / `End` | Первая/последняя колонка текущей строки |
| `Ctrl+Home` / `Ctrl+End` | Первая/последняя ячейка представления |
| `Ctrl+A` | Выделить все строки (только Extended) |
| `Ctrl+C` | Скопировать выделение как текст с табуляцией |
| `Delete` | Удалить выделенные строки (см. [Редактирование](editing.md)) |
| `Enter` / `Esc` / `F2` | Зафиксировать / отменить / начать правку |

В режиме `SingleRow` обычная навигация заодно переносит выделение; в `Extended` стрелки
двигают только текущую ячейку — удерживайте Shift (или используйте Shift+клик), чтобы
расширять выделение.

## Прокрутка к элементу

`ScrollIntoView(item)` прокручивает таблицу по вертикали, пока строка элемента не станет
видимой:

```csharp
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(viewModel.EmployeeToHighlight);
grid.SelectItem(viewModel.EmployeeToHighlight);
```

Строки заголовков групп никогда не становятся текущими — навигация автоматически их
пропускает.


## Выделение ячеек (SelectedCells)

```xml
<grid:GridControl SelectionUnit="CellOrRow" ... />
```

В режиме `CellOrRow` клики выделяют ЯЧЕЙКИ вместо строк:

- клик — ячейка; **Shift+клик** — прямоугольник до якоря; **Ctrl+клик** — переключение
  ячейки; **Ctrl+Shift+клик** — добавить прямоугольник;
- **Shift+стрелки** растягивают прямоугольник от якоря; `SelectAll` выделяет все ячейки;
- диапазоны хранятся ПО ЭЛЕМЕНТАМ: переживают сортировку/фильтрацию (прямоугольник всегда
  покрывает оба угловых элемента); диапазоны удалённых из источника строк отбрасываются;
- выделенные ячейки подсвечиваются темом выделения;
- `SelectedCells` возвращает ячейки в порядке вью; программно — `SelectCellRange`/
  `AddCellRange`/`UnselectAllCells`; событие `CellSelectionChanged`;
- **Ctrl+C** копирует bounding box выделенных диапазонов в TSV — пустые клетки внутри
  бокса выходят пустыми (семантика Excel), идеально в паре со вставкой Ctrl+V.

Строчное выделение продолжает работать через API (`SelectItem`, `SelectedItems`) в обоих
режимах.
