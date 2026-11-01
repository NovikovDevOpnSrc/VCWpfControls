---
title: Сортировка, фильтрация и группировка
description: Сортировка (клик, Shift+клик, SortBy/AddSortLevel), фильтрация (FilterDescriptions с 17 операторами) и группировка (GroupDescriptions, GridGroup, SetGroupExpanded) в GridControl из кода.
---

# Сортировка, фильтрация и группировка

Все три функции реализованы в движке данных без UI (`GridDataManager`) и управляются тремя
наблюдаемыми коллекциями грида: `SortDescriptions`, `FilterDescriptions` и
`GroupDescriptions`. Изменение коллекции немедленно перестраивает представление
(конвейер: фильтрация → сортировка → группировка → разворачивание в строки).

> **О статусе.** Описанные здесь API движка полностью работают. Встроенные **строка
> **Встроенный UI.** Установите `ShowFilterRow="True"`, чтобы получить строку фильтров
> под заголовками (Contains для текста, типизированное равенство для чисел/дат). Групповые
> строки отображаются с переключателем сворачивания; итоги добавляются в `SummaryItems`
> (`Sum`, `Min`, `Max`, `Avg`, `Count`). Описанные ниже API движка работают независимо от UI.

## Сортировка

### Мышью и с клавиатуры

Клик по заголовку переключает направление по циклу: нет → по возрастанию → по убыванию →
нет. **Shift+клик** добавляет колонку как следующий уровень сортировки, не сбрасывая
существующие (многоколоночная сортировка). Текущее направление показывается индикатором
в заголовке.

Оба действия можно отследить или отменить через событие `Sorting`:

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Total")
    {
        e.Cancel = true; // не трогаем серверный порядок для этой колонки
    }
};

grid.Sorted += (s, e) => { /* порядок применён, например обновляем статус */ };
```

### Из кода

```csharp
// Заменить все уровни сортировки:
grid.SortBy(nameof(Employee.Name), GridSortDirection.Descending);

// Или через экземпляр колонки (используется SortMemberPath ?? FieldName):
grid.SortBy(nameColumn, GridSortDirection.Ascending);

// Добавить/обновить один уровень, сохранив остальные (многоколоночная сортировка):
grid.AddSortLevel(nameof(Employee.Department), GridSortDirection.Ascending);
grid.AddSortLevel(nameof(Employee.Salary), GridSortDirection.Descending);

// Убрать все уровни:
grid.ClearSorting();
```

`SortDescriptions` — это `ObservableCollection<GridSortDescription>`, поэтому её можно
редактировать и напрямую. У каждого `GridSortDescription` есть:

- `FieldName` — путь к свойству (допустим `"Address.City"`);
- `Direction` — `Ascending` / `Descending` / `None`;
- `Comparer` — необязательный свой `IComparer` значений ячеек (заменяет учитывающее
  культуру сравнение по умолчанию; направление всё равно применяет движок);
- `Culture` — культура для сравнения строк (по умолчанию `CurrentCulture`).

```csharp
grid.SortDescriptions.Add(new GridSortDescription("Salary", GridSortDirection.Descending)
{
    Comparer = Comparer<object>.Create((a, b) => ((decimal)a!).CompareTo((decimal)b!)),
});
```

Колонка может сортироваться по другому полю через `SortMemberPath`; а `CanUserSort = false`
у колонки отключает сортировку кликом только для неё.

## Фильтрация

`FilterDescriptions` — коллекция описаний `GridFilterDescription`; несколько фильтров
объединяются по **логическому И**.

```csharp
using VCWpfControls.Grid.Data;

// Зарплата от 50 000 до 90 000:
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Salary), GridFilterOperator.Between, 50000m, 90000m));

// Имя содержит "al" (по умолчанию сравнение с учётом культуры, без учёта регистра):
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Name), GridFilterOperator.Contains, "al"));

// Удалить один / все фильтры:
grid.FilterDescriptions.RemoveAt(0);
grid.FilterDescriptions.Clear();
```

### Операторы

| Оператор | Смысл | Операнды |
|---|---|---|
| `None` | Фильтр выключен | — |
| `Equals`, `NotEquals` | (Не) равно `Value` | `Value` |
| `Contains`, `NotContains`, `StartsWith`, `EndsWith` | Текстовые проверки (по умолчанию без учёта регистра) | `Value` как текст |
| `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` | Сравнения | `Value` |
| `Between`, `NotBetween` | Внутри / вне диапазона включительно | `Value` и `Value2` |
| `In`, `NotIn` | Значение (не) содержится в списке (`Value` — `IEnumerable`) | `Value` |
| `IsNull`, `IsNotNull` | Проверка на null (или `DBNull`) | — |
| `Custom` | Ваш предикат по всему элементу данных | `CustomPredicate` |

Дополнительные настройки `GridFilterDescription`:

- `TextComparison` — `StringComparison` для текстовых операторов (по умолчанию
  `CurrentCultureIgnoreCase`);
- `CustomPredicate` — `Predicate<object>` для оператора `Custom`:

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Employee e && e.IsActive && e.Salary > 50000m,
});
```

Null-ячейки совпадают только с явным оператором `IsNull` (и со списками `In`,
содержащими `null`), поэтому комбинированные фильтры ведут себя предсказуемо.

## Группировка

`GroupDescriptions` — коллекция описаний `GridGroupDescription`, от внешнего уровня к
внутреннему:

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Department)));
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Office)));
```

При активной группировке представление грида чередует **строки заголовков групп** со
строками данных. Развёрнутый список доступен в движке: `grid.DataEngine.View` — это
`IReadOnlyList<GridRowInfo>` из `GridDataRow` (`Kind == GridRowKind.Data`, содержит `Item`)
и `GridGroupRow` (`Kind == GridRowKind.Group`, содержит `Group`).

Каждый `GridGroup` предоставляет:

- `Key` — ключ группы (значение ячейки, возможно преобразованное);
- `Level` и `Parent` — положение в дереве групп;
- `ChildGroups` — подгруппы следующего уровня;
- `Items` — элементы данных, лежащие непосредственно в группе (только самый глубокий уровень);
- `TotalItems` — общее число строк данных под группой;
- `IsExpanded` — состояние развёрнутости, **сохраняемое между обновлениями данных**.

### Сворачивание и разворачивание

Сворачивайте группы программно через движок — состояние переживёт последующие вызовы
`Refresh()`:

```csharp
foreach (var row in grid.DataEngine.View.OfType<GridGroupRow>())
{
    if (row.Group.Level == 0 && row.Group.TotalItems < 5)
    {
        grid.DataEngine.SetGroupExpanded(row.Group, expanded: false);
    }
}
```

### Свои ключи

`KeyConverter` превращает значение ячейки в ключ группы (например, группировка дат по
месяцам); `KeysComparer` задаёт порядок ключей:

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Order.CreatedAt))
{
    KeyConverter = value => ((DateTime)value!).ToString("yyyy-MM"),
});
```

Строки групп пропускаются при навигации с клавиатуры, а `ScrollIntoView`, `SelectRange` и
другие методы работают с элементами данных, а не со строками групп.

> **О статусе.** Агрегатные итоги (Sum/Min/Max/Avg/Count по группам и в подвале)
  запланированы на этап 1.2; до этого считайте их во вью-модели по
  `GridGroup.Items` / `TotalItems`.
