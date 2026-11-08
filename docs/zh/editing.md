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


## 添加行（CanUserAddRows）

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                   CanUserAddRows="True"
                   NewItemFactory="{Binding CreateEmployee}" />
```

启用 `CanUserAddRows="True"` 后，表格末尾会出现一个空输入行。编辑其单元格时：

1. 触发 `AddingNewItem`（设置 `e.NewItem` 提供元素，或 `e.Cancel` 中止）；
2. 否则调用 `NewItemFactory`；否则用元素类型的无参构造创建实例（`ItemsSource` 必须是
   可写的 `IList`）；
3. 元素加入数据源，编辑在新行中继续。

首次编辑期间按 **Esc** 会再次移除刚添加的元素（与 DataGrid 一致）；提交则保留。
只读表格不显示输入行。

## 从剪贴板粘贴

`PasteFromClipboard()`（也可用 **Ctrl+V**）读取 TSV 文本（Excel 剪贴板格式），从当前
单元格开始写入表格：

- 粘贴列与可视列对齐；只读列对应的单元格被丢弃而不偏移；
- 值按当前区域设置转换为属性类型；无法转换的单元格被跳过；
- 超出数据的行/列被丢弃；add-row 输入行不会作为粘贴目标；
- 粘贴前触发 `PastingFromClipboard`（`e.Text` 为文本，`e.Cancel` 取消）。

返回写入的单元格数。


## 向导（Wizard）

```xml
<wizard:Wizard>
    <wizard:WizardPage Header="欢迎" Content="..."/>
    <wizard:WizardPage Header="填写" Content="..."/>
    <wizard:WizardPage Header="完成" Content="..."/>
</wizard:Wizard>
```

- `Pages`（内容属性）——有序的 <c>WizardPage</c>；`CurrentPageIndex` 自动收敛到范围内；
  `CurrentPage`/`CanGoBack`/`CanGoNext`/`CanFinish` 反映当前状态。
- `WizardPage.CanLeave`——绑定校验状态：为 false 时 Next/Finish 禁用，`GoNext`
  忽略点击（始终允许后退）。
- **PageChanging**（可取消——设置 `e.Cancel` 留在 `e.From`）、**PageChanged**、
  **Finished**（最后一页的 Next）、**Cancelled**。
- 最后一页的 Next 按钮变为 **Finish**；按钮文案已本地化（中/英/俄）。自定义模板：
  `PART_BackButton`/`PART_NextButton`/`PART_CancelButton`。


## 条件构造器

```xml
<conditions:ConditionBuilder Schema="{Binding Schema}" Condition="{Binding Filter, Mode=TwoWay}" />
```

- `Schema` —— 可筛选字段（`ConditionSchema.FromType(typeof(Person))` 自动推断名称、类型、
  `[Display]` 标题与枚举建议）。
- 可视树：递归的**且（全部）/ 或（任一）**分组；每行一个字段下拉、类型感知的运算符
  下拉（字符串：包含/开头；数字与日期：比较与 `between`；枚举与布尔：相等）以及类型化
  取值编辑器（TextBox / CheckBox / DatePicker / 枚举下拉；`between` 两个编辑器，`in`
  逗号分隔列表）。`+ 添加条件`、`+ 添加分组`、`✕` 删除。
- **文本页**编辑 `ConditionParser` 语言（`[Salary] > 1000 And [Dept] In ('IT')`）；解析
  错误带位置显示，树保持不变。
- 实时 `PreviewText`；每次编辑触发 `ConditionChanged`；`ErrorText` 为最近错误。求值：
  `ConditionEvaluator.Matches(item, Condition)` —— 与表格筛选一致的语义。

## 与表格/透视集成（第三阶段）

- **表头菜单 →「筛选...」**：打开基于表格元素类型的模态 `ConditionBuilder`，并
  预置右键所在列的一个 Equals 条件（按条件筛选；值勾选列表移至单独的「按值
  筛选...」条目）；**确定**将条件树应用为 `GridControl.CustomFilterPredicate`（经
  `ConditionEvaluator`），**清除**移除筛选，**取消**保持不变。
- `GridControl.CustomFilterPredicate` —— 任意 `Predicate<object?>`，与引擎筛选 AND 组合
  （保留原始数据源：即使筛选清空视图也不会丢失元素类型，且可随时撤下筛选）。
- `PivotGrid.Condition` —— 在分桶前过滤**源行**的条件树（与 `FieldFilter` 组合）；
  `null` 禁用。

```csharp
grid.CustomFilterPredicate = item => ConditionEvaluator.Matches(item!, tree);
pivot.Condition = tree;
```
