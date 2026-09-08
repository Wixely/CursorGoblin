using System.Runtime.InteropServices;
using SkiaSharp;

namespace CursorGoblin;

internal sealed class CursorStore
{
    private static readonly (string Name, IntPtr Id)[] CursorKinds =
    [
        ("arrow", NativeMethods.IdcArrow),
        ("ibeam", NativeMethods.IdcIBeam),
        ("wait", NativeMethods.IdcWait),
        ("cross", NativeMethods.IdcCross),
        ("uparrow", NativeMethods.IdcUpArrow),
        ("sizenwse", NativeMethods.IdcSizeNwSe),
        ("sizenesw", NativeMethods.IdcSizeNeSw),
        ("sizewe", NativeMethods.IdcSizeWe),
        ("sizens", NativeMethods.IdcSizeNs),
        ("sizeall", NativeMethods.IdcSizeAll),
        ("no", NativeMethods.IdcNo),
        ("hand", NativeMethods.IdcHand),
        ("appstarting", NativeMethods.IdcAppStarting),
        ("help", NativeMethods.IdcHelp),
        ("pin", NativeMethods.IdcPin),
        ("person", NativeMethods.IdcPerson)
    ];

    private readonly object sync = new();
    private Dictionary<IntPtr, CursorImage> images = [];

    internal CursorStore()
    {
        CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CursorGoblin", "Cursors");
    }

    internal string CacheDirectory { get; }
    internal int Count { get { lock (sync) return images.Count; } }

    internal void Refresh()
    {
        Directory.CreateDirectory(CacheDirectory);
        var refreshed = new Dictionary<IntPtr, CursorImage>();

        foreach (var (name, id) in CursorKinds)
        {
            var handle = NativeMethods.LoadCursor(IntPtr.Zero, id);
            if (handle == IntPtr.Zero)
                continue;

            var image = CursorImage.Capture(handle);
            SavePng(image, Path.Combine(CacheDirectory, name + ".png"));
            refreshed.TryAdd(handle, image);
        }

        lock (sync)
            images = refreshed;
    }

    internal CursorImage? Get(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;

        lock (sync)
        {
            if (images.TryGetValue(handle, out var image))
                return image;

            image = CursorImage.Capture(handle);
            images.Add(handle, image);
            return image;
        }
    }

    private static void SavePng(CursorImage image, string path)
    {
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        Marshal.Copy(image.Pixels, 0, bitmap.GetPixels(), image.Pixels.Length);
        using var skImage = SKImage.FromBitmap(bitmap);
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
