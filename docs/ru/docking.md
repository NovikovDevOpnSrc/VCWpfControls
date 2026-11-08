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

## Автоскрытие

```xml
<docking:DockPane Header="Инструменты" Zone="Left" IsAutoHidden="True" ... />
```

Панель в автоскрытии сворачивается в тонкий ярлык у своего края (левые/правые — с
повёрнутым текстом); наведение на ярлык ~250 мс или клик выдвигает панель оверлеем —
компоновку он не сдвигает. Уход курсора ~400 мс сворачивает обратно; кнопка 📌 в шапке
вылета возвращает панель в её зону. Центральные панели автоскрытие игнорируют (флаг
сбрасывается). `IsAutoHidden` сохраняется в `SaveLayout`/`LoadLayout`; свёрнутые панели
доступны в коллекциях `AutoHiddenLeftPanes`/`Top`/`Right`/`Bottom`.

## Пока нет (см. ROADMAP)

Вложенные сплиттер-группы.


## Вложенные сплиттер-группы

```xml
<docking:DockPane Header="Инструменты" Zone="Left" />                  <!-- группа 0 -->
<docking:DockPane Header="Проводник" Zone="Left" />                     <!-- вкладки с Tools -->
<docking:DockPane Header="Вывод" Zone="Left" SplitGroup="1" />          <!-- свой слот ниже -->
<docking:DockPane Header="Ошибки" Zone="Left" SplitGroup="1" />         <!-- вкладки с Вывод -->
```

Панели одной зоны с общим <c>SplitGroup</c> (по умолчанию 0) группируются во вкладки;
каждая отдельная группа получает свой хост внутри зоны с **независимым GridSplitter** —
левые/правые зоны стекаются вертикально, верхние/нижние горизонтально. Группы идут по
возрастанию; скрытие последней панели группы удаляет её хост (и сплиттер); зона видима,
пока в любой группе есть панели. `SplitGroup` переносится через
`SaveLayout`/`LoadLayout` (атрибут `sg`). Центральные панели группы игнорируют.
