---
title: 排序、筛选与分组
description: 如何在代码中对 GridControl 排序（点击、Shift+点击、SortBy/AddSortLevel）、筛选（带 17 种运算符的 FilterDescriptions）与分组（GroupDescriptions、GridGroup、SetGroupExpanded）。
---

# 排序、筛选与分组

这三个功能都由与 UI 无关的数据引擎（`GridDataManager`）实现，并受表格上的三个
可观察集合驱动：`SortDescriptions`、`FilterDescriptions` 和 `GroupDescriptions`。
集合一旦变化，视图立即重建（管线：筛选 → 排序 → 分组 → 展平）。

> **状态说明。** 本文描述的引擎 API 完全可用。内置的**筛选行/筛选菜单**以及
> **内置 UI**：设置 `ShowFilterRow="True"` 即可在表头下方获得筛选行（文本列为包含匹配，数值/日期列为类型化相等）；分组行自带展开/折叠开关；通过 `SummaryItems` 配置页脚汇总。以下引擎 API 与 UI 无关。基于
> 这些集合构建自己的 UI。

> **内置 UI**：设置 `ShowFilterRow="True"` 即可在表头下方获得筛选行（文本列为包含匹配，
> 数值/日期列为类型化相等；输入后有 300 毫秒防抖）。每个列标题都有漏斗按钮，可打开
> 值列表筛选菜单（搜索框 + 唯一值复选列表，含“全选/清除/确定”）。数值列和日期列
> 在列表上方还有 **从/到** 输入框：两端都填应用 `Between`，只填一端应用 `>=` / `<=`
> （按当前区域设置解析；范围优先于复选列表，重新打开菜单会回填当前范围）。
> 将列标题拖到表头上方的**分组面板**即可按该列分组（默认 `CanUserGroupColumns="True"`）；
> 面板中的标签可移除分组级别。分组行自带展开/折叠开关；通过 `SummaryItems` 配置页脚
> 汇总（`Sum`/`Min`/`Max`/`Avg`/`Count`）。以下引擎 API 与 UI 无关。

## 性能模式

- **增量更新**：`ObservableCollection` 中的单条 `Add`/`Remove`/`Replace` 不会整体重建
  视图——引擎按当前排序二分查找插入位置，并跳过被过滤掉的行（分组视图仍整体重建）。
  选中项、当前单元格和汇总会同步更新。
- **后台刷新**：`UseAsyncRefresh="True"` 将过滤/排序/分组移到线程池执行，并在 UI
  线程应用结果。计算期间状态已过期的结果会被丢弃并重算，因此连续操作最终一定收敛
  到最新状态。演示页“Virtualization (100k rows)”提供了开关体验差异。

## 排序

### 鼠标与键盘

点击表头按循环切换排序方向：无 → 升序 → 降序 → 无。**Shift+点击**会把该列追加为
下一个排序级别，而不是替换现有排序（多列排序）。当前方向由表头中的指示器显示。

以上两种操作都可以通过 `Sorting` 事件观测或取消：

```csharp
grid.Sorting += (s, e) =>
{
    if (e.Column.FieldName == "Total")
    {
        e.Cancel = true; // 保护该列的服务端排序顺序
    }
};

grid.Sorted += (s, e) => { /* 排序已应用，例如更新状态栏 */ };
```

### 通过代码

```csharp
// 替换所有排序级别：
grid.SortBy(nameof(Employee.Name), GridSortDirection.Descending);

// 或传入列实例（使用 SortMemberPath ?? FieldName）：
grid.SortBy(nameColumn, GridSortDirection.Ascending);

// 添加/更新一个级别，保留其他级别（多列排序）：
grid.AddSortLevel(nameof(Employee.Department), GridSortDirection.Ascending);
grid.AddSortLevel(nameof(Employee.Salary), GridSortDirection.Descending);

// 移除所有级别：
grid.ClearSorting();
```

`SortDescriptions` 是 `ObservableCollection<GridSortDescription>`，因此也可以直接
编辑。每个 `GridSortDescription` 包含：

- `FieldName` —— 属性路径（`"Address.City"` 也合法）；
- `Direction` —— `Ascending` / `Descending` / `None`；
- `Comparer` —— 可选的自定义 `IComparer`，用于比较单元格值（替换默认的区域感知
  比较；方向仍由引擎应用）；
- `Culture` —— 字符串比较所用的区域（默认 `CurrentCulture`）。

```csharp
grid.SortDescriptions.Add(new GridSortDescription("Salary", GridSortDirection.Descending)
{
    Comparer = Comparer<object>.Create((a, b) => ((decimal)a!).CompareTo((decimal)b!)),
});
```

列可以通过 `SortMemberPath` 按其他成员排序；将列的 `CanUserSort = false` 可仅对该列
禁用点击表头排序。

## 筛选

`FilterDescriptions` 是 `GridFilterDescription` 的集合；多个筛选器按**逻辑与**组合。

```csharp
using VCWpfControls.Grid.Data;

// 薪资在 50,000 到 90,000 之间：
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Salary), GridFilterOperator.Between, 50000m, 90000m));

// 姓名包含 "al"（默认区域感知、不区分大小写）：
grid.FilterDescriptions.Add(new GridFilterDescription(
    nameof(Employee.Name), GridFilterOperator.Contains, "al"));

// 移除一个 / 全部筛选器：
grid.FilterDescriptions.RemoveAt(0);
grid.FilterDescriptions.Clear();
```

### 运算符

| 运算符 | 含义 | 使用的操作数 |
|---|---|---|
| `None` | 禁用筛选 | — |
| `Equals`、`NotEquals` | （不）等于 `Value` | `Value` |
| `Contains`、`NotContains`、`StartsWith`、`EndsWith` | 文本匹配（默认不区分大小写） | `Value` 作为文本 |
| `GreaterThan`、`GreaterThanOrEqual`、`LessThan`、`LessThanOrEqual` | 比较运算 | `Value` |
| `Between`、`NotBetween` | 位于/位于闭区间之外 | `Value` 与 `Value2` |
| `In`、`NotIn` | 值（不）包含在列表中（`Value` 为 `IEnumerable`） | `Value` |
| `IsNull`、`IsNotNull` | 判空（或 `DBNull`） | — |
| `Custom` | 作用于整个数据项的自定义谓词 | `CustomPredicate` |

`GridFilterDescription` 的其他配置项：

- `TextComparison` —— 文本运算符使用的 `StringComparison`（默认
  `CurrentCultureIgnoreCase`）；
- `CustomPredicate` —— `Custom` 运算符使用的 `Predicate<object>`：

```csharp
grid.FilterDescriptions.Add(new GridFilterDescription("...")
{
    Operator = GridFilterOperator.Custom,
    CustomPredicate = item => item is Employee e && e.IsActive && e.Salary > 50000m,
});
```

null 单元格只会命中显式的 `IsNull` 运算符（以及包含 `null` 的 `In` 列表），
因此组合筛选器的行为是可预期的。

## 分组

`GroupDescriptions` 是 `GridGroupDescription` 的集合，从最外层到最内层：

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Department)));
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Employee.Office)));
```

启用分组后，表格视图会以**分组标题行**与数据行交错展开。展平后的视图可在引擎上
获取：`grid.DataEngine.View` 是 `IReadOnlyList<GridRowInfo>`，元素为 `GridDataRow`
（`Kind == GridRowKind.Data`，暴露 `Item`）或 `GridGroupRow`
（`Kind == GridRowKind.Group`，暴露 `Group`）。

每个 `GridGroup` 提供：

- `Key` —— 分组键（单元格值，可能经过转换）；
- `Level` 与 `Parent` —— 在分组树中的位置；
- `ChildGroups` —— 下一级的子分组；
- `Items` —— 直接落在这个分组中的数据项（仅最深一层有值）；
- `TotalItems` —— 该分组下的数据行总数；
- `IsExpanded` —— 展开状态，**在数据刷新后保留**。

### 分组标题模板

设置 `GroupHeaderTemplate`（`DataTemplate`）可替换所有分组行的默认标题。
数据上下文是 `GridGroupHeaderInfo`：

| 属性 | 含义 |
|---|---|
| `Key` | 分组键（单元格值或其转换结果） |
| `ItemCount` | 组内数据项数量（含全部后代） |
| `Level` | 嵌套层级（0 = 最外层） |
| `ColumnName` | 被分组列的标题文本 |
| `Caption` | 默认本地化标题（可作回退） |

```xml
<grid:GridControl.GroupHeaderTemplate>
    <DataTemplate>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding ColumnName}" FontWeight="SemiBold" />
            <TextBlock Text="{Binding Key}" Margin="8,0,0,0" />
            <TextBlock Text="{Binding ItemCount, StringFormat='({0})'}" Margin="8,0,0,0" Opacity="0.7" />
        </StackPanel>
    </DataTemplate>
</grid:GridControl.GroupHeaderTemplate>
```

运行时更换模板会立即应用到所有已渲染的分组行。

### 展开与折叠

通过引擎以编程方式展开或折叠分组 —— 状态会保留到之后的 `Refresh()` 调用：

```csharp
foreach (var row in grid.DataEngine.View.OfType<GridGroupRow>())
{
    if (row.Group.Level == 0 && row.Group.TotalItems < 5)
    {
        grid.DataEngine.SetGroupExpanded(row.Group, expanded: false);
    }
}
```

### 自定义分组键

`KeyConverter` 把单元格值转换为分组键（例如把日期按月份分桶）；`KeysComparer`
控制分组键的顺序：

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription(nameof(Order.CreatedAt))
{
    KeyConverter = value => ((DateTime)value!).ToString("yyyy-MM"),
});
```

键盘导航会自动跳过分组行；`ScrollIntoView`、`SelectRange` 等方法面向数据项而非
分组行。

> **状态说明。** 聚合汇总（每个分组及页脚的 Sum/Min/Max/Avg/Count）计划于
> 阶段 1.2 提供；在此之前请在视图模型中基于 `GridGroup.Items` / `TotalItems`
> 自行计算。

### 自定义分组标题

`GridGroupDescription.HeaderStringFormat` 自定义分组行文本：`{0}` = 键，`{1}` = 条数，`{2}` = 列标题。默认为本地化的“键： N 项”。

```csharp
grid.GroupDescriptions.Add(new GridGroupDescription("Asset")
{
    HeaderStringFormat = "{2}: {0} ({1} 项)",
});
```
