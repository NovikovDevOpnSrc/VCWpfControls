---
title: 菜单
description: 主题化主菜单栏与上下文菜单 —— 每个调色板中的隐式 Menu/MenuItem/ContextMenu 样式，Win11 规格徽标、访问键、快捷键、可勾选项与分隔线。
---

# 菜单

库为主题化标准 WPF 菜单类型 —— `Menu`、`MenuItem` 与 `ContextMenu` —— 提供隐式样式，
由 ThemeManager 合并到整个应用（与主题化 Button/ComboBox/TextBox 相同的机制）。
无需学习新类型：照常写 WPF 菜单，它们就会跟随 Light/Dark/HighContrast。

```xml
<Menu>
    <MenuItem Header="_文件(_F)">
        <MenuItem Header="_打开…" InputGestureText="Ctrl+O" Click="Open" />
        <Separator />
        <MenuItem Header="_退出" InputGestureText="Alt+F4" Click="Exit" />
    </MenuItem>
    <MenuItem Header="_编辑">
        <MenuItem Header="自动保存" IsCheckable="True" IsChecked="True" />
    </MenuItem>
</Menu>
```

- **规格**（按设计指南）：菜单项最小 32px，8px 圆角、4px 内边距的表面卡片，
  hover 走 `HoverBackgroundKey`，禁用项 45% 不透明度。
- **角色**：一个模板服务顶层标题/项与子菜单项/标题 —— 顶层折叠勾选/快捷键/箭头
  列；打开的顶层项显示强调下划线（Win11）。子菜单项显示勾选标记（`IsCheckable` 时）、
  `InputGestureText` 快捷键（弱化色、右对齐）与子菜单箭头。
- **分隔线**通过 `MenuItem.SeparatorStyleKey` 主题化（菜单内的 `<Separator/>` 自动
  采用）—— 1px 的 `SeparatorLineKey` 细线。
- **键盘**：标准 WPF 行为 —— 标题中的 `_` 访问键（Alt+F 打开文件）、方向键导航、
  首字母跳转；焦点使用共享焦点环。
- **高对比度**：所有画笔走调色板令牌（别名实时系统颜色）—— 菜单跟随用户的对比度
  方案。
- **表格列头上下文菜单**（右键列头）自动跟随主题 —— 它由普通
  `ContextMenu`/`MenuItem` 构建。

演示的 Menus 页展示菜单栏（访问键、快捷键、可勾选项、禁用项、分隔线）与上下文
菜单区域。
