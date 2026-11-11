---
title: 主题样式
description: GridControl 默认主题、GridResources 的 ComponentResourceKey 资源键，以及如何在应用中覆盖画刷（强调色、选择、网格线）。
---

# 主题样式

## ThemeManager — 集中切换器

库内置应用级主题管理器（DevExpress/Fiori 模式，遵循微软「在应用层重定义主题资源」
的建议）：

```csharp
using VCWpfControls.Themes;

ThemeManager.Apply(GridTheme.Dark);                          // 应用级调色板切换
ThemeManager.SetAccent(new("Purple", "#8B5CF6", "#B9A2F8")); // 品牌覆盖，两种主题
ThemeManager.SetAccent(null);                                // 恢复主题品牌色
ThemeManager.ThemeChanged += (_, _) => RefreshIcons();       // 重新读取缓存的画笔
```

所有控件都通过 `GridResources` 键以 `DynamicResource` 解析画笔 — 整个 UI 即时
跟随，无需重载或逐控件接线。配合读取系统偏好（`AppsUseLightTheme`）即可实现
.NET 9 `ThemeMode="System"` 的等价物（演示的 Settings 页正是如此，还带 2 秒的
系统主题监视器）。`GridThemeManager.Apply(grid, theme)` 保留用于单控件场景。
调色板完备性由契约测试守护：每个主题字典定义每个画笔令牌。

## 高对比度（Windows 对比度方案）

`GridTheme.HighContrast` 是无障碍调色板：**每个**画笔令牌都别名到实时的系统颜色
（通过 Color 上的 `DynamicResource` 引用 `SystemColors`），因此 UI 跟随用户选择的任何
Windows 对比度方案，且切换方案时立即重绘 — 无需重启应用。用户的无障碍设置永不被
覆盖（UX 配置 A11Y-003）：

- 文字与表面 → `Window`/`WindowText`；线条、条形与分隔线 → `WindowFrame`；
- 选中态成对使用 `Highlight`/`HighlightText`：选中行文字随背景翻转
  （`GridResources.SelectionTextKey`），反转方案（白色高亮、黑色文字）下依然可读，
  而不是消失；
- 强调色跟随 `HotTrack`；该主题激活期间忽略 `SetAccent` 的覆盖，切回 Light/Dark 时
  自动恢复；
- 装饰性状态（悬停底色、隔行条纹）折叠为窗口色。

演示的 Settings 页提供该模式；`System` 模式下对比度方案解析为 HighContrast
（监视器在浅色/深色偏好之外还轮询 `SystemParameters.HighContrast`）。

## Fluent 2 对齐

默认 Light/Dark 调色板遵循 **Fluent 2** 基础（微软设计系统）：品牌主色
`#0F6CBD`（亮）/ `#479EF5`（暗）、带色调的中性梯度（文字 `#242424`/`#FFFFFF`、表面
`#FAFAFA`/`#1F1F1F`、描边 `#E0E0E0`/`#3D3D3D`）、品牌色调选中态（`#EBF3FC`/`#115EA3`）
与中性悬停。调色板之上暴露 Fluent 基础令牌：

- `GridResources.FontFamilyKey` — 「Segoe UI Variable Text」，回退 Segoe UI；
- 字号梯度 — `FontSizeCaptionKey`（12）/ `FontSizeBodyKey`（14，控件默认）/
  `FontSizeSubtitleKey`（16）/ `FontSizeTitleKey`（20）；
- `GridResources.FocusVisualKey` — 2px 圆角强调色焦点环（表格与列头）；
- 圆角遵循 Fluent 4/8 约定（控件 4，卡片 8）。

应用可通过相同键覆盖任意令牌；高密度图表内部刻意保留更小字号。

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
| `SelectionTextKey` | 所选行文字（高对比度下为 HighlightText） | `#FF1F2937` |
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

`ThemeManager.Apply(GridTheme.Dark)` 会自动完成；手动写出的 — 上面的这些键已足以让
表格以深色模式运行：

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
