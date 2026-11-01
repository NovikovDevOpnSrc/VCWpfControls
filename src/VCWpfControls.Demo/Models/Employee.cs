using System.ComponentModel.DataAnnotations;

namespace VCWpfControls.Demo.Models;

/// <summary>Sample data model used across the demo pages.</summary>
public class Employee
{
    /// <summary>Unique identifier.</summary>
    [Display(Name = "ID", Order = 0)]
    public int Id { get; set; }

    /// <summary>Full name.</summary>
    [Display(Name = "Name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Email address.</summary>
    [Display(Name = "Email", Order = 2)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Department.</summary>
    [Display(Name = "Department", Order = 3)]
    public Department Department { get; set; }

    /// <summary>Salary.</summary>
    [Display(Name = "Salary", Order = 4)]
    public decimal Salary { get; set; }

    /// <summary>Hire date.</summary>
    [Display(Name = "Hired", Order = 5)]
    public DateTime HireDate { get; set; }

    /// <summary>Whether the employee is active.</summary>
    [Display(Name = "Active", Order = 6)]
    public bool IsActive { get; set; }
}

/// <summary>Departments used by <see cref="Employee"/>.</summary>
public enum Department
{
    /// <summary>Engineering.</summary>
    It,

    /// <summary>Human resources.</summary>
    Hr,

    /// <summary>Sales.</summary>
    Sales,

    /// <summary>Marketing.</summary>
    Marketing,
}
