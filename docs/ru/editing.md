---
title: Редактирование
description: Жизненный цикл правки ячеек GridControl (F2/Enter/Esc, двойной клик), события CellEdit*, валидация IDataErrorInfo/INotifyDataErrorInfo и маршрутизируемые команды GridCommands.
---

# Редактирование

## Включение редактирования

Редактирование включено по умолчанию (`IsReadOnly = false`) для колонок, у которых есть
редактор. Отключите его на уровне грида или колонки:

```xml
<grid:GridControl ItemsSource="{Binding Orders}" IsReadOnly="False">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Number" Header="Заказ" ReadOnly="True" />
        <columns:GridNumericColumn FieldName="Quantity" Header="Кол-во" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

`ReadOnly` колонки сильнее настройки грида: read-only колонка не войдёт в режим правки,
даже если грид редактируемый. Отдельно `CanUserDeleteRows` (по умолчанию `true`) разрешает
или запрещает удаление выделенных строк клавишей Delete.

## Жизненный цикл правки

| Действие | Результат |
|---|---|
| Двойной клик по ячейке / `F2` | Начать правку текущей ячейки |
| `Enter` | Записать значение через привязку |
| `Esc` | Отменить правку и вернуть прежнее значение |
| Клик по другой ячейке / стрелки во время правки | Сначала правка фиксируется |

Эквивалентный API: `BeginEdit()`, `BeginEdit(item, column)`, `CommitEdit()`,
`CancelEdit()`; свойство `IsEditing` сообщает, находится ли ячейка в режиме правки.

```csharp
grid.BeginEdit(selectedItem, salaryColumn);
// ...
if (!grid.CommitEdit())
{
    // валидация не прошла — ячейка остаётся в режиме правки
}
```

Ячейки-чекбоксы устроены иначе: они интерактивны без входа в режим правки и применяют
изменение сразу по клику; `Space` переключает чекбокс текущей ячейки.

## События

Жизненный цикл покрывают три события. Все используют `GridCellEditEventArgs` с полями
`Row` (элемент данных), `Column`, `EditingElement` (может быть `null` в
`CellEditStarting`), `CancelEdit` (true в `CellEditEnding`/`CellEditEnded`, когда правка
отменяется) и флагом `Cancel`, позволяющим отменить действие.

```csharp
// Запретить правку для отгружённых заказов:
grid.CellEditStarting += (s, e) =>
{
    if (e.Column.FieldName == "Quantity" && ((Order)e.Row).IsShipped)
    {
        e.Cancel = true;
    }
};

// Последний шанс вмешаться в фиксацию значения:
grid.CellEditEnding += (s, e) =>
{
    if (e.CancelEdit) return;                 // была нажата Esc
    var box = e.EditingElement as TextBox;
    if (box?.Text.Contains("зло") == true)
    {
        e.Cancel = true;                      // продолжаем редактировать
    }
};

// Пост-фактум:
grid.CellEditEnded += (s, e) =>
{
    if (!e.CancelEdit)
    {
        Logger.LogInformation("Зафиксировано поле {Field}", e.Column.FieldName);
    }
};
```

## Валидация

Двусторонние привязки, создаваемые гридом, включают `ValidatesOnDataErrors`,
`ValidatesOnNotifyDataErrors` и `ValidatesOnExceptions`. Это значит, что ваши существующие
модели с `IDataErrorInfo` / `INotifyDataErrorInfo` работают без изменений:

```csharp
public class Order : IDataErrorInfo
{
    public int Quantity { get; set; }

    public string Error => this[nameof(Quantity)];

    public string this[string columnName]
        => columnName == nameof(Quantity) && Quantity <= 0
            ? "Количество должно быть положительным"
            : string.Empty;
}
```

При фиксации грид вызывает `UpdateSource()` у привязки и проверяет
`Validation.GetHasError`. Если значение невалидно, **ячейка остаётся в режиме правки**,
чтобы пользователь исправил ошибку; `CommitEdit()` возвращает `false`. Ошибки отображаются
стандартным WPF-шаблоном валидации элемента редактирования.

С `CellEditingTemplate` грид не записывает значения сам — элемент данных обновляют
привязки вашего шаблона (например, с `UpdateSourceTrigger=PropertyChanged`), а фиксация
сводится к завершению сессии правки.

## GridCommands

`GridCommands` содержит маршрутизируемые команды, уже подключённые к контролу (включая
жесты по умолчанию), поэтому кнопки тулбара или меню интегрируются без code-behind:

| Команда | Жест по умолчанию | Действие |
|---|---|---|
| `GridCommands.BeginEdit` | `F2` | Начать правку текущей ячейки |
| `GridCommands.CancelEdit` | `Esc` | Отменить текущую правку |
| `GridCommands.CommitEdit` | `Enter` | Зафиксировать текущую правку |
| `GridCommands.Delete` | `Delete` | Удалить выделенные строки |
| `GridCommands.SelectAll` | `Ctrl+A` | Выделить все строки (режим Extended) |
| `GridCommands.Copy` | `Ctrl+C` | Скопировать выделение в буфер обмена |

```xml
<StackPanel Orientation="Horizontal">
    <Button Content="Править (F2)"
            Command="grid:GridCommands.BeginEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="Сохранить (Enter)"
            Command="grid:GridCommands.CommitEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="Удалить"
            Command="grid:GridCommands.Delete"
            CommandTarget="{Binding ElementName=Grid}" />
</StackPanel>
```

`CommandTarget` должен указывать на грид: иначе маршрутизируемая команда выполнится на
элементе в фокусе.

## Удаление строк

`DeleteSelectedItems()` (и клавиша Delete) удаляет выделенные элементы **из коллекции-
источника** — это работает, только если `ItemsSource` реализует `IList` (например,
`ObservableCollection<T>`), `CanUserDeleteRows` равен `true` и грид не в `IsReadOnly`.
Сначала выделение сбрасывается; удаление возвращается в грид через
`INotifyCollectionChanged`.
