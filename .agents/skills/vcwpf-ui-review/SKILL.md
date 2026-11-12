---
name: vcwpf-ui-review
description: Acceptance review ("приёмка") of WPF UI surfaces — demo pages, library control templates, dialogs — against the project's design profiles (docs/ui-ux-requirements.md, docs/ui-guidelines.md, AGENTS.md) with objective, measured gates adapted from ux-ui-agent-skills. Use whenever the user asks to accept/review/check a UI page or control ("прими", "проверь страницу", "прогони ревью", "соответствует ли гайдлайнам", "UI audit"), before shipping a new demo page or control, and after any theming work — even if the word "review" is not used.
---

# UI acceptance review (vcwpf-ui-review)

You are the acceptance reviewer, not the author's friend. The bar comes from the
project's two profiles — `docs/ui-ux-requirements.md` (MUST/SHOULD, IDs UX-xxx/A11Y-xxx)
and `docs/ui-guidelines.md` (Windows 11 / Fluent XAML rules, condensed in AGENTS.md) —
plus gates adapted from https://github.com/plugin87/ux-ui-agent-skills (MIT; browser-only
gates like responsive-width/axe/RTL are deliberately NOT ported).

Three principles, non-negotiable:

1. **Measured over opinionated.** Every finding carries a number or a `file:line`.
   "Looks fine" is not a verdict.
2. **Couldn't look ≠ passed.** If a gate could not run (RDP disconnected, control not
   realizable), report it as `SKIPPED (reason)` — never count it toward the pass total.
3. **Argue for rejection.** The default verdict is REWORK; ACCEPTED must be earned by
   evidence. Findings that pass all gates but read wrong (an empty state that looks like
   a failure, a toast claiming something that did not happen) still block acceptance.

## Step 0 — Scope

Determine (ask only if not inferable):

- **Surface**: one demo page (`src/VCWpfControls.Demo/Views/*.xaml`), a library
  template (`src/VCWpfControls/Themes/Generic.xaml` + the control's code), or a window.
- **Themes**: default sweep = Light + Dark. Add HighContrast when the surface defines or
  draws colors (custom OnRender, palette values). Use `ThemeManager.ApplyTo` in probes.
- **Depth**: `static` (file-level only — quick pre-check), `full` (static + runtime +
  critique — the default for acceptance), `critique-only`.

Read `references/gates.md` now — it is the gate catalog (S = static, R = runtime,
C = critique) with exact methods, thresholds and the environment traps.

## Workflow (full review)

1. **Static gates (S1–S10)** — grep/read the XAML + code-behind of the surface.
   Report per gate: PASS / FAIL(evidence) / N/A(why).
2. **Runtime gates (R1–R7)** — write a THROWAWAY STA test in
   `tests/VCWpfControls.Control.Tests/` (patterns: `TestWindowHost.Show/Pump`,
   `ThemeManager.ApplyTo`, the VisualBrush offscreen render — see gates.md R5 for the
   environment traps). Run it, capture numbers, DELETE the test afterwards (its findings
   may become permanent regressions later, but that is the author's follow-up, not part
   of the review). If the RDP session is disconnected (empty renders on a known-good
   control), stop runtime gates and mark them SKIPPED — do not debug code.
3. **Critique (C1–C10)** — adversarial pass over what the gates cannot see. For a full
   acceptance, prefer spawning a FRESH general-purpose subagent with only the artifact
   paths + gates.md group C (no conversation history) — a critic holding the maker's
   reasoning reviews the argument instead of the artifact.
4. **Verdict** — the report format below, then a numbered findings list. Severity:
   **Major** (violates a MUST / WCAG / breaks a theme) or **Minor** (polish).

## Report format

```
UI acceptance — <surface> — <date>
Themes: Light, Dark[, HighContrast] | Depth: full

| Gate | Verdict | Evidence |
|---|---|---|
| S1 no hardcoded colors | FAIL | SchedulerSampleView.xaml:88 `Foreground="#FF000000"` |
| R1 contrast (text/surface) | PASS | Light 14.8:1, Dark 13.2:1, HC 21:1 |
| R2 control min-height | PASS | min realized button 32.1px (9 buttons) |
| R5 pixel probe (chip text) | SKIPPED | RDP session renders empty |

Static: 9/10 · Runtime: 5/6 (1 skipped) · Critique findings: 2 Major, 1 Minor

VERDICT: REWORK (2 Major must be fixed; re-run S1, R1 after)

1. [Major] S1 — literal color in ... (file:line). Fix: token `TextBrushKey`.
2. [Minor] C7 — "OK" button without a verb (file:line). Fix: "Save changes".
```

Numbers or `file:line` in every row. No prose rows. The verdict line always states
ACCEPTED or REWORK(+what to re-run).

## After the review

- Do NOT fix findings as part of the review unless the user asks — report them.
- If the user then asks for fixes: each accepted fix of a FAIL gate becomes a permanent
  regression test (project rule: injection-validated), not a throwaway.
- Update the report with re-run results after fixes (gates named in the verdict line).
