using CupriFace.Dom;
using CupriFace.Interaction;
using CupriFace.Shell;
using CursorGoblin;

var isSmokeTest = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);
var mutexName = isSmokeTest
    ? $"Local\\CursorGoblin.SmokeTest.{Environment.ProcessId}"
    : "Local\\CursorGoblin";
using var instance = new Mutex(true, mutexName, out var isFirstInstance);
if (!isFirstInstance)
{
    var existingWindow = NativeMethods.FindWindow(null, ConfigurationApp.WindowTitle);
    if (existingWindow != IntPtr.Zero)
    {
        NativeMethods.ShowWindow(existingWindow, NativeMethods.SwRestore);
        NativeMethods.SetForegroundWindow(existingWindow);
    }
    else
    {
        NativeMethods.MessageBox(IntPtr.Zero, "CursorGoblin is already running.", "CursorGoblin",
            NativeMethods.MbOk | NativeMethods.MbIconInformation);
    }
    return;
}

try
{
    var settings = AppSettings.Load();
    if (isSmokeTest)
    {
        settings.OverlayEnabled = false;
        settings.HideOriginal = false;
        settings.RecolourEnabled = false;
        settings.OverlayColourArgb = unchecked((int)0xFFFF0000);
        settings.ScalePercent = 200;
    }

    var cursorStore = new CursorStore();
    cursorStore.Refresh();
    using var overlay = new NativeCursorOverlay(cursorStore, settings);
    var app = new ConfigurationApp(settings, overlay, cursorStore, !isSmokeTest);

    if (isSmokeTest)
    {
        var document = app.CreateDocument();
        using var image = RenderTransparentSnapshot(document, app.Width, app.Height);
        var switches = FindNodes(document.Root,
            node => node.Element?.ClassList.Contains("cupri-switch") == true);
        if (switches.Count < 3)
            throw new InvalidOperationException("The CupriFace settings switches were not rendered.");

        var recolourSwitch = HitTesting.AbsoluteBox(switches[2]);
        document.DispatchClick(
            recolourSwitch.X + recolourSwitch.W / 2,
            recolourSwitch.Y + recolourSwitch.H / 2);
        if (!settings.RecolourEnabled)
            throw new InvalidOperationException("The CupriFace recolour setting did not update its model.");

        if (app.Model is not ConfigurationModel configuration)
            throw new InvalidOperationException("The CupriFace configuration model was not available.");

        using (document.RenderToImage(app.Width, app.Height))
        {
            var colourTrigger = FindNodes(document.Root,
                node => node.Element?.HasAttribute("data-cupri-toggle") == true).FirstOrDefault();
            if (colourTrigger is null)
                throw new InvalidOperationException("The overlay colour trigger was not rendered.");
            var bounds = HitTesting.AbsoluteBox(colourTrigger);
            document.DispatchClick(bounds.X + bounds.W / 2, bounds.Y + bounds.H / 2);
        }
        if (!configuration.OverlayColourOpen)
            throw new InvalidOperationException("The overlay colour palette did not open.");

        using (document.RenderToImage(app.Width, app.Height))
        {
            var swatch = FindNodes(document.Root,
                node => node.Element?.GetAttribute("data-set-path") == nameof(ConfigurationModel.OverlayColour))
                .FirstOrDefault(node => node.Element?.GetAttribute("data-set-value") != "#FF0000");
            if (swatch is null)
                throw new InvalidOperationException("The overlay colour palette was not clickable.");
            var bounds = HitTesting.AbsoluteBox(swatch);
            document.DispatchClick(bounds.X + bounds.W / 2, bounds.Y + bounds.H / 2);
        }
        if (configuration.OverlayColourOpen || settings.OverlayColourArgb == unchecked((int)0xFFFF0000))
            throw new InvalidOperationException("The overlay colour palette did not select a colour and close.");

        using (document.RenderToImage(app.Width, app.Height))
        {
            var about = FindNodes(document.Root,
                node => node.Element?.ClassList.Contains("about") == true).FirstOrDefault();
            if (about is null)
                throw new InvalidOperationException("The About button was not rendered.");
            var bounds = HitTesting.AbsoluteBox(about);
            document.DispatchClick(bounds.X + bounds.W / 2, bounds.Y + bounds.H / 2);
        }
        if (!configuration.AboutOpen)
            throw new InvalidOperationException("The About dialog did not open.");

        using (document.RenderToImage(app.Width, app.Height))
        {
            var github = FindNodes(document.Root,
                node => node.Element?.GetAttribute("href") == "https://github.com/Wixely/CursorGoblin").FirstOrDefault();
            if (github is null)
                throw new InvalidOperationException("The About dialog GitHub link was not rendered.");
            var closeAbout = FindNodes(document.Root,
                node => node.Element?.ClassList.Contains("about-close") == true).FirstOrDefault();
            if (closeAbout is null)
                throw new InvalidOperationException("The About dialog close button was not rendered.");
            var bounds = HitTesting.AbsoluteBox(closeAbout);
            document.DispatchClick(bounds.X + bounds.W / 2, bounds.Y + bounds.H / 2);
        }
        if (configuration.AboutOpen)
            throw new InvalidOperationException("The About dialog did not close.");

        var snapshotIndex = Array.FindIndex(args, value => value.Equals("--snapshot", StringComparison.OrdinalIgnoreCase));
        if (snapshotIndex >= 0 && snapshotIndex + 1 < args.Length)
        {
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(args[snapshotIndex + 1]);
            data.SaveTo(stream);
        }
        return;
    }

    try
    {
        // Render on the GPU off-screen, then use Windows per-pixel alpha presentation to avoid
        // the black transparent margins of the normal WGL swap chain on this display path.
        DesktopHost.RunWithLayeredGpu(app);
    }
    finally
    {
        settings.Save();
    }
}
catch (Exception exception)
{
    NativeMethods.MessageBox(IntPtr.Zero, exception.Message, "CursorGoblin could not start",
        NativeMethods.MbOk | NativeMethods.MbIconError);
    Environment.ExitCode = 1;
}

static List<RenderNode> FindNodes(RenderNode root, Func<RenderNode, bool> predicate)
{
    var matches = new List<RenderNode>();
    Visit(root);
    return matches;

    void Visit(RenderNode node)
    {
        if (predicate(node))
            matches.Add(node);
        foreach (var child in node.Children)
            Visit(child);
    }
}

static SkiaSharp.SKImage RenderTransparentSnapshot(CupriFace.CupriDocument document, int width, int height)
{
    var pixels = document.RenderToPixels(
        width,
        height,
        SkiaSharp.SKColors.Transparent,
        straightAlpha: true);
    var imageInfo = new SkiaSharp.SKImageInfo(
        width,
        height,
        SkiaSharp.SKColorType.Rgba8888,
        SkiaSharp.SKAlphaType.Unpremul);
    return SkiaSharp.SKImage.FromPixelCopy(imageInfo, pixels, imageInfo.RowBytes);
}
