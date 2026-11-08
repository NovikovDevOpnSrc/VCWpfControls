---
title: Движок сводных таблиц
description: Вычисление сводных таблиц (строки, столбцы, меры) из любого IEnumerable через UI-free PivotEngine.
---

# Движок сводных таблиц и PivotGrid

## Контрол

```xml
<pivot:PivotGrid PivotItemsSource="{Binding Sales}" RowHeight="28">
    <!-- поля добавляются в code-behind: -->
</pivot:PivotGrid>
```

```csharp
pivot.RowFields.Add("Region");           // ведущие колонки, по порядку
pivot.ColumnFields.Add("Product");       // колонка данных на каждую комбинацию
pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
```

Контрол наследует `GridControl`: темы, UI заголовков, сортировка и виртуализация достаются
бесплатно. Рендер: ведущая колонка на каждое поле строк; звёздная колонка данных на
комбинацию полей столбцов (заголовок `A / B · Sum` при нескольких мерах); локализованная
колонка **Итого** и итоговая строка (`IncludeTotals`, по умолчанию true — движок считает
итоги только при включении). Изменение коллекций `RowFields`/`ColumnFields`/`Measures` или
`PivotItemsSource` мгновенно пересчитывает и перестраивает таблицу; `Result` отдаёт
последний `PivotResult`.

## Drill-down, фильтры измерений, CSV (этап 3)

- **Drill-down**: `PivotGrid.GetDrillDownRows(rowIndex, columnIndex)` возвращает строки
  источника, агрегированные в ячейку (движок хранит корзины пересечений, когда считает
  грид); двойной клик по ячейке данных поднимает `DrillDownRequested` с
  `Row`/`Column`/`SourceRows`/`Aggregates` — покажите их в детальном гриде.
- **Фильтр измерений**: `PivotGrid.FieldFilter` — `Func<string fieldName, object? value>`,
  применяется к каждому полю строк/столбцов до группировки; верните false, чтобы исключить
  строку источника. Итоги агрегируют только ВКЛЮЧЁННЫЕ строки. Смена фильтра — пересчёт.
- **CSV**: `PivotExporter.ToCsv(result, culture?, rowFieldNames?)` — заголовки из имён
  полей строк и подписей колонок, значения экранируются при необходимости, разделитель —
  по культуре, итоговая строка завершает таблицу (когда есть итоги).

## Движок (без UI, этап 1)

`PivotEngine` вычисляет сводные таблицы из любого неуниверсального `IEnumerable` —
группировка по значениям полей строк/столбцов и агрегация каждого пересечения общим
калькулятором итогов. Этап 1 — без UI (unit-тесты без STA); контрол `PivotGrid`,
отображающий результаты, — следующий этап.

```csharp
var result = PivotEngine.Compute(
    sales,
    rowFields: new[] { "Region", "City" },      // составные строки, по возрастанию
    columnFields: new[] { "Product" },
    measures: new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) },
    includeTotals: true);

var value = result.Cells[rowIndex][columnIndex][measureIndex];
var rowTotal = result.RowTotals[rowIndex][measureIndex];
```

- **Меры**: Sum/Min/Max/Avg/Count по полю (`PivotMeasure`; несколько мер в ячейке).
  Агрегаты приводятся к типу источника (decimal-поле даёт decimal-суммы).
- **Оси**: несколько полей строк/столбцов дают составные ключи; сортировка по
  возрастанию, null-ы первыми; `null` значения полей образуют свою группу (пустая
  подпись).
- **Пересечения**: корзины агрегируют ВСЕ строки источника с данным (строка, столбец)
  ключом; пустые пересечения дают null (Count — 0).
- **Итоги** (`includeTotals`, по умолчанию true): `RowTotals`, `ColumnTotals`,
  `GrandTotals` агрегируются по противоположной оси; false — чтобы не считать.
