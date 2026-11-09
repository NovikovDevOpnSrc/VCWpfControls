---
title: Планировщик
description: SchedulerView — календарные представления день/рабочая неделя/неделя/месяц над повторяющимися встречами: шкала времени, полосы для пересечений, полоса целых дней и месячная сетка; на безинтерфейсном SchedulerEngine.
---

# Планировщик

Планировщик (календарь в стиле Outlook) выходит слоями: безинтерфейсный движок (этап 1)
и контрол (этап 2). Перетаскивание/растягивание встреч — в планах (этап 3).

## Контрол

```xml
<scheduler:SchedulerView Appointments="{Binding Appointments}"
                          ViewType="WorkWeek"
                          DayStartHour="8"
                          DayEndHour="20"
                          OccurrenceDoubleClicked="OpenEditor" />
```

- **Day / WorkWeek / Week / Month** — представления (`ViewType`); `AnchorDate` задаёт
  показываемый день/неделю/месяц (по умолчанию — привязка к сегодня).
- **Дневные представления**: закреплённые шкала времени и заголовки дней, по колонке на
  день. Окно `DayStartHour`/`DayEndHour` (по умолчанию 0–24); блоки вне окна обрезаются.
  Пересекающиеся встречи делят колонку на **полосы** (кластеризация интервалов по дню);
  встречи через полночь рисуются по обрезанному сегменту на каждый день.
- **Полоса whole-day**: встречи с `IsAllDay` рендерятся чипами над часовой сеткой, одна
  строка на полосу пересечения (максимум 3, остальные обрезаются); чип тянется на все свои дни.
- **Месяц**: сетка из шести недель (42 ячейки, первый день недели из культуры), заголовки
  дней недели, номера дней с затемнением соседних месяцев, подсветка сегодня, до 2 чипов
  на ячейку и строка «+N ещё».
- Колонка/ячейка сегодняшнего дня подсвечена, через неё идёт пунктирная **линия сейчас**.
- Клик выделяет встречу (`SelectedOccurrence`, двусторонняя привязка); двойной клик даёт
  `OccurrenceDoubleClicked` для открытия редактора.

## Навигация

```csharp
view.Navigate(1);     // вперёд на шаг представления: день / неделя / месяц
view.Navigate(-2);    // на два шага назад
view.GoToToday();     // вернуться к сегодня
view.AnchorDate = new DateTime(2026, 11, 9);  // или привязать
```

Изменения `Appointments` (`ObservableCollection`: добавление/удаление/замена) обновляют
представления.

## Встречи

Обычная встреча — одиночный блок. Повторяющаяся несёт правило `SchedulerRecurrence` —
ежедневно/еженедельно/ежемесячно/ежегодно с `Interval`, лимитами `Count`/`Until`
(семантика RFC 5545: отсчёт от начала серии, удалённые вхождения расходуют свой слот)
и по-датными `Exceptions`:

```csharp
var standup = new SchedulerAppointment("s", "Ежедневный стендап",
    today.AddHours(9.25), today.AddHours(9.5))
{
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Daily,
        Count = 50,
    },
};

var oneOnOne = new SchedulerAppointment("1", "1:1 с Алисой",
    monday.AddHours(11), monday.AddHours(12))
{
    ResourceId = "alice",
    Recurrence = new SchedulerRecurrence
    {
        Pattern = SchedulerRecurrencePattern.Weekly,
        DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday },
    },
};
```

## Движок (без UI)

`SchedulerEngine` тестируется без STA: `Expand` материализует вхождения диапазона (с
фильтром по ресурсу), `LayoutOverlaps` раскладывает полосы бок-о-бок, `FindConflicts`
возвращает двойные бронирования по ресурсу, `ViewRange`/`MonthGrid` считают периоды
представлений. См. родственный паттерн в [движке Ганта](gantt.md).
