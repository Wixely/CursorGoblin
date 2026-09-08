using System.Drawing.Imaging;

namespace CursorGoblin;

internal sealed class CursorStore : IDisposable
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

    private readonly Dictionary<IntPtr, CursorImage> images = [];

    internal CursorStore()
    {
        CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CursorGoblin", "Cursors");
    }

    internal string CacheDirectory { get; }
    internal int Count => images.Count;

    internal void Refresh()
    {
        DisposeImages();
        Directory.CreateDirectory(CacheDirectory);

        foreach (var (name, id) in CursorKinds)
        {
            var handle = NativeMethods.LoadCursor(IntPtr.Zero, id);
            if (handle == IntPtr.Zero)
                continue;

            var image = CursorImage.Capture(handle);
            image.Bitmap.Save(Path.Combine(CacheDirectory, name + ".png"), ImageFormat.Png);
            if (!images.TryAdd(handle, image))
                image.Dispose();
        }
    }

    internal CursorImage? Get(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            return null;
        if (images.TryGetValue(handle, out var image))
            return image;

        image = CursorImage.Capture(handle);
        images.Add(handle, image);
        return image;
    }

    private void DisposeImages()
    {
        foreach (var image in images.Values)
            image.Dispose();
        images.Clear();
    }

    public void Dispose() => DisposeImages();
}
