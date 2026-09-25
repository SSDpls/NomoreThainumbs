using NoMoreThaiNums.App;
using NoMoreThaiNums.Logging;
using NoMoreThaiNums.Settings;
using NoMoreThaiNums.UI;

namespace NoMoreThaiNums;

internal static class Program
{
    /// <summary>
    /// Entry point: single-instance guard, settings load, tray + settings
    /// window + keyboard hook wiring, and clean shutdown.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstance = new SingleInstanceManager();
        if (!singleInstance.TryAcquire())
        {
            // A second launch must not create a second background instance;
            // nudge the running instance to show its settings window instead.
            SingleInstanceManager.SignalExistingInstanceToShowWindow();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ErrorLog.Write("Unhandled AppDomain exception.", e.ExceptionObject as Exception);
        Application.ThreadException += (_, e) =>
            ErrorLog.Write("Unhandled UI thread exception.", e.Exception);

        var settingsManager = new SettingsManager();
        settingsManager.MigrateLegacySettings();
        AppSettings settings = settingsManager.Load();

        var mainForm = new MainForm(settings, isActive: () => settings.Enabled);
        var tray = new TrayManager();

        using var app = new ApplicationManager(settings, settingsManager, tray, mainForm);

        // Start hidden in the tray; the user opens Settings from the tray icon.
        singleInstance.ListenForShowWindowRequests(app.ShowSettingsOnUiThread);
        app.Start();
        tray.UpdateStatus(settings.Enabled, settings.StartWithWindows);

        Application.Run(mainForm);

        // Application.Run returns only after Application.Exit (tray → Exit).
        // Shutdown order: unhook → dispose tray → save settings.
        app.Dispose();
        tray.Dispose();
        settingsManager.Save(settings);
    }
}
