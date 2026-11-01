# Contributing to Vibe Coded WPF Controls

Thank you for your interest in contributing!

## How to contribute

1. Fork the repository and create a branch from `main`.
2. Make your change. Keep public API documented (XML doc comments).
3. Add or update tests:
   - logic that has no UI belongs in `tests/VCWpfControls.Core.Tests`;
   - control behavior belongs in `tests/VCWpfControls.Control.Tests`;
   - end-to-end scenarios belong in `tests/VCWpfControls.UiTests`.
4. Run `dotnet test` — all tests must pass; the build treats warnings as errors.
5. Update `CHANGELOG.md` (Keep a Changelog format).
6. Open a pull request.

## Code style

- C# with nullable reference types enabled; file-scoped namespaces.
- The library targets `net6.0-windows;net8.0-windows` — keep the language level C# 10 compatible in `src/VCWpfControls`.
- The data engine (`Grid/Data`) must stay free of WPF dependencies so it stays unit-testable.
- XML doc comments in English on every public member.

## Reporting bugs

Open an issue with: library version, .NET version, a minimal repro (XAML + code), expected vs actual behavior.

## Licensing

By contributing you agree that your contributions are licensed under the [MIT license](LICENSE).
