using System.Windows;

namespace VCWpfControls.Demo;

/// <summary>Demo application entry point.</summary>
public partial class App : Application
{
    /// <summary>Initializes the demo.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Library strings (grid hints, menus, empty state) follow the SAVED language
        // (default en-US; see the Settings page — persisted in %AppData%).
        var settings = Services.DemoSettings.Load();
        Services.DemoTheme.ApplyLanguage(settings.Language);

        base.OnStartup(e);
        Services.DemoTheme.Apply(settings.Theme, settings.Accent);
    }
}
