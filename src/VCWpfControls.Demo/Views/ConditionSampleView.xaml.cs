using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Conditions;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Demo.Models;

namespace VCWpfControls.Demo.Views;

/// <summary>Condition editor sample: the builder drives the grid predicate.</summary>
public partial class ConditionSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public ConditionSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "ConditionSample");

        Grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = new GridColumnWidth(2, GridUnitType.Star) });
        Grid.Columns.Add(new GridComboBoxColumn { FieldName = "Department", Header = "Dept", Width = 120, ItemsSource = Enum.GetValues<Department>() });
        Grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 110 });
        Grid.Columns.Add(new GridDateColumn { FieldName = "HireDate", Header = "Hired", Width = 110 });
        Grid.ItemsSource = SampleData.CreateEmployees();
        Builder.Schema = ConditionSchema.FromType(typeof(Employee));
    }

    private void OnConditionChanged(object? sender, EventArgs e)
    {
        // Live preview only; Apply pushes the predicate.
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        Grid.CustomFilterPredicate = Builder.Condition is { IsEmpty: false } tree
            ? item => ConditionEvaluator.Matches(item!, tree)
            : null;
    }
}
