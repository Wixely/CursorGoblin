using System.Diagnostics;
using System.Globalization;
using CupriFace;
using CupriFace.Binding;
using SkiaSharp;

namespace CursorGoblin;

internal sealed class ConfigurationApp : CupriApp
{
    internal const string WindowTitle = "CursorGoblin settings";
    private readonly CursorStore cursorStore;
    private readonly ConfigurationModel model;

    internal ConfigurationApp(AppSettings settings, NativeCursorOverlay overlay, CursorStore cursorStore,
        bool persistSettings = true)
    {
        this.cursorStore = cursorStore;
        model = new ConfigurationModel(settings, overlay, cursorStore.Count, cursorStore.CacheDirectory,
            persistSettings);
    }

    public override string Title => WindowTitle;
    public override int Width => 540;
    public override int Height => 590;
    public override bool Transparent => true;
    public override bool Frameless => true;
    public override bool TopMost => true;
    public override bool CloseToTray => true;
    public override string TrayCloseLabel => "Exit CursorGoblin";
    public override SKColor Background => SKColors.Transparent;
    public override object Model => model;
    public override byte[]? Icon => EmbeddedAsset("Assets/CursorGoblin.png").ReadBytes();

    public override string Html => """
        <body>
          <section class="card">
            <header>
              <div class="drag" data-window-drag>
                <cupri-image src="Assets/CursorGoblin.png" aria-label="CursorGoblin icon"></cupri-image>
                <div>
                  <div class="title">CursorGoblin</div>
                  <div class="subtitle">Streaming cursor overlay</div>
                </div>
              </div>
              <cupri-button class="close-window" variant="ghost" aria-label="Hide settings">×</cupri-button>
            </header>

            <main>
              <p class="intro">Keep a software-rendered cursor visible to display capture software.</p>

              <div class="setting">
                <div><strong>Cursor overlay</strong><small>Mirror the active Windows pointer.</small></div>
                <cupri-switch checked="{{OverlayEnabled}}" aria-label="Enable cursor overlay"></cupri-switch>
              </div>
              <div class="setting">
                <div><strong>Hide original</strong><small>Show only CursorGoblin's software cursor.</small></div>
                <cupri-switch checked="{{HideOriginal}}" aria-label="Hide original Windows cursor"></cupri-switch>
              </div>
              <div class="setting">
                <div><strong>Recolour overlay</strong><small>Replace the cursor artwork with one colour.</small></div>
                <cupri-switch checked="{{RecolourEnabled}}" aria-label="Recolour cursor overlay"></cupri-switch>
              </div>

              <div class="field-row">
                <label><strong>Overlay colour</strong><small>Used when recolouring is enabled.</small></label>
                <cupri-color value="{{OverlayColour}}" aria-label="Overlay colour"></cupri-color>
              </div>
              <div class="field-row">
                <label><strong>Overlay size</strong><small>100% to 500% of the system cursor.</small></label>
                <cupri-number value="{{ScalePercent}}" min="100" max="500" step="25" aria-label="Overlay size percent"></cupri-number>
              </div>

              <div class="actions">
                <cupri-button class="refresh" variant="primary">Refresh cursor images</cupri-button>
                <cupri-button class="open-folder" variant="ghost">Open image folder</cupri-button>
              </div>
              <div class="status">{{CacheStatus}}</div>
            </main>
          </section>
        </body>
        """;

    public override string Css => """
        body {
          font-family:sans-serif;
          color:#f3f6ed;
          --cupri-text:#f3f6ed;
          --cupri-muted:#8f9b88;
          --cupri-surface:#222820;
          --cupri-border:#3c4738;
          --cupri-hover:#30392d;
          --cupri-accent:#9ef918;
        }
        .card {
          margin:14px;
          overflow:hidden;
          border-radius:18px;
          background:#151a14f5;
          border:1px #9ef91870;
        }
        header {
          display:flex;
          align-items:center;
          justify-content:space-between;
          background:#ffffff0a;
          border-bottom:1px #ffffff12;
        }
        .drag { display:flex; align-items:center; gap:11px; padding:12px 16px; flex-grow:1; }
        cupri-image { width:38px; height:38px; }
        .title { color:#ffffff; font-size:17px; font-weight:bold; }
        .subtitle { color:#94a18e; font-size:11px; margin-top:2px; }
        .close-window { margin-right:10px; min-width:34px; padding:5px 9px; font-size:18px; }
        main { padding:17px 20px 20px; }
        .intro { color:#aab5a4; font-size:13px; margin:0px 0px 15px; }
        .setting, .field-row {
          display:flex;
          align-items:center;
          justify-content:space-between;
          gap:18px;
          padding:12px 0px;
          border-bottom:1px #ffffff0d;
        }
        strong { display:block; color:#f4f8ef; font-size:13px; }
        small { display:block; color:#84907e; font-size:11px; margin-top:3px; }
        .field-row label { width:250px; }
        cupri-number { width:112px; }
        cupri-color { width:180px; }
        .actions { display:flex; gap:9px; margin-top:17px; }
        .refresh { flex-grow:1; }
        .status { color:#7f8a79; font-size:10px; margin-top:13px; }
        """;

    public override void Configure(CupriDocument document)
    {
        document.OnClick(".refresh", _ =>
        {
            try
            {
                cursorStore.Refresh();
                model.SetCacheStatus($"Refreshed {cursorStore.Count} cursor variants at {DateTime.Now:T}.");
            }
            catch (Exception exception)
            {
                model.SetCacheStatus("Refresh failed: " + exception.Message);
            }
            document.Refresh();
        });
        document.OnClick(".open-folder", _ => Process.Start(new ProcessStartInfo
        {
            FileName = cursorStore.CacheDirectory,
            UseShellExecute = true
        }));
        document.OnClick(".close-window", _ =>
        {
            var window = NativeMethods.FindWindow(null, WindowTitle);
            if (window != IntPtr.Zero)
                NativeMethods.PostMessage(window, NativeMethods.WmClose, UIntPtr.Zero, IntPtr.Zero);
        });
    }
}

[CupriBindable]
internal sealed partial class ConfigurationModel
{
    private readonly AppSettings settings;
    private readonly NativeCursorOverlay overlay;
    private string cacheStatus;

    internal ConfigurationModel(AppSettings settings, NativeCursorOverlay overlay,
        int cursorCount, string cacheDirectory, bool persistSettings)
    {
        this.settings = settings;
        this.overlay = overlay;
        this.persistSettings = persistSettings;
        cacheStatus = $"{cursorCount} cursor variants cached in {cacheDirectory}";
    }

    private readonly bool persistSettings;

    public bool OverlayEnabled
    {
        get => settings.OverlayEnabled;
        set { settings.OverlayEnabled = value; Changed(); }
    }

    public bool HideOriginal
    {
        get => settings.HideOriginal;
        set { settings.HideOriginal = value; Changed(); }
    }

    public bool RecolourEnabled
    {
        get => settings.RecolourEnabled;
        set { settings.RecolourEnabled = value; Changed(); }
    }

    public string OverlayColour
    {
        get => $"#{settings.OverlayColourArgb & 0xFFFFFF:X6}";
        set
        {
            var hex = value.Trim().TrimStart('#');
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                settings.OverlayColourArgb = unchecked((int)(0xFF000000 | rgb));
                Changed();
            }
        }
    }

    public int ScalePercent
    {
        get => settings.ScalePercent;
        set { settings.ScalePercent = Math.Clamp(value, 100, 500); Changed(); }
    }

    public string CacheStatus => cacheStatus;

    internal void SetCacheStatus(string value) => cacheStatus = value;

    private void Changed()
    {
        overlay.Apply(settings);
        if (persistSettings)
            settings.Save();
    }
}
