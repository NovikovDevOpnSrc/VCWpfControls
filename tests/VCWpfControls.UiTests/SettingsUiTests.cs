using System.IO;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace VCWpfControls.UiTests;

/// <summary>
/// The demo Settings page: theme/accent/language switch through the real shell. The
/// language switch observably flips LOCALIZED LIBRARY STRINGS (the wizard's Next button).
/// Runs in the "UiSerial" collection so language persistence cannot race the wizard
/// stress test; always restores en-US/Light/Default in finally.
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

    private AutomationElement Page(string automationId)
        => _window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"page '{automationId}' not found");

    [Fact]
    public void Settings_ThemeAccentLanguage_ApplyAndPersist()
    {
        try
        {
            Navigate("Settings");
            var page = Page("SettingsSample");

            // Theme: the Dark radio selects and sticks.
            var dark = page.FindFirstDescendant(cf => cf.ByAutomationId("SettingsThemeDarkRadio"))
                ?? throw new InvalidOperationException("dark radio not found");
            dark.Patterns.SelectionItem.Pattern.Select();
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            Assert.True(dark.Patterns.SelectionItem.Pattern.IsSelected, "the dark radio must stay selected");

            // Accent preset switches without crashing.
            var accent = page.FindFirstDescendant(cf => cf.ByAutomationId("SettingsAccentCombo"))!.AsComboBox();
            accent.Select("Purple");
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));

            // Language: ru-RU observably flips the LOCALIZED wizard buttons («Далее»).
            var language = page.FindFirstDescendant(cf => cf.ByAutomationId("SettingsLanguageCombo"))!.AsComboBox();
            language.Select("Русский (ru-RU)");
            _app.WaitWhileBusy(TimeSpan.FromSeconds(5));
            Navigate("Wizard");
            var wizardPage = Page("WizardSample");
            var next = wizardPage.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "") == "Далее");
            Assert.NotNull(next); // GridStrings.WizardNext in Russian — the setting reached the library

            // Settings survive a page round-trip (the VM persisted them). FlaUI caches
            // per element — re-find the page after navigation.
            Navigate("Settings");
            var settingsAgain = Page("SettingsSample");
            Assert.Equal("Purple", settingsAgain.FindFirstDescendant(cf => cf.ByAutomationId("SettingsAccentCombo"))!.AsComboBox().SelectedItem?.Text);
            Assert.False(_app.HasExited, "application crashed during settings churn");
        }
        finally
        {
            // Restore the shared defaults for other tests.
            try
            {
                Navigate("Settings");
                Page("SettingsSample").FindFirstDescendant(cf => cf.ByAutomationId("SettingsResetButton"))!
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
