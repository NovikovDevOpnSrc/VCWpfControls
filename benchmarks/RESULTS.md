# Data engine benchmarks — v0.1.0 baseline

Machine: AMD Ryzen 9 5900HX (8C/16T), Windows 10 22H2, .NET 8.0.32, BenchmarkDotNet 0.14.0.
Data: 100 000 deterministic rows (see `TestData.Create`, seed 42).

Run: `dotnet run -c Release --project benchmarks/VCWpfControls.Benchmarks -- --filter "*_100k"`

> Note: benchmark classes must be **public non-sealed** (BenchmarkDotNet generates derived
> runnables) and must not pin `[SimpleJob(RuntimeMoniker.*)]` in a `net8.0-windows` project
> (the generated project TFM must match — NU1201).

## Results (default job)

| Method             | Mean      | Error    | StdDev   | Gen0      | Gen1     | Gen2    | Allocated |
|------------------ |----------:|---------:|---------:|----------:|---------:|--------:|----------:|
| Sort_100k         | 148.22 ms | 2.805 ms | 2.486 ms | 8000.0000 | 1750.0000 | 500.0000 |  70.46 MB |
| Filter_100k       | 105.85 ms | 0.560 ms | 0.524 ms |          — |         — |       — |   2.77 MB |
| Group_100k        |  29.45 ms | 0.555 ms | 0.778 ms | 1500.0000 | 1156.2500 | 468.7500 |  14.06 MB |
| FullPipeline_100k | 111.73 ms | 1.196 ms | 0.998 ms |  600.0000 |  400.0000 | 200.0000 |   5.46 MB |
| SetSource_100k    |  12.07 ms | 0.239 ms | 0.235 ms |  671.8750 |  656.2500 | 250.0000 |   9.05 MB |

Notes:
- `Sort_100k` (two levels) allocates ~70 MB: LINQ `OrderBy` chains buffer keys per level —
  the top optimization candidate for a follow-up iteration (custom list sort).
- `Filter_100k` (~106 ms) is dominated by `Contains` string comparison over 100k rows;
  allocations are minimal (2.77 MB).
- Full pipeline (filter + sort + group) ~112 ms on 100k rows — comfortably interactive
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
