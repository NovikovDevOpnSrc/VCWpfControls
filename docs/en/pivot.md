---
title: Pivot engine
description: Compute pivot tables (rows, columns, measures) from any IEnumerable with the UI-free PivotEngine.
---

# Pivot engine and PivotGrid

## The control

```xml
<pivot:PivotGrid PivotItemsSource="{Binding Sales}" RowHeight="28">
    <!-- fields are added in code or code-behind: -->
</pivot:PivotGrid>
```

```csharp
pivot.RowFields.Add("Region");           // leading columns, in order
pivot.ColumnFields.Add("Product");       // one data column per combination
pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
```

The control inherits `GridControl`: themes, header UI, sorting and virtualization come for
free. It renders one leading column per row field, one star-sized data column per
column-field combination (caption `A / B · Sum` when there are several measures), a
localized **Total** column and a grand-total row (`IncludeTotals`, default true — the
engine computes totals only when needed). Changing `RowFields`/`ColumnFields`/`Measures`
collections or `PivotItemsSource` recomputes and rebuilds immediately; `Result` exposes
the last `PivotResult`.

## Drill-down, dimension filters, CSV (stage 3)

- **Drill-down**: `PivotGrid.GetDrillDownRows(rowIndex, columnIndex)` returns the source
  rows aggregated into a cell (the engine keeps per-intersection buckets when the grid
  computes); double-clicking a data cell raises `DrillDownRequested` with
  `Row`/`Column`/`SourceRows`/`Aggregates` — show them in a details grid.
- **Dimension filter**: `PivotGrid.FieldFilter` — `Func<string fieldName, object? value>`
  applied to every row/column field before bucketing; return false to exclude the source
  row. Totals aggregate the INCLUDED rows only. Changing the filter recomputes.
- **CSV**: `PivotExporter.ToCsv(result, culture?, rowFieldNames?)` — headers from the row
  field names and column captions, values quoted when needed, list separator follows the
  culture, a grand-total row closes the table (when totals are present).

## The engine (headless, stage 1)

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

