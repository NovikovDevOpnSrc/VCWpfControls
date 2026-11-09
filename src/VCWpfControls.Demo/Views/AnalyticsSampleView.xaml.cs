using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VCWpfControls.Demo.ViewModels;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Data;
using VCWpfControls.Grid.Export;
using VCWpfControls.Themes;
using VCWpfControls.Localization;

namespace VCWpfControls.Demo.Views;

/// <summary>Grouping, filtering (filter row) and footer summaries in one page.</summary>
public partial class AnalyticsSampleView : UserControl
{
    /// <summary>Creates the view.</summary>
    public AnalyticsSampleView()
    {
        InitializeComponent();
        SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "AnalyticsGridHost");
        Grid.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, "AnalyticsGrid");

        Grid.SummaryItems.Add(new GridSummaryItem("Salary", GridSummaryFunction.Sum, "C"));
        Grid.SummaryItems.Add(new GridSummaryItem("Name", GridSummaryFunction.Count));

        Grid.DataEngine.ViewReset += (_, _) => UpdateRowCount();
        Loaded += (_, _) => UpdateRowCount();
    }

    private void UpdateRowCount()
        => RowCountText.Text = GridStrings.Summary(GridSummaryFunction.Count, "rows", Grid.DataEngine.DataRows.Count);

    private void OnGroupByDepartment(object sender, RoutedEventArgs e)
    {
        Grid.GroupDescriptions.Clear();
        Grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
    }

    private void OnClearGroups(object sender, RoutedEventArgs e) => Grid.GroupDescriptions.Clear();

    private void OnSortByName(object sender, RoutedEventArgs e)
        => Grid.SortBy("Name", GridSortDirection.Ascending);

    private void OnExportCsv(object sender, RoutedEventArgs e)
        => GridExporter.ExportToCsv(Grid, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vcwpf_export.csv"));

    private void OnExportXlsx(object sender, RoutedEventArgs e)
        => GridExporter.ExportToXlsx(Grid, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vcwpf_export.xlsx"));

    private void OnExportHtml(object sender, RoutedEventArgs e)
        => GridExporter.ExportToHtml(Grid, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vcwpf_export.html"));

}
