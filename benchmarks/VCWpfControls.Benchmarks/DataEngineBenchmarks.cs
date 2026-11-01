using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using VCWpfControls.Grid.Data;

namespace VCWpfControls.Benchmarks;

/// <summary>
/// Shared pipeline configurations of the data-engine benchmarks: two sort levels
/// (Name, Age), two filters (name Contains, Age GreaterThan) and one group level (Age).
/// </summary>
internal static class DataEngineScenarios
{
    /// <summary>Matches the "Chen" last name (about 10% of the generated rows).</summary>
    internal const string NameFilterFragment = "chen";

    /// <summary>Keeps roughly half of the adult rows.</summary>
    internal const int AgeFilterThreshold = 40;

    internal static IReadOnlyList<GridSortDescription> CreateSorts() => new[]
    {
        new GridSortDescription(nameof(Employee.Name), GridSortDirection.Ascending),
        new GridSortDescription(nameof(Employee.Age), GridSortDirection.Descending),
    };

    internal static IReadOnlyList<GridFilterDescription> CreateFilters() => new[]
    {
        new GridFilterDescription(nameof(Employee.Name), GridFilterOperator.Contains, NameFilterFragment),
        new GridFilterDescription(nameof(Employee.Age), GridFilterOperator.GreaterThan, AgeFilterThreshold),
    };

    internal static IReadOnlyList<GridGroupDescription> CreateGroups()
        => new[] { new GridGroupDescription(nameof(Employee.Age)) };
}

/// <summary>
/// Fixed-size (100k rows, 1M reads) benchmarks of the <see cref="GridDataManager"/> data engine.
/// Each benchmark has its own <c>GlobalSetup</c> that connects the source and runs the pipeline
/// once, so the measured operation is steady-state pipeline work (<see cref="GridDataManager.Refresh"/>),
/// not data generation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Benchmark names follow the underscore size-suffix convention.")]
[SuppressMessage("Usage", "CA1515:Consider making types internal", Justification = "BenchmarkDotNet discovers benchmark classes via the public API surface.")]
public sealed class DataEngineBenchmarks
{
    private const int RowCount = 100_000;
    private const int PropertyAccessReads = 1_000_000;
    private const int PropertyAccessPasses = PropertyAccessReads / RowCount;

    private GridDataManager _manager = new();
    private List<Employee> _employees = [];
    private List<Employee> _alternateEmployees = [];
    private PropertyAccessor _ageAccessor = PropertyAccessor.GetOrCreate(typeof(Employee), nameof(Employee.Age));

    // Starts true so the very first benchmark call swaps away from the source connected in setup.
    private bool _useAlternateSource = true;
    private long _ageSum;

    [GlobalSetup(Target = nameof(Sort_100k))]
    public void SetupSort()
        => _manager = CreateManager(sorts: DataEngineScenarios.CreateSorts(), filters: [], groups: []);

    [GlobalSetup(Target = nameof(Filter_100k))]
    public void SetupFilter()
        => _manager = CreateManager(sorts: [], filters: DataEngineScenarios.CreateFilters(), groups: []);

    [GlobalSetup(Target = nameof(Group_100k))]
    public void SetupGroup()
        => _manager = CreateManager(sorts: [], filters: [], groups: DataEngineScenarios.CreateGroups());

    [GlobalSetup(Target = nameof(FullPipeline_100k))]
    public void SetupFullPipeline()
        => _manager = CreateManager(
            sorts: DataEngineScenarios.CreateSorts(),
            filters: DataEngineScenarios.CreateFilters(),
            groups: DataEngineScenarios.CreateGroups());

    [GlobalSetup(Target = nameof(SetSource_100k))]
    public void SetupSetSource()
    {
        // Two equal sources: SetSource ignores repeated references to the same instance,
        // so the benchmark alternates between them to force a full rebuild every call.
        _employees = TestData.Create(RowCount);
        _alternateEmployees = TestData.Create(RowCount);
        _manager = new GridDataManager();
        _manager.SetSource(_employees);
    }

    [GlobalSetup(Target = nameof(PropertyAccess_1M) + "," + nameof(GetCellValue_1M))]
    public void SetupPropertyAccess()
    {
        _employees = TestData.Create(RowCount);
        _manager = new GridDataManager();
        _manager.SetSource(_employees);
        _ageAccessor = _manager.GetAccessor(typeof(Employee), nameof(Employee.Age));
    }

    /// <summary>Rebuilds the view with two sort levels (Name asc, Age desc) on 100k rows.</summary>
    [Benchmark]
    public void Sort_100k() => _manager.Refresh();

    /// <summary>Rebuilds the view with two AND-combined filters (Name contains, Age &gt;) on 100k rows.</summary>
    [Benchmark]
    public void Filter_100k() => _manager.Refresh();

    /// <summary>Rebuilds the view grouped by Age (48 groups) on 100k rows.</summary>
    [Benchmark]
    public void Group_100k() => _manager.Refresh();

    /// <summary>Rebuilds the full pipeline: filter, then two-level sort, then group by Age.</summary>
    [Benchmark]
    public void FullPipeline_100k() => _manager.Refresh();

    /// <summary>Connects a fresh 100k-row source (alternating instances), including a full rebuild.</summary>
    [Benchmark]
    public void SetSource_100k()
    {
        _manager.SetSource(_useAlternateSource ? _alternateEmployees : _employees);
        _useAlternateSource = !_useAlternateSource;
    }

    /// <summary>One million property reads through a cached accessor (compiled getter only).</summary>
    [Benchmark]
    public void PropertyAccess_1M()
    {
        long sum = 0;
        for (var pass = 0; pass < PropertyAccessPasses; pass++)
        {
            for (var i = 0; i < _employees.Count; i++)
            {
                if (_ageAccessor.GetValue(_employees[i]) is int age)
                {
                    sum += age;
                }
            }
        }

        _ageSum = sum;
    }

    /// <summary>One million reads via GridDataManager.GetCellValue (accessor cache lookup per call).</summary>
    [Benchmark]
    public void GetCellValue_1M()
    {
        long sum = 0;
        for (var pass = 0; pass < PropertyAccessPasses; pass++)
        {
            for (var i = 0; i < _employees.Count; i++)
            {
                if (_manager.GetCellValue(_employees[i], nameof(Employee.Age)) is int age)
                {
                    sum += age;
                }
            }
        }

        _ageSum = sum;
    }

    private GridDataManager CreateManager(
        IReadOnlyList<GridSortDescription> sorts,
        IReadOnlyList<GridFilterDescription> filters,
        IReadOnlyList<GridGroupDescription> groups)
    {
        _employees = TestData.Create(RowCount);
        var manager = new GridDataManager
        {
            Sorts = sorts,
            Filters = filters,
            Groups = groups,
        };
        manager.SetSource(_employees);
        return manager;
    }
}

/// <summary>
/// Row-count scaling of the data engine: the same scenarios at 1k, 10k and 100k rows
/// to see how pipeline cost grows with the source size.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
[SuppressMessage("Usage", "CA1515:Consider making types internal", Justification = "BenchmarkDotNet discovers benchmark classes via the public API surface.")]
public sealed class DataEngineScalingBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    private GridDataManager _manager = new();
    private List<Employee> _employees = [];
    private List<Employee> _alternateEmployees = [];

    // Starts true so the very first benchmark call swaps away from the source connected in setup.
    private bool _useAlternateSource = true;

    [GlobalSetup(Target = nameof(FullPipeline))]
    public void SetupFullPipeline()
    {
        _employees = TestData.Create(RowCount);
        _manager = new GridDataManager
        {
            Sorts = DataEngineScenarios.CreateSorts(),
            Filters = DataEngineScenarios.CreateFilters(),
            Groups = DataEngineScenarios.CreateGroups(),
        };
        _manager.SetSource(_employees);
    }

    [GlobalSetup(Target = nameof(SetSource))]
    public void SetupSetSource()
    {
        _employees = TestData.Create(RowCount);
        _alternateEmployees = TestData.Create(RowCount);
        _manager = new GridDataManager();
        _manager.SetSource(_employees);
    }

    /// <summary>Rebuilds the full pipeline (filter + sort + group) for the current row count.</summary>
    [Benchmark]
    public void FullPipeline() => _manager.Refresh();

    /// <summary>Connects a fresh source of the current row count, including a full rebuild.</summary>
    [Benchmark]
    public void SetSource()
    {
        _manager.SetSource(_useAlternateSource ? _alternateEmployees : _employees);
        _useAlternateSource = !_useAlternateSource;
    }
}
