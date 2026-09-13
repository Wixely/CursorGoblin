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
        model = new ConfigurationModel(settings, overlay, cursorStore.Count, persistSettings);
    }

    public override string Title => WindowTitle;
    public override int Width => 540;
    public override int Height => 590;
    public override bool Transparent => true;
    public override bool Frameless => true;
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
              <div class="header-actions">
                <cupri-button class="close-window" variant="ghost" aria-label="Hide settings">&#215;</cupri-button>
              </div>
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
                <cupri-color value="{{OverlayColour}}" open="{{OverlayColourOpen}}" aria-label="Overlay colour"></cupri-color>
              </div>
              <div class="field-row">
                <label><strong>Overlay size</strong><small>100% to 500% of the system cursor.</small></label>
                <cupri-number value="{{ScalePercent}}" min="100" max="500" step="25" aria-label="Overlay size percent"></cupri-number>
              </div>

              <div class="actions">
                <cupri-button class="refresh" variant="primary">Refresh cursor images</cupri-button>
                <cupri-button class="open-folder" variant="ghost">Open image folder</cupri-button>
              </div>
              <div class="footer">
                <div class="status">{{CacheStatus}}</div>
                <cupri-button class="about" variant="ghost">About</cupri-button>
              </div>
            </main>
          </section>

          <cupri-dialog class="about-dialog" open="{{AboutOpen}}" blur="true">
            <div class="about-brand">
              <cupri-image src="Assets/CursorGoblin.png" aria-label="CursorGoblin icon"></cupri-image>
              <div>
                <div class="about-title">CursorGoblin</div>
                <div class="about-subtitle">Streaming cursor overlay for Windows</div>
              </div>
            </div>
            <p>CursorGoblin keeps a software-rendered pointer visible in streams and display captures.</p>
            <p class="about-built-with">The settings interface is powered by CupriFace.</p>
            <a class="github-link" href="https://github.com/Wixely/CursorGoblin">View CursorGoblin on GitHub &#8599;</a>
            <div class="about-buttons">
              <cupri-button class="about-close" variant="ghost">Close</cupri-button>
            </div>
          </cupri-dialog>
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
        .header-actions { display:flex; align-items:center; margin-right:10px; }
        .close-window {
          display:flex;
          align-items:center;
          justify-content:center;
          width:38px;
          height:38px;
          min-width:38px;
          padding:0px;
          font-size:20px;
          line-height:20px;
        }
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
        .refresh { flex-grow:1; color:#11170e; }
        .footer { display:flex; align-items:center; justify-content:space-between; gap:12px; margin-top:11px; }
        .status { color:#7f8a79; font-size:10px; }
        .about { padding:5px 11px; font-size:11px; }
        .about-dialog .cupri-dialog-panel {
          width:340px;
          background:#1b2119;
          color:#f3f6ed;
          border:1px #9ef91870;
        }
        .about-brand { display:flex; align-items:center; gap:12px; margin-bottom:17px; }
        .about-brand cupri-image { width:44px; height:44px; }
        .about-title { color:#ffffff; font-size:20px; font-weight:bold; }
        .about-subtitle { color:#94a18e; font-size:11px; margin-top:3px; }
        .about-dialog p { color:#aab5a4; font-size:13px; line-height:18px; margin:0px 0px 10px; }
        .about-built-with { color:#84907e; }
        .github-link { display:block; margin-top:17px; color:#9ef918; font-size:13px; font-weight:bold; }
        .about-buttons { display:flex; justify-content:flex-end; margin-top:22px; }
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
        document.OnClick(".about", _ =>
        {
            model.AboutOpen = true;
            document.Refresh();
        });
        document.OnClick(".about-close", _ =>
        {
            model.AboutOpen = false;
            document.Refresh();
        });
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
        int cursorCount, bool persistSettings)
    {
        this.settings = settings;
        this.overlay = overlay;
        this.persistSettings = persistSettings;
        cacheStatus = $"{cursorCount} cursor variants cached.";
    }

    private readonly bool persistSettings;

    public bool OverlayColourOpen { get; set; }

    public bool AboutOpen { get; set; }

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
