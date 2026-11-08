# Vibe Coded WPF Controls

[![CI](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml/badge.svg)](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

免费开源的 WPF 业务控件库：**GridControl**（快速、对 MVVM 友好、功能不逊于免费 WPF 表格的数据表格）、**TreeListView**、**PropertyGrid**、**透视表 (Pivot Grid)**、**甘特图**、条件表达式编辑器、**Wizard 向导**、停靠布局与类型化编辑器 —— 全部基于 MIT 许可证，库本身零依赖，内置 EN/RU/ZH 本地化。

**文档：** [English](README.md) | [Русский](README.ru.md) | [中文](README.zh-CN.md)

## 功能特性（GridControl，第一阶段）

- 可绑定任意 `IEnumerable` 数据源（包括 `ObservableCollection` 及实时更新）
- 自动生成列（反射 + `[Display]`/`[ReadOnly]`/`[Browsable]` 特性），也支持手动定义列
- 列类型：文本、数值、复选框、下拉框、日期、模板列
- 列宽调整、拖拽排序、隐藏、冻结列（`FrozenColumnCount`）
- UI 虚拟化 + 容器回收 —— 100 万行以上仍可流畅滚动
- 排序：单列与多列排序、排序指示器、自定义比较器
- 按内容适配：默认 Auto 列宽、`BestFitColumn`/`BestFitAllColumns`、双击分隔条适配
- 表头右键菜单（本地化）：排序、适配、分组、固定、隐藏、列选择器
- 选择：单选/扩展选择、键盘操作（Ctrl/Shift）、复选框选择、当前单元格
- 编辑：F2/Enter/Esc、列级只读、通过 `IDataErrorInfo` / `INotifyDataErrorInfo` 校验
- 筛选、分组、聚合汇总（阶段 1.2）
- 导出 CSV / Excel (.xlsx) / HTML、剪贴板支持（阶段 1.3）
- 明亮/深色主题、High-DPI、无障碍（UI Automation）、内置 中/英/俄 本地化（阶段 1.3）

完整计划见 [ROADMAP.md](ROADMAP.md)。

## 安装

```
dotnet add package VCWpfControls
```

支持 Windows 上的 **.NET 6** 与 **.NET 8**（`net6.0-windows`、`net8.0-windows`）。

## 快速开始

```xml
<Window xmlns:vc="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <vc:GridControl ItemsSource="{Binding Employees}"
                    AutoGenerateColumns="True"
                    CanUserSortColumns="True"
                    SelectionMode="Extended" />
</Window>
```

手动定义列：

```xml
<vc:GridControl ItemsSource="{Binding Employees}">
    <vc:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="姓名" Width="2*" />
        <columns:GridNumericColumn FieldName="Salary" Header="薪资" Width="100" StringFormat="C" />
        <columns:GridDateColumn FieldName="HireDate" Header="入职日期" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="在职" Width="70" />
    </vc:GridControl.Columns>
</vc:GridControl>
```

## 文档

- [文档网站 (EN/RU/ZH)](https://novikovdevopnsrc.github.io/VCWpfControls/)
- [入门指南 (ZH)](https://novikovdevopnsrc.github.io/VCWpfControls/zh/getting-started.html)
- [Getting started (EN)](https://novikovdevopnsrc.github.io/VCWpfControls/en/getting-started.html)
- [Начало работы (RU)](https://novikovdevopnsrc.github.io/VCWpfControls/ru/index.html)

示例应用 `src/VCWpfControls.Demo` 为每个功能提供可运行的实例。

## 测试

本库以测试驱动开发：

- `tests/VCWpfControls.Core.Tests` —— 数据引擎单元测试（无 UI）
- `tests/VCWpfControls.Control.Tests` —— 控件 STA 测试
- `tests/VCWpfControls.UiTests` —— 针对示例应用的 FlaUI 界面自动化测试
- `benchmarks/VCWpfControls.Benchmarks` —— BenchmarkDotNet 性能测试

## 许可证

[MIT](LICENSE) —— 可任意使用（包括商业用途），不提供任何担保。
