using System.Runtime.InteropServices;

namespace CursorGoblin;

internal static class NativeMethods
{
    internal const int SmCxCursor = 13;
    internal const int SmCyCursor = 14;
    internal const int DiNormal = 0x0003;

    internal static readonly IntPtr IdcArrow = 32512;
    internal static readonly IntPtr IdcIBeam = 32513;
    internal static readonly IntPtr IdcWait = 32514;
    internal static readonly IntPtr IdcCross = 32515;
    internal static readonly IntPtr IdcUpArrow = 32516;
    internal static readonly IntPtr IdcSizeNwSe = 32642;
    internal static readonly IntPtr IdcSizeNeSw = 32643;
    internal static readonly IntPtr IdcSizeWe = 32644;
    internal static readonly IntPtr IdcSizeNs = 32645;
    internal static readonly IntPtr IdcSizeAll = 32646;
    internal static readonly IntPtr IdcNo = 32648;
    internal static readonly IntPtr IdcHand = 32649;
    internal static readonly IntPtr IdcAppStarting = 32650;
    internal static readonly IntPtr IdcHelp = 32651;
    internal static readonly IntPtr IdcPin = 32671;
    internal static readonly IntPtr IdcPerson = 32672;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CursorInfo
    {
        internal int Size;
        internal int Flags;
        internal IntPtr Cursor;
        internal Point ScreenPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IconInfo
    {
        [MarshalAs(UnmanagedType.Bool)] internal bool IsIcon;
        internal int HotspotX;
        internal int HotspotY;
        internal IntPtr MaskBitmap;
        internal IntPtr ColorBitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

    [DllImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
    internal static extern IntPtr LoadCursor(IntPtr instance, IntPtr cursorName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetIconInfo(IntPtr icon, out IconInfo iconInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DrawIconEx(IntPtr deviceContext, int x, int y, IntPtr icon,
        int width, int height, int frame, IntPtr flickerFreeBrush, int flags);

    [DllImport("user32.dll")]
    internal static extern int ShowCursor([MarshalAs(UnmanagedType.Bool)] bool show);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(IntPtr graphicObject);
}
