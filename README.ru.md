# Vibe Coded WPF Controls

[![CI](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml/badge.svg)](https://github.com/NovikovDevOpnSrc/VCWpfControls/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Бесплатная библиотека WPF-контролов с открытым исходным кодом. **Этап 1** — **GridControl**: быстрая, дружественная к MVVM таблица данных с функционалом не хуже бесплатных аналогов.

**Документация:** [English](README.md) | [Русский](README.ru.md) | [中文](README.zh-CN.md)

## Возможности (GridControl, этап 1)

- Привязка к любому `IEnumerable` (включая `ObservableCollection` и отслеживание изменений)
- Автоматическая генерация колонок (рефлексия + атрибуты `[Display]`/`[ReadOnly]`/`[Browsable]`) и ручные колонки
- Типы колонок: текстовая, числовая, чекбокс, выпадающий список, дата, шаблонная
- Изменение ширины, перетаскивание, скрытие и закрепление колонок (`FrozenColumnCount`)
- UI-виртуализация с рециклингом контейнеров — плавный скролл 1 000 000+ строк
- Сортировка: по одной и нескольким колонкам, индикаторы, свои компараторы
- Выделение: одиночное/расширенное, клавиатура (Ctrl/Shift), чекбокс-выделение, текущая ячейка
- Редактирование: F2/Enter/Esc, read-only колонки, валидация `IDataErrorInfo` / `INotifyDataErrorInfo`
- Фильтрация, группировка, агрегатные итоги (этап 1.2)
- Экспорт в CSV / Excel (.xlsx) / HTML, буфер обмена (этап 1.3)
- Темы Light/Dark, High-DPI, доступность (UI Automation), локализация RU/EN/ZH (этап 1.3)

Полный план — в [ROADMAP.md](ROADMAP.md).

## Установка

```
dotnet add package VCWpfControls
```

Поддерживаются **.NET 6** и **.NET 8** на Windows (`net6.0-windows`, `net8.0-windows`).

## Быстрый старт

```xml
<Window xmlns:vc="clr-namespace:VCWpfControls.Grid;assembly=VCWpfControls">
    <vc:GridControl ItemsSource="{Binding Employees}"
                    AutoGenerateColumns="True"
                    CanUserSortColumns="True"
                    SelectionMode="Extended" />
</Window>
```

Ручные колонки:

```xml
<vc:GridControl ItemsSource="{Binding Employees}">
    <vc:GridControl.Columns>
        <columns:GridTextColumn FieldName="Name" Header="Имя" Width="2*" />
        <columns:GridNumericColumn FieldName="Salary" Header="Зарплата" Width="100" StringFormat="C" />
        <columns:GridDateColumn FieldName="HireDate" Header="Дата приёма" Width="120" />
        <columns:GridCheckBoxColumn FieldName="IsActive" Header="Активен" Width="70" />
    </vc:GridControl.Columns>
</vc:GridControl>
```

## Документация

- [Начало работы (RU)](docs/ru/index.md)
- [Getting started (EN)](docs/en/index.md)
- [入门指南 (ZH)](docs/zh/index.md)

Приложение `src/VCWpfControls.Demo` содержит живой пример на каждую функцию.

## Тестирование

Библиотека разрабатывается с приоритетом тестов:

- `tests/VCWpfControls.Core.Tests` — unit-тесты движка данных (без UI)
- `tests/VCWpfControls.Control.Tests` — STA-тесты самого контрола
- `tests/VCWpfControls.UiTests` — UI-автотесты (FlaUI) на демо-приложении
- `benchmarks/VCWpfControls.Benchmarks` — производительность (BenchmarkDotNet)

## Лицензия

[MIT](LICENSE) — свободное любое использование, включая коммерческое, с отказом от гарантий.
