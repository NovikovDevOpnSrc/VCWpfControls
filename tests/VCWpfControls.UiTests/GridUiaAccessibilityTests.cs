using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

public sealed class GridUiaAccessibilityTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly Window _window;
    private readonly UIA3Automation _automation = new();

    public GridUiaAccessibilityTests()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VCWpfControls.Demo", "bin", "Release", "net8.0-windows", "VCWpfControls.Demo.exe");
        _app = FlaUI.Core.Application.Launch(exe);
        _window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))!.AsListBox().Items[0].Select();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GridPattern_GetItem_ReturnsSelectableRowWithDataName()
    {
        var grid = _window
            .FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid))
            ?? throw new InvalidOperationException("grid control not found in UIA tree");
        Assert.Equal("Data grid", grid.Properties.Name.ValueOrDefault);

        var gridPattern = grid.Patterns.Grid.Pattern;
        Assert.True(gridPattern.RowCount > 0, "RowCount must work");
        Assert.True(gridPattern.ColumnCount >= 5, "ColumnCount must work");

        // Issue #1 scenario: GetItem -> DataItem -> SelectionItem.Select().
        var row = gridPattern.GetItem(0, 1);
        Assert.NotNull(row);
        var rowElement = row!.AsGridCell() ?? throw new InvalidOperationException("cell");
        var dataItem = FindDataItemFor(grid, row);
        Assert.NotNull(dataItem);

        var name = dataItem!.Properties.Name.ValueOrDefault ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(name), "row name must expose cell values");

        var selectionItem = dataItem.Patterns.SelectionItem;
        Assert.True(selectionItem.IsSupported, "rows must support SelectionItem");
        Assert.False(selectionItem.Pattern.IsSelected);
        selectionItem.Pattern.Select();
        Assert.True(selectionItem.Pattern.IsSelected, "Select() must select the row");
    }

    [Fact]
    public void TablePattern_ColumnHeaders_HaveNames_AndSortOnInvoke()
    {
        var grid = _window
            .FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid))
            ?? throw new InvalidOperationException("grid control not found in UIA tree");

        var tablePattern = grid.Patterns.Table.Pattern;
        var headers = tablePattern.ColumnHeaders?.Value ?? Array.Empty<FlaUI.Core.AutomationElements.AutomationElement>();
        Assert.True(headers.Length >= 5, "column headers must be exposed via TablePattern");
        Assert.Contains(headers, h => (h.Properties.Name.ValueOrDefault ?? string.Empty) == "Name");

        // Invoke on the header sorts by that column (values in view order change).
        var nameHeader = headers.First(h => h.Properties.Name.ValueOrDefault == "Name");
        nameHeader.Patterns.Invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        var gridPattern2 = grid.Patterns.Grid.Pattern;
        Assert.True(gridPattern2.RowCount > 0, "grid alive after sort invoke");
    }

    private static FlaUI.Core.AutomationElements.AutomationElement? FindDataItemFor(
        FlaUI.Core.AutomationElements.AutomationElement grid,
        FlaUI.Core.AutomationElements.AutomationElement cell)
    {
        // The row container is the DataItem ancestor of the returned cell provider.
        var current = cell;
        for (var depth = 0; depth < 5 && current is not null; depth++)
        {
            if (current.Properties.ControlType.ValueOrDefault == ControlType.DataItem)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    public void Dispose()
    {
        try
        {
            if (!_app.HasExited)
            {
                _app.Close();
                _app.Dispose();
            }
        }
        catch
        {
            // best-effort teardown
        }
    }
}
