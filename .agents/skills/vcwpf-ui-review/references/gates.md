# Gate catalog (vcwpf-ui-review)

Thresholds: WCAG 2.2 text contrast **4.5:1** (normal) / **3:1** (large text ≥24px, or
≥18.66px bold; also UI components and meaningful graphics). Min interactive height
**32px** (Win11/our guidelines); icon-only floor **24px** (WCAG 2.5.8). Min gap between
adjacent clickables **8px**. Spacing grid: multiples of 4 (preferred 8). Type ladder:
12/14/16/20/24/28.

## A. Static gates (S) — read the files, no WPF needed

### S1 — No hardcoded colors
Library XAML/code: no literal `#RGB`/`Colors.X`/`Brushes.X` outside `Themes/*.xaml`
(allowed: `?? Brushes.X` last-resort fallbacks in OnRender, `Path.Data` geometry).
Demo pages: colors only through `{DynamicResource {x:Static grid:GridResources.*Key}}`.
Grep patterns: `#[0-9A-Fa-f]{6,8}`, `Brushes\.(?!Transparent)`, `Colors\.`.
This is AGENTS.md theming rule #1 — a violation is always **Major**.

### S2 — Token usage complete
Code-created visual elements set colors via `SetResourceReference` (Foreground
inheritance does NOT reach code-built TextBlocks — proven). New visual roles resolve
through `GridResources` tokens; a NEW token must exist in ALL FOUR dictionaries
(Generic/Light/Dark/HighContrast) — `ThemeManagerTests.EveryTheme_DefinesEveryToken`
guards presence.

### S3 — Spacing grid
Every `Margin`/`Padding` value ∈ multiples of 4: 0,4,8,12,16,20,24,32,48. Flag anything
else (e.g. `13,7,5,11`). The 4-6px label→field gap is allowed (4 is on the grid).

### S4 — Type ladder + muted text
`FontSize` ∈ {12,14,16,20,24,28} or a token reference. Captions/secondary text =
`MutedTextKey`, not `Opacity="0.x"` hacks. Page title 24–28 SemiBold; section 16–18
SemiBold; body 14.

### S5 — Control metrics (declared)
Buttons/editors: `MinHeight` ≥ 32 (or covered by an implicit style). Icon-only buttons:
≥24px + `AutomationProperties.Name`. Clickable neighbors: gap ≥ 8.

### S6 — Accessible names
Icon-only interactive elements carry `AutomationProperties.Name` (the all-buttons-named
FlaUI regression exists app-wide — check NEW controls outside the FlaUI sweep).

### S7 — Forms and alignment
Labels ABOVE fields (not left), spacing 4–6. Text left-aligned; numbers in tables
right-aligned. Form field widths ≤ 400–480 even when the window is wider.

### S8 — Modality hygiene
No `MessageBox.Show` for info; destructive actions confirm with consequences described.
Buttons bottom-right, Windows order `[Save][Cancel]`, verb text («Delete file», not «OK»).

### S9 — Semantic color by intent
Accent ONLY for the single primary CTA + active navigation. Destructive control must not
wear accent/primary as its main fill. Semantic colors strictly by meaning.

### S10 — Adaptive layout
No hard `Width`/`Height` on containers/text blocks — `*`/`Auto` + `Min/Max`. Page/window
padding ≥ 20. `x:Name` only for elements the code-behind actually references.

## B. Runtime gates (R) — STA probes, throwaway tests, DELETE after

Environment check FIRST: render a known-good control offscreen (VisualBrush method,
R5). All-empty output ⇒ RDP session disconnected ⇒ mark R1–R7 SKIPPED, report it.

If the surface lives in the Demo assembly: `Control.Tests` has NO reference to it —
either replicate the page's controls inline (identical instances + layout; the measured
metrics come from library styles, not the page) or run the probe from UiTests.

### R1 — Contrast per theme and state (the XIAS state-aware WCAG idea)
Resolve the token pairs from each theme dictionary and compute WCAG ratios:
TextBrush×SurfaceBackground, MutedText×SurfaceBackground, HeaderForeground×HeaderBackground,
SelectionText×SelectionBackground, AccentText×HeaderBackground (sorted header), text on
Accent (if used as CTA fill). Sweep: Light, Dark, and HighContrast when the surface
draws colors. Loading a palette in an STA host:

```csharp
_ = new GridControl(); // registers pack:// (no Application in test hosts)
var dict = new ResourceDictionary {
    Source = new Uri("pack://application:,,,/VCWpfControls;component/Themes/Dark.xaml") };
var text = ((SolidColorBrush)dict[GridResources.TextBrushKey]).Color;
```

Deferred `{DynamicResource SystemColors…}` colors (HighContrast.xaml) DO evaluate on
`dict[key]`. Contrast:

```csharp
static double Lum(Color c) { // sRGB relative luminance
    double F(int v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
    return 0.2126 * F(c.R) + 0.7152 * F(c.G) + 0.0722 * F(c.B);
}
static double Contrast(Color a, Color b) { var l1 = Math.Max(Lum(a), Lum(b)); var l2 = Math.Min(Lum(a), Lum(b)); return (l1 + 0.05) / (l2 + 0.05); }
```

Report EVERY pair's numbers (see the report table format). Below 4.5:1 for text or 3:1
for large/UI = FAIL, Major. Element-level custom colors (BarBrush etc.) get measured the
same way against their actual background.

**High-contrast exemption:** WCAG 1.4.3 exempts text rendered by the OS high-contrast
mode, and pairs aliased to `SystemColors` (Highlight/HighlightText etc.) belong to the
user's accessibility palette — overriding them would violate A11Y-003. When an HC pair
measures below 4.5:1, report the measured number and verdict `EXEMPT (OS palette)`,
never a FAIL, never a "fix".

### R2 — Realized metrics
`TestWindowHost.Show(surface); Pump();` then walk the tree (`VisualTreeHelper`): every
Button/RepeatButton/ToggleButton/Editor — `ActualHeight ≥ 32` (icon-only ≥ 24),
`ActualWidth ≥ 24`; adjacent clickables in the same panel: horizontal gap ≥ 8. Report
the minimums found with counts.

**Measure inside the surface's REAL container bounds.** A row that fits a bare test
window can overflow its actual host: a flyout's `Popup`/`Border` may clamp width to
(e.g.) 380 minus padding while the replica panel was 560 wide. Before reporting PASS on
"fits", read the host XAML (popup Border `MaxWidth`/`Padding`, scroll viewer
orientation) and compare the measured row width against the USABLE width — a
non-wrapping row clipped by its host is a Major (options unreachable by mouse).

### R3 — Names in the automation tree
Walk the realized tree, `UIElementAutomationPeer.CreatePeerForElement`, collect elements
whose visible content is glyph/icon-only — name must be non-empty (Localization-aware:
localized strings are fine).

### R4 — Focus visibility
`FocusVisualStyle` resolves (not null) on interactive controls; the shared
`GridResources.FocusVisualKey` is the default. Spot-check the Tab chain reaches the
primary interactive controls of the surface (`KeyboardNavigation`/Tab order by
declaration order).

### R5 — Pixel probe (color/geometry questions only)
Offscreen render — direct `RenderTargetBitmap.Render(element)` is EMPTY in this host;
capture through a VisualBrush wrapper (see `ColumnSeparatorTests` / `GanttPolishTests`
for working copies). Sample the region, assert on luma/numbers.

KNOWN MASKING TRAPS (bit twice — both documented from real incidents):
- Themed WHITE label glyphs can sit inside the sampled region (bar labels at
  `rect.Right+4`) — disable labels or shrink the region around the artifact only.
- Row-0 artifacts sit ~4px under a header whose DATE TEXT is white — clamp the region
  to `y ≥ HeaderHeight`.
- Pbgra is premultiplied, byte order B,G,R,A. Assert DELTAS vs surface, not absolutes.

### R6 — Empty state
Grid-family surfaces: empty ItemsSource ⇒ `PART_EmptyState` visible with localized
"No data". Empty screens show a centered hint + a create affordance (guidelines UX
pattern), not a blank pane.

### R7 — Long-operation semantics (>300ms)
Progress shown + re-click blocked (IsEnabled/false-while-busy); loading NEVER drawn with
the disabled dimming (reads "you cannot" instead of "wait" — an XIAS classic). Verify in
code paths that mutate >300ms.

## C. Critique (C) — adversarial, argues for REWORK; findings need evidence, not numbers

Adapted from the XIAS `/critique` doctrine: a page passing every gate can still deserve
rework. Run C on a REALIZED surface (runtime renders or a fresh look at the running
demo); prefer a fresh subagent holding only the artifact + this list.

- **C1 Focal point** — one clear lead per screen (biggest number/action); a wall of
  equal-weight cards with no hierarchy fails.
- **C2 Display scale** — title ≥ ~1.7× body (24 vs 14 ok); everything-14 fails.
- **C3 Uniformity slop** — N identical cards doing different jobs; same radius/shadow
  everywhere; decorative gradients. Vary shape by meaning.
- **C4 Empty ≠ broken** — empty states must read "nothing here yet + how to start",
  not "the page failed".
- **C5 Loading ≠ disabled** — busy states keep full strength + spinner; disabled
  dimming is for "cannot", never for "wait".
- **C6 Honest feedback** — a status line/toast claiming a change must match an actual
  change (the "Draft created" over an unchanged list class of bug).
- **C7 Verbs + l10n** — action buttons are verbs; no bare OK/Да. RU strings grow ~+20%,
  ZH shrinks ~−30% — layouts must not depend on exact text width.
- **C8 Proximity & frames** — grouping by whitespace/contrast, not a Border around
  everything; related things near, separate sections 24–32 apart.
- **C9 Keyboard story** — the surface's primary flows work without the mouse (grid,
  scheduler keys; Tab/Enter/Esc in dialogs).
- **C10 Theme dignity** — the surface in Dark is not "Light with dark paint": elevations,
  hover/selection states, separators all distinct in every swept theme; HighContrast
  keeps structure (borders/lines) when tints collapse.

Each C finding: what the user would experience, where (file:line or repro step),
severity. No C finding is dismissed as "taste" without a stated reason.
