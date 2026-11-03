# Data engine benchmarks — v0.1.0 baseline

Machine: AMD Ryzen 9 5900HX (8C/16T), Windows 10 22H2, .NET 8.0.32, BenchmarkDotNet 0.14.0.
Data: 100 000 deterministic rows (see `TestData.Create`, seed 42).

Run: `dotnet run -c Release --project benchmarks/VCWpfControls.Benchmarks -- --filter "*_100k"`

> Note: benchmark classes must be **public non-sealed** (BenchmarkDotNet generates derived
> runnables) and must not pin `[SimpleJob(RuntimeMoniker.*)]` in a `net8.0-windows` project
> (the generated project TFM must match — NU1201).

## Results (default job, after the 2026-11-03 sort optimization)

| Method             | Mean      | Error    | StdDev   | Allocated |
|------------------ |----------:|---------:|---------:|----------:|
| Sort_100k         | 127.60 ms | 2.48 ms  | 2.07 ms  |  14.02 MB |
| Filter_100k       | 105.58 ms | 0.604 ms | 0.536 ms |   2.77 MB |
| Group_100k        |  29.11 ms | 0.576 ms | 0.979 ms |  14.06 MB |
| FullPipeline_100k | 110.40 ms | 0.24 ms  | 0.21 ms  |   3.26 MB |
| SetSource_100k    |  11.82 ms | 0.199 ms | 0.166 ms |   9.05 MB |

### Optimization history (Sort_100k: Name asc + Age desc)

| Version                                        | Mean          | Allocated |
|----------------------------------------------- |--------------:|----------:|
| v0.1.0 baseline (LINQ OrderBy/ThenBy)          | 148.22 ms     |  70.46 MB |
| + index-based stable sort (no visible effect)  | 152.14 ms     |  69.70 MB |
| + allocation-free culture compare (final)      | **127.60 ms** | **14.02 MB** |

The real allocator was `StringComparer.Create(culture)` built **per string comparison** inside
`GridValueCompare` (~3.4M comparisons × ~24 B ≈ 70 MB); replacing it with
`string.Compare(a, b, culture, CompareOptions.None)` removed 80% of the allocations while
keeping culture-aware ordering. The index-based stable sort (keys materialized once per level,
index tie-breaker for stability) keeps behavior identical to the LINQ chain.

Notes:
- `Filter_100k` (~106 ms) is dominated by `Contains` string comparison over 100k rows;
  allocations are minimal (2.77 MB).
- Full pipeline (filter + sort + group) ~110 ms on 100k rows — comfortably interactive
  with `UseAsyncRefresh` keeping the UI responsive.

## Interpretation

- `*_100k` measure `GridDataManager.Refresh()` on a pre-connected source; data generation and
  `SetSource` are excluded except in `SetSource_100k` (alternating source instances).
- Treat these numbers as the **regression baseline**: CI runs the build/tests, rerun this file's
  command manually before releases and compare.

## Coverage (release audit, same baseline)

| Suite          | Library line coverage |
|----------------|----------------------:|
| Core.Tests     | 19.6% (engine-focused) |
| Control.Tests  | 65.4%                  |
| **Union**      | **69.1%**              |

Low spots by design: `GridColumnFilterMenu` (mouse-driven popup), automation peers, theme
manager — candidates for FlaUI-level coverage later.

## Docs build

`docfx docs/docfx.json` — build succeeded, 37 HTML pages, 0 warnings, 0 errors.
