using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CursorGoblin;

internal sealed class NativeCursorOverlay : IDisposable
{
    private readonly CursorStore cursorStore;
    private readonly Thread windowThread;
    private readonly ManualResetEventSlim ready = new(false);
    private readonly object stateLock = new();
    private OverlayState state;
    private NativeMethods.WindowProcedure? windowProcedure;
    private IntPtr window;
    private Exception? startupException;
    private int hideCalls;

    internal NativeCursorOverlay(CursorStore cursorStore, AppSettings settings)
    {
        this.cursorStore = cursorStore;
        state = OverlayState.From(settings);
        windowThread = new Thread(RunWindow)
        {
            IsBackground = true,
            Name = "CursorGoblin overlay"
        };
        windowThread.SetApartmentState(ApartmentState.STA);
        windowThread.Start();
        ready.Wait();
        if (startupException is not null)
            throw new InvalidOperationException("The cursor overlay could not start.", startupException);
    }

    internal void Apply(AppSettings settings)
    {
        lock (stateLock)
            state = OverlayState.From(settings);
    }

    private void RunWindow()
    {
        var instance = NativeMethods.GetModuleHandle(null);
        var className = $"CursorGoblinOverlay.{Environment.ProcessId}";
        windowProcedure = WindowProc;
        var windowClass = new NativeMethods.WindowClass
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.WindowClass>(),
            Instance = instance,
            WindowProcedure = Marshal.GetFunctionPointerForDelegate(windowProcedure),
            ClassName = className,
            Cursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IdcArrow)
        };

        try
        {
            if (NativeMethods.RegisterClassEx(ref windowClass) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var extendedStyle = NativeMethods.WsExLayered | NativeMethods.WsExTransparent |
                NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate | NativeMethods.WsExTopMost;
            window = NativeMethods.CreateWindowEx(extendedStyle, className, "CursorGoblin cursor overlay",
                NativeMethods.WsPopup, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
            if (window == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (NativeMethods.SetTimer(window, (UIntPtr)1, 16, IntPtr.Zero) == UIntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            ready.Set();
            while (NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(ref message);
                NativeMethods.DispatchMessage(ref message);
            }
        }
        catch (Exception exception)
        {
            startupException = exception;
            ready.Set();
        }
        finally
        {
            RestoreOriginalCursor();
            window = IntPtr.Zero;
        }
    }

    private IntPtr WindowProc(IntPtr handle, uint message, UIntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (message)
            {
                case NativeMethods.WmTimer:
                    UpdateOverlay(handle);
                    return IntPtr.Zero;
                case NativeMethods.WmNcHitTest:
                    return new IntPtr(NativeMethods.HtTransparent);
                case NativeMethods.WmClose:
                    NativeMethods.DestroyWindow(handle);
                    return IntPtr.Zero;
                case NativeMethods.WmDestroy:
                    NativeMethods.KillTimer(handle, (UIntPtr)1);
                    RestoreOriginalCursor();
                    NativeMethods.PostQuitMessage(0);
                    return IntPtr.Zero;
            }
        }
        catch
        {
            RestoreOriginalCursor();
            NativeMethods.ShowWindow(handle, NativeMethods.SwHide);
        }

        return NativeMethods.DefWindowProc(handle, message, wParam, lParam);
    }

    private void UpdateOverlay(IntPtr handle)
    {
        OverlayState current;
        lock (stateLock)
            current = state;

        ApplyOriginalCursorVisibility(current.Enabled && current.HideOriginal);
        if (!current.Enabled)
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SwHide);
            return;
        }

        var cursorInfo = new NativeMethods.CursorInfo { Size = Marshal.SizeOf<NativeMethods.CursorInfo>() };
        if (!NativeMethods.GetCursorInfo(ref cursorInfo) || cursorInfo.Cursor == IntPtr.Zero)
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SwHide);
            return;
        }

        var image = cursorStore.Get(cursorInfo.Cursor);
        if (image is null)
            return;

        var scale = current.ScalePercent / 100f;
        var width = Math.Max(1, (int)Math.Ceiling(image.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(image.Height * scale));
        var pixels = ScaleAndRecolour(image, width, height, current);
        var x = cursorInfo.ScreenPosition.X - (int)Math.Round(image.HotspotX * scale);
        var y = cursorInfo.ScreenPosition.Y - (int)Math.Round(image.HotspotY * scale);
        Present(handle, pixels, width, height, x, y);
    }

    private static byte[] ScaleAndRecolour(CursorImage image, int width, int height, OverlayState state)
    {
        var output = new byte[width * height * 4];
        var red = (byte)((state.ColourArgb >> 16) & 0xff);
        var green = (byte)((state.ColourArgb >> 8) & 0xff);
        var blue = (byte)(state.ColourArgb & 0xff);

        for (var y = 0; y < height; y++)
        {
            var sourceY = Math.Min(image.Height - 1, y * image.Height / height);
            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Min(image.Width - 1, x * image.Width / width);
                var sourceIndex = (sourceY * image.Width + sourceX) * 4;
                var targetIndex = (y * width + x) * 4;
                var alpha = image.Pixels[sourceIndex + 3];
                output[targetIndex + 3] = alpha;
                if (state.Recolour)
                {
                    output[targetIndex] = (byte)(blue * alpha / 255);
                    output[targetIndex + 1] = (byte)(green * alpha / 255);
                    output[targetIndex + 2] = (byte)(red * alpha / 255);
                }
                else
                {
                    output[targetIndex] = image.Pixels[sourceIndex];
                    output[targetIndex + 1] = image.Pixels[sourceIndex + 1];
                    output[targetIndex + 2] = image.Pixels[sourceIndex + 2];
                }
            }
        }

        return output;
    }

    private static void Present(IntPtr handle, byte[] pixels, int width, int height, int x, int y)
    {
        var screen = NativeMethods.GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        var memory = NativeMethods.CreateCompatibleDC(screen);
        if (memory == IntPtr.Zero)
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        var bitmapInfo = CursorImage.CreateBitmapInfo(width, height);
        var bitmap = NativeMethods.CreateDIBSection(screen, ref bitmapInfo,
            NativeMethods.DibRgbColors, out var bits, IntPtr.Zero, 0);
        if (bitmap == IntPtr.Zero || bits == IntPtr.Zero)
        {
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var oldBitmap = NativeMethods.SelectObject(memory, bitmap);
        try
        {
            Marshal.Copy(pixels, 0, bits, pixels.Length);
            var destination = new NativeMethods.Point(x, y);
            var size = new NativeMethods.Size(width, height);
            var source = new NativeMethods.Point(0, 0);
            var blend = new NativeMethods.BlendFunction
            {
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AcSrcAlpha
            };
            if (!NativeMethods.UpdateLayeredWindow(handle, screen, ref destination, ref size,
                    memory, ref source, 0, ref blend, NativeMethods.UlwAlpha))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            NativeMethods.SetWindowPos(handle, NativeMethods.HwndTopMost, 0, 0, 0, 0,
                NativeMethods.SwpNoMove | NativeMethods.SwpNoSize |
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }
        finally
        {
            NativeMethods.SelectObject(memory, oldBitmap);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
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

    public void Dispose()
    {
        var handle = window;
        if (handle != IntPtr.Zero)
            NativeMethods.PostMessage(handle, NativeMethods.WmClose, UIntPtr.Zero, IntPtr.Zero);
        if (windowThread.IsAlive)
            windowThread.Join(TimeSpan.FromSeconds(5));
        ready.Dispose();
    }

    private readonly record struct OverlayState(
        bool Enabled, bool HideOriginal, bool Recolour, int ColourArgb, int ScalePercent)
    {
        internal static OverlayState From(AppSettings settings) => new(
            settings.OverlayEnabled,
            settings.HideOriginal,
            settings.RecolourEnabled,
            settings.OverlayColourArgb,
            settings.ScalePercent);
    }
}
