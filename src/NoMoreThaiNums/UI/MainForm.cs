using NoMoreThaiNums.Settings;

namespace NoMoreThaiNums.UI;

/// <summary>
/// Small, native-looking settings window.
/// Closing the window (X) hides it to the tray; the application keeps running.
/// The app terminates only via the tray Exit item (Application.Exit), which
/// closes the form with CloseReason.ApplicationExitCall.
/// </summary>
public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly Func<bool> _isActive;
    private bool _suppressEvents;

    private CheckBox _convertCheckBox = null!;
    private CheckBox _startWithWindowsCheckBox = null!;
    private Label _statusLabel = null!;

    /// <summary>Raised when the user toggles the conversion checkbox.</summary>
    public event Action<bool>? ConvertToggled;

    /// <summary>Raised when the user toggles the start-with-Windows checkbox.</summary>
    public event Action<bool>? StartWithWindowsToggled;

    public MainForm(AppSettings settings, Func<bool> isActive)
    {
        _settings = settings;
        _isActive = isActive;

        Text = "No more Thai Nums";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(360, 176);
        Font = new Font("Segoe UI", 9F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14, 12, 14, 12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 5; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var title = new Label
        {
            Text = "No more Thai Nums",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            AutoSize = true,
        };

        _convertCheckBox = new CheckBox
        {
            Text = "Convert Thai numerals (๐-๙) to 0-9",
            AutoSize = true,
            Checked = settings.Enabled,
        };
        _convertCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                ConvertToggled?.Invoke(_convertCheckBox.Checked);
            }
        };

        _startWithWindowsCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            AutoSize = true,
            Checked = settings.StartWithWindows,
        };
        _startWithWindowsCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                StartWithWindowsToggled?.Invoke(_startWithWindowsCheckBox.Checked);
            }
        };

        _statusLabel = new Label
        {
            Text = string.Empty,
            AutoSize = true,
        };

        var hideButton = new Button
        {
            Text = "Hide",
            AutoSize = true,
        };
        hideButton.Click += (_, _) => Hide();

        var buttonRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
        };
        buttonRow.Controls.Add(hideButton);

        layout.Controls.Add(title);
        layout.Controls.Add(_convertCheckBox);
        layout.Controls.Add(_startWithWindowsCheckBox);
        layout.Controls.Add(_statusLabel);
        layout.Controls.Add(buttonRow);
        Controls.Add(layout);

        UpdateStatus(_isActive());
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Hide to the system tray instead of exiting.
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    /// <summary>Re-reads checkbox states from the settings object.</summary>
    public void RefreshFromSettings(AppSettings settings)
    {
        _suppressEvents = true;
        try
        {
            _convertCheckBox.Checked = settings.Enabled;
            _startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    /// <summary>Updates the status line text and color.</summary>
    public void UpdateStatus(bool active)
    {
        _statusLabel.Text = active ? "Status: Active" : "Status: Disabled";
        _statusLabel.ForeColor = active ? Color.ForestGreen : Color.Gray;
    }

    /// <summary>Shows and activates the settings window.</summary>
    public void ShowSettings()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Show();
        Activate();
    }
}
