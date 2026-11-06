using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using VCWpfControls.Control.Tests.Infrastructure;
using VCWpfControls.Grid;
using VCWpfControls.Grid.Columns;
using Xunit;
using static VCWpfControls.Control.Tests.TestModels;

namespace VCWpfControls.Control.Tests;

[CollectionDefinition("Clipboard", DisableParallelization = true)]
public sealed class ClipboardTestsFixture
{
}

/// <summary>Pasting TSV clipboard text starting at the current cell (Ctrl+V).</summary>
[Collection("Clipboard")]
public class PasteTests
{
    private static (GridControl Grid, ObservableCollection<Employee> Source, Window Window) Create()
    {
        var source = new ObservableCollection<Employee>(CreateEmployees(4));
        var grid = new GridControl { Width = 500, RowHeight = 24, HeaderHeight = 26 };
        grid.Columns.Add(new GridTextColumn { FieldName = "Name", Header = "Name", Width = 120 });
        grid.Columns.Add(new GridTextColumn { FieldName = "Department", Header = "Dept", Width = 120, ReadOnly = true });
        grid.Columns.Add(new GridNumericColumn { FieldName = "Salary", Header = "Salary", Width = 120 });
        var window = TestWindowHost.Show(grid);
        grid.ItemsSource = source;
        TestWindowHost.Pump();
        return (grid, source, window);
    }

    private static void SetClipboard(string text)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(text, copy: true);
                return;
            }
            catch (COMException) when (attempt < 10)
            {
                System.Threading.Thread.Sleep(50);
            }
        }
    }

    [StaFact]
    public void Paste_FillsFromAnchor_SkipsReadOnlyColumns()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SetCurrentCellForTest(source[1], grid.Columns[0]);
            SetClipboard("pasted1\tignored\t111\npasted2\tignored\t222");
            var written = grid.PasteFromClipboard();
            TestWindowHost.Pump();

            // 2 rows × (Name + Salary) — the read-only Department column is skipped.
            Assert.Equal(4, written);
            Assert.Equal("pasted1", source[1].Name);
            Assert.Equal("pasted2", source[2].Name);
            Assert.Equal(111m, source[1].Salary);
            Assert.Equal(222m, source[2].Salary);
            Assert.NotEqual("ignored", source[1].Department);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Paste_UnconvertibleCell_SkipsThatCellOnly()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SetCurrentCellForTest(source[0], grid.Columns[2]);
            SetClipboard("not-a-number");
            var written = grid.PasteFromClipboard();

            Assert.Equal(0, written);
            Assert.NotEqual(0m, source[0].Salary == default ? -1m : source[0].Salary); // untouched
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Paste_BeyondData_IsDropped()
    {
        var (grid, source, window) = Create();
        try
        {
            grid.SetCurrentCellForTest(source[3], grid.Columns[0]);
            SetClipboard("one\nthree\nfour"); // only one row fits
            var written = grid.PasteFromClipboard();
            Assert.Equal(1, written);
            Assert.Equal("one", source[3].Name);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Paste_CancelEvent_BlocksPaste()
    {
        var (grid, source, window) = Create();
        grid.PastingFromClipboard += (_, e) => e.Cancel = true;
        try
        {
            SetClipboard("nope");
            Assert.Equal(0, grid.PasteFromClipboard());
            Assert.NotEqual("nope", source[0].Name);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Paste_EmptyClipboard_ReturnsZero()
    {
        var (grid, _, window) = Create();
        try
        {
            System.Windows.Clipboard.Clear();
            Assert.Equal(0, grid.PasteFromClipboard());
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Paste_IgnoresAddRowPlaceholder()
    {
        var (grid, source, window) = Create();
        grid.CanUserAddRows = true;
        TestWindowHost.Pump();
        try
        {
            grid.SetCurrentCellForTest(source[^1], grid.Columns[0]);
            SetClipboard("last-row");
            Assert.Equal(1, grid.PasteFromClipboard());
            Assert.Equal("last-row", source[^1].Name);
            Assert.Equal(source.Count + 1, grid.Items.Count); // placeholder still there, untouched
        }
        finally
        {
            window.Close();
        }
    }
}
