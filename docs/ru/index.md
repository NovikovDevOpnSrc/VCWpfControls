---
title: Обзор GridControl
description: VCWpfControls — бесплатная WPF-библиотека контролов под лицензией MIT. Этап 1 — GridControl, быстрая виртуализованная таблица данных для .NET 6/8 на Windows.
---

# Обзор GridControl

**Vibe Coded WPF Controls** — бесплатная библиотека WPF-контролов с открытым кодом под
лицензией [MIT](https://github.com/NovikovDevOpnSrc/VCWpfControls/blob/main/LICENSE).
**Этап 1** — **GridControl**: быстрая, виртуализованная, дружественная к MVVM таблица данных.

Грид привязывается к любому `IEnumerable`, генерирует колонки по типу элемента (или берёт
ручные определения), виртуализирует контейнеры строк с рециклингом, а его движок данных
(`GridDataManager`) не зависит от UI и легко покрывается unit-тестами.

## Возможности

- **Привязка данных** — любой `IEnumerable`, живые обновления через `ObservableCollection<T>`
- **Колонки** — текстовая, числовая, чекбокс, комбобокс, дата, шаблонная; автогенерация по
  атрибутам `[Display]`/`[ReadOnly]`/`[Browsable]` или ручное определение
- **Компоновка** — изменение ширины, перетаскивание, скрытие и закрепление колонок
  (`FrozenColumnCount`); ширины в пикселях, звёздочках (`2*`) или `Auto`
- **Виртуализация** — UI-виртуализация с рециклингом контейнеров; 1 000 000+ строк
  скроллятся плавно
- **Сортировка** — по одной и нескольким колонкам, клик и Shift+клик, индикаторы
  направления, свои компараторы
- **Фильтрация** — программный API `FilterDescriptions` с операторами
  Equals/Contains/Between/In/IsNull/Custom + встроенная строка фильтров (`ShowFilterRow`)
- **Группировка** — программный API `GroupDescriptions` со сворачиванием групп, состояние
  которых переживает обновления (групповая панель и агрегатные итоги — этап 1.2)
- **Редактирование** — F2/Enter/Esc, read-only на уровне колонки, валидация через
  `IDataErrorInfo` / `INotifyDataErrorInfo`
- **Выделение** — режимы SingleRow/Extended, полная поддержка клавиатуры, текущая ячейка
- **Буфер обмена** — Ctrl+C копирует выделение как текст с табуляцией
- **Темы** — тема по умолчанию построена на ресурсах `ComponentResourceKey`, которые можно
  переопределять в приложении

Также реализованы: строка фильтров, итоги, экспорт (CSV/Excel/HTML), тёмная тема и локализация. Дальше —
смотрите [дорожную карту](https://github.com/NovikovDevOpnSrc/VCWpfControls/blob/main/ROADMAP.md).

## Быстрый старт (XAML)

```xml
<Window x:Class="Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
        Title="Сотрудники">

    <grid:GridControl ItemsSource="{Binding Employees}"
                      AutoGenerateColumns="True"
                      CanUserSortColumns="True"
                      SelectionMode="Extended" />
</Window>
```

При `AutoGenerateColumns="True"` (по умолчанию) и пустой коллекции `Columns` грид создаст
типизированную колонку для каждого публичного читаемого свойства типа элемента.

## Быстрый старт (C#)

```csharp
using System.Collections.ObjectModel;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;

public class Employee
{
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

// Во вью-модели или code-behind окна:
var employees = new ObservableCollection<Employee>
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob", Salary = 87000m, HireDate = new DateTime(2022, 7, 15), },
};

var grid = new GridControl { ItemsSource = employees };

// Сортировка и фильтрация из кода — через коллекции описаний:
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.FilterDescriptions.Add(
    new GridFilterDescription(nameof(Employee.Salary), GridFilterOperator.GreaterThanOrEqual, 90000m));
```

## Что читать дальше

- [Начало работы](getting-started.md) — установка, требования, первый грид
- [Колонки](columns.md) — типы колонок, автогенерация, ширины, шаблоны
- [Сортировка, фильтрация и группировка](sorting-filtering-grouping.md) — API движка данных
- [Редактирование](editing.md) — жизненный цикл правки, валидация, `GridCommands`
- [Выделение](selection.md) — режимы выделения и текущая ячейка
- [Темы оформления](theming.md) — ключи ресурсов и их переопределение
- [Рецепты](cookbook.md) — готовые приёмы
