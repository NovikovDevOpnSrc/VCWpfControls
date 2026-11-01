---
title: GridControl 概述
description: VCWpfControls 是基于 MIT 许可证的免费开源 WPF 控件库。第一阶段提供 GridControl —— 一个快速、支持虚拟化、对 MVVM 友好的数据表格，面向 Windows 上的 .NET 6/8。
---

# GridControl 概述

**Vibe Coded WPF Controls** 是基于 [MIT 许可证](https://github.com/vibe-coded/VCWpfControls/blob/main/LICENSE)
的免费开源 WPF 控件库。**第一阶段**提供 **GridControl** —— 一个快速、支持虚拟化、
对 MVVM 友好的数据表格。

表格可绑定到任意 `IEnumerable`，可根据数据项类型自动生成列（也支持手动定义），
行容器采用带回收机制的虚拟化，其数据引擎（`GridDataManager`）与 UI 完全解耦，
便于单元测试。

## 功能特性

- **数据绑定** —— 任意 `IEnumerable`，通过 `ObservableCollection<T>` 支持实时更新
- **列** —— 文本、数值、复选框、下拉框、日期、模板列；支持基于
  `[Display]`/`[ReadOnly]`/`[Browsable]` 特性的自动生成，也支持手动定义
- **布局** —— 列宽调整、拖拽换位、隐藏、冻结（`FrozenColumnCount`）；
  宽度支持像素、比例（`2*`）和 `Auto`
- **虚拟化** —— 带容器回收的 UI 虚拟化；100 万行以上仍可流畅滚动
- **排序** —— 单列与多列排序、点击与 Shift+点击、方向指示器、自定义比较器
- **筛选** —— 引擎级 `FilterDescriptions` API，支持
  Equals/Contains/Between/In/IsNull/Custom 等运算符（内置筛选 UI 计划于阶段 1.2 提供）
- **分组** —— 引擎级 `GroupDescriptions` API，分组可展开/折叠且状态在刷新后保留
  （分组面板与聚合汇总计划于阶段 1.2 提供）
- **编辑** —— F2/Enter/Esc、列级只读、通过 `IDataErrorInfo` / `INotifyDataErrorInfo` 校验
- **选择** —— SingleRow/Extended 两种模式、完整的键盘支持、当前单元格
- **剪贴板** —— Ctrl+C 将所选行复制为制表符分隔的文本
- **主题** —— 默认主题基于 `ComponentResourceKey` 资源键，可在应用中覆盖

后续计划（筛选行、汇总、导出、深色主题、本地化）请查看
[路线图](https://github.com/vibe-coded/VCWpfControls/blob/main/ROADMAP.md)。

## 快速开始（XAML）

```xml
<Window x:Class="Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls"
        xmlns:columns="clr-namespace:VCWpfControls.Grid.Columns;assembly=VCWpfControls"
        Title="员工">

    <grid:GridControl ItemsSource="{Binding Employees}"
                      AutoGenerateColumns="True"
                      CanUserSortColumns="True"
                      SelectionMode="Extended" />
</Window>
```

当 `AutoGenerateColumns="True"`（默认值）且 `Columns` 集合为空时，表格会为数据项类型的
每个公开可读属性创建一个对应类型的列。

## 快速开始（C#）

```csharp
using System.Collections.ObjectModel;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;

public class Employee
{
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

// 在视图模型或窗口代码中：
var employees = new ObservableCollection<Employee>
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob", Salary = 87000m, HireDate = new DateTime(2022, 7, 15), },
};

var grid = new GridControl { ItemsSource = employees };

// 通过描述集合进行编程式排序与筛选：
grid.SortBy(nameof(Employee.Name), GridSortDirection.Ascending);
grid.FilterDescriptions.Add(
    new GridFilterDescription(nameof(Employee.Salary), GridFilterOperator.GreaterThanOrEqual, 90000m));
```

## 接下来阅读

- [入门指南](getting-started.md) —— 安装、环境要求、第一个表格
- [列](columns.md) —— 列类型、自动生成、宽度、模板
- [排序、筛选与分组](sorting-filtering-grouping.md) —— 数据引擎 API
- [编辑](editing.md) —— 编辑生命周期、校验、`GridCommands`
- [选择](selection.md) —— 选择模式与当前单元格
- [主题样式](theming.md) —— 资源键及其覆盖方式
- [实用示例](cookbook.md) —— 现成的实用配方
