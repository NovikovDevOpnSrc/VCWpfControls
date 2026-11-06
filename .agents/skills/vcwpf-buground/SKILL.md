---
name: vcwpf-buground
description: >
  Handle a VCWpfControls bug report the evidence-first way: user message with a symptom
  ("не работает", "падает", "налезает", "криво"), a screenshot, a GitHub issue, or a demo
  misbehavior. Use BEFORE writing any fix: this skill prevents the two most expensive
  mistakes made in this project — debugging a stale exe and shipping regression tests that
  never fail. Covers reproduction probes, live-process dumps, fix + injection-validated
  tests, and the push/CI loop.
---

# VCWpfControls bug round

## 0. Before anything

- Check `git status` and `git log origin/main..main` — the user sometimes fixes things
  themselves; review their uncommitted work instead of duplicating it.
- **Stale-exe rule**: a running demo locks bin DLLs AND may be an old build. `taskkill //IM VCWpfControls.Demo.exe //F`
  and `taskkill //IM testhost.exe //F` before building; when a report says "still broken",
  verify the running process start time vs the exe timestamp
  (`Get-Process ... | Select StartTime`) before trusting it. A bug report must be checked
  against a FRESHLY built exe.

## 1. Reproduce with a probe, not a guess

Write a temporary `[StaFact]` probe in `tests/VCWpfControls.Control.Tests` using
`TestWindowHost.Show(grid)` + `TestWindowHost.Pump()`, replicating the exact scenario
(page setup, FrozenColumnCount, DPI, window width). For visual questions render offscreen
(see the `wpf-pixel-forensics` skill) and write diagnostics to
`%TEMP%/probe.txt` — xUnit swallows console output; `cat "$TEMP/probe.txt"` from bash.

Probe hygiene: use SHORT distinctive values ("AB" / "SCROLLSCROLL...") so target zones
don't collide with their own glyphs. If a probe is inconclusive, change ONE variable,
don't pile hypotheses.

For "what is the user actually looking at": attach FlaUI to the RUNNING demo
(`Application.Attach(process)`, walk the tree, dump names+BoundingRectangles to a file).
Note: `Application.Launch` from the test host gets Win32Exception "Отказано в доступе"
here — launch the exe via `cmd /c start` from bash, then attach. Detecting dumps whose
values differ by a constant ~1.5 factor = DPI mixing (physical px vs DIP).

## 2. Known environment traps (check before debugging code)

- All-transparent renders from a test that used to work → the RDP session dropped.
  Run a known-green pixel test; if it also renders empty, it's the environment.
- Core.Tests testhost crashes intermittently on this VM — rerun/CI is the source of truth.
- Empty-render + opaque-render flips between runs = same session issue, not flaky code.

## 3. Fix

Usual suspects by symptom (project history):
- "text climbs into the neighbor column" → column-frame geometry: template margins,
  per-cell clips, measure unboundedness (see GridCellsPanel ArrangeOverride).
- "separator/gridline misaligned" → coordinate-space mixing (panel-local vs grid vs
  physical px); the invariant test is cells-panel origin == headers-presenter origin.
- "line didn't move after X" → OnRender caching: needs InvalidateVisual, not Measure.
- crash on scroll/DP → read-only DP must go through its DependencyPropertyKey.
- XAML `IsChecked="True"` + Checked handler → NRE during InitializeComponent; set in
  code-behind after InitializeComponent.

## 4. Regression test — validate by injection (mandatory)

A geometry/behavior test that never failed proves nothing. Temporarily revert the fix
in source (comment/old value with `// TEMP-BUG`), run the test — it MUST fail — restore
the fix, run again — green. Only then commit. This caught two tautological tests here.

## 5. Ship the round

1. Full `dotnet test` on `tests/VCWpfControls.Control.Tests`; Core.Tests optionally
   locally (CI covers it when the VM flakes).
2. `dotnet build src/VCWpfControls.Demo -c Release` (kills of stale exe first!).
3. CHANGELOG `[Unreleased]` entry (what + why + test note).
4. Commit: **stage explicitly** — `git add <files>`, never `git add -A` (it has swept
   leftover probe files and the local release.yml into commits twice).
5. Delete leftover probes (`rm` the probe .cs) BEFORE `git add tests`.
6. Push with
   `git -c credential.helper= -c "credential.helper=!gh auth git-credential" push origin main`
   (commits touching `.github/workflows` must go through the user).
7. `gh run watch` the CI run; a background watcher printing FAILED may be its own
   timeout — re-check `gh run view`.
8. Tell the user what to verify visually in the fresh demo exe.
