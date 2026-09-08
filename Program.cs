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
    }

    var cursorStore = new CursorStore();
    cursorStore.Refresh();
    using var overlay = new NativeCursorOverlay(cursorStore, settings);
    var app = new ConfigurationApp(settings, overlay, cursorStore, !isSmokeTest);

    if (isSmokeTest)
    {
        var document = app.CreateDocument();
        using var image = document.RenderToImage(app.Width, app.Height);
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
        DesktopHost.Run(app);
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
