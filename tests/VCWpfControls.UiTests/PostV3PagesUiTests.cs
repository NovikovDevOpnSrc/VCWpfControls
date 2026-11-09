using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// End-to-end coverage for the post-v0.3 demo pages (Pivot, Gantt, Conditions,
/// Charts &amp; Ribbon) driven by UIA patterns only — the real shell and page wiring,
/// which the STA suites cannot see. Content presence is asserted through Table/DataItem
/// patterns and toggles/selections through Toggle/SelectionItem/Invoke.
/// </summary>
public sealed class PostV3PagesUiTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly Window _window;
    private readonly UIA3Automation _automation = new();

    public PostV3PagesUiTests()
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
        System.Threading.Thread.Sleep(400); // let the page settle
        Assert.False(_app.HasExited, $"application crashed on '{navItemText}'");
    }

    private AutomationElement Page(string automationId)
        => _window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"page '{automationId}' not found");

    private static AutomationElement GridOf(AutomationElement page)
        => page.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid))
            ?? throw new InvalidOperationException("grid not found on page");

    private static void AssertRowNamesContain(AutomationElement grid, string substring)
    {
        var names = RowNames(grid);
        Assert.Contains(names, n => n.Contains(substring, StringComparison.Ordinal));
    }

    private static List<string> RowNames(AutomationElement grid)
        => grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
            .Where(d => d.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataItem)) is null) // rows, not cells
            .Select(d => d.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToList();

    [Fact]
    public void Pivot_Page_ShowsTotalsColumnAndGrandRow()
    {
        Navigate("Pivot");
        var grid = GridOf(Page("PivotSample"));

        var headers = grid.Patterns.Table.Pattern.ColumnHeaders.Value
            .Select(h => h.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToArray();
        Assert.Contains("Total", headers); // totals column (localized, demo pinned en-US)

        var names = RowNames(grid);
        Assert.True(names.Count >= 3, $"expected region rows + grand row, got {names.Count}");
        Assert.Contains(names, n => n.Contains("Total", StringComparison.Ordinal)); // grand row
        Assert.Contains(names, n => n.Contains("East", StringComparison.Ordinal)); // a region row
    }

    [Fact]
    public void Gantt_Page_TreeHeaders_Toggles_ZoomAndReset()
    {
        Navigate("Gantt");
        var page = Page("GanttSample");
        var tree = GridOf(page);

        var headers = tree.Patterns.Table.Pattern.ColumnHeaders.Value
            .Select(h => h.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToArray();
        Assert.Contains("Task", headers);
        Assert.Contains("Start", headers);
        AssertRowNamesContain(tree, "Planning");
        AssertRowNamesContain(tree, "Build");

        // Critical-path toggle off: page must stay consistent (full rebuild behind it).
        var critical = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox)
            .And(cf.ByName("Critical path", PropertyConditionFlags.MatchSubstring)))
            ?? throw new InvalidOperationException("critical path toggle not found");
        critical.Patterns.Toggle.Pattern.Toggle();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "crashed toggling the critical path");
        AssertRowNamesContain(tree, "Planning");

        // Zoom to months through the combo, then Reset (full rebuild path in real UI).
        var zoom = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox))
            ?.AsComboBox() ?? throw new InvalidOperationException("zoom combo not found");
        zoom.Select("Month");
        System.Threading.Thread.Sleep(300);
        Assert.False(_app.HasExited, "crashed switching the timescale");

        var reset = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button)
            .And(cf.ByName("Reset")))
            ?? throw new InvalidOperationException("reset button not found");
        reset.Patterns.Invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        AssertRowNamesContain(tree, "Planning");
    }

    [Fact]
    public void Conditions_Page_BuilderRowsAndApply()
    {
        Navigate("Conditions");
        var page = Page("ConditionSample");

        // The builder starts with ONE combo (the empty group's All/Any switch).
        var combos = page.FindAllDescendants(cf => cf.ByControlType(ControlType.ComboBox));
        Assert.True(combos.Length >= 1, "the builder's group combo not found");

        // "Add condition" creates a row: field + operator combos join the group combo.
        var add = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button)
            .And(cf.ByName("Add condition")))
            ?? throw new InvalidOperationException("Add condition button not found");
        add.Patterns.Invoke.Pattern.Invoke();
        System.Threading.Thread.Sleep(300);
        var afterAdd = page.FindAllDescendants(cf => cf.ByControlType(ControlType.ComboBox));
        Assert.True(afterAdd.Length >= 3, $"expected group+field+operator combos, got {afterAdd.Length}");

        // Removing the row returns the builder to the empty group.
        var remove = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button)
            .And(cf.ByName("✕")))
            ?? throw new InvalidOperationException("row remove button not found");
        remove.Patterns.Invoke.Pattern.Invoke();
        System.Threading.Thread.Sleep(200);

        // Empty condition matches everything: Apply must not filter any rows.
        var apply = page.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button)
            .And(cf.ByName("Apply")))
            ?? throw new InvalidOperationException("Apply button not found");
        var grid = GridOf(page);
        var before = RowNames(grid).Count;
        Assert.True(before >= 2, "the unfiltered grid must show rows");

        apply.Patterns.Invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "crashed applying the condition");
        Assert.Equal(before, RowNames(grid).Count);
    }

    [Fact]
    public void ChartsRibbon_Page_RibbonAndGridRender()
    {
        Navigate("Charts & Ribbon");
        var page = Page("IntegrationSample");

        var grid = GridOf(page);
        var rows = RowNames(grid);
        Assert.True(rows.Count >= 2, $"expected the sample rows, got {rows.Count}");

        // The Fluent ribbon is present (a Custom 'Ribbon' element). Its inner commands are
        // NOT exposed to UIA (Fluent weakness, same for the SkiaSharp chart) — third-party
        // internals are out of scope; our grid on the page must render regardless.
        var ribbon = page.FindFirstDescendant(cf => cf.ByName("Ribbon"));
        Assert.NotNull(ribbon);
    }

    [Fact]
    public void BasicGrid_CellValuePattern_ReadsAndWrites()
    {
        Navigate("Basic binding");
        var grid = GridOf(Page("BasicGrid"));

        var table = grid.Patterns.Table.Pattern;
        var headers = table.ColumnHeaders.Value
            .Select(h => h.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToArray();
        var nameColumn = Array.IndexOf(headers, "Name");
        Assert.True(nameColumn >= 0, "the Name column was not found");

        var cell = grid.Patterns.Grid.Pattern.GetItem(0, nameColumn);
        var value = cell.Patterns.Value;
        Assert.True(value.IsSupported, "cells must expose the UIA Value pattern");
        var before = value.Pattern.Value;
        Assert.False(string.IsNullOrEmpty(before), "the value must read the formatted cell text");

        value.Pattern.SetValue("UIA-Edited");
        Assert.Equal("UIA-Edited", value.Pattern.Value); // wrote through the model and back
    }

    [Fact]
    public void Scheduler_Page_SwitchesViewsAndNavigates()
    {
        Navigate("Scheduler");
        var page = Page("SchedulerSample");

        // The scheduler's own chrome (combo + buttons) drives the control through the same
        // paths a user takes; the panels are custom-drawn (no UIA children of their own).
        var combo = page.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerViewTypeCombo"))!.AsComboBox();
        combo.Select("Month");
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "crashed switching the scheduler to the month view");
        Assert.Equal("Month", combo.SelectedItem.Text);

        combo.Select("Day");
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "crashed switching the scheduler to the day view");

        page.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerPrevButton"))!.AsButton().Invoke();
        page.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerNextButton"))!.AsButton().Invoke();
        page.FindFirstDescendant(cf => cf.ByAutomationId("SchedulerTodayButton"))!.AsButton().Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        Assert.False(_app.HasExited, "crashed navigating the scheduler");
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
