using System.Windows;

namespace VCWpfControls.Demo;

/// <summary>Demo application entry point.</summary>
public partial class App : Application
{
    /// <summary>Initializes the demo.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // The demo texts are English; pin the UI culture so library strings (grid hints,
        // menus, empty state) match. Remove this line to see the library follow the OS
        // language (e.g. Russian on a Russian Windows).
        System.Threading.Thread.CurrentThread.CurrentUICulture =
            System.Globalization.CultureInfo.GetCultureInfo("en-US");

        base.OnStartup(e);
    }
}
