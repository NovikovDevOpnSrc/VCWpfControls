---
title: 编辑器
description: 掩码输入与数值调节器。
---

# 编辑器

`VCWpfControls.Editors` 命名空间包含独立的值编辑器，共用库主题（`GridResources.*`）并遵循
“无 UI 引擎”原则。

## MaskedTextBox

```xml
<editors:MaskedTextBox Mask="+7 (000) 000-00-00" />
```

掩码语言：`0` 数字、`9` 可选数字、`#` 数字或符号、`L`/`?` 必填/可选字母、`A`/`a`
必填/可选字母或数字、`C`/`c` 任意字符，`\` 将下一个字符转义为字面量。其他字符均为字面量。

- 字面量自动显示并在输入时跳过；`PromptChar`（默认 `_`）标记空位。
- `Value` 保存输入的字符；`IncludeLiterals="True"` 会包含字面量。
- `IsMaskComplete` 指示所有必填位是否已填。
- Backspace 清除前一个可编辑位，Delete 清除当前位，粘贴按掩码校验。
- 继承自 `TextBox`，选区、剪贴板和样式均照常工作。

## NumericUpDown

```xml
<editors:NumericUpDown MinValue="0" MaxValue="10" Increment="0.25" FormatString="C2" Value="2.5" />
```

- `Value`（decimal，双向绑定）始终被约束在 `MinValue`/`MaxValue` 内。
- 步进方式：自动重复按钮、上/下箭头、PageUp/PageDown（10 步）、鼠标滚轮；输入文本按当前
  区域性解析（非法输入回退）。
- 显示：`DecimalPlaces`（默认按步长推导）或 `FormatString`；`IsReadOnly` 禁止编辑；
  `ValueChanged` 路由事件通知变更。
