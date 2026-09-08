using System.Diagnostics;

namespace CursorGoblin;

internal sealed class ConfigurationForm : Form
{
    private readonly CursorOverlayForm overlay;
    private readonly CursorStore cursorStore;
    private readonly AppSettings settings;
    private readonly Button colourButton;
    private readonly Label statusLabel;

    internal ConfigurationForm(CursorOverlayForm overlay, CursorStore cursorStore, AppSettings settings)
    {
        this.overlay = overlay;
        this.cursorStore = cursorStore;
        this.settings = settings;

        Text = "CursorGoblin";
        Icon = SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(450, 365);
        ClientSize = new Size(470, 390);
        AutoScaleMode = AutoScaleMode.Dpi;

        var title = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Streaming cursor overlay"
        };
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = "Adds a click-through software cursor above the Windows pointer so display capture software can see it."
        };
        var enabledCheck = new CheckBox
        {
            AutoSize = true,
            Checked = settings.OverlayEnabled,
            Text = "Enable cursor overlay"
        };
        var hideCheck = new CheckBox
        {
            AutoSize = true,
            Checked = settings.HideOriginal,
            Text = "Hide the original Windows cursor"
        };
        var recolourCheck = new CheckBox
        {
            AutoSize = true,
            Checked = settings.RecolourEnabled,
            Text = "Recolour the overlay"
        };
        colourButton = new Button
        {
            AutoSize = true,
            Text = "Choose colour…",
            BackColor = settings.OverlayColour,
            ForeColor = ContrastingTextColour(settings.OverlayColour),
            UseVisualStyleBackColor = false
        };
        var scaleLabel = new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Text = "Overlay size:"
        };
        var scaleInput = new NumericUpDown
        {
            Minimum = 100,
            Maximum = 500,
            Increment = 25,
            Value = settings.ScalePercent,
            Width = 85
        };
        var percentLabel = new Label { Anchor = AnchorStyles.Left, AutoSize = true, Text = "%" };
        var refreshButton = new Button { AutoSize = true, Text = "Refresh cursor images" };
        var openFolderButton = new Button { AutoSize = true, Text = "Open image folder" };
        statusLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = $"{cursorStore.Count} cursor variants cached in {cursorStore.CacheDirectory}"
        };

        enabledCheck.CheckedChanged += (_, _) =>
        {
            settings.OverlayEnabled = enabledCheck.Checked;
            overlay.ApplySettings();
        };
        hideCheck.CheckedChanged += (_, _) =>
        {
            settings.HideOriginal = hideCheck.Checked;
            overlay.ApplySettings();
        };
        recolourCheck.CheckedChanged += (_, _) =>
        {
            settings.RecolourEnabled = recolourCheck.Checked;
            overlay.ApplySettings();
        };
        colourButton.Click += ChooseColour;
        scaleInput.ValueChanged += (_, _) =>
        {
            settings.ScalePercent = scaleInput.Value;
            overlay.ApplySettings();
        };
        refreshButton.Click += RefreshCursorImages;
        openFolderButton.Click += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = cursorStore.CacheDirectory,
            UseShellExecute = true
        });

        var scalePanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        scalePanel.Controls.AddRange([scaleLabel, scaleInput, percentLabel]);

        var cursorButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        cursorButtons.Controls.AddRange([refreshButton, openFolderButton]);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(18)
        };
        layout.Controls.Add(title);
        layout.Controls.Add(description);
        layout.Controls.Add(enabledCheck);
        layout.Controls.Add(hideCheck);
        layout.Controls.Add(recolourCheck);
        layout.Controls.Add(colourButton);
        layout.Controls.Add(scalePanel);
        layout.Controls.Add(cursorButtons);
        layout.Controls.Add(statusLabel);
        foreach (RowStyle style in layout.RowStyles)
            style.SizeType = SizeType.AutoSize;
        Controls.Add(layout);

        AcceptButton = refreshButton;
        FormClosing += (_, _) => settings.Save();
    }

    private void ChooseColour(object? sender, EventArgs eventArgs)
    {
        using var dialog = new ColorDialog
        {
            Color = settings.OverlayColour,
            FullOpen = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        settings.OverlayColour = dialog.Color;
        colourButton.BackColor = dialog.Color;
        colourButton.ForeColor = ContrastingTextColour(dialog.Color);
        overlay.ApplySettings();
    }

    private void RefreshCursorImages(object? sender, EventArgs eventArgs)
    {
        try
        {
            overlay.ResetCursorCache();
            cursorStore.Refresh();
            statusLabel.Text = $"Refreshed {cursorStore.Count} cursor variants at {DateTime.Now:T}.";
        }
        catch (Exception exception)
        {
            statusLabel.Text = "Refresh failed: " + exception.Message;
        }
    }

    private static Color ContrastingTextColour(Color colour) =>
        (colour.R * 299 + colour.G * 587 + colour.B * 114) / 1000 >= 128
            ? Color.Black
            : Color.White;
}
