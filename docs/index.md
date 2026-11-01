---
title: Vibe Coded WPF Controls — Documentation
description: Free, open-source WPF control library. Stage 1 delivers GridControl — a fast, virtualized, MVVM-friendly data grid. Documentation in English, Russian and Chinese.
---

# Vibe Coded WPF Controls

A free, open-source library of WPF controls (MIT license). **Stage 1** delivers
**GridControl** — a fast, virtualized, MVVM-friendly data grid that stays smooth with
1,000,000+ rows.

Выберите язык документации · Choose your language · 选择文档语言：

- [English documentation](en/index.md)
- [Документация на русском](ru/index.md)
- [中文文档](zh/index.md)

## What is inside (stage 1)

- `GridControl` — data grid with UI virtualization and container recycling
- Six column types: Text, Numeric, CheckBox, ComboBox, Date, Template
- Single- and multi-column sorting, engine-level filtering and grouping
- Row editing with validation (`IDataErrorInfo` / `INotifyDataErrorInfo`)
- Single/extended selection, clipboard copy, keyboard support
- Themeable through `ComponentResourceKey` resource keys

## Links

- GitHub repository: <https://github.com/vibe-coded/VCWpfControls>
- License: [MIT](../LICENSE) — free for any use, commercial included
- NuGet: `dotnet add package VCWpfControls`
- Roadmap: [ROADMAP.md](../ROADMAP.md)

## Installation

```
dotnet add package VCWpfControls
```

Supports **.NET 6** and **.NET 8** on Windows (`net6.0-windows`, `net8.0-windows`).
