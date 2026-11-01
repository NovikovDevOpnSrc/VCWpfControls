---
title: 入门指南
description: 从 NuGet 安装 VCWpfControls，了解系统要求（.NET 6/8，Windows），几分钟内创建你的第一个数据表格。
---

# 入门指南

## 系统要求

| 要求 | 说明 |
|---|---|
| 操作系统 | Windows（WPF 仅支持 Windows） |
| 框架 | .NET 6 或 .NET 8（`net6.0-windows` / `net8.0-windows`） |
| 包 | `VCWpfControls`（NuGet） |

本库是纯 WPF 控件库 —— 没有原生依赖，也没有外部包引用。

## 安装

安装 NuGet 包：

```
dotnet add package VCWpfControls
```

或在 `.csproj` 中添加引用：

```xml
<ItemGroup>
    <PackageReference Include="VCWpfControls" Version="*" />
</ItemGroup>
```

默认控件主题（`Themes/Generic.xaml`）随包发布并自动生效 —— 无需调用 `MergeDictionaries`。

## 第一个表格

定义一个模型。注意数据注解：它们驱动列的自动生成（完整说明见[列](columns.md)）：

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Employee
{
    [Display(Name = "姓名", Order = 0)]
    public string Name { get; set; } = "";

    [Display(Name = "薪资", Order = 1)]
    public decimal Salary { get; set; }

    [Display(Name = "入职日期", Order = 2)]
    public DateTime HireDate { get; set; }

    [Display(Name = "在职", Order = 3)]
    public bool IsActive { get; set; }
}
```

将数据以 `ObservableCollection<Employee>` 的形式暴露，让表格感知增删操作：

```csharp
public ObservableCollection<Employee> Employees { get; } = new()
{
    new() { Name = "Alice", Salary = 95000m, HireDate = new DateTime(2021, 3, 1), IsActive = true },
    new() { Name = "Bob",   Salary = 87000m, HireDate = new DateTime(2022, 7, 15), IsActive = false },
};
```

将表格放入窗口并绑定 `ItemsSource`：

```xml
<Window xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <grid:GridControl ItemsSource="{Binding Employees}" />
</Window>
```

这样就完成了：列会根据 `Employee` 自动生成，点击表头排序，双击单元格（或按 F2）
开始编辑，Ctrl+C 复制所选内容。

## 上手初期常用的属性

| 属性 | 默认值 | 用途 |
|---|---|---|
| `AutoGenerateColumns` | `true` | 当 `Columns` 为空时自动生成列 |
| `IsReadOnly` | `false` | 整个表格禁止编辑 |
| `SelectionMode` | `Extended` | `SingleRow` 或 `Extended`（Ctrl/Shift） |
| `RowHeight` | `NaN`（自适应） | 固定行高可最大化虚拟化收益 |
| `FrozenColumnCount` | `0` | 水平滚动时保持前 N 列可见 |
| `ClipboardCopyMode` | `IncludeHeaders` | 设为 `None` 可禁用 Ctrl+C |
| `CanUserDeleteRows` | `true` | Delete 键删除所选行（数据源须为 `IList`） |

## 实时更新

当 `ItemsSource` 实现 `INotifyCollectionChanged`（例如 `ObservableCollection<T>`）时，
表格会在每次变更后重建视图。请注意：阶段 1 中每次变更都会重建整个
筛选 → 排序 → 分组 管线；面向超大集合的增量和异步更新计划于阶段 1.2 提供。

如需手动强制整体重建（例如批量修改后集合未发出通知），调用：

```csharp
grid.Refresh();
```

## 后续步骤

- 手动定义列 —— 见[列](columns.md)
- 用代码排序、筛选、分组 —— 见[排序、筛选与分组](sorting-filtering-grouping.md)
- 为应用定制样式 —— 见[主题样式](theming.md)
