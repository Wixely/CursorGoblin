using System.Runtime.InteropServices;

namespace CursorGoblin;

internal sealed record CursorImage(byte[] Pixels, int Width, int Height, int HotspotX, int HotspotY)
{
    internal static CursorImage Capture(IntPtr cursor)
    {
        var width = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCxCursor));
        var height = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCyCursor));
        var hotspotX = 0;
        var hotspotY = 0;

        if (NativeMethods.GetIconInfo(cursor, out var iconInfo))
        {
            hotspotX = iconInfo.HotspotX;
            hotspotY = iconInfo.HotspotY;
            if (TryGetBitmapSize(iconInfo.ColorBitmap, false, out var bitmapWidth, out var bitmapHeight) ||
                TryGetBitmapSize(iconInfo.MaskBitmap, true, out bitmapWidth, out bitmapHeight))
            {
                width = bitmapWidth;
                height = bitmapHeight;
            }
            if (iconInfo.MaskBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(iconInfo.MaskBitmap);
            if (iconInfo.ColorBitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(iconInfo.ColorBitmap);
        }

        var bitmapInfo = CreateBitmapInfo(width, height);
        var screen = NativeMethods.GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
            throw new InvalidOperationException("Windows could not access the screen device context.");
        var memory = NativeMethods.CreateCompatibleDC(screen);
        if (memory == IntPtr.Zero)
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            throw new InvalidOperationException("Windows could not create a cursor device context.");
        }
        var bitmap = NativeMethods.CreateDIBSection(screen, ref bitmapInfo,
            NativeMethods.DibRgbColors, out var bits, IntPtr.Zero, 0);
        if (bitmap == IntPtr.Zero || bits == IntPtr.Zero)
        {
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            throw new InvalidOperationException("Windows could not allocate a cursor bitmap.");
        }

        var oldBitmap = NativeMethods.SelectObject(memory, bitmap);
        try
        {
            var clear = new byte[width * height * 4];
            Marshal.Copy(clear, 0, bits, clear.Length);
            if (!NativeMethods.DrawIconEx(memory, 0, 0, cursor, width, height, 0,
                    IntPtr.Zero, NativeMethods.DiNormal))
                throw new InvalidOperationException("Windows could not draw the current cursor.");

            var pixels = new byte[clear.Length];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            return new CursorImage(pixels, width, height, hotspotX, hotspotY);
        }
        finally
        {
            NativeMethods.SelectObject(memory, oldBitmap);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
    }

    private static bool TryGetBitmapSize(IntPtr bitmap, bool monochromeMask,
        out int width, out int height)
    {
        width = 0;
        height = 0;
        if (bitmap == IntPtr.Zero || NativeMethods.GetBitmapObject(bitmap,
                Marshal.SizeOf<NativeMethods.NativeBitmap>(), out var details) == 0)
            return false;

        width = Math.Abs(details.Width);
        height = Math.Abs(details.Height);
        if (monochromeMask)
            height /= 2;
        return width > 0 && height > 0;
    }

    internal static NativeMethods.BitmapInfo CreateBitmapInfo(int width, int height) => new()
    {
        Header = new NativeMethods.BitmapInfoHeader
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(),
            Width = width,
            Height = -height,
            Planes = 1,
            BitCount = 32,
            Compression = NativeMethods.BiRgb,
            SizeImage = (uint)(width * height * 4)
        }
    };
}
