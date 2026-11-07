---
title: Движок сводных таблиц
description: Вычисление сводных таблиц (строки, столбцы, меры) из любого IEnumerable через UI-free PivotEngine.
---

# Движок сводных таблиц (этап 1: без UI)

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
