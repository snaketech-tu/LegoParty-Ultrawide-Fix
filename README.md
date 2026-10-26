# LegoParty Ultrawide Fix

A [BepInEx](https://github.com/BepInEx/BepInEx) plugin for **LEGO Party!** (Steam) that removes the game's
forced 16:9 pillarboxing, so the 3D view fills 21:9 and 32:9 screens.

- The game world renders across the full width. Unity cameras use a vertical field of view, so you see
  more at the sides instead of a stretched or zoomed image (Hor+).
- Menus and the HUD stay in the centred 16:9 area, exactly where the game lays them out.
- Picture-in-picture minigame previews keep their correct 16:9 proportions.

Tested on 3440x1440 with game version 1.6.62 (Unity 6000.3.11f1, IL2CPP).

## Install

### Patcher (recommended)
1. Download `LegoPartyUltrawidePatcher-<version>.exe` from [Releases](../../releases).
2. Run it. It finds the game folder through Steam; otherwise use **Browse...**.
3. Press **Patch**, then start the game normally from Steam. The first start takes a few extra minutes
   while BepInEx prepares itself.

The patcher downloads the pinned BepInEx build (see below) from `builds.bepinex.dev`, checks its SHA-256,
and only then installs it together with the plugin. BepInEx's console window and log file are off unless
you tick **Enable BepInEx logs**. No game files are modified.

Offline install: put the BepInEx archive (exact file name below) next to the patcher and it will use that
copy instead of downloading. It still has to match the checksum.

### Manual
1. Install [BepInEx 6.0.0-be.791 IL2CPP x64](https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip)
   into the game folder (the folder that contains `LEGO Party.exe`).
2. Extract `LegoPartyUltrawide-plugin-<version>.zip` into the same folder.

Every release lists SHA-256 checksums for its downloads in `SHA256SUMS.txt`.

## Configuration

`BepInEx\config\snaketech.legoparty.ultrawide.cfg` is created on the first start.

| Setting | Default | Meaning |
| --- | --- | --- |
| `General.Enabled` | `true` | Master switch. |
| `General.MaxAspectRatio` | `0` | Widest aspect ratio to render; `0` = full screen width. Use e.g. `2.37` to cap a 32:9 screen at 21:9. |
| `General.HideLetterboxBorders` | `true` | Hide the game's black border overlay. |
| `Resolution.ForceWidth` / `ForceHeight` / `ForceMode` | `0` / `0` / `FullScreenWindow` | Force a resolution at startup (`0` = use the game's setting). |
| `Debug.VerboseLogging` | `false` | Log camera rects for troubleshooting. |

## Uninstall

- Disable only this fix: set `Enabled = false` in the config above, or delete `BepInEx\plugins\LegoPartyUltrawide`.
- Disable BepInEx: set `enabled = false` in `doorstop_config.ini`.
- Remove everything: delete `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `dotnet\` and `BepInEx\`
  from the game folder.

## How it works

`Koala.Core.Cameras.CameraService.ComputeCameraPixelRect` clamps the camera viewport to exactly 16:9. Its
min/max ratios are constants compiled into native code, so changing the static fields does nothing (and
writing them crashes the game). A Harmony postfix widens the rect it returns instead.
`MinigamePIPGameView` sizes its render texture from that same rect but shows it in a fixed 16:9 frame, so the
plugin hands it the original 16:9 rect while the texture is created.

## Building

Requires the .NET 8 SDK (or newer) on Windows.

```
dotnet build src/Patcher/LegoPartyUltrawidePatcher.csproj -c Release
```

This builds the plugin (`src/LegoPartyUltrawide/bin/Release/net6.0/LegoPartyUltrawide.dll`) and the patcher,
which embeds it (`src/Patcher/bin/Release/net48/LegoPartyUltrawidePatcher.exe`).

### Reference assemblies (`stubs/`)
BepInEx generates "interop" assemblies from the game on first start. They are derived from the game, so they
are not in this repository, and CI cannot build against them. Instead, `stubs/` contains small hand-written
assemblies with the same names that declare only the types and members the plugin uses. They are used for
compiling only; at runtime the plugin binds to the real interop assemblies BepInEx generated.

Trade-off: when the plugin starts using a new game or Unity member, it has to be added to the stubs with the
exact signature. To check the stubs against a real install, or to build against it directly:

```
dotnet run --project tools/StubCheck -- src/LegoPartyUltrawide/bin/Release/net6.0/LegoPartyUltrawide.dll "<game>\BepInEx\interop"
dotnet build src/LegoPartyUltrawide -c Release -p:GameDir="<game>"
```

`StubCheck` verifies that every member the compiled plugin references exists with the same signature in the
real interop assemblies. Building with `GameDir` compiles against the real interop and copies the DLL into
that install's `plugins` folder.

### Releases
Pushing a tag such as `v1.0.0` runs `.github/workflows/release.yml`, which builds both downloads, writes
`SHA256SUMS.txt` and publishes a GitHub release.

### Updating the pinned BepInEx
`Directory.Build.props` holds `BepInExVersion`, `BepInExUrl` and `BepInExSha256`. Change all three together;
the plugin compiles against that version's NuGet packages and the patcher refuses any archive with a different
hash.

## License

Apache License 2.0, see [LICENSE](LICENSE). BepInEx is licensed under LGPL-2.1 and is not included in this
repository or its releases; the patcher downloads the official build.

This project is not affiliated with or endorsed by the LEGO Group, SMG Studio or Fictions.
