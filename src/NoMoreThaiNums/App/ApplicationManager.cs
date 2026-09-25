using NoMoreThaiNums.Keyboard;
using NoMoreThaiNums.Logging;
using NoMoreThaiNums.Settings;
using NoMoreThaiNums.Startup;
using NoMoreThaiNums.UI;

namespace NoMoreThaiNums.App;

/// <summary>
/// Central coordinator. Owns the keyboard engine, tray icon, settings window,
/// and settings persistence, and keeps every surface (tray menu, tooltip,
/// window controls, registry, disk) in sync with one authoritative state.
/// </summary>
public sealed class ApplicationManager : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SettingsManager _settingsManager;
    private readonly KeyboardProcessor _processor = new();
    private readonly KeyboardHook _hook;
    private readonly TrayManager _tray;
    private readonly MainForm _mainForm;
    private bool _disposed;

    public ApplicationManager(
        AppSettings settings,
        SettingsManager settingsManager,
        TrayManager tray,
        MainForm mainForm)
    {
        _settings = settings;
        _settingsManager = settingsManager;
        _tray = tray;
        _mainForm = mainForm;

        _tray.EnableToggleRequested += OnEnableToggleRequested;
        _tray.StartWithWindowsToggleRequested += OnStartWithWindowsToggleRequested;
        _tray.ShowSettingsRequested += OnShowSettingsRequested;
        _tray.ExitRequested += OnExitRequested;

        _mainForm.ConvertToggled += OnConvertToggled;
        _mainForm.StartWithWindowsToggled += OnStartWithWindowsToggled;

        _hook = new KeyboardHook(_processor);
    }

    /// <summary>Installs the hook. Call once during startup, after construction.</summary>
    public void Start()
    {
        try
        {
            _hook.Start();
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Keyboard hook failed to start; conversion will be unavailable.", ex);
            _settings.Enabled = false;
            MessageBox.Show(
                "No more Thai Nums could not install its keyboard hook. " +
                "Conversion is disabled for this session. " +
                "Details were written to the error log.",
                "No more Thai Nums",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        RefreshAllSurfaces();
    }

    private void OnEnableToggleRequested()
    {
        SetEnabled(!_settings.Enabled);
    }

    private void OnConvertToggled(bool enabled)
    {
        SetEnabled(enabled);
    }

    private void SetEnabled(bool enabled)
    {
        _settings.Enabled = enabled;
        _processor.Enabled = enabled;
        _settingsManager.Save(_settings);
        RefreshAllSurfaces();
    }

    private void OnStartWithWindowsToggleRequested()
    {
        SetStartWithWindows(!_settings.StartWithWindows);
    }

    private void OnStartWithWindowsToggled(bool enabled)
    {
        SetStartWithWindows(enabled);
    }

    private void SetStartWithWindows(bool enabled)
    {
        bool ok = enabled ? StartupManager.Enable() : StartupManager.Disable();
        _settings.StartWithWindows = enabled && ok;
        if (!ok)
        {
            _settings.StartWithWindows = StartupManager.IsEnabled();
        }

        _settingsManager.Save(_settings);
        RefreshAllSurfaces();
    }

    private void OnShowSettingsRequested()
    {
        ShowSettingsOnUiThread();
    }

    /// <summary>Shows the settings window, marshaling to the UI thread if needed.</summary>
    public void ShowSettingsOnUiThread()
    {
        if (_mainForm.InvokeRequired)
        {
            _mainForm.BeginInvoke(ShowSettingsOnUiThread);
            return;
        }

        RefreshAllSurfaces();
        _mainForm.ShowSettings();
    }

    private void OnExitRequested()
    {
        Application.Exit();
    }

    /// <summary>
    /// Pushes the authoritative state to the processor, tray menu, tooltip, and
    /// settings window so every surface always shows the real state.
    /// </summary>
    private void RefreshAllSurfaces()
    {
        _processor.Enabled = _settings.Enabled;
        _tray.UpdateStatus(_settings.Enabled, _settings.StartWithWindows);
        _mainForm.RefreshFromSettings(_settings);
        _mainForm.UpdateStatus(_settings.Enabled);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _tray.EnableToggleRequested -= OnEnableToggleRequested;
        _tray.StartWithWindowsToggleRequested -= OnStartWithWindowsToggleRequested;
        _tray.ShowSettingsRequested -= OnShowSettingsRequested;
        _tray.ExitRequested -= OnExitRequested;

        _hook.Dispose();
        _tray.Dispose();
    }
}
