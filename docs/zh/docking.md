---
title: 停靠
description: 可停靠面板，支持分隔条、关闭/显示与 XML 布局持久化。
---

# 停靠

`VCWpfControls.Docking.DockingManager` 将 `DockPane` 面板安置在五个区域：左、上、右、下
（可用分隔条调整大小，为空时自动折叠）以及居中的标签区。

## 快速开始

```xml
<dock:DockingManager xmlns:dock="clr-namespace:VCWpfControls.Docking;assembly=VCWpfControls">
    <dock:DockPane Header="编辑器" Zone="Center">...</dock:DockPane>
    <dock:DockPane Header="属性" Zone="Right" DockWidth="300">...</dock:DockPane>
    <dock:DockPane Header="输出" Zone="Bottom" DockHeight="140">...</dock:DockPane>
    <dock:DockPane Header="工具箱" Zone="Left" IsClosable="False">...</dock:DockPane>
</dock:DockingManager>
```

## 面板

- `Zone`（`Left`/`Top`/`Right`/`Bottom`/`Center`）可随时修改，面板会自动移动。
- `DockWidth`/`DockHeight` 设定侧边区域尺寸；分隔条可实时调整。
- `IsClosable` 控制标签上的关闭按钮；`Close()` 隐藏面板并冒泡 `Closed` 事件（管理器上为
  `PaneClosed`）。`ShowPane`/`HidePane` 管理可见性；隐藏的面板仍保留在布局中。
- 同一区域的面板共享一条标签栏；活动区域中选中的面板通过双向 `ActivePane` 暴露。

## 布局持久化

```csharp
string xml = manager.SaveLayout();   // 紧凑 XML：区域、尺寸、隐藏状态
manager.LoadLayout(xml);             // 按面板 Name（回退到标题）匹配
```

序列化器不依赖 UI（`DockLayoutSerializer`）——布局可存入设置文件并用单元测试校验。

## 自动隐藏

```xml
<docking:DockPane Header="工具" Zone="Left" IsAutoHidden="True" ... />
```

处于自动隐藏的面板会折叠为所在边缘的细标签（左右标签文字旋转 90°）；悬停约 250 毫秒
或单击标签，面板以覆盖层形式弹出——不会挤压布局。鼠标移开约 400 毫秒后自动收回；
弹出层标题栏的 📌 按钮可将面板重新停靠回原区域。中心面板不支持自动隐藏（设置会被
还原）。`IsAutoHidden` 参与 `SaveLayout`/`LoadLayout`；折叠的面板可通过
`AutoHiddenLeftPanes`/`Top`/`Right`/`Bottom` 集合访问。

## 暂不支持（见 ROADMAP）

嵌套分隔组。


## 嵌套分隔组

```xml
<docking:DockPane Header="工具" Zone="Left" />                  <!-- 组 0（主） -->
<docking:DockPane Header="资源管理器" Zone="Left" />             <!-- 与工具同页签 -->
<docking:DockPane Header="输出" Zone="Left" SplitGroup="1" />    <!-- 独立下方槽位 -->
<docking:DockPane Header="错误" Zone="Left" SplitGroup="1" />    <!-- 与输出同页签 -->
```

同一区域中 <c>SplitGroup</c> 相同（默认 0）的面板合并为页签；每个不同的组获得自己
的宿主，区域内通过**独立的 GridSplitter** 堆叠 —— 左/右区域垂直堆叠，上/下区域水平
堆叠。组按升序排列；隐藏组的最后一个面板会移除其宿主（及分隔条）；只要任一组仍有
面板，区域保持可见。`SplitGroup` 通过 `SaveLayout`/`LoadLayout` 往返（`sg` 属性）。
中心面板忽略分组（始终单一宿主）。
