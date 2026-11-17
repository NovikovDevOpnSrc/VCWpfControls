---
title: 状态栏
description: 每个调色板中的隐式主题化 StatusBar/StatusBarItem 样式 —— 说明文字字号、1px 顶部分隔线、主题化条目分隔；演示外壳停靠实时状态栏（页面 + 主题）。
---

# 状态栏

库为主题化标准 WPF `StatusBar` 与 `StatusBarItem` 提供隐式样式，由 ThemeManager 合并
到整个应用 —— 照常写 WPF 状态栏，它们就会跟随 Light/Dark/HighContrast。

```xml
<StatusBar>
    <StatusBarItem>
        <TextBlock Text="{Binding Status}" />
    </StatusBarItem>
    <StatusBarItem>
        <!-- 条目分隔是主题化的 1px Rectangle -->
        <Rectangle Width="1" Margin="4,6,4,6"
                   Fill="{DynamicResource {x:Static grid:GridResources.SeparatorLineKey}}" />
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <TextBlock Text="{Binding ItemCount}" />
    </StatusBarItem>
</StatusBar>
```

- **规格**（按设计指南）：28px 条带上的说明文字字号（12）、表面背景、1px
  `SeparatorLineKey` 顶部分隔线、8px 条目节奏。
- **条目分隔**：WPF 的 `StatusBarItem` 没有 `SeparatorStyleKey`（与 `MenuItem`
  不同）—— 普通 `<Separator/>` 会画一条水平线。按上面的方式使用主题化的 1px
  `Rectangle`。
- **高对比度**：所有画笔走调色板令牌（别名实时系统颜色）。
- 演示外壳底部停靠**实时状态栏**：当前页面标题与当前主题名，随导航和
  `ThemeManager.ThemeChanged` 更新。

## 宣告变更

状态栏报告自行变化的状态 —— 用 live 标记文本区域，让辅助技术播报更新：
`AutomationProperties.LiveSetting="Polite"`（演示的主题条目即如此）。
