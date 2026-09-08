using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CursorGoblin;

internal sealed class CursorOverlayForm : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private readonly CursorStore cursorStore;
    private readonly AppSettings settings;
    private readonly System.Windows.Forms.Timer trackingTimer;
    private CursorImage? currentImage;
    private IntPtr currentHandle;
    private int hideCalls;

    internal CursorOverlayForm(CursorStore cursorStore, AppSettings settings)
    {
        this.cursorStore = cursorStore;
        this.settings = settings;

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Fuchsia;
        DoubleBuffered = true;
        TransparencyKey = Color.Fuchsia;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Size = new Size(1, 1);

        trackingTimer = new System.Windows.Forms.Timer { Interval = 16 };
        trackingTimer.Tick += TrackCursor;
        trackingTimer.Start();
        ApplySettings();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    protected override bool ShowWithoutActivation => true;

    internal void ApplySettings()
    {
        ApplyOriginalCursorVisibility(settings.OverlayEnabled && settings.HideOriginal);
        if (!settings.OverlayEnabled)
            Hide();
        Invalidate();
    }

    internal void ResetCursorCache()
    {
        currentImage = null;
        currentHandle = IntPtr.Zero;
    }

    private void TrackCursor(object? sender, EventArgs eventArgs)
    {
        if (!settings.OverlayEnabled)
            return;

        var info = new NativeMethods.CursorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.CursorInfo>() };
        if (!NativeMethods.GetCursorInfo(ref info) || info.Cursor == IntPtr.Zero)
        {
            Hide();
            return;
        }

        if (info.Cursor != currentHandle || currentImage is null)
        {
            currentHandle = info.Cursor;
            currentImage = cursorStore.Get(info.Cursor);
        }

        if (currentImage is null)
            return;

        var scale = (float)settings.ScalePercent / 100f;
        var width = Math.Max(1, (int)Math.Ceiling(currentImage.Bitmap.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(currentImage.Bitmap.Height * scale));
        var x = info.ScreenPosition.X - (int)Math.Round(currentImage.Hotspot.X * scale);
        var y = info.ScreenPosition.Y - (int)Math.Round(currentImage.Hotspot.Y * scale);

        if (Size.Width != width || Size.Height != height)
            Size = new Size(width, height);
        if (Location.X != x || Location.Y != y)
            Location = new Point(x, y);
        if (!Visible)
            Show();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (currentImage is null)
            return;

        eventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        eventArgs.Graphics.CompositingMode = CompositingMode.SourceOver;

        var destination = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
        if (!settings.RecolourEnabled)
        {
            eventArgs.Graphics.DrawImage(currentImage.Bitmap, destination);
            return;
        }

        var colour = settings.OverlayColour;
        var matrix = new ColorMatrix
        {
            Matrix00 = 0,
            Matrix11 = 0,
            Matrix22 = 0,
            Matrix40 = colour.R / 255f,
            Matrix41 = colour.G / 255f,
            Matrix42 = colour.B / 255f
        };
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix);
        eventArgs.Graphics.DrawImage(currentImage.Bitmap, destination, 0, 0,
            currentImage.Bitmap.Width, currentImage.Bitmap.Height, GraphicsUnit.Pixel, attributes);
    }

    private void ApplyOriginalCursorVisibility(bool shouldHide)
    {
        if (shouldHide && hideCalls == 0)
        {
            do
            {
                hideCalls++;
            }
            while (NativeMethods.ShowCursor(false) >= 0 && hideCalls < 32);
        }
        else if (!shouldHide)
        {
            RestoreOriginalCursor();
        }
    }

    private void RestoreOriginalCursor()
    {
        while (hideCalls > 0)
        {
            NativeMethods.ShowCursor(true);
            hideCalls--;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            trackingTimer.Stop();
            trackingTimer.Dispose();
        }
        RestoreOriginalCursor();
        base.Dispose(disposing);
    }
}
