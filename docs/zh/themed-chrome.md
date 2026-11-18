---
title: 主题化外观
description: 其余 WPF 原生表面 —— 选项卡、展开器、分组框、滑块、进度条、列表、树、工具提示 —— 通过隐式样式在每个调色板中主题化。
---

# 主题化外观

除库定义的控件外，每个常用的 WPF 原生表面都通过由 ThemeManager 合并的隐式样式
跟随 Light/Dark/HighContrast：`Button`、`TextBox`、`ComboBox`/`ComboBoxItem`、
`CheckBox`、`RadioButton`、`Hyperlink`、`ToggleButton`、`Menu`/`MenuItem`/
`ContextMenu`、`StatusBar`/`StatusBarItem` —— 以及「外观补全」集合：

- **TabControl / TabItem** —— 透明条带，1px 下划线连接页面；选中选项卡带强调
  下划线（与打开的菜单一致）；选项卡 32px，带键盘焦点视觉；
- **Expander** —— 带旋转箭头的切换标题；内容在下（Space 切换 —— 原生通道）；
- **GroupBox** —— 1px 圆角边框，标题打断上边框；
- **Slider** —— 轨道使用 `BarFillKey`，强调色圆形滑块带表面描边（必需的
  `PART_Track` 保持原生箭头/键盘）；
- **ProgressBar** —— `BarFill` 轨道 + 强调色指示器（`PART_Track`/
  `PART_Indicator`；指示器尺寸由控件自身驱动）；
- **ListBox / ListBoxItem** 与 **TreeView / TreeViewItem** —— 表面 + 边框、32px
  条目、悬停与 SelectionBackground/SelectionText 配对（表格行模式）；树条目每级
  缩进 16px，带主题化展开器；
- **ToolTip** —— 表面卡片（4px 圆角、说明文字字号）。

调色板对等回归守护整个家族：隐式样式类型集在 Light/Dark/HighContrast 中必须
一致。高对比度借助令牌别名 —— 表面跟随用户的对比度方案。

演示的 **Chrome** 页实时展示完整集合。
