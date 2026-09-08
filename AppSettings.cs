namespace CursorGoblin;

internal sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CursorGoblin", "settings.txt");

    internal bool OverlayEnabled { get; set; } = true;
    internal bool HideOriginal { get; set; }
    internal bool RecolourEnabled { get; set; }
    internal int OverlayColourArgb { get; set; } = unchecked((int)0xFFFF0000);
    internal int ScalePercent { get; set; } = 200;

    internal static AppSettings Load()
    {
        var settings = new AppSettings();
        if (!File.Exists(FilePath))
            return settings;

        foreach (var line in File.ReadLines(FilePath))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            switch (parts[0])
            {
                case "OverlayEnabled" when bool.TryParse(parts[1], out var enabled):
                    settings.OverlayEnabled = enabled;
                    break;
                case "HideOriginal" when bool.TryParse(parts[1], out var hidden):
                    settings.HideOriginal = hidden;
                    break;
                case "RecolourEnabled" when bool.TryParse(parts[1], out var recolour):
                    settings.RecolourEnabled = recolour;
                    break;
                case "OverlayColour" when int.TryParse(parts[1], out var argb):
                    settings.OverlayColourArgb = argb;
                    break;
                case "ScalePercent" when int.TryParse(parts[1], out var scale):
                    settings.ScalePercent = Math.Clamp(scale, 100, 500);
                    break;
            }
        }
        return settings;
    }

    internal void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllLines(FilePath,
        [
            $"OverlayEnabled={OverlayEnabled}",
            $"HideOriginal={HideOriginal}",
            $"RecolourEnabled={RecolourEnabled}",
            $"OverlayColour={OverlayColourArgb}",
            $"ScalePercent={ScalePercent}"
        ]);
    }
}
