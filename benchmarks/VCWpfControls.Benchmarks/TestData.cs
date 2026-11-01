namespace VCWpfControls.Benchmarks;

/// <summary>Flat data model consumed by the data-engine benchmarks.</summary>
internal sealed class Employee(
    int id,
    string name,
    int age,
    decimal salary,
    DateTime hireDate,
    bool isActive)
{
    /// <summary>Sequential employee id.</summary>
    public int Id { get; } = id;

    /// <summary>Randomly combined first and last name, e.g. <c>Alice Chen</c>.</summary>
    public string Name { get; } = name;

    /// <summary>Age between 18 and 65.</summary>
    public int Age { get; } = age;

    /// <summary>Salary between 20,000 and 100,000.</summary>
    public decimal Salary { get; } = salary;

    /// <summary>Hire date between 2000-01-01 and ~2024.</summary>
    public DateTime HireDate { get; } = hireDate;

    /// <summary>Random active flag.</summary>
    public bool IsActive { get; } = isActive;
}

/// <summary>
/// Generates deterministic <see cref="Employee"/> data sets: the same row count always
/// produces the same data (fixed seed), so benchmark runs are comparable.
/// </summary>
internal static class TestData
{
    private const int Seed = 42;

    private static readonly string[] FirstNames =
    {
        "Alice", "Bob", "Carol", "David", "Elena", "Frank", "Grace", "Henry", "Irina", "Jack",
    };

    private static readonly string[] LastNames =
    {
        "Anderson", "Brown", "Chen", "Diaz", "Evans", "Ford", "Garcia", "Hill", "Ivanov", "Johnson",
    };

    /// <summary>Creates <paramref name="count"/> deterministic employee rows.</summary>
    public static List<Employee> Create(int count)
    {
        var random = new Random(Seed);
        var employees = new List<Employee>(count);
        for (var i = 0; i < count; i++)
        {
            var name = FirstNames[random.Next(FirstNames.Length)] + " " + LastNames[random.Next(LastNames.Length)];
            employees.Add(new Employee(
                id: i,
                name: name,
                age: random.Next(18, 66),
                salary: Math.Round(20_000m + (decimal)(random.NextDouble() * 80_000), 2),
                hireDate: new DateTime(2000, 1, 1).AddDays(random.Next(0, 9_000)),
                isActive: random.Next(2) == 1));
        }

        return employees;
    }
}
