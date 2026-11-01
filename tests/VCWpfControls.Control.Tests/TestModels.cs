namespace VCWpfControls.Control.Tests;

/// <summary>Shared test models.</summary>
public static class TestModels
{
    /// <summary>Employee model for control tests.</summary>
    public class Employee
    {
        /// <summary>Identifier.</summary>
        public int Id { get; set; }

        /// <summary>Name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Department.</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>Salary.</summary>
        public decimal Salary { get; set; }

        /// <summary>Hire date.</summary>
        public DateTime HireDate { get; set; }

        /// <summary>Active flag.</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>Creates 24 deterministic employees.</summary>
    public static List<Employee> CreateEmployees(int count = 24, int seed = 3)
    {
        var random = new Random(seed);
        var departments = new[] { "IT", "HR", "Sales" };
        var result = new List<Employee>(count);
        for (var i = 1; i <= count; i++)
        {
            result.Add(new Employee
            {
                Id = i,
                Name = $"Employee {random.Next(1000):0000}",
                Department = departments[random.Next(departments.Length)],
                Salary = 2000 + random.Next(48) * 100,
                HireDate = new DateTime(2018, 1, 1).AddDays(random.Next(2000)),
                IsActive = random.Next(4) != 0,
            });
        }

        return result;
    }
}
