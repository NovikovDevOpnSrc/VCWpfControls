using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// End-to-end interaction paths through the demo app, driven by UIA patterns (Invoke/
/// Toggle/Select — no raw mouse/keyboard input, which RDP sessions can deny). Raw-input
/// paths (header drag, typing into the add-row) are covered by the STA suites; these
/// tests exercise the real application shell: page navigation, the funnel filter popup,
/// and the cell-selection mode toggle.
/// </summary>
public sealed class MousePathsUiTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly Window _window;
    private readonly UIA3Automation _automation = new();

    public MousePathsUiTests()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VCWpfControls.Demo", "bin", "Release", "net8.0-windows", "VCWpfControls.Demo.exe");
        _app = FlaUI.Core.Application.Launch(exe);
        _window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
    }

    private void Navigate(string navItemText)
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))?.AsListBox()
            ?? throw new InvalidOperationException("nav list not found");
        // The ListItem name is the view-model type; the friendly caption is a Text child.
        var item = navList.Items.FirstOrDefault(i => i.FindFirstDescendant(cf => cf.ByText(navItemText)) is not null)
            ?? throw new InvalidOperationException($"nav item '{navItemText}' not found");
        item.Select(); // SelectionItem pattern: no raw mouse input
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300); // let the page settle
    }

    private AutomationElement Grid()
        => _window.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid))
            ?? throw new InvalidOperationException("grid not found");

    [Fact]
    public void FilterMenu_FunnelInvoke_FiltersRows()
    {
        Navigate("Basic binding");
        var grid = Grid();
        var rowsBefore = grid.Patterns.Grid.Pattern.RowCount;
        Assert.True(rowsBefore > 2, $"expected several rows, got {rowsBefore}");

        // The funnel button sits inside the Department header item.
        var header = grid.FindFirstDescendant(cf => cf.ByControlType(ControlType.HeaderItem)
            .And(cf.ByName("Department", PropertyConditionFlags.MatchSubstring)))
            ?? throw new InvalidOperationException("Department header not found");
        var funnel = header.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button))
            ?? throw new InvalidOperationException("funnel button not found");
        funnel.Patterns.Toggle.Pattern.Toggle(); // ToggleButton: opens the menu when checked
        System.Threading.Thread.Sleep(400);

        // WPF popups live in a separate UIA window: search from the desktop.
        var desktop = _automation.GetDesktop();
        var popup = desktop.FindFirstChild(cf => cf.ByClassName("Popup"))
            ?? throw new InvalidOperationException("filter popup window not found");
        var dbg = new System.Text.StringBuilder();
        foreach (var child in popup.FindAllDescendants())
        {
            dbg.AppendLine(child.Properties.ControlType.ValueOrDefault + " '" + (child.Properties.Name.ValueOrDefault ?? string.Empty) + "'");
        }

        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "uitest.txt"), dbg.ToString());
        var checkbox = popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox)
            .And(cf.ByName("It")))
            ?? throw new InvalidOperationException("It value checkbox not found; popup=" + dbg);
        checkbox.Patterns.Toggle.Pattern.Toggle();
        // Scrollbar buttons are enabled too: OK is the LEFT one of the two bottom buttons.
        var ok = popup.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
            .Where(b => b.Properties.IsEnabled.ValueOrDefault && b.Patterns.Invoke.IsSupported)
            .OrderByDescending(b => b.BoundingRectangle.Y)
            .ThenBy(b => b.BoundingRectangle.X)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("OK button not found");
        ok.Patterns.Invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);

        // RowCount reflects the virtualized viewport; assert on the data instead:
        // after the filter no visible row's UIA name may contain the excluded department.
        var rowNames = grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
            .Where(d => d.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataItem)) is null) // rows, not cells
            .Select(d => d.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToList();
        Assert.True(rowNames.Count > 0, "no rows visible after filtering");
        Assert.DoesNotContain(rowNames, n => n.Contains(" It ", StringComparison.Ordinal));
    }

    [Fact]
    public void CellUnit_ToggleOn_RowSelectionDoesNotLeakIntoCells()
    {
        Navigate("Selection");
        var toggle = _window.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox)
            .And(cf.ByName("Cell selection", PropertyConditionFlags.MatchSubstring)))
            ?? throw new InvalidOperationException("cell selection toggle not found");
        toggle.Patterns.Toggle.Pattern.Toggle();
        System.Threading.Thread.Sleep(300);

        // The live counter must exist and stay at zero cells while nothing is clicked.
        var counter = _window.FindFirstDescendant(cf => cf.ByText("cells selected", PropertyConditionFlags.MatchSubstring))
            ?? throw new InvalidOperationException("counter not found");
        Assert.Contains("0", counter.Properties.Name.ValueOrDefault ?? string.Empty);

        // Selecting ROWS through UIA must not leak into the cell selection.
        var grid = Grid();
        var row = grid.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataItem))
            ?? throw new InvalidOperationException("first row not found");
        row.Patterns.SelectionItem.Pattern.Select();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);

        var after = _window.FindFirstDescendant(cf => cf.ByText("cells selected", PropertyConditionFlags.MatchSubstring))
            ?.Properties.Name.ValueOrDefault ?? string.Empty;
        Assert.Contains("0", after);
    }

    [Fact]
    public void Table_ColumnHeaders_ExposeHeaderNames()
    {
        Navigate("Column types");
        var grid = Grid();
        var headers = grid.Patterns.Table.Pattern.ColumnHeaders.Value
            .Select(h => h.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToArray();
        Assert.Contains("Name", headers);
        Assert.Contains("Salary", headers);
        Assert.True(headers.Length >= 5, $"expected the page's columns, got {headers.Length}");
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
            // teardown is best-effort
        }

        _automation.Dispose();
    }
}
