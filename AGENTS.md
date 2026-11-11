# AGENTS.md — конвенции проекта VCWpfControls

MIT-лицензированная библиотека WPF-контролов + демо + документация на трёх языках.
Репозиторий: https://github.com/NovikovDevOpnSrc/VCWpfControls. Изменения принимаются
только с тестами и записью в CHANGELOG.

## Карта репозитория

| Путь | Что это |
|---|---|
| `src/VCWpfControls/` | библиотека: `Grid/` (движок `Grid/Data/` — БЕЗ WPF-типов; контролы, примитивы, автоматизация), `PropertyGrid/`, `TreeList/`, `Editors/`, `Docking/`, `Themes/` (Generic/Light/Dark), `Localization/` (Strings.resx + ru-RU/zh-CN) |
| `src/VCWpfControls.Demo/` | демо-приложение (страница на каждую функцию; целевой стенд UI-тестов) |
| `tests/VCWpfControls.Core.Tests/` | unit-тесты движка, БЕЗ STA |
| `tests/VCWpfControls.Control.Tests/` | STA-тесты контролов (`[StaFact]`, `TestWindowHost`) |
| `tests/VCWpfControls.UiTests/` | FlaUI-тесты демо-приложения |
| `benchmarks/` | BenchmarkDotNet; история оптимизаций — `benchmarks/RESULTS.md` |
| `docs/` | DocFX: `en/`, `ru/`, `zh/` (источники), `docs/_site/` — СГЕНЕРИРОВАН, не править руками; `docs/ui-ux-requirements.md` — UI/UX-профиль |
| `.github/workflows/` | `ci.yml`, `docs.yml`, `release.yml` (Trusted Publishing на nuget.org) |
| `VCWpfControls.slnx` | решение в формате SDK 10 (XML) — не «чинить» на .sln |
| `Directory.Packages.props` | Central Package Management — версии пакетов ТОЛЬКО здесь |

## Языки и их ограничения

- **C#**: библиотека мульти-таргетится на `net6.0-windows;net8.0-windows` → в СРКах
  библиотеки только **C# 10**: никаких primary constructors, `ArgumentException.ThrowIf*`,
  коллекционных выражений. Тесты/демо/бенчмарки — .NET 8, можно новее.
- **XAML**: шаблоны в `Themes/Generic.xaml` (+ Light/Dark переопределяют ресурсы через
  `GridResources.*Key`). Готовые элементы ячеек получают отступы САМИ (Margin 8,0,6,0;
  числа справа 6,0,8,0), панели строк — БЕЗ margin (иначе ломается выравнивание с
  заголовками — уже было). `IsChecked="True"` + обработчик Checked = NRE при
  InitializeComponent; выставлять в code-behind после `InitializeComponent()`.
- **Документация**: каждый пользовательский API описан в `docs/en|ru|zh/<topic>.md`
  СИНХРОННО на трёх языках. `docs/_site/` не редактировать.
- **PowerShell в workflows**: `pwsh`; шаги с `env:` вместо `if: secret` (GitHub
  отклоняет секреты в условиях). YAML с `${{ }}` править инструментом Edit, не python.
- **Python для скриптов правок**: только heredoc `python - <<'PYEOF'` (бэктики в
  double-quoted `-c` съедаются bash), и НИКОГДА не передавать невалидный `newline=`
  в `io.open(..., "w")` — файл обнуляется ДО валидации аргументов.

## Темизация — правила (уроки bug-rounds)

1. **Никаких литеральных цветов** в XAML/коде библиотеки — только токены `GridResources`
   (`DynamicResource` / `TryFindResource` / `SetResourceReference`). Хардкод = будущий
   баг тёмной темы (класс: чёрные имена PropertyGrid, белые чётные строки).
2. **Код-созданные визуальные элементы** — `SetResourceReference(токен)`: наследование
   Foreground от контрола до code-built TextBlock'ов НЕ доходит (доказано пробой).
3. **Новая тема/палитра** = определить ВСЕ кистевые токены; тест
   `ThemeManagerTests.EveryTheme_DefinesEveryToken` охраняет контракт.
4. **App-level свап тем** — только через `ThemeManager` (библиотека); per-grid —
   `GridThemeManager` (opt-in). Не дублировать механизм свапа.
5. При удалении общего margin/стиля из шаблона — grep по шаблону на остальные КОПИИ
   (фильтр-строка и summary держали свои Margin="8" после чистки ячеек).

## UI/UX-требования

Любая работа над пользовательским интерфейсом (новые контролы, шаблоны, взаимодействие,
темы, доступность) ЧИТАЕТ `docs/ui-ux-requirements.md` (профиль «Windows Desktop UI and
UX Requirements», MUST/SHOULD-требования с ID вида UX-xxx/A11Y-xxx) и применяет
подходящие пункты к изменяемой поверхности; нарушения MUST — баг. Существующие контролы
библиотеки сложились до профиля — расхождения исправляем по мере касания, отдельным
аудитом (как rounds Oracle/Fiori/Fluent). Мастер-копия профиля живёт снаружи
(`Z:\Work\VibeCoding\windows-ui-ux-requirements-for-ai.md`); обновил мастер —
перенеси копию в `docs/`.

## XAML-гайдлайны дизайна (Windows 11 / Fluent)

Полная версия — `docs/ui-guidelines.md` (мастер-копия снаружи:
`Z:\Work\VibeCoding\UI_GUIDELINES.md`; обновил мастер — перенеси копию). Конденсированные
правила для любой XAML-работы (библиотека и демо):

**Сетка и отступы**
- `Margin`/`Padding` кратны 4 (лучше 8): `4, 8, 12, 16, 20, 24, 32, 48`. Произвольные
  значения (`13,7,5,11`) запрещены.
- Отступ окна/страницы от краёв ≥ `20–24px`. Подпись → поле `4–6px`; поля одной группы
  `12–16px`; разные секции `24–32px` (закон близости).
- Разделяй блоки whitespace'ом и контрастом фона, НЕ рамками вокруг каждого элемента.
- Без жёстких `Width`/`Height` для контейнеров и текста: `*`/`Auto`, `Min/MaxWidth`.

**Типографика**
- Шрифт — токен `FontFamilyKey` (Segoe UI Variable → Segoe UI fallback).
- Лесенка: Page Title `24–28` SemiBold; Section `16–18` SemiBold; Body `14`; Field
  Label `12–14` Medium; Caption `12` приглушённо (`MutedTextKey`).
- Текст слева; числа в таблицах справа.

**Цвет и слои** (дополняет правила темизации выше)
- Слои Fluent: фон приложения (Base) → карточки/панели (Card) с границей `1px` и
  `CornerRadius="8"`.
- Акцент — ТОЛЬКО одно главное действие на экране (Primary CTA) + активная навигация;
  второстепенные кнопки нейтральны. Семантические цвета строго по назначению
  (критично/предупреждение/успех).

**Контролы и формы**
- `CornerRadius`: кнопки/поля/пункты списков `4–6`; карточки/диалоги `8`.
- Мин. высота кнопок и полей `32px`; мин. зазор между кликабельными кнопками `8px`.
- Подписи НАД полями (не слева); ширина полей в формах ≤ `400–480px`.
- Кнопки действий — справа внизу; порядок Windows: `[Сохранить] [Отмена]`; текст —
  глагол действия («Удалить файл», не «ОК»).

**UX-паттерны**
- Операция > `300 мс` → индикатор загрузки + блокировка повторного нажатия.
- Пустые состояния: центрированная иконка + заголовок («Пока нет проектов») + кнопка
  создания первого элемента (у грида — `PART_EmptyState`).
- Деструктивные действия — с подтверждением и описанием последствий; валидация мягкая
  (`LostFocus`), текст ошибки под полем, БЕЗ `MessageBox`.
- Минимум модалок: инфо-баннеры/уведомления вместо `MessageBox.Show()`.
- Клавиатура: `Tab` между полями, `Enter` подтверждение, `Esc` закрытие.

**Чистый XAML**
- Общие параметры (отступы, шрифты, скругления) — в `ResourceDictionary`, не в каждом
  контроле.
- Векторные иконки (`Path`/SymbolIcon), не PNG. `AutomationProperties.Name` для
  иконочных кнопок (охраняется FlaUI-регрессионом all-buttons-named). `x:Name` только
  для элементов, к которым реально обращается code-behind.

## Сборка и тесты

```bash
dotnet build VCWpfControls.slnx -c Release          # 0 warnings (TreatWarningsAsErrors + latest analyzers)
dotnet test tests/VCWpfControls.Control.Tests/...   # STA-тесты
```

- Анализаторы строгие: культура-зависимые API требуют явный CultureInfo (CA1310/CA1305),
  `EndsWith(char)` вместо строкового оверлоада, и т.п. — это ошибки, не предупреждения.
- Перед пересборкой убивай залоченные процессы: `taskkill //IM testhost.exe //F` и
  `taskkill //IM VCWpfControls.Demo.exe //F`.
- Пути проектов не печатать руками (опечатки `VCWwpControls`): `$(find tests -name "*.csproj" -path "*X*")`.

## Архитектурные правила

- Движок данных (`Grid/Data/`) не знает WPF — тестируется без STA. UI-слой в
  `Grid/Primitives/`, общая геометрия колонок в `GridLayoutController`.
- Сентинел add-row живёт ТОЛЬКО в обёртке `GridItemsView` — внутрь `DataEngine.View`
  его класть нельзя (рассинхрон индексов ListCollectionView).
- Виртуализация: правки, затрагивающие раскладку ячеек, должны инвалидировать
  `InvalidateVisual` (OnRender кэшируется; Measure/Arrange при неизменном RenderSize
  его не перезапускают).
- Регрессионный тест поведения/геометрии обязан быть проверен инъекцией старого бага
  (вернул фикс → тест красный → вернул фикс).

## Окружение этой машины (Windows + RDP)

- Захват экрана над RDP даёт чёрные кадры; пиксельная проверка — через offscreen-рендер
  (VisualBrush-захват; прямой `RenderTargetBitmap.Render(grid)` даёт пустой растр).
  Если даже это пусто — отключилась RDP-сессия, это не код.
- Core.Tests локально может падать по testhost-крешу (окружение) — истина в CI.
- Коммит, трогающий `.github/workflows/`, пушится ТОЛЬКО кредитами пользователя
  (у gh-токена workflow-scope у активного аккаунта теперь есть, но пушить всё равно
  надёжнее из пользовательского git).
- Скилы в `.agents/skills/`: `vcwpf-release` (релизный runbook), `vcwpf-buground`
  (приём багрепортов), `wpf-pixel-forensics` (разбор скриншотов).

## Релиз

Полный runbook — скил `vcwpf-release`. Кратко: бамп `VersionPrefix` в
`Directory.Build.props` (НЕ забыть — уже был инцидент), CHANGELOG, тег `vX.Y.Z`, пуш,
проверка guard'а «Verify package version matches tag» и индекса nuget.org (~10 минут).
