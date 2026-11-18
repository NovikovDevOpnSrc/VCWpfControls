using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// The Menus page through the LIVE UIA channel (the STA suite cannot open bar submenu
/// popups — inactive test host): expand a top-level header (ExpandCollapse), invoke a
/// leaf item (Invoke), toggle a checkable item (Toggle), and verify the status line
/// (a DP — updates for real) plus the disabled item staying inert.
/// </summary>
public sealed class MenusPageE2eTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly FlaUI.Core.AutomationElements.Window _window;
    private readonly UIA3Automation _automation = new();

    public MenusPageE2eTests()
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
    public void MenuBar_Expand_Invoke_Toggle_StatusUpdates()
    {
        Navigate("Menus");

        // The bar: WPF Menu maps to ControlType.MenuBar in UIA.
        // The page bar: WPF Menu maps to ControlType.Menu in UIA (MenuBar is the
        // window's SYSTEM menu — "Система" — a different element).
        var bar = FindPageMenuBar()
            ?? throw new InvalidOperationException("menu bar not found");
        var items = bar.FindAllChildren();
        Assert.True(items.Length >= 4, $"expected >= 4 top-level items, got {items.Length}");

        // Names come from the headers (access-key underscores stripped by UIA).
        var edit = items.FirstOrDefault(i => (i.Properties.Name.ValueOrDefault ?? string.Empty).StartsWith("Edit", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Edit header not found");

        // ExpandCollapse opens the live popup (a top-level HWND like the settings flyout).
        var expand = edit.Patterns.ExpandCollapse;
        Assert.True(expand.IsSupported, "MenuItem headers must expose ExpandCollapsePattern");
        expand.Pattern.Expand();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(300);

        var popup = FindOpenMenuPopup()
            ?? throw new InvalidOperationException("the Edit submenu popup did not appear");

        // Toggle the checkable Autosave item FIRST — checkable items keep the menu open
        // (invoking a leaf closes it, and re-expansion over UIA is flaky).
        var autosave = popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
            .FirstOrDefault(mi => (mi.Properties.Name.ValueOrDefault ?? string.Empty).Contains("Autosave", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Autosave MenuItem not found; items=[" + string.Join("|",
                popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem)).Select(mi => $"'{mi.Properties.Name.ValueOrDefault ?? ""}'")) + "]");
        var toggle = autosave.Patterns.Toggle;
        Assert.True(toggle.IsSupported, "checkable menu items must expose TogglePattern");
        var before = toggle.Pattern.ToggleState;
        var statusBefore = (_window.FindFirstDescendant(cf => cf.ByAutomationId("MenusLastAction"))
            ?.Properties.Name.ValueOrDefault) ?? string.Empty;
        toggle.Pattern.Toggle();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(400);

        // The stock Toggle pattern flips IsChecked WITHOUT raising Click — the demo
        // reports Checked/Unchecked explicitly; the status line must reflect the flip.
        var statusAfterToggle = (_window.FindFirstDescendant(cf => cf.ByAutomationId("MenusLastAction"))
            ?.Properties.Name.ValueOrDefault) ?? string.Empty;
        Assert.Contains("Autosave", statusAfterToggle, StringComparison.Ordinal);
        Assert.NotEqual(statusBefore, statusAfterToggle);

        // Then collapse and re-expand to invoke the Cut leaf (a fresh popup each time).
        expand.Pattern.Collapse();
        System.Threading.Thread.Sleep(200);
        expand.Pattern.Expand();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(400);
        var popupInvoke = FindOpenMenuPopup()
            ?? throw new InvalidOperationException("the Edit submenu popup did not reappear");
        var cut = popupInvoke.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
            .FirstOrDefault(mi => (mi.Properties.Name.ValueOrDefault ?? string.Empty).Contains("Cu", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Cut MenuItem not found");
        var invoke = cut.Patterns.Invoke;
        Assert.True(invoke.IsSupported, "leaf menu items must expose InvokePattern");
        invoke.Pattern.Invoke();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(300);

        var status = _window.FindFirstDescendant(cf => cf.ByAutomationId("MenusLastAction"))
            ?? throw new InvalidOperationException("status line not found");
        var text1 = status.Properties.Name.ValueOrDefault ?? string.Empty;
        Assert.Contains("Edit", text1, StringComparison.Ordinal);
        Assert.Contains("Cu", text1, StringComparison.Ordinal);
    }

    [Fact]
    public void MenuBar_DisabledItem_IsNotInvokable()
    {
        Navigate("Menus");
        var bar = FindPageMenuBar()
            ?? throw new InvalidOperationException("menu bar not found");
        var view = bar.FindAllChildren()
            .FirstOrDefault(i => (i.Properties.Name.ValueOrDefault ?? string.Empty).StartsWith("View", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("View header not found");

        view.Patterns.ExpandCollapse.Pattern.Expand();
        _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
        System.Threading.Thread.Sleep(300);
        var popup = FindOpenMenuPopup()
            ?? throw new InvalidOperationException("the View submenu popup did not appear");

        var disabled = popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
            .FirstOrDefault(mi => (mi.Properties.Name.ValueOrDefault ?? string.Empty).Contains("Unavailable", StringComparison.Ordinal)
                || (mi.Properties.Name.ValueOrDefault ?? string.Empty).Contains("недоступ", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "disabled MenuItem not found; items=[" + string.Join("|",
                    popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                        .Select(mi => $"'{mi.Properties.Name.ValueOrDefault ?? ""}'{(mi.Properties.IsEnabled.ValueOrDefault ? "" : " [disabled]")}")) + "]");
        Assert.False(disabled.Properties.IsEnabled.ValueOrDefault,
            "the disabled item must not report enabled");
    }

    /// <summary>The PAGE's menu bar (the window-level MenuBar is the SYSTEM menu).</summary>
    private FlaUI.Core.AutomationElements.AutomationElement? FindPageMenuBar()
    {
        // The page's WPF Menu = ControlType.Menu (the window-level MenuBar is the SYSTEM
        // menu); scope to the page element so the system menu never matches.
        return _window.FindFirstDescendant(cf => cf.ByAutomationId("MenusSample"))
            ?.FindFirstDescendant(cf => cf.ByControlType(ControlType.Menu));
    }

    /// <summary>Finds the open menu popup (a top-level HWND like any WPF popup).</summary>
    private FlaUI.Core.AutomationElements.AutomationElement? FindOpenMenuPopup()
    {
        var desktop = _automation.GetDesktop();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            foreach (var top in desktop.FindAllChildren())
            {
                if (top.Properties.NativeWindowHandle == _window.Properties.NativeWindowHandle)
                {
                    continue; // the MAIN window hosts the collapsed bar (MenuItems too) — only POPUPS count
                }

                if (top.Properties.ProcessId.IsSupported && top.Properties.ProcessId.ValueOrDefault != _app.ProcessId)
                {
                    continue; // only our app's windows
                }

                var descendants = top.FindAllDescendants();
                foreach (var d in descendants)
                {
                    if (d.ControlType == ControlType.MenuItem)
                    {
                        return top; // a popup window hosting menu items
                    }
                }
            }

            System.Threading.Thread.Sleep(100);
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
            // teardown best effort
        }

        _automation.Dispose();
    }
}
