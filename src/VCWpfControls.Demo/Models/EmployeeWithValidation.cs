using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VCWpfControls.Demo.Models;

/// <summary>Sample model implementing <see cref="INotifyDataErrorInfo"/> for the editing demo.</summary>
public class EmployeeWithValidation : INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    /// <summary>Identifier.</summary>
    public int Id { get; set; }

    private string _name = string.Empty;

    /// <summary>Full name; required, at least 3 characters.</summary>
    [Display(Name = "Name")]
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            SetErrors(nameof(Name), string.IsNullOrWhiteSpace(value)
                ? new[] { "Name is required." }
                : value.Length < 3
                    ? new[] { "Name must be at least 3 characters." }
                    : Array.Empty<string>());
        }
    }

    private string _email = string.Empty;

    /// <summary>Email; must contain '@'.</summary>
    [Display(Name = "Email")]
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            SetErrors(nameof(Email), value.Contains('@')
                ? Array.Empty<string>()
                : new[] { "Email must contain '@'." });
        }
    }

    private decimal _salary;

    /// <summary>Salary; must be within 1000..50000.</summary>
    [Display(Name = "Salary")]
    public decimal Salary
    {
        get => _salary;
        set
        {
            _salary = value;
            SetErrors(nameof(Salary), value is >= 1000 and <= 50000
                ? Array.Empty<string>()
                : new[] { "Salary must be between 1000 and 50000." });
        }
    }

    /// <summary>Hire date.</summary>
    [Display(Name = "Hired")]
    public DateTime HireDate { get; set; }

    /// <inheritdoc />
    public bool HasErrors => _errors.Count > 0;

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public IEnumerable GetErrors(string? propertyName)
        => propertyName is not null && _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Array.Empty<string>();

    private void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
        {
            _errors.Remove(propertyName);
        }
        else
        {
            _errors[propertyName] = list;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
