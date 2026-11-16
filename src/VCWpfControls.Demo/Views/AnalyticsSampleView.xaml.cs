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
        => RowCountText.Text = GridStrings.Summary(GridSummaryFunction.Count, GridStrings.RowsWord, Grid.DataEngine.DataRows.Count);

    private void OnGroupByDepartment(object sender, RoutedEventArgs e)
    {
        Grid.GroupDescriptions.Clear();
        Grid.GroupDescriptions.Add(new GridGroupDescription("Department"));
    }

    private void OnClearGroups(object sender, RoutedEventArgs e) => Grid.GroupDescriptions.Clear();

    private void OnSortByName(object sender, RoutedEventArgs e)
        => Grid.SortBy("Name", GridSortDirection.Ascending);

    private void OnExportCsv(object sender, RoutedEventArgs e)
        => RunExport(path => GridExporter.ExportToCsv(Grid, path), "vcwpf_export.csv", sender);

    private void OnExportXlsx(object sender, RoutedEventArgs e)
        => RunExport(path => GridExporter.ExportToXlsx(Grid, path), "vcwpf_export.xlsx", sender);

    private void OnExportHtml(object sender, RoutedEventArgs e)
        => RunExport(path => GridExporter.ExportToHtml(Grid, path), "vcwpf_export.html", sender);

    /// <summary>
    /// Exports are no longer silent: the localized completion message carries the target
    /// path, and the clicked button locks for the operation (no double-click = double
    /// file; the &gt;300ms rule's re-click block).
    /// </summary>
    private void RunExport(Action<string> export, string fileName, object sender)
    {
        if (sender is not System.Windows.Controls.Primitives.ButtonBase { IsEnabled: true } button)
        {
            return;
        }

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
        button.IsEnabled = false;
        try
        {
            export(path);
            RowCountText.Text = $"{GridStrings.ExportCompleted}: {path}";
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}
