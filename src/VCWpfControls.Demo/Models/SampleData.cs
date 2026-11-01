namespace VCWpfControls.Demo.Models;

/// <summary>Generates sample data sets for the demo pages.</summary>
public static class SampleData
{
    private static readonly string[] FirstNames =
    {
        "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi",
        "Ivan", "Judy", "Kevin", "Linda", "Mallory", "Niaj", "Olivia", "Peggy",
    };

    private static readonly string[] LastNames =
    {
        "Anderson", "Brown", "Chen", "Diaz", "Evans", "Fischer", "Garcia", "Hugo",
        "Ibrahim", "Johnson", "Kowalski", "Lopez", "Miller", "Novak", "Olsen", "Petrov",
    };

    /// <summary>Creates a small set of employees (deterministic).</summary>
    public static List<Employee> CreateEmployees(int count = 24, int seed = 1)
    {
        var random = new Random(seed);
        var result = new List<Employee>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(CreateEmployee(random, i + 1));
        }

        return result;
    }

    /// <summary>Creates a large set for virtualization testing.</summary>
    public static List<Employee> CreateLargeEmployeeSet(int count)
        => CreateEmployees(count, seed: 42);

    /// <summary>Creates employees with validation support.</summary>
    public static List<EmployeeWithValidation> CreateValidatingEmployees(int count = 12, int seed = 7)
    {
        var random = new Random(seed);
        var result = new List<EmployeeWithValidation>(count);
        for (var i = 0; i < count; i++)
        {
            var employee = CreateEmployee(random, i + 1);
            result.Add(new EmployeeWithValidation
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Salary = employee.Salary,
                HireDate = employee.HireDate,
            });
        }

        return result;
    }

    private static Employee CreateEmployee(Random random, int id)
    {
        var name = $"{FirstNames[random.Next(FirstNames.Length)]} {LastNames[random.Next(LastNames.Length)]}";
        return new Employee
        {
            Id = id,
            Name = name,
            Email = $"{name.Split(' ')[0].ToLowerInvariant()}{id}@example.com",
            Department = (Department)random.Next(4),
            Salary = 2000 + random.Next(48) * 1000,
            HireDate = new DateTime(2015, 1, 1).AddDays(random.Next(3000)),
            IsActive = random.Next(4) != 0,
        };
    }
}
