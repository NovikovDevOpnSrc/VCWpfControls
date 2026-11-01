---
title: 编辑
description: GridControl 单元格编辑生命周期（F2/Enter/Esc、双击）、CellEdit* 事件、通过 IDataErrorInfo/INotifyDataErrorInfo 校验以及 GridCommands 路由命令。
---

# 编辑

## 启用编辑

默认启用编辑（`IsReadOnly = false`），但仅对提供了编辑元素的列生效。可以在表格级
或列级关闭：

```xml
<grid:GridControl ItemsSource="{Binding Orders}" IsReadOnly="False">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Number" Header="订单号" ReadOnly="True" />
        <columns:GridNumericColumn FieldName="Quantity" Header="数量" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

列的 `ReadOnly` 优先于表格设置：即使表格可编辑，只读列也不会进入编辑模式。
另一个属性 `CanUserDeleteRows`（默认 `true`）单独控制 Delete 键是否可以删除所选行。

## 编辑生命周期

| 操作 | 结果 |
|---|---|
| 双击单元格 / `F2` | 当前单元格进入编辑 |
| `Enter` | 通过绑定提交值 |
| `Esc` | 取消编辑并恢复原值 |
| 编辑时点击其他单元格 / 按方向键 | 先提交当前编辑 |

等价的 API：`BeginEdit()`、`BeginEdit(item, column)`、`CommitEdit()`、`CancelEdit()`；
`IsEditing` 表示是否有单元格正处于编辑状态。

```csharp
grid.BeginEdit(selectedItem, salaryColumn);
// ...
if (!grid.CommitEdit())
{
    // 校验失败 —— 单元格仍停留在编辑模式
}
```

复选框单元格则不同：无需进入编辑模式即可交互，点击后立即提交；`Space` 切换当前
单元格的复选框。

## 事件

三个事件覆盖整个生命周期，都使用 `GridCellEditEventArgs`：`Row`（数据项）、
`Column`、`EditingElement`（在 `CellEditStarting` 时可能为 `null`）、`CancelEdit`
（在 `CellEditEnding`/`CellEditEnded` 上为 true 表示编辑被取消）以及可设置的
`Cancel` 标志。

```csharp
// 禁止编辑已发货订单：
grid.CellEditStarting += (s, e) =>
{
    if (e.Column.FieldName == "Quantity" && ((Order)e.Row).IsShipped)
    {
        e.Cancel = true;
    }
};

// 干预提交的最后机会：
grid.CellEditEnding += (s, e) =>
{
    if (e.CancelEdit) return;                 // 按下了 Esc
    var box = e.EditingElement as TextBox;
    if (box?.Text.Length > 100)
    {
        e.Cancel = true;                      // 继续编辑
    }
};

// 事后处理：
grid.CellEditEnded += (s, e) =>
{
    if (!e.CancelEdit)
    {
        Logger.LogInformation("已提交字段 {Field}", e.Column.FieldName);
    }
};
```

## 校验

表格创建的双向单元格绑定启用了 `ValidatesOnDataErrors`、`ValidatesOnNotifyDataErrors`
和 `ValidatesOnExceptions`。这意味着你现有的 `IDataErrorInfo` /
`INotifyDataErrorInfo` 模型无需修改即可工作：

```csharp
public class Order : IDataErrorInfo
{
    public int Quantity { get; set; }

    public string Error => this[nameof(Quantity)];

    public string this[string columnName]
        => columnName == nameof(Quantity) && Quantity <= 0
            ? "数量必须为正数"
            : string.Empty;
}
```

提交时表格会对绑定调用 `UpdateSource()` 并检查 `Validation.GetHasError`。值无效时，
**单元格停留在编辑模式**等待用户修正；`CommitEdit()` 返回 `false`。错误由编辑元素
的标准 WPF 校验模板渲染。

使用 `CellEditingTemplate` 时，表格不会自己推送值 —— 由模板自身的绑定（例如
`UpdateSourceTrigger=PropertyChanged`）更新数据项，提交只是结束本次编辑会话。

## GridCommands

`GridCommands` 提供已接入控件的路由命令（含默认键手势），工具栏按钮或菜单无需
code-behind 即可集成：

| 命令 | 默认手势 | 效果 |
|---|---|---|
| `GridCommands.BeginEdit` | `F2` | 开始编辑当前单元格 |
| `GridCommands.CancelEdit` | `Esc` | 取消当前编辑 |
| `GridCommands.CommitEdit` | `Enter` | 提交当前编辑 |
| `GridCommands.Delete` | `Delete` | 删除所选行 |
| `GridCommands.SelectAll` | `Ctrl+A` | 全选（Extended 模式） |
| `GridCommands.Copy` | `Ctrl+C` | 把所选内容复制到剪贴板 |

```xml
<StackPanel Orientation="Horizontal">
    <Button Content="编辑 (F2)"
            Command="grid:GridCommands.BeginEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="保存 (Enter)"
            Command="grid:GridCommands.CommitEdit"
            CommandTarget="{Binding ElementName=Grid}" />
    <Button Content="删除"
            Command="grid:GridCommands.Delete"
            CommandTarget="{Binding ElementName=Grid}" />
</StackPanel>
```

`CommandTarget` 必须指向表格：否则路由命令会在焦点元素上执行。

## 删除行

`DeleteSelectedItems()`（以及 Delete 键）会把所选项**从数据源集合中删除** —— 仅当
`ItemsSource` 实现 `IList`（例如 `ObservableCollection<T>`）、`CanUserDeleteRows` 为
`true` 且表格不处于 `IsReadOnly` 时才有效。删除前会先清除选择；删除操作随后通过
`INotifyCollectionChanged` 回流到表格。
