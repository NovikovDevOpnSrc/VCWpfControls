using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

public sealed class DemoSmokeTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly Window _window;

    public DemoSmokeTests()
    {
        var exe = FindDemoExecutable();
        Assert.True(File.Exists(exe), $"Demo exe not found at {exe}; build the solution first.");
        _app = FlaUI.Core.Application.Launch(exe);
        var automation = new UIA3Automation();
        _window = _app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
        Assert.NotNull(_window);
    }

    [Fact]
    public void MainWindow_ShowsNavigation_AndAllSamplesOpen()
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))?.AsListBox();
        Assert.NotNull(navList);
        Assert.True(navList!.Items.Length >= 6, $"expected at least 6 samples, got {navList.Items.Length}");

        // Every navigation click must switch the page: the sample's content control
        // (registered automation id) must appear after selecting the item.
        var expectedIds = new Dictionary<string, string>
        {
            ["Basic binding"] = "BasicGrid",
            ["Column types"] = "ColumnTypesGrid",
            ["Sorting"] = "SortingGrid",
            ["Selection"] = "SelectionGrid",
            ["Editing & validation"] = "EditingGrid",
            ["Virtualization (100k rows)"] = "VirtualizationGrid",
            ["Grouping & filters"] = "AnalyticsGrid",
            ["Property grid"] = "PropertyGridSample",
            ["Tree list"] = "TreeListSample",
            ["Editors"] = "EditorsSample",
            ["Docking"] = "DockingSample",
        };

        foreach (var item in navList.Items)
        {
            var label = item.Text;
            item.Select();
            _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
            Assert.False(_app.HasExited, $"application crashed after opening '{label}'");

            if (expectedIds.TryGetValue(label, out var automationId))
            {
                var content = _window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                Assert.True(content is not null,
                    $"page '{label}' did not switch (automation id '{automationId}' not found)");
            }
        }
    }

    [Fact]
    public void BasicGrid_ShowsRows()
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))!.AsListBox();
        navList.Items[0].Select();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));

        var grid = _window.FindFirstDescendant(cf => cf.ByAutomationId("BasicGrid"));
        Assert.NotNull(grid);
    }

    private static string FindDemoExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VCWpfControls.Demo", "bin", "Release", "net8.0-windows", "VCWpfControls.Demo.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VCWpfControls.Demo", "bin", "Debug", "net8.0-windows", "VCWpfControls.Demo.exe"),
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
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
            // best effort teardown
        }
    }
}
