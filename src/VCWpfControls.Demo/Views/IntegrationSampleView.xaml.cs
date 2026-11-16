using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using VCWpfControls.Demo.Models;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;

namespace VCWpfControls.Demo.Views;

/// <summary>LiveCharts2 + Fluent.Ribbon integration sample over grid aggregates.</summary>
public partial class IntegrationSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public IntegrationSampleView()
    {
        InitializeComponent();
        this.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "IntegrationSample");
        DataContext = this;

        Grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = new GridColumnWidth(2, GridUnitType.Star) });
        Grid.Columns.Add(new GridComboBoxColumn { FieldName = "Department", Header = "Dept", Width = 120, ItemsSource = Enum.GetValues<Department>() });
        Grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 110 });
        Grid.ItemsSource = SampleData.CreateEmployees();
        RefreshChart();
    }

    /// <summary>Groups the grid by department (guarded: no duplicate descriptions).</summary>
    public IRelayCommand GroupByDeptCommand => new RelayCommand(() =>
    {
        if (Grid.GroupDescriptions.Any(d => d.FieldName == "Department"))
        {
            return; // already grouped — the old code stacked a duplicate per click
        }

        Grid.GroupDescriptions.Add(new VCWpfControls.Grid.Data.GridGroupDescription("Department"));
        RefreshChart();
    });

    /// <summary>Clears the grouping.</summary>
    public IRelayCommand ClearGroupingCommand => new RelayCommand(() =>
    {
        Grid.GroupDescriptions.Clear();
        RefreshChart();
    });

    private void RefreshChart()
    {
        // Salary totals per department, FROM THE GRID's live data (filters/grouping shape
        // the chart now) — it used to recompute from a fresh SampleData copy regardless
        // of what the user did to the grid, while the page promised "grid aggregates".
        var totals = Grid.DataEngine.DataRows
            .Select(r => ((Grid.Data.GridDataRow)r).Item)
            .Cast<Employee>()
            .GroupBy(e => e.Department)
            .OrderBy(g => g.Key)
            .ToList();
        Chart.Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Salary total",
                Values = totals.Select(g => (double)g.Sum(e => e.Salary)).ToArray(),
                DataLabelsPaint = new SolidColorPaint(SKColors.DimGray),
            },
        };
        Chart.XAxes = new Axis[]
        {
            new Axis { Labels = totals.Select(g => g.Key.ToString()).ToArray() },
        };
        Chart.YAxes = new Axis[] { new Axis { Labeler = v => v.ToString("N0", System.Globalization.CultureInfo.CurrentCulture) } };
    }
}
