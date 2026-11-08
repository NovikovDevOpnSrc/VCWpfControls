---
title: Диаграмма Ганта
description: GanttView — дерево задач + бары таймлайна, стрелки зависимостей, milestone, линия «сегодня», критический путь; движок GanttEngine без UI.
---

# Диаграмма Ганта

Форумное исследование назвало Gantt крупнейшим open-source пробелом WPF; VCWpfControls закрывает его в три слоя.

## Контрол

```xml
<gantt:GanttView Tasks="{Binding Tasks}"
                 Dependencies="{Binding Links}"
                 AutoSchedule="True"
                 ShowCriticalPath="True"
                 TimescaleUnit="Week" />
```

- **Слева**: иерархия задач в TreeListView (Name/Start/Finish/%; иерархия через
  `GanttTask.ParentId`); сплиттер регулирует ширину.
- **Справа**: таймлайн — бары с заполнением по %, ромбы-milestone, стрелки зависимостей
  FS/SS/FF/SF, пунктирная линия «сегодня», зум день/неделя/месяц (`TimescaleUnit`).
- `TaskRowHeight` держит обе половины в lock-step; таймлайн скроллится горизонтально и
  зеркалит вертикальный скролл дерева.
- `AutoSchedule` запускает движок при загрузке/изменении (нарушающие последователи
  сдвигаются); `ShowCriticalPath` обводит критические бары/связи акцентом. Двойной клик
  по бару → `BarDoubleClicked` (на `GanttTimelinePanel`).

## Движок (без UI)

`GanttTask`/`GanttDependency` (FS/SS/FF/SF + лаги/опережения), `GanttEngine.AutoSchedule`
(только вперёд; `GanttCycleException` несёт путь цикла), `CriticalPath` (CPM-флоаты),
`BuildTimescale` (День/Неделя/Месяц, первый день недели по культуре). Unit-тесты без STA.
