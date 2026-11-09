using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// The demo Settings FLYOUT (the gear button under the nav list): theme/accent/language
/// switch through the real shell. The language switch observably flips LOCALIZED LIBRARY
/// STRINGS (the wizard's Next button). Runs in the "UiSerial" collection so language
/// persistence cannot race the wizard stress test; always restores defaults in finally.
/// </summary>
[Collection("UiSerial")]
public sealed class SettingsUiTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly FlaUI.Core.AutomationElements.Window _window;
    private readonly UIA3Automation _automation = new();

    public SettingsUiTests()
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
        System.Threading.Thread.Sleep(350);
        Assert.False(_app.HasExited, $"application crashed on '{navItemText}'");
    }

    /// <summary>Opens the settings flyout and returns its popup element (a top-level HWND).</summary>
    private FlaUI.Core.AutomationElements.AutomationElement OpenSettings()
    {
        var button = _window.FindFirstDescendant(cf => cf.ByAutomationId("SettingsMenuButton"))
            ?? throw new InvalidOperationException("settings menu button not found");
        button.Patterns.Invoke.Pattern.Invoke();

        // WPF popups are top-level windows — find the one carrying the settings content.
        var desktop = _automation.GetDesktop();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            foreach (var top in desktop.FindAllChildren())
            {
                if (top.FindFirstDescendant(cf => cf.ByAutomationId("SettingsThemeDarkRadio")) is not null)
                {
                    System.Threading.Thread.Sleep(200); // let it settle
                    return top;
                }
            }

            System.Threading.Thread.Sleep(100);
        }

        throw new InvalidOperationException("settings flyout did not appear");
    }

    private void CloseSettings()
    {
        var button = _window.FindFirstDescendant(cf => cf.ByAutomationId("SettingsMenuButton"));
        button?.Patterns.Invoke.Pattern.Invoke(); // toggles the StaysOpen=False popup closed
        System.Threading.Thread.Sleep(150);
    }

    [Fact]
    public void SettingsFlyout_ThemeAccentLanguage_ApplyAndPersist()
    {
        try
        {
            var flyout = OpenSettings();

            // Theme: the Dark radio selects and sticks.
            var dark = flyout.FindFirstDescendant(cf => cf.ByAutomationId("SettingsThemeDarkRadio"))
                ?? throw new InvalidOperationException("dark radio not found");
            dark.Patterns.SelectionItem.Pattern.Select();
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            Assert.True(dark.Patterns.SelectionItem.Pattern.IsSelected, "the dark radio must stay selected");

            // The System mode radio exists and selects without crashing (the OS preference
            // itself can't be flipped from a test — only the wiring is asserted).
            var system = flyout.FindFirstDescendant(cf => cf.ByAutomationId("SettingsThemeSystemRadio"))
                ?? throw new InvalidOperationException("system radio not found");
            system.Patterns.SelectionItem.Pattern.Select();
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            Assert.True(system.Patterns.SelectionItem.Pattern.IsSelected, "the system radio must stay selected");
            dark.Patterns.SelectionItem.Pattern.Select(); // back to dark for the rest of the test

            // Accent preset switches without crashing.
            var accent = flyout.FindFirstDescendant(cf => cf.ByAutomationId("SettingsAccentCombo"))!.AsComboBox();
            accent.Select("Purple");
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));

            // Language: ru-RU observably flips the LOCALIZED wizard buttons (Далее).
            var language = flyout.FindFirstDescendant(cf => cf.ByAutomationId("SettingsLanguageCombo"))!.AsComboBox();
            language.Select("Русский (ru-RU)");
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            CloseSettings();

            Navigate("Wizard");
            var wizardPage = _window.FindFirstDescendant(cf => cf.ByAutomationId("WizardSample"))
                ?? throw new InvalidOperationException("wizard page not found");
            var next = wizardPage.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "") == "Далее");
            Assert.NotNull(next); // GridStrings.WizardNext in Russian — the setting reached the library

            // Settings survive a flyout round-trip (the VM persisted them).
            var reopened = OpenSettings();
            Assert.Equal("Purple", reopened.FindFirstDescendant(cf => cf.ByAutomationId("SettingsAccentCombo"))!.AsComboBox().SelectedItem?.Text);
            Assert.False(_app.HasExited, "application crashed during settings churn");
        }
        finally
        {
            // Restore the shared defaults for other tests.
            try
            {
                var flyout = OpenSettings();
                flyout.FindFirstDescendant(cf => cf.ByAutomationId("SettingsResetButton"))!
                    .Patterns.Invoke.Pattern.Invoke();
                _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // best effort cleanup
            }
        }
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
