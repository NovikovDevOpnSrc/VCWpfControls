---
title: 透视引擎
description: 用无 UI 的 PivotEngine 从任意 IEnumerable 计算透视表（行、列、度量）。
---

# 透视引擎与 PivotGrid

## 控件

```xml
<pivot:PivotGrid PivotItemsSource="{Binding Sales}" RowHeight="28">
    <!-- 字段在 code-behind 中添加： -->
</pivot:PivotGrid>
```

```csharp
pivot.RowFields.Add("Region");           // 前导列，按顺序
pivot.ColumnFields.Add("Product");       // 每个组合一列数据
pivot.Measures.Add(new PivotMeasure("Amount", GridSummaryFunction.Sum));
```

控件继承自 `GridControl`：主题、表头 UI、排序与虚拟化直接复用。渲染：每个行字段一
个前导列；每个列字段组合一个星号数据列（多度量时标题为 `A / B · Sum`）；本地化的
**总计**列与总计行（`IncludeTotals`，默认 true）。修改 `RowFields`/`ColumnFields`/
`Measures` 集合或 `PivotItemsSource` 会立即重算并重建；`Result` 公开最近的
`PivotResult`。

## 引擎（无 UI，第一阶段）

`PivotEngine` 从任意非泛型 `IEnumerable` 计算透视表——按行/列字段值分组，并用共享的
页脚计算器聚合每个交叉单元格。第一阶段无 UI（无需 STA 的单元测试）；渲染这些结果的
`PivotGrid` 控件属于下一阶段。

```csharp
var result = PivotEngine.Compute(
    sales,
    rowFields: new[] { "Region", "City" },      // 复合行键，升序
    columnFields: new[] { "Product" },
    measures: new[] { new PivotMeasure("Amount", GridSummaryFunction.Sum) },
    includeTotals: true);

var value = result.Cells[rowIndex][columnIndex][measureIndex];
var rowTotal = result.RowTotals[rowIndex][measureIndex];
```

- **度量**：对某字段求 Sum/Min/Max/Avg/Count（`PivotMeasure`；每格可有多度量）。
  聚合结果会还原为源类型（decimal 字段得到 decimal 求和）。
- **轴**：多个行/列字段构成复合键；升序排序，null 在前；字段值为 `null` 自成一组
  （空标题）。
- **交叉单元格**：桶聚合该（行，列）键下的**所有**源行；空交叉为 null（Count 为 0）。
- **总计**（`includeTotals`，默认 true）：`RowTotals`、`ColumnTotals`、`GrandTotals`
  沿另一轴聚合；传 false 可省去计算。
