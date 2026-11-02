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

## 暂不支持（见 ROADMAP）

浮动窗口、拖拽换区、自动隐藏、嵌套分隔组。
