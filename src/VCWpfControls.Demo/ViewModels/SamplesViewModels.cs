using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VCWpfControls.Demo.Models;
using VCWpfControls.Gantt;
using VCWpfControls.Scheduler;

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

/// <summary>Third-party integration: LiveCharts2 chart of grid data + Fluent.Ribbon.</summary>
public partial class IntegrationSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Charts & Ribbon";

    /// <inheritdoc />
    public override string Description => "LiveCharts2 (MIT) charting the grid aggregates; Fluent.Ribbon (MIT) hosting the page toolbar.";
}

/// <summary>Condition editor: builder + text tab applied to the grid and pivot.</summary>
public partial class ConditionSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Conditions";

    /// <inheritdoc />
    public override string Description => "ConditionBuilder (visual tree + text language) filtering a grid via CustomFilterPredicate.";
}

/// <summary>Sales pivot: region rows x product columns with totals.</summary>
public partial class PivotSampleViewModel : SampleViewModelBase
{
    /// <inheritdoc />
    public override string Title => "Pivot";

    /// <inheritdoc />
    public override string Description => "PivotEngine + PivotGrid: rows x columns aggregation with totals.";

    /// <summary>The raw sales fed into the pivot.</summary>
    public List<SaleRow> Sales { get; } = new()
    {
        new("East", "A", 10m), new("East", "B", 20m), new("East", "A", 7m),
        new("West", "A", 5m), new("West", "B", 15m), new("West", "B", 30m),
        new("North", "A", 2m), new("North", "B", 8m),
    };

    /// <summary>One raw sale row.</summary>
    /// <param name="region">Region.</param>
    /// <param name="product">Product.</param>
    /// <param name="amount">Amount.</param>
    public sealed record SaleRow(string Region, string Product, decimal Amount);
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

/// <summary>Gantt chart: hierarchy, dependencies, auto-scheduling, critical path, drags.</summary>
public partial class GanttSampleViewModel : SampleViewModelBase
{
    private static readonly DateTime Nov9 = new(2026, 11, 9);

    /// <inheritdoc />
    public override string Title => "Gantt";

    /// <inheritdoc />
    public override string Description => "Drag bars to move/resize (day-snapped), Alt+drag from an edge to link; auto-schedule and the critical path recompute live.";

    /// <summary>The release-plan tasks (hierarchy through ParentId).</summary>
    public ObservableCollection<GanttTask> Tasks { get; private set; } = new(CreateTasks());

    /// <summary>The dependency arrows.</summary>
    public ObservableCollection<GanttDependency> Dependencies { get; } = new()
    {
        new("plan", "analysis"),
        new("analysis", "design"),
        new("design", "build"),
        new("build", "test"),
        new("analysis", "docs", GanttLinkType.StartToStart),
        new("test", "ship"),
    };

    /// <summary>The available zoom units for the combo box.</summary>
    public GanttTimescaleUnit[] Units { get; } = Enum.GetValues<GanttTimescaleUnit>();

    /// <summary>Whether the engine re-schedules on every edit.</summary>
    [ObservableProperty]
    private bool _autoSchedule = true;

    /// <summary>Whether the critical chain renders accented.</summary>
    [ObservableProperty]
    private bool _showCriticalPath = true;

    /// <summary>Whether weekends/holidays are non-working (working-day scheduling + tint).</summary>
    [ObservableProperty]
    private bool _skipWeekends;

    /// <summary>Whether baseline bars render under the actual bars.</summary>
    [ObservableProperty]
    private bool _showBaseline = true;

    /// <summary>Whether the timeline shows the legend strip.</summary>
    [ObservableProperty]
    private bool _showLegend = true;

    /// <summary>Whether task names render after the bars.</summary>
    [ObservableProperty]
    private bool _showBarLabels;

    /// <summary>The working-time calendar (null = every day works).</summary>
    public GanttCalendar? Calendar => SkipWeekends ? WeekendCalendar : null;

    private static readonly GanttCalendar WeekendCalendar = CreateWeekendCalendar();

    private static GanttCalendar CreateWeekendCalendar()
    {
        var calendar = new GanttCalendar();
        calendar.Holidays.Add(new DateTime(2026, 11, 20)); // a sample holiday inside the plan
        return calendar;
    }

    partial void OnSkipWeekendsChanged(bool value)
        => OnPropertyChanged(nameof(Calendar));

    /// <summary>The timeline zoom unit.</summary>
    [ObservableProperty]
    private GanttTimescaleUnit _timescaleUnit = GanttTimescaleUnit.Day;

    /// <summary>Last interaction (drag/link) reported by the view.</summary>
    [ObservableProperty]
    private string _lastAction = "Drag a bar to move it; drag an edge to resize; Alt+drag an edge onto another bar to link.";

    /// <summary>Restores the original sample plan.</summary>
    public void Reset()
    {
        Tasks = new ObservableCollection<GanttTask>(CreateTasks());
        OnPropertyChanged(nameof(Tasks));
    }

    private static IEnumerable<GanttTask> CreateTasks()
    {
        GanttTask T(string id, string name, int start, int duration, string? parent = null, bool milestone = false)
            => new(id, name, Nov9.AddDays(start), Nov9.AddDays(start + duration), 0, milestone, parent);

        var tasks = new[]
        {
            T("rel", "Release 1.0", 0, 32),
            T("plan", "Planning", 0, 3, "rel"),
            T("analysis", "Analysis", 3, 5, "rel"),
            T("design", "Design", 8, 7, "rel"),
            T("build", "Build", 15, 10, "rel"),
            T("test", "Testing", 25, 7, "rel"),
            T("docs", "Documentation", 3, 8, "rel"),
            T("ship", "Ship it!", 32, 0, "rel", milestone: true),
        };

        // A category color (the Fiori qualitative-palette pattern) on one task.
        tasks[6].BarBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 134, 87));

        // Baselines: the original plan — build and test have slipped against it.
        tasks[4].BaselineStart = Nov9.AddDays(14);
        tasks[4].BaselineFinish = Nov9.AddDays(23);
        tasks[5].BaselineStart = Nov9.AddDays(24);
        tasks[5].BaselineFinish = Nov9.AddDays(30);
        return tasks;
    }
}

/// <summary>Scheduler: day/work-week/week/month views of recurring appointments.</summary>
public partial class SchedulerSampleViewModel : SampleViewModelBase
{
    private static readonly DateTime Today = DateTime.Today;

    /// <inheritdoc />
    public override string Title => "Scheduler";

    /// <inheritdoc />
    public override string Description => "Day / work week / week / month views with recurrence, overlap lanes, an all-day band and a now line; double-click an occurrence.";

    /// <summary>The sample appointments (relative to today, so the views always have content).</summary>
    public ObservableCollection<SchedulerAppointment> Appointments { get; } = new(CreateAppointments());

    /// <summary>The available view types for the combo box (display names — the raw enum identifiers are dev-speak).</summary>
    public string[] ViewTypeOptions { get; } = { "Day", "Work week", "Week", "Month" };

    /// <summary>The selected view type's display name (maps to <see cref="ViewType"/>).</summary>
    [ObservableProperty]
    private string _selectedViewType = "Work week";

    /// <summary>The shown view granularity.</summary>
    [ObservableProperty]
    private SchedulerViewType _viewType = SchedulerViewType.WorkWeek;

    partial void OnSelectedViewTypeChanged(string value)
        => ViewType = value switch
        {
            "Day" => SchedulerViewType.Day,
            "Week" => SchedulerViewType.Week,
            "Month" => SchedulerViewType.Month,
            _ => SchedulerViewType.WorkWeek,
        };

    /// <summary>Last interaction reported by the view.</summary>
    [ObservableProperty]
    private string _lastAction = "Switch views, navigate weeks, double-click an occurrence.";

    private static IEnumerable<SchedulerAppointment> CreateAppointments()
    {
        var standup = new SchedulerAppointment("standup", "Daily standup", Today.AddHours(9.25), Today.AddHours(9.5))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Daily },
        };
        var oneOnOne = new SchedulerAppointment("1to1", "1:1 with Alice", Today.AddDays(1).AddHours(11), Today.AddDays(1).AddHours(12))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Weekly, DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday } },
        };
        var planning = new SchedulerAppointment("planning", "Sprint planning", Today.AddDays(2).AddHours(14), Today.AddDays(2).AddHours(15.5))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Weekly, DaysOfWeek = new[] { DayOfWeek.Friday } },
        };
        var retro = new SchedulerAppointment("retro", "Retrospective", Today.AddHours(16), Today.AddHours(17))
        {
            Recurrence = new SchedulerRecurrence { Pattern = SchedulerRecurrencePattern.Monthly, DayOfMonth = 28 },
        };
        yield return standup;
        yield return oneOnOne;
        yield return planning;
        yield return retro;
        yield return new SchedulerAppointment("review", "Design review", Today.AddHours(15), Today.AddHours(16.5));
        yield return new SchedulerAppointment("budget", "Budget sync", Today.AddHours(15.5), Today.AddHours(16.5)); // overlaps the review → lanes
        yield return new SchedulerAppointment("conference", "TechConf", Today.AddDays(1), Today.AddDays(4)) { IsAllDay = true }; // 3-day all-day band
        yield return new SchedulerAppointment("oncall", "On-call handover", Today.AddHours(22), Today.AddHours(26)); // overnight
    }
}

/// <summary>Global demo settings: theme, accent preset and library language (persisted).</summary>
public partial class SettingsSampleViewModel : SampleViewModelBase
{
    private readonly Services.DemoSettings _settings;

    /// <inheritdoc />
    public override string Title => "Settings";

    /// <inheritdoc />
    public override string Description => "Application-wide settings: the Light/Dark/High-contrast theme, the accent preset and the library language. Applied immediately, persisted to %AppData% and restored on the next launch.";

    /// <summary>Creates the view model and applies the loaded settings.</summary>
    public SettingsSampleViewModel()
    {
        _settings = Services.DemoSettings.Load();
        Theme = _settings.Theme;
        Accent = _settings.Accent;
        Language = _settings.Language;
        Apply();
    }

    /// <summary>The theme name ("Light", "Dark", "HighContrast" or "System").</summary>
    [ObservableProperty]
    private string _theme = "Light";

    /// <summary>The accent preset name.</summary>
    [ObservableProperty]
    private string _accent = "Default";

    /// <summary>The library UI culture name.</summary>
    [ObservableProperty]
    private string _language = "en-US";

    /// <summary>The selectable options.</summary>
    public string[] ThemeOptions { get; } = { "Light", "Dark", "HighContrast", "System" };

    /// <summary>The selectable accent presets.</summary>
    public string[] AccentOptions { get; } = Services.DemoAccent.All.Select(a => a.Name).ToArray();

    /// <summary>The selectable languages (display name).</summary>
    public string[] LanguageOptions { get; } = { "English (en-US)", "Русский (ru-RU)", "中文 (zh-CN)" };

    /// <summary>Restores the shipped defaults.</summary>
    public void Reset()
    {
        Theme = "Light";
        Accent = "Default";
        Language = "English (en-US)";
        Apply();
    }

    /// <summary>Re-applies after the OS preference flipped (System mode only).</summary>
    public void ReapplyFromSystem() => Apply();

    partial void OnThemeChanged(string value) => Apply();
    partial void OnAccentChanged(string value) => Apply();
    partial void OnLanguageChanged(string value) => Apply();

    private void Apply()
    {
        _settings.Theme = Theme;
        _settings.Accent = Accent;
        _settings.Language = CultureNameOf(Language);
        _settings.Save();
        Services.DemoTheme.Apply(Theme, Accent);
        Services.DemoTheme.ApplyLanguage(_settings.Language);
    }

    private static string CultureNameOf(string display)
        => display.Split('(').Length < 2 ? "en-US" : display.Split('(')[1].TrimEnd(')').Trim();
}
