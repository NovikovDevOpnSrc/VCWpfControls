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
| `docs/` | DocFX: `en/`, `ru/`, `zh/` (источники), `docs/_site/` — СГЕНЕРИРОВАН, не править руками |
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
