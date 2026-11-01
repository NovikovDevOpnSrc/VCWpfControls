---
title: Колонки
description: Типы колонок GridControl (текстовая, числовая, чекбокс, комбобокс, дата, шаблонная), автогенерация по атрибутам, ширины, закрепление и шаблоны ячеек.
---

# Колонки

Все типы колонок живут в `VCWpfControls.Grid.Columns` и наследуются от `GridColumnBase`.

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## Типы колонок

| Тип | Отображение | Редактор | Особенности |
|---|---|---|---|
| `GridTextColumn` | `TextBlock` | `TextBox` | Универсальная колонка |
| `GridNumericColumn` | текст с выравниванием вправо | `TextBox` с выравниванием вправо | Для числовых полей |
| `GridCheckBoxColumn` | интерактивный `CheckBox` | — | Изменение применяется сразу по клику; `Space` переключает текущую ячейку |
| `GridComboBoxColumn` | текст выбранного элемента | `ComboBox` | `ItemsSource`, `DisplayMemberPath`, `SelectedValuePath` |
| `GridDateColumn` | текст даты (формат `"d"` по умолчанию) | `DatePicker` | `StringFormat` переопределяет формат отображения |
| `GridTemplateColumn` | ваш `CellTemplate` | ваш `CellEditingTemplate` | Полностью свои ячейки; `CellTemplate` обязателен |

## Общие свойства (GridColumnBase)

| Свойство | По умолчанию | Назначение |
|---|---|---|
| `Header` | `""` | Содержимое заголовка (текст или любой объект) |
| `FieldName` | `""` | Путь привязки внутри элемента строки; составные пути вида `Address.City` поддерживаются |
| `Width` | `1*` | `"120"`, `"Auto"`, `"*"`, `"2*"` — см. ниже |
| `MinWidth` / `MaxWidth` | `0` / `∞` | Ограничения в пикселях для любого режима ширины |
| `Visible` | `true` | Скрыть колонку, не удаляя её |
| `ReadOnly` | `false` | Ячейки колонки не входят в режим правки |
| `CanUserSort` | `true` | Сортировка по клику на заголовок для конкретной колонки |
| `SortMemberPath` | `null` | Сортировать по пути, отличному от `FieldName` |
| `StringFormat` | `null` | Формат отображения, например `"C"` или `"{}{0:N2}"` |
| `HorizontalContentAlignment` | `Left` | Выравнивание содержимого ячейки |
| `ElementStyle` | `null` | Стиль генерируемых элементов ячейки |
| `HeaderStyle` | `null` | Стиль заголовка колонки |
| `CellTemplate` / `CellEditingTemplate` | `null` | Подмена отображения/редактора своим `DataTemplate` |

Метод `GetCellValue(item)` читает значение ячейки элемента по пути колонки.

## Ширины

`Width` принимает три вида единиц (строки XAML конвертируются автоматически):

- **Пиксели** — `Width="120"`: фиксированные DIP-пиксели.
- **Звёздочки** — `Width="*"` или `Width="2*"`: пропорциональная доля оставшейся ширины
  (по умолчанию `1*`).
- **Auto** — `Width="Auto"`: по самому широкому содержимому.

`MinWidth` и `MaxWidth` действуют во всех режимах, включая перетаскивание края заголовка
пользователем.

## Автогенерация и атрибуты

При `AutoGenerateColumns="True"` (по умолчанию) и **пустой** коллекции `Columns` грид при
помощи рефлексии создаёт по колонке на каждое публичное читаемое свойство типа элемента.
Процессом можно управлять атрибутами:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Display(Name = "Номер заказа", Order = 0)]   // текст заголовка и позиция
    public int Number { get; set; }

    [Display(Name = "Сумма", Order = 2)]
    public decimal Total { get; set; }

    [ReadOnly(true)]                              // колонка станет read-only
    public DateTime CreatedAt { get; set; }

    [Browsable(false)]                            // колонки не будет вовсе
    public byte[] Blob { get; set; } = Array.Empty<byte>();
}
```

Правила генератора:

- `[Display]` — `ShortName`/`Name` становится заголовком, `Order` задаёт порядок колонок
  (`[DisplayName]` используется как запасной вариант для текста заголовка);
- `[ReadOnly(true)]` — сгенерированная колонка получает `ReadOnly = true`;
- `[Browsable(false)]` — свойство пропускается;
- Маппинг типов: `bool` → чекбокс, `DateTime`/`DateTimeOffset`/`DateOnly` → дата,
  перечисления → комбобокс (его `ItemsSource` заполняется `Enum.GetValues`), числовые
  типы → числовая, всё остальное → текстовая.

### Событие AutoGeneratingColumn

Возникает для каждой сгенерированной колонки; колонку можно заменить или отменить:

```csharp
grid.AutoGeneratingColumn += (s, e) =>
{
    if (e.Column.FieldName == "Blob")
    {
        e.Cancel = true;                      // пропустить эту колонку
        return;
    }

    if (e.Column is GridNumericColumn { FieldName: "Salary" })
    {
        e.Column.StringFormat = "C";          // донастроить сгенерированную колонку
    }
};
```

## Ручные колонки

Определите колонки явно в XAML — пока `Columns` не пуст, автогенерация не включается:

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  FrozenColumnCount="1"
                  CanUserResizeColumns="True"
                  CanUserReorderColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="Имя" Width="2*" MinWidth="120" />
        <columns:GridNumericColumn FieldName="Salary" Header="Зарплата" Width="110"
                                   StringFormat="C" HorizontalContentAlignment="Right" />
        <columns:GridDateColumn FieldName="HireDate" Header="Дата приёма" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="Активен" Width="70" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

## Закрепление

`FrozenColumnCount` (свойство грида, а не колонки) закрепляет первые N колонок при
горизонтальной прокрутке. Отрицательные значения отклоняются; скрытые колонки не
считаются. Вместе с `CanUserReorderColumns` это позволяет пользователю перетащить нужную
колонку в начало и закрепить её, увеличив счётчик.

## Шаблоны

Используйте `GridTemplateColumn` (или `CellTemplate`/`CellEditingTemplate` на любой
колонке), когда встроенных элементов недостаточно. `DataContext` шаблона — элемент данных
строки:

```xml
<columns:GridTemplateColumn FieldName="Progress" Header="Прогресс" Width="160">
    <columns:GridTemplateColumn.CellTemplate>
        <DataTemplate>
            <ProgressBar Value="{Binding Progress}" Maximum="100" Height="14" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellTemplate>
    <columns:GridTemplateColumn.CellEditingTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Progress, UpdateSourceTrigger=PropertyChanged}" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellEditingTemplate>
</columns:GridTemplateColumn>
```

Обратите внимание: с `CellEditingTemplate` грид не записывает значения сам — элемент данных
обновляют привязки вашего шаблона (подробнее в [Редактировании](editing.md)).

## Колонки-комбобоксы

`GridComboBoxColumn` выбирает режим привязки по наличию `SelectedValuePath`:

- **Без `SelectedValuePath`** — значением ячейки является весь элемент списка; комбобокс
  привязывается через `SelectedItem`.
- **С `SelectedValuePath`** — значением ячейки является ключ; комбобокс привязывается через
  `SelectedValue`, а ячейка отображения превращает ключ в текст по `DisplayMemberPath`.

```xml
<columns:GridComboBoxColumn FieldName="DepartmentId" Header="Отдел"
                            ItemsSource="{Binding Departments}"
                            DisplayMemberPath="Name"
                            SelectedValuePath="Id" Width="180" />
```
