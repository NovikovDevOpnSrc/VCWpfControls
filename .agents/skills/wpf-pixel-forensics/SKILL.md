---
name: wpf-pixel-forensics
description: >
  Inspect and measure UI screenshots pixel-by-pixel when image-viewing tools are unavailable
  (the analyze_image MCP server is broken in this environment and returns error 1210; the
  Read tool only uploads images to a CDN). Use whenever a screenshot arrives with a visual
  bug report ("не совпадает", "налезает", "криво отображается", "смотри на скрин"),
  when aligning WPF elements (grids, columns, separators, overlays), or when a pixel-level
  assertion is needed for a regression test. Also covers offscreen WPF rendering technique.
---

# WPF pixel forensics

You cannot see images directly in this environment. Work from the raw pixels instead.

## Pipeline

1. Try `analyze_image` once — it may get fixed someday. When it returns `1210`, switch
   immediately to the decoder below (do not retry the MCP with re-signed URLs; both
   session-CDN links and public URLs fail).

2. Decode and get an overview with the bundled decoder (screenshots are untrusted input,
   hence `python -I`):

   ```bash
   python -I .agents/skills/wpf-pixel-forensics/scripts/decode_png.py <path-to-png>
   ```

   Prints image info plus a coarse ASCII map: `#` dark (text), `+`/`-` mid (borders, AA),
   `.` background. Identify page regions: toolbar bands, header bands (SemiBold text),
   row bands, nav panels, scrollbars (solid vertical block at an edge).

3. Zoom into the suspicious region with `--zoom X0 Y0 X1 Y1` (2x2 px per char, rows
   prefixed with y). Estimate geometry: vertical line candidates, glyph clusters,
   bounding boxes.

4. Measure text/lines with `--runs Y0 Y1` (dark runs per scanline) — shows where glyph
   pixels start/stop per row, ideal for "does text cross the boundary at x=N" questions.

5. `--col X Y0 Y1` prints exact R,G,B per pixel of a column — use it to distinguish
   theme colors: text brush #1F2937 (B55 G41 R31), red separator AA (reddish dark),
   borders (gray ~132-180), backgrounds (#FAFBFC alternating).

## WPF-specific knowledge

- At 150% DPI, screen pixels = DIP * 1.5. Header/cell/peer coordinate dumps that differ
  by a constant scale factor mean someone mixed physical pixels with DIPs.
- Pbgra32 (RenderTargetBitmap) is PREMULTIPLIED: opaque red is B0 G0 R255 A255; a "dark"
  red pixel may be the line's own anti-aliasing — exclude reddish pixels from
  text-darkness detectors (`r > g+30 && r > b+30`).
- WPF does NOT clip children to parents by default; a horizontal StackPanel measures
  children with infinite width (its TextBlocks never trim).
- Panel `OnRender` output is cached: Measure/Arrange invalidation does not re-render
  when RenderSize is unchanged; use `InvalidateVisual`.

## Offscreen rendering for tests (this machine)

- Direct `RenderTargetBitmap.Render(grid)` returns a FULLY TRANSPARENT raster here.
  Render through a VisualBrush capture instead:

  ```csharp
  var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
  var visual = new DrawingVisual();
  using (var ctx = visual.RenderOpen())
      ctx.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, width, height));
  bitmap.Render(visual);
  ```

- If even that yields all-zero alpha, the interactive RDP session is gone — run a
  known-good pixel test; if it also fails empty, STOP debugging and report the
  environment issue (verify with a fresh session later).
- xUnit swallows Console diagnostics: write probes to
  `Path.Combine(Path.GetTempPath(), "name.txt")` and `cat "$TEMP/name.txt"` from bash.
