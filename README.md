# CursorGoblin

CursorGoblin is a small, Windows-only cursor overlay for streaming and screen-capture workflows. It draws a software-rendered, always-on-top, click-through copy over the active Windows cursor, including pointer, hand, text, wait, and resize variants. Its floating settings window is built with CupriFace.

The original project artwork is stored in `Assets\CursorGoblin.png`; `tools\Create-Icon.ps1` creates the multi-resolution Windows icon used by the executable and configuration form. The artwork was generated specifically for CursorGoblin and does not use third-party icon assets.

## Features

- Mirrors the cursor currently selected by Windows.
- Keeps the overlay topmost, non-activating, and click-through, and actively reasserts its topmost position while running.
- Optionally hides the original Windows cursor.
- Recolours the overlay to a solid chosen colour while retaining transparency.
- Scales the overlay from 100% to 500%.
- Exports the active standard Windows cursor variants as PNG files and refreshes them on demand.
- Saves settings under `%LOCALAPPDATA%\CursorGoblin`.

The same in-memory images used by the overlay are exported to the PNG cache. The live cursor handle is also accepted as a fallback, so application-specific cursors that are not one of the standard cached variants still appear.

## Build and run

Requires the .NET 10 SDK on Windows.

```powershell
dotnet build .\CursorGoblin.csproj
dotnet run --project .\CursorGoblin.csproj
```

CursorGoblin uses the released `CupriFace.Shell` 0.23.0 NuGet package. The package and its
transitive `CupriFace` dependency are vendored from the corresponding GitHub release so
private GitLab and local builds do not need GitHub Packages credentials. See
[`packages/README.md`](packages/README.md) for provenance and update instructions.
CursorGoblin declares Per-Monitor-V2 awareness and uses CupriFace's device-scale tracking
for crisp logical sizing across monitors.

The settings window uses CupriFace's off-screen GPU rendering with native Windows per-pixel alpha, released after [CupriFace PR #140](https://github.com/Wixely/CupriFace/pull/140). Skia draws on the GPU, then reads changed frames back for Windows alpha presentation. This avoids black transparent margins reproduced with the normal OpenGL swap chain on the tested Windows 11 machine. It is GPU-accelerated drawing with a CPU presentation copy, not a zero-copy DirectComposition backend. Idle windows do not repeatedly render or read back. The separate always-on-top cursor overlay is unchanged.

For troubleshooting, set `CUPRIFACE_SOFTWARE=1` before launching to use the fully software-rendered alpha path. The startup diagnostic identifies which rendering path is active.

Use the VS Code `Build` task or `Launch CursorGoblin` debug configuration for interactive development.

For a non-interactive launch/cache/UI smoke test, run `CursorGoblin.exe --smoke-test`. Add `--snapshot <path.png>` to save the headless CupriFace settings render.

## Publish

The project is configured for a self-contained, single-file Windows x64 executable:

```powershell
dotnet publish .\CursorGoblin.csproj -c Release
```

The result is written below `bin\Release\net10.0-windows\win-x64\publish`.

NativeAOT and trimming remain disabled; changing the settings presentation path does not establish AOT/trimming compatibility for the remaining dependencies. The executable embeds the .NET runtime.

## Safety note

Cursor hiding is restored when the application exits normally. During development, stop the application normally rather than terminating it abruptly. If an interrupted debug session leaves the Windows cursor hidden, signing out or restarting Windows restores the display state.

This is a private, closed-source project. No redistribution licence has been selected.
