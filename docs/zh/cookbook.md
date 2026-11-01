---
title: 实用示例
description: 现成的 GridControl 配方 —— 海量数据、货币格式、复选框与枚举下拉列、剪贴板复制、拦截 Sorting、滚动到指定行。
---

# 实用示例

针对常见任务的简短完整配方。所有片段都使用以下命名空间：

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## 1. 数十万行数据

带容器回收的虚拟化始终开启。通过固定行高、并尽量避免自动宽度列，可以充分发挥
其性能：

```xml
<grid:GridControl x:Name="Grid"
                  ItemsSource="{Binding Trades}"
                  RowHeight="24"
                  HeaderHeight="28"
                  FrozenColumnCount="2"
                  CanUserSortColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Symbol" Header="代码" Width="90" />
        <columns:GridTextColumn FieldName="Side" Header="方向" Width="60" />
        <columns:GridNumericColumn FieldName="Price" Header="价格" Width="110" StringFormat="N4" />
        <columns:GridNumericColumn FieldName="Quantity" Header="数量" Width="100" />
        <columns:GridDateColumn FieldName="Timestamp" Header="时间" Width="140" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

超大数据源的注意事项：

- 阶段 1 中，引擎在每次集合变更时都会重建完整管线（筛选 → 排序 → 分组）；
  建议批量更新（填好 `ObservableCollection` 后调用 `Refresh()`），而不是逐项发送
  通知。增量和异步处理已列入阶段 1.2 计划。
- 百万行级别的代码排序就是 `grid.SortBy(...)`；如何监听排序见配方 6。

## 2. 货币与数字格式

任意列上的 `StringFormat`，与 `Binding.StringFormat` 完全一致：

```xml
<columns:GridNumericColumn FieldName="Salary" Header="薪资" Width="120"
                           StringFormat="C" />          <!-- 当前区域设置的货币 -->
<columns:GridNumericColumn FieldName="Ratio" Header="占比" Width="90"
                           StringFormat="{}{0:P1}" />   <!-- 百分比，1 位小数 -->
<columns:GridDateColumn FieldName="HireDate" Header="入职日期" Width="120"
                        StringFormat="yyyy-MM-dd" />    <!-- 日期列默认为 "d" -->
```

## 3. 点击即提交的复选框列

`GridCheckBoxColumn` 无需进入编辑模式即可交互，并立即通过双向绑定写回值。`Space`
切换当前单元格，`ReadOnly` 可将其禁用：

```xml
<columns:GridCheckBoxColumn FieldName="IsActive" Header="在职" Width="70" />
<columns:GridCheckBoxColumn FieldName="IsArchived" Header="已归档" Width="80" ReadOnly="True" />
```

## 4. 绑定枚举的下拉框列

手动定义（适用于任何查找表）：

```xml
<columns:GridComboBoxColumn FieldName="Status" Header="状态" Width="140"
                            ItemsSource="{Binding Statuses}"
                            DisplayMemberPath="Label"
                            SelectedValuePath="Id" />
```

枚举则什么都不用做：自动生成会把枚举属性映射为 `GridComboBoxColumn`，其
`ItemsSource` 由 `Enum.GetValues(type)` 填充 —— 单元格显示值名称，下拉框开箱即用：

```csharp
public enum OrderStatus { Draft, Confirmed, Shipped, Closed }

public class Order
{
    public OrderStatus Status { get; set; }   // 自动生成为下拉框列
}
```

## 5. 复制到剪贴板（对 Excel 友好）

`Ctrl+C`（或 `GridCommands.Copy`、`CopyToClipboard()`）把所选行 —— 未选择任何内容时
为当前行 —— 复制为制表符分隔文本。默认的 `ClipboardCopyMode="IncludeHeaders"` 会让
第一行包含表头，因此粘贴到 Excel/表格即得到整齐的表格：

```xml
<grid:GridControl ItemsSource="{Binding Orders}" ClipboardCopyMode="IncludeHeaders" />
```

```csharp
grid.SelectRange(firstOrder, lastOrder);
grid.CopyToClipboard();
```

设为 `ClipboardCopyMode="None"` 可完全禁用复制。

## 6. 拦截排序

可以取消、限制，或仅仅观察：

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Timestamp" && e.NewDirection == GridSortDirection.Descending)
    {
        e.Cancel = true;   // 禁止时间列按降序排序
    }
};

grid.Sorted += (s, e) =>
{
    StatusText = $"已按 {string.Join(", ", grid.SortDescriptions.Select(d => d.FieldName))} 排序";
};
```

编程式排序无需鼠标：`SortBy(...)` 替换所有级别，`AddSortLevel(...)` 追加级别，
`ClearSorting()` 重置；如果更愿意在视图模型中驱动，`SortDescriptions` 本身也支持
数据绑定。

## 7. 滚动到某行并高亮

```csharp
// 加载或搜索之后：
grid.Refresh();                                   // 数据被外部修改时重建视图
grid.SortBy(nameof(Customer.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(customer);                    // 让该行进入可视区
grid.UnselectAll();
grid.SelectItem(customer);
grid.CurrentItem = customer;                      // 把当前单元格定位到该行
```

## 8. 使用自定义谓词筛选

当 17 个内置运算符不够用时，`Custom` 运算符接受作用于整个数据项的谓词。可与普通
筛选器组合（它们按逻辑与叠加）：

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription(nameof(Trade.Symbol),
    GridFilterOperator.NotEquals, "CANCELLED"));

grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Trade t && t.Quantity * t.Price > 10_000m,
});
```

界面上的迷你搜索框只需在每次输入时替换 `Contains` 筛选器：

```csharp
_searchFilter = new GridFilterDescription(nameof(Customer.Name), GridFilterOperator.Contains, text);
// 文本变化时：
grid.FilterDescriptions.Clear();
grid.FilterDescriptions.Add(_searchFilter);
```
