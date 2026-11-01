---
title: 选择
description: GridControl 的选择模式（SingleRow/Extended）、SelectedItem/SelectedItems、当前单元格（CurrentItem/CurrentColumn）、Select* API 与键盘导航。
---

# 选择

## 选择模式

`SelectionMode` 提供两种模式：

- **`GridSelectionMode.SingleRow`** —— 一次只能选择一行；点击或导航即选中该行。
- **`GridSelectionMode.Extended`**（默认）—— Ctrl+点击切换单行，Shift+点击选择
  范围，`Ctrl+A` 全选。

已选择多行时切换到 `SingleRow`，只会保留第一行。

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  SelectionMode="Extended"
                  SelectedItem="{Binding SelectedEmployee}" />
```

## 所选项

| 成员 | 类型 | 含义 |
|---|---|---|
| `SelectedItem` | 依赖属性（默认双向） | 第一个所选项；可绑定到视图模型 |
| `SelectedItems` | `IList` | 选择内容的实时视图 |
| `SelectedItemsCollection` | `ObservableCollection<object>` | 同样选择内容的可观察集合 |

`SelectedItem` 是带 `BindsTwoWayByDefault` 的依赖属性 —— 绑定时无需 `Mode=TwoWay`。

## 编程式选择

```csharp
grid.SelectItem(alice);            // 加入选择
grid.UnselectItem(bob);            // 移除一项
grid.SelectRange(alice, dave);     // 选择两个项之间的所有行（按视图顺序）
grid.SelectAll();                  // 仅 Extended 模式
grid.UnselectAll();                // 清空
```

`SelectAll` 在 `SingleRow` 模式下不做任何事。启用分组时，`SelectRange` 与
`SelectAll` 只作用于数据行，跳过分组标题行。

选择**在筛选和排序后保留**：即使某项被筛选条件暂时隐藏，它仍保持选中；只有从
数据源中移除才会取消选择。

## 当前单元格

当前单元格是键盘导航和编辑的锚点：

- `CurrentItem` —— 当前行所在的数据项（支持双向绑定）；
- `CurrentColumn` —— 当前单元格所在列，否则为 `null`；
- `CurrentCellChanged` —— 当前单元格移动时触发的 CLR 事件。

```csharp
grid.CurrentItem = employees[4];
grid.ScrollIntoView(employees[4]);
```

## SelectionChanged

```csharp
grid.SelectionChanged += (s, e) =>
{
    foreach (var added in e.AddedItems) Console.WriteLine($"已选择: {added}");
    foreach (var removed in e.RemovedItems) Console.WriteLine($"已取消: {removed}");
};
```

## 键盘参考

| 按键 | 行为 |
|---|---|
| 方向键 | 移动当前单元格（未提交的编辑会先提交） |
| `PageUp` / `PageDown` | 当前单元格移动约 3/4 页 |
| `Home` / `End` | 当前行第一/最后一列 |
| `Ctrl+Home` / `Ctrl+End` | 视图第一/最后一个单元格 |
| `Ctrl+A` | 全选（仅 Extended） |
| `Ctrl+C` | 把所选内容复制为制表符分隔文本 |
| `Delete` | 删除所选行（见[编辑](editing.md)） |
| `Enter` / `Esc` / `F2` | 提交 / 取消 / 开始编辑 |

在 `SingleRow` 模式下普通导航会同时移动选择；在 `Extended` 模式下方向键只移动
当前单元格 —— 按住 Shift（或 Shift+点击）可扩展选择。

## 滚动到某项

`ScrollIntoView(item)` 垂直滚动，直到该数据项的行可见：

```csharp
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.ScrollIntoView(viewModel.EmployeeToHighlight);
grid.SelectItem(viewModel.EmployeeToHighlight);
```

分组标题行永远不会成为当前行 —— 导航会自动跳过它们。
