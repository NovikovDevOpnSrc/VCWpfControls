---
title: Third-party integration
description: Proven recipes for charting (LiveCharts2) and ribbons (Fluent.Ribbon) alongside VCWpfControls — both MIT.
---

# Third-party integration

The library stays dependency-free; these recipes add charts and ribbons from the MIT
ecosystem. Both are **verified in the demo** (the "Charts & Ribbon" page) — the same
markup compiles and runs with the versions below.

## LiveCharts2 (charts)

`LiveChartsCore.SkiaSharpView.WPF` **2.0.5** (MIT). Two notes:

- its transitive SkiaSharp/OpenTK assets fall back to net-framework (NuGet **NU1701**) —
  add `<NoWarn>$(NoWarn);NU1701</NoWarn>` to the consuming project (demo-only here; the
  library itself never references it);
- chart the grid's aggregates directly — any grouping/summary source works:

```xml
<lvc:CartesianChart x:Name="Chart" />
```

```csharp
var totals = employees.GroupBy(e => e.Department).OrderBy(g => g.Key).ToList();
Chart.Series = new ISeries[]
{
    new ColumnSeries<double>
    {
        Name = "Salary total",
        Values = totals.Select(g => (double)g.Sum(e => e.Salary)).ToArray(),
    },
};
Chart.XAxes = new Axis[] { new() { Labels = totals.Select(g => g.Key.ToString()).ToArray() } };
```

Pivot users: `PivotResult.RowTotals`/`ColumnTotals` feed a series the same way.

## Fluent.Ribbon (ribbons)

`Fluent.Ribbon` **11.0.2** (MIT). The namespace is a plain CLR reference —
`xmlns:fluent="clr-namespace:Fluent;assembly=Fluent"` (the `urn:`/`https:` forms do not
resolve). A ribbon inside a page works fine for toolbars:

```xml
<fluent:Ribbon AutomaticStateManagement="False">
    <fluent:RibbonTabItem Header="Chart">
        <fluent:RibbonGroupBox Header="Grouping">
            <fluent:Button Header="Group by Dept" Command="{Binding GroupByDeptCommand}" />
        </fluent:RibbonGroupBox>
    </fluent:RibbonTabItem>
</fluent:Ribbon>
```

For a full-window chrome, host the app in Fluent's `RibbonWindow` and keep VCWpfControls
grids in the content area — the themes compose (both respect standard theming).
