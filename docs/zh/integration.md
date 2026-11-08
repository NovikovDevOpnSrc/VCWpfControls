---
title: 第三方集成
description: 经过验证的图表（LiveCharts2）与功能区（Fluent.Ribbon）集成方案 —— 均为 MIT。
---

# 第三方集成

本库自身零依赖；以下方案从 MIT 生态引入图表与功能区。两者均**已在演示中验证**
（"Charts & Ribbon" 页面）——相同标记可在下列版本下编译运行。

## LiveCharts2（图表）

`LiveChartsCore.SkiaSharpView.WPF` **2.0.5**（MIT）。两点注意：

- 其传递依赖 SkiaSharp/OpenTK 的资产回退到 net-framework（NuGet **NU1701**）——在使
  用方项目中加 `<NoWarn>$(NoWarn);NU1701</NoWarn>`（仅演示需要；库本身不引用它）；
- 直接绘制表格聚合 —— 任何分组/汇总来源都可以：

```xml
<lvc:CartesianChart x:Name="Chart" />
```

```csharp
var totals = employees.GroupBy(e => e.Department).OrderBy(g => g.Key).ToList();
Chart.Series = new ISeries[]
{
    new ColumnSeries<double>
    {
        Name = "工资总额",
        Values = totals.Select(g => (double)g.Sum(e => e.Salary)).ToArray(),
    },
};
Chart.XAxes = new Axis[] { new() { Labels = totals.Select(g => g.Key.ToString()).ToArray() } };
```

透视用户：`PivotResult.RowTotals`/`ColumnTotals` 同样可以直接作为序列来源。

## Fluent.Ribbon（功能区）

`Fluent.Ribbon` **11.0.2**（MIT）。命名空间是普通 CLR 引用：
`xmlns:fluent="clr-namespace:Fluent;assembly=Fluent"`（`urn:`/`https:` 形式无法解析）。
页面内的功能区作为工具栏完全可用：

```xml
<fluent:Ribbon AutomaticStateManagement="False">
    <fluent:RibbonTabItem Header="图表">
        <fluent:RibbonGroupBox Header="分组">
            <fluent:Button Header="按部门" Command="{Binding GroupByDeptCommand}" />
        </fluent:RibbonGroupBox>
    </fluent:RibbonTabItem>
</fluent:Ribbon>
```

要完整窗口装饰，请将应用宿主在 Fluent 的 `RibbonWindow` 中，把 VCWpfControls 表格放在
内容区 —— 两者主题可组合。
