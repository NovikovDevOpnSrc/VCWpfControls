---
title: 甘特图
description: GanttView —— 任务树 + 时间线条形图、依赖箭头、里程碑、今日线、关键路径；由无 UI 的 GanttEngine 驱动。
---

# 甘特图

论坛调研显示甘特图是 WPF 开源生态最大的空白；VCWpfControls 以三层架构填补它。

## 控件

```xml
<gantt:GanttView Tasks="{Binding Tasks}"
                 Dependencies="{Binding Links}"
                 AutoSchedule="True"
                 ShowCriticalPath="True"
                 TimescaleUnit="Week" />
```

- **左侧**：TreeListView 任务层级（名称/开始/完成/% 列；通过 `GanttTask.ParentId`
  表达层级）；分隔条调节宽度。
- **右侧**：时间线 —— 带完成度填充的条形、里程碑菱形、FS/SS/FF/SF 依赖箭头、虚线
  今日线、日/周/月缩放（`TimescaleUnit`）。
- `TaskRowHeight` 使两半行高同步；时间线横向滚动并镜像树视图的纵向滚动。
- `AutoSchedule` 在加载/变更时运行引擎（违反约束的后续任务前移）；`ShowCriticalPath`
  用强调色描出关键条形与连线。双击条形 → `BarDoubleClicked`（在 `GanttTimelinePanel` 上）。

## 设计规范符合度（Oracle FusionFX / SAP Fiori / Smartsheet）

图表遵循三大甘特图设计规范：

- **两级时间轴** — 上层（月份；按月缩放时为年份）位于缩放单元行之上（Oracle 与
  Fiori 的要求）。
- **汇总条** — 父任务绘制为 Oracle 汇总条（带端帽的细带），显示所有后代的按
  工期加权完成度（`GanttEngine.SummaryCompletion`），而非自身数值。
- **图例** — 固定在时间轴底部的条带（`ShowLegend` 切换），列出屏幕上的形状：
  任务、汇总、里程碑、关键路径、基线、非工作日（本地化 ×3）。
- **条形标签** — `ShowBarLabels`（默认关闭，遵循 Oracle）在条形后绘制任务名并
  按图表宽度裁剪。
- **离散滚轮缩放** — 鼠标滚轮在 Day ↔ Week ↔ Month 间步进，经由视图的
  `TimescaleUnit`（绑定与工具栏保持同步）。
- **拖拽平移** — 拖动图表空白处滚动时间轴（手型光标）。
- **光标反馈** — 条形边缘 `SizeWE`（调整），条形本体 `SizeAll`（移动）。
- **任务颜色** — `GanttTask.BarBrush`（null = 主题）：Fiori 的分类/状态配色
  模式。

## 基线与就地编辑

- **基线** — 在任务上设置 `BaselineStart`/`BaselineFinish`：实际色块下方绘制一条
  细灰色基线带（用 `ShowBaseline` 切换）；色块提示显示基线区间与起始偏差天数。
- **就地编辑** — 树形列（Name/Start/Finish/%）直接写回任务：提交日期会重新运行
  自动排程与关键路径，并触发 `TaskEdited` — 与拖动色块完全一致。

## 非工作日

`Calendar`（`GanttCalendar` 类：周末星期 + 显式节假日日期）让图表切换到工作时间：

- 时间轴在网格下方为周末/节假日着色。
- 开启 `AutoSchedule` 时，工期与延迟按**工作日**计算，被移动的任务绝不会从
  非工作日开始（「周五 + 1」是周一而不是周六）。已满足链接约束的手工日期保持不变。
- `CriticalPath` 仍按日历日计算（相对浮动）。
- `null`（默认）保持纯日历日算法 — 现有图表行为不变。

```csharp
var calendar = new GanttCalendar();                 // 默认周六/周日
calendar.Holidays.Add(new DateTime(2026, 11, 20));  // 公司假日
view.Calendar = calendar;
```

## 拖拽交互

所有编辑按天对齐，日期写入任务后触发事件：

- **移动** —— 拖动条形主体；开始与完成日期一起移动（里程碑固定不动）。
- **调整时长** —— 拖动条形左/右边缘；时长随之变化，至少保留一天。
- **连线** —— 按住 **Alt** 从条形边缘拖到另一条形上：橡皮筋线跟随指针，松开即创建
  `FinishToStart` 依赖（加入 `Dependencies`，`AutoSchedule` 开启时立即重排计划）。

```csharp
view.TaskEdited += (_, task) => Save(task);                       // 移动/调整
view.LinkCreated += (_, dependency) => Save(dependency);          // 绘制的连线
```

编辑后两半同步刷新：树视图列读取新日期，引擎重跑 `AutoSchedule`/`CriticalPath`，
后续任务即时跳转，关键链路实时更新。

## 引擎（无 UI）

`GanttTask`/`GanttDependency`（FS/SS/FF/SF + 延后/提前天数）、`GanttEngine.AutoSchedule`
（仅前移；`GanttCycleException` 携带环路路径）、`CriticalPath`（CPM 浮动）、
`BuildTimescale`（日/周/月，周首日随区域设置）。无需 STA 的单元测试。
