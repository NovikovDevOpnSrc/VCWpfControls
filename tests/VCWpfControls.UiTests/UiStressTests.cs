using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// The UI-layer stress round: sorting through header Invoke, cell validation through the
/// Value pattern (valid + unconvertible input), the Wizard gate/finish flow through Toggle
/// and Invoke, rapid navigation sweeps and view-switch churn — all through UIA patterns
/// against the real demo shell.
/// </summary>
[Collection("UiSerial")]
public sealed class UiStressTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly FlaUI.Core.AutomationElements.Window _window;
    private readonly UIA3Automation _automation = new();

    public UiStressTests()
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
        var item = navList.Items.FirstOrDefault(i => i.FindFirstDescendant(cf => cf.ByText(navItemText)) is not null)
            ?? throw new InvalidOperationException($"nav item '{navItemText}' not found");
        item.Select();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);
        Assert.False(_app.HasExited, $"application crashed on '{navItemText}'");
    }

    private AutomationElement Page(string automationId)
        => _window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"page '{automationId}' not found");

    private static AutomationElement GridOf(AutomationElement page)
        => page.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid))
            ?? throw new InvalidOperationException("grid not found on page");

    private static List<string> RowNames(AutomationElement grid)
        => grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
            .Where(d => d.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataItem)) is not null) // ROWS carry cell DataItems; bare DataItems are cells
            .Select(d => d.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToList();

    private static string NameOfRow(string row)
    {
        // "1 David Brown david1@..." -> "David Brown" (Id, first, last are the first three tokens).
        var tokens = row.Split(' ');
        return tokens.Length >= 3 ? tokens[1] + " " + tokens[2] : row;
    }

    [Fact]
    public void Sorting_NameHeader_CyclesAscendingThenDescending()
    {
        Navigate("Basic binding");
        var grid = GridOf(Page("BasicGrid"));

        var nameHeader = grid.Patterns.Table.Pattern.ColumnHeaders.Value
            .FirstOrDefault(h => (h.Properties.Name.ValueOrDefault ?? "") == "Name")
            ?? throw new InvalidOperationException("Name header not found");

        nameHeader.Patterns.Invoke.Pattern.Invoke(); // ascending
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);
        var ascending = RowNames(grid).Select(NameOfRow).ToList();
        Assert.Equal(ascending.OrderBy(n => n, StringComparer.Ordinal), ascending);

        nameHeader.Patterns.Invoke.Pattern.Invoke(); // descending
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);
        var descending = RowNames(grid).Select(NameOfRow).ToList();
        Assert.Equal(descending.OrderByDescending(n => n, StringComparer.Ordinal), descending);

        // Cycle every remaining sortable header once — nothing may crash.
        foreach (var header in grid.Patterns.Table.Pattern.ColumnHeaders.Value)
        {
            if (header.Patterns.Invoke.IsSupported)
            {
                header.Patterns.Invoke.Pattern.Invoke();
                _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            }
        }

        Assert.False(_app.HasExited, "application crashed while sorting");
    }

    [Fact]
    public void Validation_ThroughValuePattern_AcceptsValidRejectsGarbage()
    {
        Navigate("Editing & validation");
        var page = Page("EditingGrid");
        var grid = GridOf(page);

        var headers = grid.Patterns.Table.Pattern.ColumnHeaders.Value
            .Select(h => h.Properties.Name.ValueOrDefault ?? "")
            .ToArray();
        var salaryColumn = Array.IndexOf(headers, "Salary");
        Assert.True(salaryColumn >= 0, "Salary column not found");

        var cell = grid.Patterns.Grid.Pattern.GetItem(0, salaryColumn);
        var value = cell.Patterns.Value;
        Assert.True(value.IsSupported, "cells must expose the Value pattern");

        value.Pattern.SetValue("12345");
        Assert.Equal("12345", value.Pattern.Value); // wrote through

        value.Pattern.SetValue("not-a-number");
        Assert.Equal("12345", value.Pattern.Value); // garbage rejected, previous value kept
        Assert.False(_app.HasExited, "application crashed on garbage input");
    }

    [Fact]
    public void Wizard_GateToggleThenNextUntilFinish()
    {
        Navigate("Wizard");
        var page = Page("WizardSample");

        // Page 1 is gated: Next must start disabled. NOTE: FlaUI caches properties per
        // element — every fresh IsEnabled/Name read below RE-FINDS the button.
        AutomationElement FindNext() => page.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
            .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "") is "Next" or "Finish")
            ?? throw new InvalidOperationException("Next button not found");

        Assert.False(FindNext().Properties.IsEnabled.ValueOrDefault, "Next must be gated on page 1");

        var gate = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox))
            ?? throw new InvalidOperationException("gate checkbox not found");
        gate.Patterns.Toggle.Pattern.Toggle(); // allow leaving
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(300);

        Assert.True(FindNext().Properties.IsEnabled.ValueOrDefault, "Next must enable after the gate");

        for (var step = 0; step < 3; step++)
        {
            var stepButton = FindNext();
            if (!stepButton.Properties.IsEnabled.ValueOrDefault)
            {
                break;
            }

            stepButton.Patterns.Invoke.Pattern.Invoke(); // Next on pages 1..2, Finish on page 3
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            System.Threading.Thread.Sleep(300);
        }

        var status = page.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(t => t.Properties.Name.ValueOrDefault ?? "")
            .FirstOrDefault(t => t.Contains("Finished", StringComparison.Ordinal));
        Assert.NotNull(status);
        Assert.False(_app.HasExited, "application crashed finishing the wizard");
    }

    [Fact]
    public void RapidNavigation_ThreeFullSweeps()
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))!.AsListBox();
        var labels = navList.Items.Select(i => i.Text).ToList();
        Assert.True(labels.Count >= 15, $"expected the full nav list, got {labels.Count}");

        foreach (var sweep in Enumerable.Range(0, 3))
        {
            foreach (var item in navList.Items)
            {
                item.Select(); // deliberately NO settling waits — churn
            }
        }

        _app.WaitWhileBusy(TimeSpan.FromSeconds(15));
        System.Threading.Thread.Sleep(400);
        Assert.False(_app.HasExited, "application crashed during rapid navigation sweeps");
    }

    [Fact]
    public void ViewChurn_SchedulerAndGantt()
    {
        Navigate("Scheduler");
        var schedulerPage = Page("SchedulerSample");
        var combo = schedulerPage.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerViewTypeCombo"))!.AsComboBox();
        var next = schedulerPage.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerNextButton"))!;
        var prev = schedulerPage.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerPrevButton"))!;
        for (var i = 0; i < 5; i++)
        {
            combo.Select(i % 2 == 0 ? "Month" : "Day");
            next.Patterns.Invoke.Pattern.Invoke();
            prev.Patterns.Invoke.Pattern.Invoke();
        }

        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "application crashed churning scheduler views");

        Navigate("Gantt");
        var ganttPage = Page("GanttSample");
        var zoom = ganttPage.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox))!.AsComboBox();
        var reset = ganttPage.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
            .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "") == "Reset")
            ?? throw new InvalidOperationException("Gantt Reset button not found");
        foreach (var unit in new[] { "Week", "Month", "Day" })
        {
            zoom.Select(unit);
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        }

        reset.Patterns.Invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "application crashed churning gantt views");
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
