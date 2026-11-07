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

    /// <summary>Name suggestions for the auto-complete column.</summary>
    public string[] Names { get; } =
    {
        "Alice", "Albert", "Alexander", "Alexandra", "Alina",
        "Boris", "Bogdan", "Barbara", "Vasily", "Vera", "Viktor",
        "Galina", "Grigory", "Dmitry", "Diana", "Elena", "Evgeny",
    };
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

/// <summary>Grouping, filter row and footer summaries.</summary>
public partial class AnalyticsSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Grouping & filters";

    /// <inheritdoc />
    public override string Description => "Group rows by column, use the built-in filter row and footer summaries.";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());
}

/// <summary>Docking manager with zones, splitters and layout persistence.</summary>
public partial class DockingSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Docking";

    /// <inheritdoc />
    public override string Description => "Dock panes left/top/right/bottom/center with splitters, close/show and XML layout persistence.";
}

/// <summary>Masked input and numeric spinners.</summary>
public partial class EditorsSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Editors";

    /// <inheritdoc />
    public override string Description => "MaskedTextBox (phone, serial, date masks) and NumericUpDown spinners.";
}

/// <summary>A three-step wizard with a blocked page and events.</summary>
public partial class WizardSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Wizard";

    /// <inheritdoc />
    public override string Description => "Paged wizard: Next/Back/Finish, page validation (CanLeave), cancelable PageChanging.";
}

/// <summary>TreeListView with an organization chart.</summary>
public partial class TreeListSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Tree list";

    /// <inheritdoc />
    public override string Description => "Hierarchy with columns: expand/collapse nodes, sort children within each level.";

    /// <summary>The org units (roots).</summary>
    public ObservableCollection<OrgUnit> Units { get; } = new(BuildOrg());

    private static List<OrgUnit> BuildOrg()
    {
        var platform = new OrgUnit("Platform", 20, "Dana");
        var mobile = new OrgUnit("Mobile", 10, "Evan");
        var desktop = new OrgUnit("Desktop", 20, "Grace");
        var apps = new OrgUnit("Apps", 30, "Bob");
        apps.Children.Add(mobile);
        apps.Children.Add(desktop);
        var engineering = new OrgUnit("Engineering", 50, "Alice");
        engineering.Children.Add(platform);
        engineering.Children.Add(apps);
        var emea = new OrgUnit("EMEA", 6, "Ivan");
        var apac = new OrgUnit("APAC", 4, "Linda");
        var sales = new OrgUnit("Sales", 10, "Carol");
        sales.Children.Add(emea);
        sales.Children.Add(apac);
        return new List<OrgUnit> { engineering, sales };
    }
}

/// <summary>Property grid over the selected employee.</summary>
public partial class PropertyGridSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Property grid";

    /// <inheritdoc />
    public override string Description => "Select a row: its properties are edited through the PropertyGrid (categories, typed editors, search).";

    /// <summary>Sample rows.</summary>
    public ObservableCollection<Employee> Employees { get; } = new(SampleData.CreateEmployees());
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
