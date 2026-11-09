---
title: 日程控件
description: SchedulerView — 在重复日程之上提供日/工作周/周/月视图：时间刻度、重叠并排泳道、全天条带与月历网格；由无 UI 的 SchedulerEngine 驱动。
---

# 日程控件

日程控件（Outlook 风格日历）分两层发布：无 UI 引擎（第 1 阶段）与控件（第 2 阶段）。
拖拽/调整日程交互已列入路线图（第 3 阶段）。

## 控件

```xml
<scheduler:SchedulerView Appointments="{Binding Appointments}"
                          ViewType="WorkWeek"
                          DayStartHour="8"
                          DayEndHour="20"
                          OccurrenceDoubleClicked="OpenEditor" />
```

- **日 / 工作周 / 周 / 月** 视图（`ViewType`）；`AnchorDate` 决定显示的日/周/月
  （默认锚定到今天）。
- **日类视图**：固定的时间刻度与日期表头，每天一列。`DayStartHour`/`DayEndHour`
  设定可见时间窗（默认 0–24）；窗外的色块被裁剪。重叠的日程把列拆分为并排
  **泳道**（按天的区间图聚类）；跨午夜的日程每天绘制一个裁剪段。
- **全天条带**：`IsAllDay` 日程在小时网格上方渲染为色签，每条重叠泳道占一行
  （最多 3 行，超出裁剪）；色签横跨它覆盖的每一天。
- **月视图**：六周网格（42 格，周首日依区域文化）、星期表头、日期数字（相邻
  月份变暗）、今日高亮，每格最多 2 个色签外加「+N 条」溢出行。
- 今日列/今日格着色，并有虚线「当前时刻线」穿过。
- 单击选中（`SelectedOccurrence`，支持双向绑定）；双击触发
  `OccurrenceDoubleClicked` 以接入编辑器。

## 交互

日类视图支持编辑（月视图暂为只读）：

- **移动** — 拖动色块：垂直方向按吸附改变时间，水平方向改变日期。
  全天条带的色签只能按整天拖动。
- **调整** — 拖动色块的上/下边缘；至少保留一个吸附步长的时长。
- **新建** — 在空白处拖出一个范围：日程以本地化的「新日程」主题加入
  `Appointments`；触发 `AppointmentCreated` 以便打开编辑器（随后双击即可改名）。
- 所有操作吸附到 `SnapMinutes` 网格（默认 15；设为 1 可精确到分钟）。
- 重复系列**不可**拖拽编辑（时间由重复规则决定）——对系列实例的拖动会被忽略，
  请修改规则本身。

```csharp
view.AppointmentEdited += (_, appointment) => Persist(appointment);   // 拖动/调整
view.AppointmentCreated += (_, appointment) => OpenEditor(appointment);
```

## 导航

```csharp
view.Navigate(1);     // 按视图步长前进：一天 / 一周 / 一个月
view.Navigate(-2);    // 后退两步
view.GoToToday();     // 回到今天
view.AnchorDate = new DateTime(2026, 11, 9);  // 或直接绑定
```

`Appointments` 的变更（`ObservableCollection` 增/删/替换）会自动刷新视图。

## 日程数据

普通日程是单个色块。重复日程携带 `SchedulerRecurrence` 规则 — 按
日/周/月/年重复，可设 `Interval`、`Count`/`Until` 限制（RFC 5545 语义：从系列
开始计数，被删除的实例仍占名额）以及按日期的 `Exceptions`：

```csharp
var standup = new SchedulerAppointment("s", "每日站会",
    today.AddHours(9.25), today.AddHours(9.5))
{
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Daily,
        Count = 50,
    },
};

var oneOnOne = new SchedulerAppointment("1", "与 Alice 的一对一",
    monday.AddHours(11), monday.AddHours(12))
{
    ResourceId = "alice",
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Weekly,
        DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday },
    },
};
```

## 引擎（无 UI）

`SchedulerEngine` 无需 STA 即可测试：`Expand` 物化一个区间的所有实例（可按资源
过滤），`LayoutOverlaps` 计算并排泳道，`FindConflicts` 返回同一资源的双重预订，
`ViewRange`/`MonthGrid` 计算视图周期。姊妹实现见[甘特引擎](gantt.md)。
