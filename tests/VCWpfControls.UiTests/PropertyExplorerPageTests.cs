using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// The Property explorer page end-to-end: the PropertyGrid edits the LIVE GridControl
/// instance — toggling ShowColumnHeaders through the real UI removes the header band
/// from the sample on the left.
/// </summary>
public sealed class PropertyExplorerPageTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly FlaUI.Core.AutomationElements.Window _window;
    private readonly UIA3Automation _automation = new();

    public PropertyExplorerPageTests()
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
        System.Threading.Thread.Sleep(400);
        Assert.False(_app.HasExited, $"application crashed on '{navItemText}'");
    }

    [Fact]
    public void TogglingShowColumnHeaders_EditsTheLiveGrid()
    {
        Navigate("Property explorer");

        // The properties sit in collapsed category expanders — open them first.
        foreach (var header in _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Header)))
        {
            var expand = header.Patterns.ExpandCollapse;
            if (expand.IsSupported && expand.Pattern.ExpandCollapseState == ExpandCollapseState.Collapsed)
            {
                expand.Pattern.Expand();
                _app.WaitWhileBusy(TimeSpan.FromSeconds(3));
            }
        }

        // The page shows the control's API (declared-properties-only, no ItemsControl noise).
        var showColumnHeaders = Retry.WhileNull(
            () => _window.FindFirstDescendant(cf => cf.ByText("ShowColumnHeaders")),
            TimeSpan.FromSeconds(8)).Result;
        Assert.NotNull(showColumnHeaders);
        Assert.Null(_window.FindFirstDescendant(cf => cf.ByText("ItemTemplate"))); // platform noise filtered

        // The LIVE sample's headers are on screen (the UIA-visible oracle).
        Assert.True(_window.FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem)).Length >= 6,
            "the live grid must show its headers before the edit");

        // Flip the property through the property grid's checkbox editor. UIA flattens the
        // row (Border>Grid>label+editor) into sibling children of the category panel — the
        // editor follows its label in order, so scan FORWARD from the label for the first
        // checkbox.
        var container = showColumnHeaders.Parent
            ?? throw new InvalidOperationException("property container not found");
        var children = container.FindAllChildren();
        var labelIndex = Array.FindIndex(children,
            c => c.ControlType == ControlType.Text && (c.Properties.Name.ValueOrDefault ?? "") == "ShowColumnHeaders");
        Assert.True(labelIndex >= 0, "the ShowColumnHeaders label must be a direct child of the property container");
        FlaUI.Core.AutomationElements.AutomationElement? editorElement = null;
        for (var i = labelIndex + 1; i < children.Length && editorElement is null; i++)
        {
            if (children[i].ControlType == ControlType.CheckBox)
            {
                editorElement = children[i]; // the editor of this row (next label is far away)
            }
            else if (children[i].ControlType == ControlType.Text)
            {
                break; // reached the next row's label without an editor
            }
        }

        var checkbox = editorElement?.AsCheckBox()
            ?? throw new InvalidOperationException("ShowColumnHeaders checkbox editor not found");
        checkbox.Toggle();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(400);

        // The live grid followed: the header band is gone.
        Assert.Empty(_window.FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem)));
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
