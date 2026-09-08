using CursorGoblin;

Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var isSmokeTest = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);
var mutexName = isSmokeTest
    ? $"Local\\CursorGoblin.SmokeTest.{Environment.ProcessId}"
    : "Local\\CursorGoblin";
using var instance = new Mutex(true, mutexName, out var isFirstInstance);
if (!isFirstInstance)
{
    MessageBox.Show("CursorGoblin is already running.", "CursorGoblin",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

var settings = AppSettings.Load();
if (isSmokeTest)
{
    settings.OverlayEnabled = false;
    settings.HideOriginal = false;
}
using var cursorStore = new CursorStore();
try
{
    cursorStore.Refresh();
}
catch (Exception exception)
{
    MessageBox.Show("Cursor images could not be loaded.\n\n" + exception.Message,
        "CursorGoblin", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

using var overlay = new CursorOverlayForm(cursorStore, settings);
using var configuration = new ConfigurationForm(overlay, cursorStore, settings, !isSmokeTest);
using var smokeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
if (isSmokeTest)
{
    smokeTimer.Tick += (_, _) =>
    {
        smokeTimer.Stop();
        configuration.Close();
    };
    smokeTimer.Start();
}
Application.Run(configuration);
