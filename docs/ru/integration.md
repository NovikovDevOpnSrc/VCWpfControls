---
title: Интеграция со сторонними библиотеками
description: Проверенные рецепты графиков (LiveCharts2) и риббонов (Fluent.Ribbon) вместе с VCWpfControls — обе MIT.
---

# Интеграция со сторонними библиотеками

Библиотека остаётся без зависимостей; эти рецепты добавляют графики и риббоны из
MIT-экосистемы. Оба **проверены в демо** (страница «Charts & Ribbon») — эта же разметка
компилируется и работает на указанных версиях.

## LiveCharts2 (графики)

`LiveChartsCore.SkiaSharpView.WPF` **2.0.5** (MIT). Два замечания:

- транзитивные SkiaSharp/OpenTK ассеты fallback-ятся на net-framework (NuGet **NU1701**) —
  добавьте `<NoWarn>$(NoWarn);NU1701</NoWarn>` в потребляющий проект (только демо; сама
  библиотека её не ссылает);
- графирование агрегатов грида — годится любой источник группировок/итогов:

```xml
<lvc:CartesianChart x:Name="Chart" />
```

```csharp
var totals = employees.GroupBy(e => e.Department).OrderBy(g => g.Key).ToList();
Chart.Series = new ISeries[]
{
    new ColumnSeries<double>
    {
        Name = "Итог по зарплате",
        Values = totals.Select(g => (double)g.Sum(e => e.Salary)).ToArray(),
    },
};
Chart.XAxes = new Axis[] { new() { Labels = totals.Select(g => g.Key.ToString()).ToArray() } };
```

Для pivot: `PivotResult.RowTotals`/`ColumnTotals` питают серию так же.

## Fluent.Ribbon (риббоны)

`Fluent.Ribbon` **11.0.2** (MIT). Неймспейс — обычная CLR-ссылка:
`xmlns:fluent="clr-namespace:Fluent;assembly=Fluent"` (формы `urn:`/`https:` не
резолвятся). Риббон внутри страницы отлично работает как тулбар:

```xml
<fluent:Ribbon AutomaticStateManagement="False">
    <fluent:RibbonTabItem Header="График">
        <fluent:RibbonGroupBox Header="Группировка">
            <fluent:Button Header="По отделам" Command="{Binding GroupByDeptCommand}" />
        </fluent:RibbonGroupBox>
    </fluent:RibbonTabItem>
</fluent:Ribbon>
```

Для полнооконного хрома разместите приложение в `RibbonWindow` Fluent и держите гриды
VCWpfControls в области контента — темы компонуются.
