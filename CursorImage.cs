using System.Drawing.Imaging;

namespace CursorGoblin;

internal sealed class CursorImage : IDisposable
{
    internal CursorImage(Bitmap bitmap, Point hotspot)
    {
        Bitmap = bitmap;
        Hotspot = hotspot;
    }

    internal Bitmap Bitmap { get; }
    internal Point Hotspot { get; }

    internal static CursorImage Capture(IntPtr cursor)
    {
        var width = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCxCursor));
        var height = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCyCursor));
        var hotspot = Point.Empty;

        if (NativeMethods.GetIconInfo(cursor, out var info))
        {
            hotspot = new Point(info.HotspotX, info.HotspotY);
            if (info.MaskBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(info.MaskBitmap);
            if (info.ColorBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(info.ColorBitmap);
        }

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        var deviceContext = graphics.GetHdc();
        try
        {
            NativeMethods.DrawIconEx(deviceContext, 0, 0, cursor, width, height, 0,
                IntPtr.Zero, NativeMethods.DiNormal);
        }
        finally
        {
            graphics.ReleaseHdc(deviceContext);
        }

        return new CursorImage(bitmap, hotspot);
    }

    public void Dispose() => Bitmap.Dispose();
}
