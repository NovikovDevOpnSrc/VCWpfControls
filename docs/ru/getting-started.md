---
title: Начало работы
description: Установка VCWpfControls из NuGet, системные требования (.NET 6/8, Windows) и создание первой таблицы за несколько минут.
---

# Начало работы

## Системные требования

| Требование | Детали |
|---|---|
| ОС | Windows (WPF работает только на Windows) |
| Платформа | .NET 6 или .NET 8 (`net6.0-windows` / `net8.0-windows`) |
| Пакет | `VCWpfControls` (NuGet) |

Библиотека — обычная WPF-библиотека контролов: без нативных зависимостей и внешних пакетов.

## Установка

Установите пакет NuGet:

```
dotnet add package VCWpfControls
```

Или добавьте ссылку в `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="VCWpfControls" Version="*" />
</ItemGroup>
```

Тема по умолчанию (`Themes/Generic.xaml`) поставляется внутри пакета и подхватывается
автоматически — вызывать `MergeDictionaries` не нужно.

## Первый грид

Опишите модель. Атрибуты данных управляют автогенерацией колонок (полный список — в разделе
[Колонки](columns.md)):

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Employee
{
    [Display(Name = "Имя", Order = 0)]
    public string Name { get; set; } = "";

    [Display(Name = "Зарплата", Order = 1)]
    public decimal Salary { get; set; }

    [Display(Name = "Дата приёма", Order = 2)]
    public DateTime HireDate { get; set; }

    [Display(Name = "Активен", Order = 3)]
    public bool IsActive { get; set; }
}
```

Выдайте данные как `ObservableCollection<Employee>`, чтобы грид видел добавления и удаления:

```csharp
public ObservableCollection<Employee> Employees { get; } = new()
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob",   Salary = 87000m, HireDate = new DateTime(2022, 7, 15), IsActive = false },
};
```

Положите грид в окно и привяжите `ItemsSource`:

```xml
<Window xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <grid:GridControl ItemsSource="{Binding Employees}" />
</Window>
```

Этого достаточно: колонки сгенерируются по `Employee`, клик по заголовку сортирует,
двойной клик по ячейке (или F2) начинает редактирование, Ctrl+C копирует выделение.

## Полезные свойства в первые дни

| Свойство | По умолчанию | Назначение |
|---|---|---|
| `AutoGenerateColumns` | `true` | Генерировать колонки, пока `Columns` пуст |
| `IsReadOnly` | `false` | Запретить редактирование во всём гриде |
| `SelectionMode` | `Extended` | `SingleRow` или `Extended` (Ctrl/Shift) |
| `RowHeight` | `NaN` (авто) | Фиксированная высота строк выжимает максимум из виртуализации |
| `FrozenColumnCount` | `0` | Первые N колонок закреплены при горизонтальном скролле |
| `ClipboardCopyMode` | `IncludeHeaders` | `None` отключает Ctrl+C |
| `CanUserDeleteRows` | `true` | Delete удаляет выделенные строки (источник должен быть `IList`) |

## Живые обновления

Если `ItemsSource` реализует `INotifyCollectionChanged` (например,
`ObservableCollection<T>`), грид перестраивает представление при каждом изменении.
Учтите: на этапе 1 каждое изменение перестраивает весь конвейер
фильтрация → сортировка → группировка; инкрементальные и асинхронные обновления для очень
больших коллекций запланированы на этап 1.2.

Чтобы принудительно полностью перестроить представление (например, после массовых правок
без уведомлений коллекции), вызовите:

```csharp
grid.Refresh();
```

## Дальше

- Явное определение колонок — [Колонки](columns.md)
- Сортировка, фильтрация и группировка из кода — [Соответствующий раздел](sorting-filtering-grouping.md)
- Стилизация грида под приложение — [Темы оформления](theming.md)
