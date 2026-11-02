---
title: 主题样式
description: GridControl 默认主题、GridResources 的 ComponentResourceKey 资源键，以及如何在应用中覆盖画刷（强调色、选择、网格线）。
---

# 主题样式

## 默认主题

默认（浅色）主题位于库内的 `Themes/Generic.xaml`，自动生效 —— 无需合并任何字典。
WPF 之所以能找到它，是因为 `GridControl` 重写了自身的 `DefaultStyleKey`。

所有主题取值都是以 **`ComponentResourceKey`** 寻址的资源，这些键作为静态字段暴露
在 `GridResources` 类上；控件模板通过 `DynamicResource` 使用它们。由此带来两个
实用结论：

- 覆盖值可以放在查找链中比主题字典更靠近控件的位置（例如
  `Application.Resources`），**无需替换模板**即可生效；
- 也可以在运行时动态更换。

## 资源键

| 键（`GridResources.*`） | 影响范围 | 默认值 |
|---|---|---|
| `SurfaceBackgroundKey` | 整个表格表面背景 | `#FFFFFFFF` |
| `HeaderBackgroundKey` | 列头背景 | `#FFF3F4F6` |
| `HeaderForegroundKey` | 列头文字 | `#FF374151` |
| `HeaderBorderKey` | 列头边框与分隔线 | `#FFD1D5DB` |
| `SelectionBackgroundKey` | 所选行背景 | `#FFCFE3F5` |
| `CurrentCellBorderBrushKey` | 当前单元格边框 | `#FF2563EB` |
| `GridLineKey` | 行与行之间的水平分隔线 | `#FFE5E7EB` |
| `AccentKey` | 排序指示符、冻结列边缘 | `#FF2563EB` |
| `HoverBackgroundKey` | 行/列头悬停背景 | `#FFEDF2F9` |
| `AccentTextKey` | 强调文字（按下的列头、链接） | `#FF1D4ED8` |

## 覆盖资源键

在 `Application.Resources`（或任意作用域字典）中重新定义同名键 —— 同一个键只需
在查找链中更早解析即可：

```xml
<Application xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <Application.Resources>
        <ResourceDictionary>
            <!-- 无需触碰控件模板即可定制品牌色 -->
            <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.CurrentCellBorderBrushKey}" Color="#FF7C3AED" />
            <SolidColorBrush x:Key="{x:Static grid:GridResources.SelectionBackgroundKey}" Color="#FFEDE9FE" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

这些键是普通的静态字段，因此在 `x:Key` 中写
`{x:Static grid:GridResources.AccentKey}` 即可 —— 不必自己编写
`ComponentResourceKey` 标记。

主题通过 `DynamicResource` 解析画刷，因此运行时覆盖（例如来自主题切换服务）同样
有效。

## 实例级画刷

对于临时性的微调，若干外观项本身就是控件上的普通依赖属性，完全不需要主题资源：

| 属性 | 含义 |
|---|---|
| `RowBackground` | 偶数数据行画刷 |
| `AlternatingRowBackground` | 奇数（交替）数据行画刷 |
| `GridLinesBrush` | 水平网格线画刷 |
| `ShowColumnHeaders` | 完全隐藏表头行 |
| `RowHeight` / `HeaderHeight` | 固定高度（`NaN` 表示按内容） |

```xml
<grid:GridControl ItemsSource="{Binding Employees}"
                  RowBackground="White"
                  AlternatingRowBackground="#FFF9FAFB"
                  GridLinesBrush="#FFE5E7EB"
                  RowHeight="28" />
```

## 最小深色主题配方

目前还没有内置的浅色/深色主题切换（计划于阶段 1.3），但上面这十个键已足以让表格
以深色模式运行：

```xml
<ResourceDictionary xmlns:grid="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <SolidColorBrush x:Key="{x:Static grid:GridResources.SurfaceBackgroundKey}" Color="#FF111827" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderBackgroundKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderForegroundKey}" Color="#FFE5E7EB" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HeaderBorderKey}" Color="#FF374151" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.SelectionBackgroundKey}" Color="#FF1E3A5F" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.CurrentCellBorderBrushKey}" Color="#FF60A5FA" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.GridLineKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentKey}" Color="#FF60A5FA" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.HoverBackgroundKey}" Color="#FF1F2937" />
    <SolidColorBrush x:Key="{x:Static grid:GridResources.AccentTextKey}" Color="#FF93C5FD" />
</ResourceDictionary>
```

按条件合并该字典（默认浅色，应用处于深色模式时切换为它）—— `DynamicResource`
查找会自动采用当前合并的值。

## 更进一步

如需更深入的样式定制，控件在替换整个 `ControlTemplate` 时暴露常见的模板部件
（`PART_ScrollViewer`、`PART_HorizontalScrollBar`、`PART_ColumnHeadersPresenter`）。
不过对绝大多数应用来说，覆盖上述资源键已经足够。

### 文本画笔

`GridResources.TextBrushKey` 承载数据文字颜色并随主题切换（Light `#1F2937`，Dark `#D8DAE3`）。库内所有控件的前景都绑定到它 —— 自定义模板也应如此，不要硬编码颜色。

### UX 标准

库遵循 [ux-checklist.md](../ux-checklist.md)：Fluent 风格排版（正文 14 px，表头 SemiBold、高 32 px）、4 px 间距网格、hover+按下态、本地化空状态、两种主题均满足 WCAG AA 对比度。
