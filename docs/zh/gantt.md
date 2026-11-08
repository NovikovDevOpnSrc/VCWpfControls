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

## 引擎（无 UI）

`GanttTask`/`GanttDependency`（FS/SS/FF/SF + 延后/提前天数）、`GanttEngine.AutoSchedule`
（仅前移；`GanttCycleException` 携带环路路径）、`CriticalPath`（CPM 浮动）、
`BuildTimescale`（日/周/月，周首日随区域设置）。无需 STA 的单元测试。
