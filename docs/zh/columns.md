---
title: 列
description: GridControl 的列类型（文本、数值、复选框、下拉框、日期、模板），基于特性注解的自动生成、宽度、冻结列与单元格模板。
---

# 列

所有列类型都位于 `VCWpfControls.Grid.Columns` 命名空间，并派生自 `GridColumnBase`。

```xml
xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
```

## 列类型

| 类型 | 显示 | 编辑控件 | 说明 |
|---|---|---|---|
| `GridTextColumn` | `TextBlock` | `TextBox` | 通用列 |
| `GridNumericColumn` | 右对齐文本 | 右对齐的 `TextBox` | 用于数值字段 |
| `GridCheckBoxColumn` | 可交互的 `CheckBox` | — | 点击立即提交；`Space` 切换当前单元格 |
| `GridComboBoxColumn` | 所选项的显示文本 | `ComboBox` | `ItemsSource`、`DisplayMemberPath`、`SelectedValuePath` |
| `GridDateColumn` | 日期文本（默认 `"d"` 格式） | `DatePicker` | `StringFormat` 可覆盖显示格式 |
| `GridTemplateColumn` | 你的 `CellTemplate` | 你的 `CellEditingTemplate` | 完全自定义单元格；必须设置 `CellTemplate` |

## 公共属性（GridColumnBase）

| 属性 | 默认值 | 用途 |
|---|---|---|
| `Header` | `""` | 表头内容（文本或任意对象） |
| `FieldName` | `""` | 行数据项内的绑定路径；支持 `Address.City` 这样的点分路径 |
| `Width` | `1*` | `"120"`、`"Auto"`、`"*"`、`"2*"` —— 见下文 |
| `MinWidth` / `MaxWidth` | `0` / `∞` | 对所有宽度模式生效的像素限制 |
| `Visible` | `true` | 隐藏列而不删除它 |
| `ReadOnly` | `false` | 该列的单元格不能进入编辑模式 |
| `CanUserSort` | `true` | 单列级别启用/禁用点击表头排序 |
| `SortMemberPath` | `null` | 使用与 `FieldName` 不同的排序路径 |
| `StringFormat` | `null` | 显示格式，如 `"C"` 或 `"{}{0:N2}"` |
| `HorizontalContentAlignment` | `Left` | 单元格内容的水平对齐 |
| `ElementStyle` | `null` | 应用于生成的单元格元素的样式（仅显示模式；不影响编辑元素） |
| `HeaderStyle` | `null` | 应用于列头的样式 |
| `CellTemplate` / `CellEditingTemplate` | `null` | 用自定义 `DataTemplate` 替换显示/编辑元素 |

`GetCellValue(item)` 方法按列的字段路径读取某个数据项的单元格值。

## 宽度

`Width` 接受三种单位（XAML 字符串会自动转换）：

- **像素** —— `Width="120"`：固定的设备无关像素。
- **比例（星号）** —— `Width="*"` 或 `Width="2*"`：按比例瓜分剩余视口宽度（默认 `1*`）。
- **自适应** —— `Width="Auto"`：按最宽内容自适应。

`MinWidth` 与 `MaxWidth` 在所有模式下生效 —— 包括用户拖拽调整列宽时。

## 自动生成与特性注解

当 `AutoGenerateColumns="True"`（默认值）且 `Columns` 集合**为空**时，表格会反射
数据项类型，为每个公开可读属性生成一列。可以用特性注解来控制这一过程：

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Order
{
    [Display(Name = "订单号", Order = 0)]         // 表头文本与位置
    public int Number { get; set; }

    [Display(Name = "总额", Order = 2)]
    public decimal Total { get; set; }

    [ReadOnly(true)]                              // 生成的列变为只读
    public DateTime CreatedAt { get; set; }

    [Browsable(false)]                            // 完全不生成列
    public byte[] Blob { get; set; } = Array.Empty<byte>();
}
```

生成器遵循的规则：

- `[Display]` —— `ShortName`/`Name` 作为表头文本，`Order` 决定列顺序
  （表头文本亦支持 `[DisplayName]` 作为后备）；
- `[ReadOnly(true)]` —— 生成的列获得 `ReadOnly = true`；
- `[Browsable(false)]` —— 跳过该属性；
- 类型映射：`bool` → 复选框，`DateTime`/`DateTimeOffset`/`DateOnly` → 日期，
  枚举 → 下拉框（其 `ItemsSource` 会填充 `Enum.GetValues`），数值类型 → 数值列，
  其余 → 文本列。

### AutoGeneratingColumn 事件

每生成一列都会触发该事件；可以替换列实例或取消生成：

```csharp
grid.AutoGeneratingColumn += (s, e) =>
{
    if (e.Column.FieldName == "Blob")
    {
        e.Cancel = true;                      // 跳过这一列
        return;
    }

    if (e.Column is GridNumericColumn { FieldName: "Salary" })
    {
        e.Column.StringFormat = "C";          // 修改生成的列
    }
};
```

## 手动定义列

在 XAML 中显式定义列 —— 只要 `Columns` 不为空，自动生成就不会介入：

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  FrozenColumnCount="1"
                  CanUserResizeColumns="True"
                  CanUserReorderColumns="True">
    <grid:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="姓名" Width="2*" MinWidth="120" />
        <columns:GridNumericColumn FieldName="Salary" Header="薪资" Width="110"
                                   StringFormat="C" HorizontalContentAlignment="Right" />
        <columns:GridDateColumn FieldName="HireDate" Header="入职日期" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="在职" Width="70" />
    </grid:GridControl.Columns>
</grid:GridControl>
```

## 冻结列

`FrozenColumnCount`（表格的属性，而非列的属性）在水平滚动时固定前 N 列。小于 0 的
值会被拒绝；隐藏的列不计入。配合 `CanUserReorderColumns`，用户可以先把某列拖到
最前面，再调整计数将其固定。

## 模板

当内置元素不够用时，使用 `GridTemplateColumn`（或任意列上的
`CellTemplate`/`CellEditingTemplate`）。模板的 `DataContext` 是行数据项：

```xml
<columns:GridTemplateColumn FieldName="Progress" Header="进度" Width="160">
    <columns:GridTemplateColumn.CellTemplate>
        <DataTemplate>
            <ProgressBar Value="{Binding Progress}" Maximum="100" Height="14" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellTemplate>
    <columns:GridTemplateColumn.CellEditingTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Progress, UpdateSourceTrigger=PropertyChanged}" />
        </DataTemplate>
    </columns:GridTemplateColumn.CellEditingTemplate>
</columns:GridTemplateColumn>
```

注意：使用 `CellEditingTemplate` 时表格不会自己推送值 —— 由模板自身的绑定更新
数据项（详见[编辑](editing.md)）。

## 下拉框列

`GridComboBoxColumn` 依据 `SelectedValuePath` 选择绑定模式：

- **不设 `SelectedValuePath`** —— 单元格的值就是整个列表项；下拉框通过
  `SelectedItem` 绑定。
- **设置 `SelectedValuePath`** —— 单元格的值是一个键；下拉框通过 `SelectedValue`
  绑定，显示单元格依据 `DisplayMemberPath` 把键解析为显示文本。

```xml
<columns:GridComboBoxColumn FieldName="DepartmentId" Header="部门"
                            ItemsSource="{Binding Departments}"
                            DisplayMemberPath="Name"
                            SelectedValuePath="Id" Width="180" />
```

### 按内容适配（BestFit）

自动生成的列默认按内容设定宽度（`Width="Auto"`：表头宽度加上已呈现行的宽度，随行的
呈现而增长）。内容宽于视口时出现水平滚动条；`FrozenColumnCount` 在滚动时固定前几列。

一次性精确适配（默认测量当前视图最多 1000 行，传 `0` 测量全部行）：

```csharp
grid.BestFitColumn(column);        // 单列（含表头）
grid.BestFitAllColumns();          // 所有可见列
```

双击列的分隔条可按内容适配该列（类似 Excel）。

## 表头右键菜单

右键单击任意列标题可打开内置（中/英/俄本地化）菜单：

- **升序排序 / 降序排序** — 当前排序项带勾选标记；
- **取消排序** — 存在排序时可用；
- **筛选...** — 打开该列的筛选菜单（值列表）；`CanUserFilter` 开启时可用；
- **适配列宽（按内容） / 适配所有列宽** — 一次性精确适配；
- **按此列分组** — 开关，添加/移除一个分组级别；
- **固定至此列** — 切换 `FrozenColumnCount`；
- **隐藏列** — 最后一个可见列时禁用；
- **列** — 复选子菜单，切换各列可见性，首项为 **列选择器...** 对话框；
- **重置布局** — 恢复首次构建列时捕获的布局：列顺序、全部宽度、可见性和固定列数。

**列选择器**对话框（`OpenColumnChooser`）按当前顺序列出所有列的可见性复选框，
并提供“上移/下移”按钮调整顺序。`ResetLayout()` 同为公开 API，可绑定到您自己的界面。

菜单由 `BuildColumnHeaderMenu` 构建；如需自定义，请处理表格的 `ContextMenuOpening`
事件并指定自己的 `ContextMenu`。

### 空值显示

列上的 `NullText="—"` 在空单元格处显示标记（通过绑定的 `TargetNullValue` 实现；文本列与数值列均适用）。

### 列分隔线

`ShowColumnSeparators="True"` 在每行内的列之间绘制竖线（在行分隔线之外）。`ColumnSeparatorBrush` 可自定义颜色，默认使用 `GridLinesBrush`。分隔线会正确处理冻结列与水平滚动。
