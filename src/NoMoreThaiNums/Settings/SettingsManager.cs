using System.Text.Json;
using NoMoreThaiNums.Logging;

namespace NoMoreThaiNums.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in
/// %LocalAppData%\NoMoreThaiNums\settings.json.
/// Never throws for load: missing, corrupted, or unreadable files fall back to
/// safe defaults. Save failures are reported through the return value.
/// </summary>
public sealed class SettingsManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Settings folder used before the app was renamed (v0.1.0).</summary>
    private const string LegacyDirectoryName = "ThaiNumberFix";

    private readonly string _directory;
    private readonly string _filePath;

    public SettingsManager()
        : this(GetDefaultDirectory())
    {
    }

    /// <summary>Constructor with an explicit directory, used by unit tests.</summary>
    public SettingsManager(string directory)
    {
        _directory = directory;
        _filePath = Path.Combine(directory, "settings.json");
    }

    public string FilePath => _filePath;

    public static string GetDefaultDirectory()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "NoMoreThaiNums");
    }

    /// <summary>
    /// Loads settings, falling back to defaults when the file is missing,
    /// corrupted, or unreadable. Never throws.
    /// </summary>
    public AppSettings Load() => LoadFrom(_filePath);

    /// <summary>
    /// One-time migration from the pre-rename app version: when no settings
    /// exist under the new folder yet but the old version stored some under
    /// %LocalAppData%\ThaiNumberFix, they are carried over so the user's
    /// enabled / start-with-windows state survives the rename. The old file is
    /// left in place. Never throws.
    /// </summary>
    public void MigrateLegacySettings()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        MigrateLegacySettings(Path.Combine(baseDir, LegacyDirectoryName));
    }

    /// <summary>Migration core with an explicit legacy directory (used by tests).</summary>
    public void MigrateLegacySettings(string legacyDirectory)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                return;
            }

            string legacyPath = Path.Combine(legacyDirectory, "settings.json");
            if (!File.Exists(legacyPath))
            {
                return;
            }

            AppSettings legacy = LoadFrom(legacyPath);
            Save(legacy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Migration is best-effort; defaults apply when it fails.
        }
    }

    private static AppSettings LoadFrom(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            using FileStream stream = File.OpenRead(path);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(stream, SerializerOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            ErrorLog.Write($"Failed to load settings from {path}; using defaults.", ex);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings atomically (write to temp file, then replace).
    /// Returns true on success; failures are logged and reported, never thrown.
    /// </summary>
    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            string tempPath = _filePath + ".tmp";
            using (FileStream stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, settings, SerializerOptions);
            }

            File.Move(tempPath, _filePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            ErrorLog.Write("Failed to save settings.", ex);
            return false;
        }
    }
}
