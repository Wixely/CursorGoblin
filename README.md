# CursorGoblin

CursorGoblin is a small, Windows-only cursor overlay for streaming and screen-capture workflows. It draws a software-rendered, click-through copy over the active Windows cursor, including pointer, hand, text, wait, and resize variants.

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

Use the VS Code `Build` task or `Launch CursorGoblin` debug configuration for interactive development.

For a non-interactive launch/cache smoke test, run `CursorGoblin.exe --smoke-test`; it exits normally after one second.

## Publish

The project is configured for a self-contained, single-file Windows x64 executable:

```powershell
dotnet publish .\CursorGoblin.csproj -c Release
```

The result is written below `bin\Release\net10.0-windows\win-x64\publish`.

NativeAOT is intentionally disabled because .NET 10 does not support trimming WinForms applications (`NETSDK1175`). The published executable therefore embeds the normal .NET runtime.

## Safety note

Cursor hiding is restored when the application exits normally. During development, stop the application normally rather than terminating it abruptly. If an interrupted debug session leaves the Windows cursor hidden, signing out or restarting Windows restores the display state.

This is a private, closed-source project. No redistribution licence has been selected.
