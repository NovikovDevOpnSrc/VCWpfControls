---
title: Pivot engine
description: Compute pivot tables (rows, columns, measures) from any IEnumerable with the UI-free PivotEngine.
---

# Pivot engine (stage 1: headless)

The `PivotEngine` computes pivot tables from any non-generic `IEnumerable` — grouping by
row/column field values and aggregating each intersection with the shared footer
calculator. Stage 1 is UI-free (unit-tested without STA); a `PivotGrid` control rendering
these results is the next stage.

```csharp
var result = PivotEngine.Compute(
    sales,
    rowFields: new[] { "Region", "City" },      // composite rows, ascending order
    columnFields: new[] { "Product" },
    measures: new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) },
    includeTotals: true);

var value = result.Cells[rowIndex][columnIndex][measureIndex];
var rowTotal = result.RowTotals[rowIndex][measureIndex];
```

- **Measures**: Sum/Min/Max/Avg/Count over a field (`PivotMeasure`; multiple measures per
  cell). Aggregates round back to the source-like type (a decimal field yields decimal
  sums).
- **Axes**: multiple row/column fields produce composite keys; values sort ascending with
  nulls first; `null` field values form their own group (empty caption).
- **Intersections**: buckets aggregate ALL source rows with that (row, column) key;
  empty intersections yield null (Count yields 0).
- **Totals** (`includeTotals`, default true): `RowTotals`, `ColumnTotals`, `GrandTotals`
  aggregate across the opposite axis; pass false to save the work.
