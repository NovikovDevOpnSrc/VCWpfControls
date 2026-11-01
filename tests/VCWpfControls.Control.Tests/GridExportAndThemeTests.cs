using System.IO;
using ClosedXML.Excel;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Export;
using VCWpfControls.Themes;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

public class GridExportAndThemeTests
{
    [StaFact]
    public void ToCsv_ExportsHeadersAndDataRows()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(5);
        TestWindowHost.Pump();

        try
        {
            var csv = GridExporter.ToCsv(grid);
            var lines = csv.TrimEnd().Split('\n');
            Assert.Equal(6, lines.Length); // header + 5 rows
            Assert.Contains("Name", lines[0]);
            Assert.Contains("Employee", lines[1]);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ToCsv_QuotesValuesWithSeparators()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = CreateEmployees(1);
        data[0].Name = "Semicolon;Name";
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU"); // separator ';'
            var csv = GridExporter.ToCsv(grid, includeHeader: false, culture);
            Assert.Contains("\"Semicolon;Name\"", csv);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ToHtml_EscapesMarkup()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        var data = CreateEmployees(1);
        data[0].Name = "<script>";
        grid.ItemsSource = data;
        TestWindowHost.Pump();

        try
        {
            var html = GridExporter.ToHtml(grid, includeHeader: false);
            Assert.Contains("&lt;script&gt;", html);
            Assert.DoesNotContain("<script>", html);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ExportToXlsx_CreatesReadableWorkbook()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(7);
        TestWindowHost.Pump();

        try
        {
            var path = Path.Combine(Path.GetTempPath(), $"vcwpf_test_{Guid.NewGuid():N}.xlsx");
            GridExporter.ExportToXlsx(grid, path);
            Assert.True(File.Exists(path));

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheets.First();
            Assert.Equal(8, sheet.LastRowUsed()!.RowNumber()); // header + 7
            File.Delete(path);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void ThemeManager_SwitchesThemeResources()
    {
        var grid = new GridControl();
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = CreateEmployees(3);
        TestWindowHost.Pump();

        try
        {
            var light = grid.Background;
            GridThemeManager.Apply(grid, GridTheme.Dark);
            TestWindowHost.Pump();
            var dark = grid.Background;

            var lightColor = ((System.Windows.Media.SolidColorBrush)light).Color;
            var darkColor = ((System.Windows.Media.SolidColorBrush)dark).Color;
            Assert.NotEqual(lightColor, darkColor);

            GridThemeManager.Apply(grid, GridTheme.Light);
            TestWindowHost.Pump();
            Assert.Equal(lightColor, ((System.Windows.Media.SolidColorBrush)grid.Background).Color);
        }
        finally
        {
            window.Close();
        }
    }
}
