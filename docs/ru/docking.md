---
title: Докинг
description: Закрепляемые панели со сплиттерами, закрытием/показом и сохранением layout в XML.
---

# Докинг

`VCWpfControls.Docking.DockingManager` содержит панели `DockPane` в пяти зонах: слева, сверху,
справа и снизу (изменяемые сплиттерами, сворачиваются, когда пусты) и таб-зона в центре.

## Быстрый старт

```xml
<dock:DockingManager xmlns:dock="clr-namespace:VCWpfControls.Docking;assembly=VCWpfControls">
    <dock:DockPane Header="Редактор" Zone="Center">...</dock:DockPane>
    <dock:DockPane Header="Свойства" Zone="Right" DockWidth="300">...</dock:DockPane>
    <dock:DockPane Header="Вывод" Zone="Bottom" DockHeight="140">...</dock:DockPane>
    <dock:DockPane Header="Инструменты" Zone="Left" IsClosable="False">...</dock:DockPane>
</dock:DockingManager>
```

## Панели

- `Zone` (`Left`/`Top`/`Right`/`Bottom`/`Center`) — можно менять в любой момент, панель
  переместится.
- `DockWidth`/`DockHeight` задают размер боковых зон; сплиттеры меняют его на лету.
- `IsClosable` управляет кнопкой закрытия на вкладке; `Close()` скрывает панель и бросает
  всплывающее событие `Closed` (на менеджере — `PaneClosed`). `ShowPane`/`HidePane` управляют
  видимостью; скрытые панели остаются в layout.
- Панели зоны делят одну полосу вкладок; выбранная панель активной зоны доступна как
  двустороннее свойство `ActivePane`.

## Сохранение layout

```csharp
string xml = manager.SaveLayout();   // компактный XML: зоны, размеры, скрытость
manager.LoadLayout(xml);             // сопоставление по Name панели (или заголовку)
```

Сериализатор не зависит от UI (`DockLayoutSerializer`) — layout можно хранить в настройках и
проверять unit-тестами.

## Пока нет (см. ROADMAP)

Auto-hide и вложенные сплиттер-группы.
