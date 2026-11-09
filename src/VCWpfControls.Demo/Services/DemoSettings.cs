using System.IO;
using System.Text.Json;

namespace VCWpfControls.Demo.Services;

/// <summary>One demo settings record (JSON-serialized to %AppData%).</summary>
public sealed class DemoSettings
{
    /// <summary>The UI theme.</summary>
    public string Theme { get; set; } = "Light";

    /// <summary>The accent preset name ("Default" removes the override).</summary>
    public string Accent { get; set; } = "Default";

    /// <summary>The UI culture name for library strings.</summary>
    public string Language { get; set; } = "en-US";

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VCWpfControls.Demo",
            "settings.json");

    /// <summary>Loads the settings; missing or corrupt files yield the defaults.</summary>
    public static DemoSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<DemoSettings>(File.ReadAllText(SettingsPath)) ?? new DemoSettings();
            }
        }
        catch
        {
            // corrupt/unreadable settings fall back to defaults
        }

        return new DemoSettings();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Persists the settings (best effort).</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // best effort — the demo works without persistence
        }
    }
}
