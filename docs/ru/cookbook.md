---
title: Рецепты
description: Готовые приёмы работы с GridControl — сотни тысяч строк, формат валюты, чекбокс и комбобокс по Enum, копирование в буфер, перехват Sorting, прокрутка к строке.
---

# Рецепты

Короткие и законченные рецепты для типовых задач. Во всех сниппетах используются
пространства имён:

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## 1. Сотни тысяч строк

Виртуализация с рециклингом контейнеров включена всегда. Выжмите из неё максимум,
зафиксировав высоту строк и по возможности избегая колонок с авто-шириной:

```xml
<grid:GridControl x:Name="Grid"
                  ItemsSource="{Binding Trades}"
                  RowHeight="24"
                  HeaderHeight="28"
                  FrozenColumnCount="2"
                  CanUserSortColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Symbol" Header="Инструмент" Width="90" />
        <columns:GridTextColumn FieldName="Side" Header="Сторона" Width="60" />
        <columns:GridNumericColumn FieldName="Price" Header="Цена" Width="110" StringFormat="N4" />
        <columns:GridNumericColumn FieldName="Quantity" Header="Кол-во" Width="100" />
        <columns:GridDateColumn FieldName="Timestamp" Header="Время" Width="140" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

Замечания для очень больших источников:

- На этапе 1 движок перестраивает весь конвейер (фильтрация → сортировка → группировка)
  при каждом изменении коллекции; предпочтительнее массовые обновления (наполнить
  `ObservableCollection` и вызвать `Refresh()`), а не поодиночные уведомления.
  Инкрементальная и асинхронная обработка — в планах этапа 1.2.
- Сортировка 1 млн+ строк из кода — это `grid.SortBy(...)`; как её отслеживать, см. рецепт 6.

## 2. Формат валюты и чисел

`StringFormat` на любой колонке — в точности как `Binding.StringFormat`:

```xml
<columns:GridNumericColumn FieldName="Salary" Header="Зарплата" Width="120"
                           StringFormat="C" />          <!-- валюта текущей культуры -->
<columns:GridNumericColumn FieldName="Ratio" Header="Доля" Width="90"
                           StringFormat="{}{0:P1}" />   <!-- проценты, 1 знак -->
<columns:GridDateColumn FieldName="HireDate" Header="Дата приёма" Width="120"
                        StringFormat="dd.MM.yyyy" />    <!-- у дата-колонок по умолчанию "d" -->
```

## 3. Чекбокс-колонка с фиксацией по клику

`GridCheckBoxColumn` интерактивен без входа в режим правки и сразу записывает значение
через двустороннюю привязку. `Space` переключает текущую ячейку, `ReadOnly` отключает
взаимодействие:

```xml
<columns:GridCheckBoxColumn FieldName="IsActive" Header="Активен" Width="70" />
<columns:GridCheckBoxColumn FieldName="IsArchived" Header="В архиве" Width="80" ReadOnly="True" />
```

## 4. Колонка-комбобокс по Enum

Ручное определение (годится для любого справочника):

```xml
<columns:GridComboBoxColumn FieldName="Status" Header="Статус" Width="140"
                            ItemsSource="{Binding Statuses}"
                            DisplayMemberPath="Label"
                            SelectedValuePath="Id" />
```

Для перечислений ничего делать не нужно: автогенерация превращает свойство-Enum в
`GridComboBoxColumn`, чей `ItemsSource` заполнен `Enum.GetValues(type)` — ячейка показывает
имя значения, а комбобокс редактирует его из коробки:

```csharp
public enum OrderStatus { Draft, Confirmed, Shipped, Closed }

public class Order
{
    public OrderStatus Status { get; set; }   // сгенерируется как колонка-комбобокс
}
```

## 5. Копирование в буфер обмена (дружелюбно к Excel)

`Ctrl+C` (или `GridCommands.Copy`, или `CopyToClipboard()`) копирует выделенные строки —
либо текущую строку, если ничего не выделено, — как текст с табуляцией. При значении по
умолчанию `ClipboardCopyMode="IncludeHeaders"` первая строка содержит заголовки, поэтому
вставка в Excel/Таблицы даёт аккуратную таблицу:

```xml
<grid:GridControl ItemsSource="{Binding Orders}" ClipboardCopyMode="IncludeHeaders" />
```

```csharp
grid.SelectRange(firstOrder, lastOrder);
grid.CopyToClipboard();
```

Установите `ClipboardCopyMode="None"`, чтобы полностью отключить копирование.

## 6. Перехват сортировки

Отмените её, ограничьте или просто наблюдайте:

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Timestamp" && e.NewDirection == GridSortDirection.Descending)
    {
        e.Cancel = true;   // запрещаем сортировку по убыванию для колонки времени
    }
};

grid.Sorted += (s, e) =>
{
    StatusText = $"Отсортировано по {string.Join(", ", grid.SortDescriptions.Select(d => d.FieldName))}";
};
```

Программная сортировка доступна и без мыши: `SortBy(...)` заменяет все уровни,
`AddSortLevel(...)` расширяет их, `ClearSorting()` сбрасывает, а сами `SortDescriptions`
можно привязывать, если удобнее управлять ими из вью-модели.

## 7. Прокрутка к строке и её подсветка

```csharp
// после загрузки или поиска:
grid.Refresh();                                   // перестроить представление, если данные менялись извне
grid.SortBy(nameof(Customer.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(customer);                    // показать строку
grid.UnselectAll();
grid.SelectItem(customer);
grid.CurrentItem = customer;                      // поставить на неё текущую ячейку
```

## 8. Фильтрация со своим предикатом

Когда 17 встроенных операторов мало, оператор `Custom` принимает предикат по всему
элементу данных. Комбинируйте его с обычными фильтрами (они объединяются через И):

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription(nameof(Trade.Symbol),
    GridFilterOperator.NotEquals, "CANCELLED"));

grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Trade t && t.Quantity * t.Price > 10_000m,
});
```

Мини-поиск в вашем UI может просто заменять фильтр `Contains` на каждое нажатие клавиши:

```csharp
_searchFilter = new GridFilterDescription(nameof(Customer.Name), GridFilterOperator.Contains, text);
// при изменении текста:
grid.FilterDescriptions.Clear();
grid.FilterDescriptions.Add(_searchFilter);
```
