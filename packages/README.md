# CupriFace release packages

These packages are unmodified assets from the public
[CupriFace v0.23.0 GitHub release](https://github.com/Wixely/CupriFace/releases/tag/v0.23.0).
They are kept locally because GitHub Packages requires authentication even for public
packages. `NuGet.config` exposes this directory as a repository-local package source.

| File | SHA-256 |
|---|---|
| `CupriFace.0.23.0.nupkg` | `b8f3231ea77039ef242fcba8d3f79d618fd5f39cd875b035df490a7f357fa1ff` |
| `CupriFace.Shell.0.23.0.nupkg` | `4e6257f901ea2d95c5e62f80f484c2f6f78c0c345d56222775f943f7dd650eec` |

To update, download the new `CupriFace` and `CupriFace.Shell` `.nupkg` release assets,
verify their hashes against GitHub's release metadata, update the `PackageReference`,
and regenerate `packages.lock.json` with `dotnet restore --force-evaluate`.
