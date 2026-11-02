---
title: 属性网格（PropertyGrid）
description: 使用类型化编辑器、分类和搜索查看并编辑任意对象的属性。
---

# 属性网格（PropertyGrid）

`VCWpfControls.PropertyGrid.PropertyGrid` 显示并编辑任意对象的公共属性：类型化编辑器、
按类别分组、搜索、可展开的嵌套对象，以及通过 `INotifyPropertyChanged` 实时更新。

## 快速开始

```xml
<Window xmlns:pg="clr-namespace:VCWpfControls.PropertyGrid;assembly=VCWpfControls">
    <pg:PropertyGrid SelectedObject="{Binding SelectedEmployee}" />
</Window>
```

常见用法是与 `GridControl` 搭配：

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="380" />
    </Grid.ColumnDefinitions>
    <grid:GridControl x:Name="Grid" ItemsSource="{Binding Employees}" />
    <pg:PropertyGrid Grid.Column="1" SelectedObject="{Binding SelectedItem, ElementName=Grid}" />
</Grid>
```

## 功能特性

- **类型化编辑器**（按属性类型选择）：文本框、多行文本（`[DataType(DataType.MultilineText)]`）、
  复选框、枚举下拉框、右对齐数值框、日期选择器；集合与不支持的类型显示为只读文本。
- **元数据特性**：`[Display]`（`Name`、`Description`、`Order`、`GroupName`）、`[Category]`、
  `[DisplayName]`、`[ReadOnly]`、`[Browsable(false)]`。
- **分类**以折叠组显示（`ShowCategories="False"` 显示平面列表）。
- **搜索框**（`ShowSearchBox`）按显示名或 CLR 名称筛选属性。
- **可展开属性**：复杂对象带 “+” 开关，懒加载嵌套属性并缩进显示。
- **实时更新**：当对象实现 `INotifyPropertyChanged` 时，值变化会刷新对应编辑器。
- **事件**：`PropertyValueChanged` 在每次提交编辑后触发（值已写入对象）。
- **选项**：`LabelWidth` 控制标签列宽度。

## 编辑语义

- 文本编辑器按 **Enter** 或失去焦点时提交；非法输入（例如数值框中输入字母）会静默
  回退为当前值。
- 下拉框不会产生非法值；复选框和日期立即提交。
- 只读属性和集合仅展示，不可编辑。

## 主题与本地化

该控件与表格控件使用相同的 `GridResources.*` 资源键（见[主题](theming.md)）；搜索提示
和“其他”分类名称已本地化（包内自带 中/英/俄）。
