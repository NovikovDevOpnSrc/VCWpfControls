using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCWpfControls.Demo.Models;

namespace VCWpfControls.Demo.ViewModels;

/// <summary>Base of a demo page: title + description.</summary>
public abstract partial class SampleViewModelBase : ObservableObject
{
    /// <summary>Page title.</summary>
    public abstract string Title { get; }

    /// <summary>Short description shown under the title.</summary>
    public abstract string Description { get; }
}

/// <summary>Auto-generated columns bound to a plain list.</summary>
public partial class BasicSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Basic binding";

    /// <inheritdoc />
    public override string Description => "Columns are generated from the item type ([Display] attributes are honored).";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());
}

/// <summary>All column types defined manually, including a template column.</summary>
public partial class ColumnTypesSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Column types";

    /// <inheritdoc />
    public override string Description => "Text, numeric, combo, date, check box and template columns with star/absolute/auto widths.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());

    /// <summary>Departments for the combo column.</summary>
    public Department[] Departments { get; } = Enum.GetValues<Department>();
}

/// <summary>Editing with INotifyDataErrorInfo validation.</summary>
public partial class EditingSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Editing & validation";

    /// <inheritdoc />
    public override string Description => "Double-click or F2 to edit. Values are validated through INotifyDataErrorInfo.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<EmployeeWithValidation> Employees { get; } = new(SampleData.CreateValidatingEmployees());
}

/// <summary>Selection modes demo.</summary>
public partial class SelectionSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Selection";

    /// <inheritdoc />
    public override string Description => "Extended selection with Ctrl/Shift; the selected count is bound to SelectedItems.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());

    /// <summary>Selected rows (updated by the page code-behind through SelectedItems).</summary>
    [ObservableProperty]
    private int _selectedCount;
}

/// <summary>Virtualization with a large data set.</summary>
public partial class VirtualizationSampleViewModel : SampleViewModelBase
{
    /// <summary>Rows in the sample.</summary>
    public const int RowCount = 100_000;

    /// <inheritdoc />
    public override string Title => "Virtualization (100k rows)";

    /// <inheritdoc />
    public override string Description => "UI virtualization with container recycling: only visible rows are realized.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(LoadRows());

    private static List<Employee> LoadRows()
    {
        var rows = SampleData.CreateLargeEmployeeSet(RowCount);
        return rows;
    }
}

/// <summary>Sorting demo (single- and multi-column).</summary>
public partial class SortingSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Sorting";

    /// <inheritdoc />
    public override string Description => "Click a header to sort; Shift+click adds a column to a multi-column sort.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());

    /// <summary>Current sort description shown on the page.</summary>
    [ObservableProperty]
    private string _sortInfo = "Unsorted";
}
