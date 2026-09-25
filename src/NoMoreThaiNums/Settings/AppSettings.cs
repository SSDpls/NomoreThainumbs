using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoMoreThaiNums.Settings;

/// <summary>
/// Persisted user settings. Kept as a plain data class with safe defaults so
/// that missing or corrupted configuration files never crash the app.
/// </summary>
public sealed class AppSettings
{
    public const int CurrentVersion = 1;

    /// <summary>When true, Thai numerals are converted to Western digits.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>When true, the app registers itself in HKCU\...\Run for auto-start.</summary>
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Creates a deep copy of these settings.</summary>
    public AppSettings Clone() => new()
    {
        Enabled = Enabled,
        StartWithWindows = StartWithWindows,
        Version = Version,
    };
}
