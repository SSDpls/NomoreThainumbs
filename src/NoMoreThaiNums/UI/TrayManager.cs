namespace NoMoreThaiNums.UI;

/// <summary>
/// Owns the system tray (notification area) icon and its context menu:
/// Enable, Start with Windows, Settings, About, Exit.
/// Events are raised on the UI thread that created this component.
/// </summary>
public sealed class TrayManager : IDisposable
{
    private readonly Icon _icon;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _enableItem;
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private bool _suppressEvents;
    private bool _disposed;

    public event Action? EnableToggleRequested;
    public event Action? StartWithWindowsToggleRequested;
    public event Action? ShowSettingsRequested;
    public event Action? ExitRequested;

    public TrayManager()
    {
        _icon = LoadAppIcon();
        _enableItem = new ToolStripMenuItem("Enable") { CheckOnClick = true };
        _enableItem.CheckedChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                EnableToggleRequested?.Invoke();
            }
        };

        _startWithWindowsItem = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
        _startWithWindowsItem.CheckedChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                StartWithWindowsToggleRequested?.Invoke();
            }
        };

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettingsRequested?.Invoke();

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (_, _) => ShowAbout();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enableItem);
        menu.Items.Add(_startWithWindowsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "No more Thai Nums",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettingsRequested?.Invoke();
    }

    /// <summary>
    /// Reflects the real current state in the menu checks and tooltip.
    /// </summary>
    public void UpdateStatus(bool enabled, bool startWithWindows)
    {
        _suppressEvents = true;
        try
        {
            _enableItem.Checked = enabled;
            _startWithWindowsItem.Checked = startWithWindows;
            _notifyIcon.Text = enabled ? "No more Thai Nums — Active" : "No more Thai Nums — Disabled";
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (exePath is not null)
            {
                using Icon? extracted = Icon.ExtractAssociatedIcon(exePath);
                if (extracted is not null)
                {
                    return (Icon)extracted.Clone();
                }
            }
        }
        catch (Exception)
        {
            // Fall through to the system default icon.
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private static void ShowAbout()
    {
        string version = typeof(TrayManager).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        MessageBox.Show(
            $"No more Thai Nums {version}\n\n" +
            "Converts Thai numerals (๐-๙) typed on a Thai keyboard into Western digits (0-9), system-wide.\n\n" +
            "Keyboard input is processed locally. Nothing is recorded, stored, or sent over the internet.\n\n" +
            "MIT License — Copyright (c) 2026 No more Thai Nums contributors",
            "About No more Thai Nums",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }
}
