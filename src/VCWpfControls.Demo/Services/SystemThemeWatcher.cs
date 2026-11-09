using Microsoft.Win32;

namespace VCWpfControls.Demo.Services;

/// <summary>
/// Watches the Windows "apps use light theme" preference (Personalize registry key) so the
/// demo's System mode follows the OS light/dark switch live — a small stand-in for .NET 9's
/// <c>ThemeMode="System"</c>. Polls every 2 seconds (the key has no reliable change event).
/// </summary>
public static class SystemThemeWatcher
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";

    private static System.Windows.Threading.DispatcherTimer? _timer;
    private static bool _lastLight = true;

    /// <summary>Raised (on the UI thread) when the OS light/dark preference flips.</summary>
    public static event EventHandler<bool>? LightChanged;

    /// <summary>Whether the OS currently prefers light apps (unknown -> light).</summary>
    public static bool IsLightPreferred()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) is int light && light == 1;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Starts the 2-second poll (idempotent; raises on the dispatcher thread).</summary>
    public static void Start()
    {
        if (_timer is not null)
        {
            return;
        }

        _lastLight = IsLightPreferred();
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        _timer.Tick += (_, _) =>
        {
            var light = IsLightPreferred();
            if (light != _lastLight)
            {
                _lastLight = light;
                LightChanged?.Invoke(null, light);
            }
        };
        _timer.Start();
    }
}
