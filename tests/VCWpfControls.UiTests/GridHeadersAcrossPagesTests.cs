using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// Every demo page that hosts a grid must materialize header captions in the visual
/// tree — the blank-header launch glitch (see the CHANGELOG [Unreleased] entry) was
/// reported on TWO different pages, so the guard walks the whole navigation. The
/// captions are read from the raw visual tree (ControlType.Text descendants of each
/// HeaderItem): "EMPTY" means the caption never materialized.
/// </summary>
public sealed class GridHeadersAcrossPagesTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly FlaUI.Core.AutomationElements.Window _window;
    private readonly UIA3Automation _automation = new();

    public GridHeadersAcrossPagesTests()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VCWpfControls.Demo", "bin", "Release", "net8.0-windows", "VCWpfControls.Demo.exe");
        _app = FlaUI.Core.Application.Launch(exe);
        _window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
    }

    private void Navigate(string navItemText)
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))!.AsListBox();
        var item = navList.Items.First(i => i.FindFirstDescendant(cf => cf.ByText(navItemText)) is not null);
        item.Select();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(10));
        System.Threading.Thread.Sleep(300);
        Assert.False(_app.HasExited, $"application crashed on '{navItemText}'");
    }

    [Fact]
    public void EveryGridPage_MaterializesAllHeaderCaptions()
    {
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId("NavList"))!.AsListBox();
        var pages = navList.Items
            .Select(i => i.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Properties.Name.ValueOrDefault ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
           .ToList()!;
        Assert.NotEmpty(pages);

        var pagesWithHeaders = 0;
        foreach (var page in pages)
        {
            Navigate(page);

            foreach (var delay in new[] { 150, 700 })
            {
                System.Threading.Thread.Sleep(delay);
                var headers = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem));
                if (headers.Length == 0)
                {
                    continue; // a page without a grid
                }

                pagesWithHeaders++;
                var blanks = headers
                    .Where(h => h.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).Length == 0)
                    .ToList();
                Assert.True(blanks.Count == 0,
                    $"'{page}': {blanks.Count}/{headers.Length} headers have no caption element in the visual tree");
            }
        }

        Assert.True(pagesWithHeaders > 0, "no page with a grid was visited");
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
            // teardown best effort
        }

        _automation.Dispose();
    }
}
