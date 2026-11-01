# VCWpfControls.Benchmarks

BenchmarkDotNet benchmarks of the `GridControl` data engine (`VCWpfControls.Grid.Data.GridDataManager`).
The engine is UI-free, so the benchmarks are plain console benchmarks — no window is created.

## What is measured

`DataEngineBenchmarks` (fixed sizes):

| Benchmark | Scenario |
|---|---|
| `Sort_100k` | `Refresh()` with 2 sort levels (Name asc, Age desc) on 100k rows |
| `Filter_100k` | `Refresh()` with 2 AND-combined filters (Name contains "chen", Age > 40) on 100k rows |
| `Group_100k` | `Refresh()` grouped by Age (~48 groups) on 100k rows |
| `FullPipeline_100k` | `Refresh()` with filter + 2-level sort + group on 100k rows |
| `SetSource_100k` | `SetSource()` of a fresh 100k-row list (source instances alternate so every call performs a full rebuild) |
| `PropertyAccess_1M` | 1M cell reads through a cached `PropertyAccessor` obtained via `GridDataManager.GetAccessor` (compiled getter only) |
| `GetCellValue_1M` | 1M reads via `GridDataManager.GetCellValue` (includes the per-call accessor cache lookup) |

`DataEngineScalingBenchmarks` runs `FullPipeline` and `SetSource` with `[Params(1_000, 10_000, 100_000)]` rows to show how the engine scales.

Measurement notes:

- Data generation and the initial `SetSource` run in `GlobalSetup` (per benchmark, via `GlobalSetup(Target = ...)`); iterations measure steady-state `Refresh()` / read work only.
- Test data is a deterministic `Employee` model (`Id`, `Name`, `Age`, `Salary`, `HireDate`, `IsActive`) generated with a fixed seed, so runs are comparable.
- `[MemoryDiagnoser]` reports allocations; note that property reads return `object?`, so value-type cells box.
- Jobs are pinned to `RuntimeMoniker.Net80`.

## Running

From the repository root (Release build is required — BenchmarkDotNet refuses Debug):

```bash
# all benchmarks
dotnet run -c Release -p benchmarks/VCWpfControls.Benchmarks

# a single benchmark class
dotnet run -c Release -p benchmarks/VCWpfControls.Benchmarks -- --filter "*DataEngineBenchmarks*"

# a single benchmark, quick run (~30s instead of several minutes)
dotnet run -c Release -p benchmarks/VCWpfControls.Benchmarks -- --filter "*Sort_100k*" --job short

# scaling benchmarks only
dotnet run -c Release -p benchmarks/VCWpfControls.Benchmarks -- --filter "*DataEngineScalingBenchmarks*"
```

BenchmarkDotNet arguments go after `--`. Useful switches:

- `--filter "<pattern>"` — glob pattern over the full benchmark name `Namespace.ClassName.MethodName` (parameterized benchmarks also include the parameter values, e.g. `*DataEngineScalingBenchmarks.FullPipeline*100000*`).
- `--job short` — fewer warmup/actual iterations for a quick sanity run.
- `--list flat` — print all discovered benchmarks without running them.
- `--memory` — force the memory diagnoser from the command line.

Results are printed to the console and written to `BenchmarkDotNet.Artifacts/` (git-ignored), including a `BenchmarkDotNet.Artifacts/results/*-report-github.md` summary.

## Requirements

- Windows (the project targets `net8.0-windows` with `UseWPF=true`, matching the library).
- .NET 8 SDK.
