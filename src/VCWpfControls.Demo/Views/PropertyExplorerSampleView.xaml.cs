using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.Models;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Scheduler;
using VCWpfControls.TreeList;

namespace VCWpfControls.Demo.Views;

/// <summary>
/// The DevExpress-style live property explorer: pick a LIBRARY control and edit its real
/// instance through the library's own PropertyGrid (ShowDeclaredPropertiesOnly keeps the
/// list to the control's API — the inherited ItemsControl surface stays out).
/// </summary>
public partial class PropertyExplorerSampleView : UserControl
{
    private readonly GridControl _grid = new();
    private readonly TreeListView _tree = new();
    private readonly SchedulerView _scheduler = new();

    public PropertyExplorerSampleView()
    {
        InitializeComponent();
        BuildSamples();
        SampleHost.Content = _grid;
    }

    private void BuildSamples()
    {
        // GridControl: auto-generated columns over the shared employee sample.
        _grid.AutoGenerateColumns = true;
        _grid.FrozenColumnCount = 1;
        _grid.SelectionMode = GridSelectionMode.Extended;
        _grid.RowHeight = 28;
        _grid.ItemsSource = new ObservableCollection<Employee>(SampleData.CreateEmployees(8));

        // TreeListView: a small org tree with explicit columns.
        var research = new OrgUnit("Research", 12, "Dana");
        research.Children.Add(new OrgUnit("Platform", 5, "Ivan"));
        research.Children.Add(new OrgUnit("Tooling", 4, "Mei"));
        _tree.ChildMemberPath = nameof(OrgUnit.Children);
        _tree.RowHeight = 28;
        _tree.ItemsSource = new ObservableCollection<OrgUnit>
        {
            research,
            new("Sales", 9, "Ravi"),
        };
        _tree.Columns.Add(new GridTextColumn { FieldName = nameof(OrgUnit.Name), Header = "Unit", Width = 2 });
        _tree.Columns.Add(new GridTextColumn { FieldName = nameof(OrgUnit.Lead), Header = "Lead", Width = 1 });
        _tree.Columns.Add(new GridNumericColumn { FieldName = nameof(OrgUnit.Headcount), Header = "Headcount", Width = 110 });

        // SchedulerView: a few appointments around today.
        var today = DateTime.Today;
        _scheduler.Appointments = new ObservableCollection<SchedulerAppointment>
        {
            new("standup", "Daily standup", today.AddHours(9.25), today.AddHours(9.5)),
            new("review", "Design review", today.AddDays(1).AddHours(11), today.AddDays(1).AddHours(12)),
            new("planning", "Sprint planning", today.AddDays(2).AddHours(14), today.AddDays(2).AddHours(15.5)),
        };
    }

    private void OnTargetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleHost is null)
        {
            return; // template not applied yet
        }

        SampleHost.Content = TargetCombo.SelectedIndex switch
        {
            1 => _tree,
            2 => _scheduler,
            _ => _grid,
        };
    }
}
