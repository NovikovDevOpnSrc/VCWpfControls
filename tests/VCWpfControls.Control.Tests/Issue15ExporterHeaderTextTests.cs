using System.Windows.Controls;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using VCWpfControls.Grid.Export;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

/// <summary>
/// Issue #15: headers that are ELEMENTS (TextBlock — the standard trick for header
/// tooltips) exported as "System.Windows.Controls.TextBlock". Every header-to-text path
/// must unwrap elements through the shared GridColumnBase.HeaderText extractor.
/// </summary>
public class Issue15ExporterHeaderTextTests
{
    [StaFact]
    public void Exporter_CsvAndHtml_UnwrapElementHeaders()
    {
        var grid = new GridControl { Width = 400, RowHeight = 24, HeaderHeight = 26 };
        var window = TestWindowHost.Show(grid);
        grid.Columns.Add(new GridTextColumn
        {
            FieldName = "Name",
            Header = new TextBlock { Text = "Employee name", ToolTip = "full name" },
            Width = 150,
        });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 150 });
        grid.ItemsSource = CreateEmployees(2);
        TestWindowHost.Pump();

        try
        {
            var csv = GridExporter.ToCsv(grid, includeHeader: true);
            Assert.Contains("Employee name", csv);
            Assert.DoesNotContain("TextBlock", csv);

            var html = GridExporter.ToHtml(grid, includeHeader: true);
            Assert.Contains("<th>Employee name</th>", html);
            Assert.DoesNotContain("TextBlock", html);

            // BestFit measures the unwrapped header too (no "System.Windows..." width).
            grid.BestFitColumn(grid.Columns[0]);
            Assert.True(grid.Layout.Slots[0].Width < 200, "BestFit measured the type name, not the header text");
        }
        finally
        {
            window.Close();
        }
    }
}
