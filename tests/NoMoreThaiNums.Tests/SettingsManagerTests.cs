using NoMoreThaiNums.Logging;
using NoMoreThaiNums.Settings;
using Xunit;

namespace NoMoreThaiNums.Tests;

public sealed class SettingsManagerTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoMoreThaiNumsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        ErrorLog.DirectoryOverride = _tempDir;
    }

    public void Dispose()
    {
        ErrorLog.DirectoryOverride = null;
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsSafeDefaults()
    {
        var manager = new SettingsManager(_tempDir);
        AppSettings settings = manager.Load();

        Assert.True(settings.Enabled);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var manager = new SettingsManager(_tempDir);
        var original = new AppSettings { Enabled = false, StartWithWindows = true };

        Assert.True(manager.Save(original));
        AppSettings loaded = manager.Load();

        Assert.False(loaded.Enabled);
        Assert.True(loaded.StartWithWindows);
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsDefaultsInsteadOfThrowing()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "{ this is not json !!!");
        var manager = new SettingsManager(_tempDir);

        AppSettings settings = manager.Load();

        Assert.True(settings.Enabled);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), string.Empty);
        var manager = new SettingsManager(_tempDir);

        AppSettings settings = manager.Load();

        Assert.True(settings.Enabled);
    }

    [Fact]
    public void Load_UnexpectedJsonShape_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "[1, 2, 3]");
        var manager = new SettingsManager(_tempDir);

        AppSettings settings = manager.Load();

        Assert.True(settings.Enabled);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        string nested = Path.Combine(_tempDir, "created", "by", "save");
        var manager = new SettingsManager(nested);
        var settings = new AppSettings { Enabled = false };

        Assert.True(manager.Save(settings));
        Assert.True(File.Exists(manager.FilePath));
    }

    [Fact]
    public void Load_DirectoryUnreadable_ReturnsDefaults()
    {
        // A file path where a directory would be needed: force failure by using
        // an invalid path character is platform-dependent; instead point at a
        // path whose "file" is actually a directory.
        string dirAsFile = Path.Combine(_tempDir, "settings.json");
        Directory.CreateDirectory(dirAsFile);
        var manager = new SettingsManager(_tempDir);

        AppSettings settings = manager.Load();

        Assert.True(settings.Enabled);
    }

    [Fact]
    public void Clone_IndependentCopy()
    {
        var original = new AppSettings { Enabled = true, StartWithWindows = false };
        AppSettings copy = original.Clone();

        copy.Enabled = false;
        copy.StartWithWindows = true;

        Assert.True(original.Enabled);
        Assert.False(original.StartWithWindows);
    }

    [Fact]
    public void MigrateLegacySettings_CopiesLegacyValuesIntoNewFolder()
    {
        string legacyDir = Path.Combine(_tempDir, "legacy", "ThaiNumberFix");
        Directory.CreateDirectory(legacyDir);
        File.WriteAllText(
            Path.Combine(legacyDir, "settings.json"),
            """{ "enabled": false, "startWithWindows": true }""");

        var manager = new SettingsManager(_tempDir);
        manager.MigrateLegacySettings(Path.Combine(_tempDir, "legacy", "ThaiNumberFix"));

        AppSettings migrated = manager.Load();
        Assert.False(migrated.Enabled);
        Assert.True(migrated.StartWithWindows);
        Assert.True(File.Exists(manager.FilePath));
    }

    [Fact]
    public void MigrateLegacySettings_DoesNotOverwriteExistingSettings()
    {
        var manager = new SettingsManager(_tempDir);
        Assert.True(manager.Save(new AppSettings { Enabled = true, StartWithWindows = false }));

        string legacyDir = Path.Combine(_tempDir, "legacy2");
        Directory.CreateDirectory(legacyDir);
        File.WriteAllText(
            Path.Combine(legacyDir, "settings.json"),
            """{ "enabled": false, "startWithWindows": true }""");

        manager.MigrateLegacySettings(legacyDir);

        AppSettings current = manager.Load();
        Assert.True(current.Enabled);
        Assert.False(current.StartWithWindows);
    }

    [Fact]
    public void MigrateLegacySettings_NoLegacyFile_IsSafeNoOp()
    {
        var manager = new SettingsManager(_tempDir);

        manager.MigrateLegacySettings(Path.Combine(_tempDir, "does-not-exist"));

        Assert.False(File.Exists(manager.FilePath));
    }

    [Fact]
    public void MigrateLegacySettings_CorruptedLegacyFile_FallsBackToDefaults()
    {
        string legacyDir = Path.Combine(_tempDir, "legacy-broken");
        Directory.CreateDirectory(legacyDir);
        File.WriteAllText(Path.Combine(legacyDir, "settings.json"), "not json at all");

        var manager = new SettingsManager(_tempDir);
        manager.MigrateLegacySettings(legacyDir);

        AppSettings migrated = manager.Load();
        Assert.True(migrated.Enabled);
        Assert.False(migrated.StartWithWindows);
    }
}
