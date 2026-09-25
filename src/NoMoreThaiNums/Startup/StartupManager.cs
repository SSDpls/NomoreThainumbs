using Microsoft.Win32;
using NoMoreThaiNums.Logging;

namespace NoMoreThaiNums.Startup;

/// <summary>
/// Manages per-user auto-start via HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
///
/// Chosen mechanism: the HKCU Run key is the standard, documented way to start
/// a tray utility at sign-in. It requires no Administrator privileges, needs no
/// scheduled-task plumbing, and is trivially reversible (delete one value).
/// The entry references the exact running executable path, quoted, since the
/// install path may contain spaces.
/// </summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NoMoreThaiNums";

    /// <summary>Run-key value used before the app was renamed (v0.1.0).</summary>
    private const string LegacyValueName = "ThaiNumberFix";

    /// <summary>Registers the current executable to run at user sign-in.</summary>
    public static bool Enable()
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                ErrorLog.Write("Startup enable failed: ProcessPath was empty.");
                return false;
            }

            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("HKCU Run key could not be opened for writing.");
            key.SetValue(ValueName, $"\"{exePath}\"", RegistryValueKind.String);
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Failed to enable start-with-Windows.", ex);
            return false;
        }
    }

    /// <summary>Removes the auto-start registry value if present.</summary>
    public static bool Disable()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is not null)
            {
                if (key.GetValue(ValueName) is not null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }

                // Clean up the entry created by the pre-rename version.
                if (key.GetValue(LegacyValueName) is not null)
                {
                    key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Failed to disable start-with-Windows.", ex);
            return false;
        }
    }

    /// <summary>True when the auto-start registry value currently exists.</summary>
    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Failed to read start-with-Windows state.", ex);
            return false;
        }
    }
}
