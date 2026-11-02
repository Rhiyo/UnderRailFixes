# UnderRailFixes

Display and quality-of-life fixes for [UnderRail](https://store.steampowered.com/app/250520/UnderRail/): ultrawide and other resolutions, a scalable interface, seeing characters behind walls, and a 64-bit mode.

- **Resolution fix:** every resolution your monitor supports (5120x1440, 3440x1440, 2560x1080…). Save screenshots also work on screens over 2048 pixels.
- **Insets:** move the HUD and menus into a centred area while the world fills the screen.
- **Interface scale:** go beyond the game's largest Interface Size (automatic above 4K).
- **Zoom fix:** the world looks the same size on any screen, with more room to zoom out.
- **Frame rate** (off by default, still being tested): no 60 FPS cap, with an optional limit.
- **Menu backgrounds:** optionally show the whole artwork instead of a crop.
- **Occluded characters** (off by default): see characters behind walls with a keyhole, pattern, fill or outline.
- **Options → Fixes:** change everything in game, no restart needed.
- **64-bit mode** (the default): runs the game on [FNA](https://fna-xna.github.io/) as a 64-bit process. A 32-bit mode on the game's own engine is one option away.

It patches your own `underrail.exe` after backing it up. No launch options are needed, the game's own config files are never changed, and saves stay compatible with the unmodified game.

## Download

Get `UnderRailFixes-X.Y.Z.zip` from [Releases](https://github.com/Rhiyo/UnderRailFixes/releases).

## Install

1. Close the game.
2. Copy the contents of the zip's `UnderRailFixes-X.Y.Z` folder into the UnderRail folder (Steam: right-click UnderRail → Manage → Browse local files).
3. Run the patcher: `UnderrailFix-Patcher.exe` on Windows, `./underrailfix-patcher` on Linux or Steam Deck.

This installs the [64-bit mode](#64-bit-mode). For the 32-bit mode instead, run `UnderrailFix-Patcher-32bit.bat` on Windows or `./underrailfix-patcher --32bit` on Linux; running the other one switches back. The patcher keeps the original as `underrail.exe.backup`. The Linux patcher also accepts the game folder as an argument.

**Updating:** after a game update or Steam's "Verify integrity of game files", run the patcher again. For a new version of UnderRailFixes, copy its files over the old ones and run the patcher. If a game update changed the code the fixes rely on, the patcher stops with "this game version isn't supported" and changes nothing.

**Uninstalling:** run the patcher with `--uninstall` (on Windows from a command prompt), then delete the mod's files: `UnderrailFix*`, `underrailfix-patcher`, `FNA.dll`, `NVorbis.dll`, `UnderrailSteamHost.exe`, `Mono.Cecil.dll`, `README.md`, `LICENSE.txt`, `THIRD-PARTY-NOTICES.txt`, and the `linux` and `x64` folders.

## Settings

Most settings are in the game under **Options → Fixes**, with a section for each fix that's switched on; changes apply when you press Accept. Interface scale and inset changes ask to be kept, and revert after 15 seconds otherwise.

All settings are in `UnderrailFix.ini` in the game folder (created on first launch; edits apply on the next start). Each group has a switch that turns it off completely: `ResolutionFix`, `ZoomFix`, `UiScaleFix`, `FrameRateFix`, `GameplayFixes`.

<details>
<summary>All settings</summary>

```ini
ResolutionFix=1    ; resolution list from your monitor, resolution kept here (0 = the game's list and config.dat)
Width=native       ; native = your desktop's size, or pixels (the in-game menu writes these)
Height=native
SaveScreenshots=1  ; save screenshots over 2048 px: the 1080p view from the screen's centre (0 = the game's crop)
ZoomFix=1          ; zoom as on a 1080p screen, on every screen (0 = the game's own zoom)
ZoomDefault=150    ; starting zoom, also with Allow Zoom off (the game's 1080p default)
ZoomMin=75         ; how far Allow Zoom (SHIFT+wheel) zooms out (the game stops at 100)
ZoomMax=200        ; how far it zooms in (the game's 1080p limit)
UiScaleFix=1       ; interface scale, insets and menu backgrounds below (0 = the game's own interface)
UiScale=auto       ; auto = whole multiples by screen height (100% below 4320p, 200% from 4320p...), or a percentage
UiInsetX=0         ; space kept clear at the sides, in total: 0.1 or 10% = 5% each side, 256 or 256px = 128 px each side
UiInsetY=0         ; the same for the top and bottom
BackgroundFit=0    ; 1 = show the whole menu artwork, letterboxed, never stretched (0 = the game's crop)
GameplayFixes=1    ; the gameplay fixes below
ShowCovered=0      ; show characters hidden behind walls and scenery
ShowCoveredPlayer=keyhole   ; for you: off, keyhole, lines, dither, fill or outline
ShowCoveredHostile=lines    ; for hostile and unfriendly characters
ShowCoveredOther=lines      ; for everyone else
ShowCoveredTransparency=75  ; how see-through the lines, dots, fill and outline are (%)
KeyholeTransparency=75      ; how see-through the keyhole is at its centre (%)
KeyholeSize=100             ; keyhole size (% of the default, about a character's height)
DpiAware=1         ; Windows: stop display scaling from stretching the game
WindowIcon=1       ; give the game window its icon (Proton)
FrameRateFix=0     ; 1 = no 60 FPS cap (not tested thoroughly yet)
MaxFps=0           ; with FrameRateFix=1: 0 = no cap, or e.g. 120
Log=0              ; 1 = write UnderrailFix.log, for bug reports
```

</details>

### Zoom

The game's zoom depends on your resolution. With the zoom fix it works as on a 1920x1080 screen everywhere: the world is the same size on screen and the zoom indicator shows the same numbers (starts at 150%, Allow Zoom goes from 75% to 200% in steps of 25). The shorter side of the screen counts, so ultrawide screens see more to the sides.

### Interface scale and insets

The game's interface comes in three fixed sizes, the largest made for 4K. `UiScale` scales it further: `auto` uses whole multiples (200% from 8K), which keeps the pixel art sharp; in-between values such as 150% work but look slightly uneven.

Insets keep the interface (HUD, menus, windows) in a centred area while the world still fills the screen, e.g. `UiInsetX=0.5` on a 32:9 screen keeps it in the middle 16:9. Menu backgrounds ignore the insets.

### Occluded characters

Walls and scenery in the isometric view hide the characters behind them. With **Show occluded characters** on (Options → Fixes → Gameplay), they are shown where hidden, with an effect chosen separately for **Player**, **Hostile** (hostile or unfriendly to you) and **Other**:

| Effect | Where the character is hidden |
|---|---|
| **Keyhole** (Player's default) | A soft round see-through hole in the scenery in front of them, like Neverwinter Nights: Enhanced Edition's keyholing |
| **Line fill** (default for the others) | Their shape in slowly drifting lines, in their Tab outline colour |
| **Dithered fill** / **Fill** | The same as dots / solid |
| **Outline** | The Tab outline on the hidden part |

It follows the game's outlines: the characters holding Tab would outline, in the same colours, and nothing with **Character Outlines** set to Never. **Reset to recommended** restores the defaults.

### Frame rate

> Not tested thoroughly yet, so off by default (`FrameRateFix=1` to try it). Please report anything that runs too fast or too slow.

The game is capped at 60 FPS. With the fix it runs at your monitor's refresh rate (tested at 240 Hz), with an optional cap in Options → Fixes.

## 64-bit mode

> The default since 1.0.8. Played under Proton so far, not yet on Windows: if something misbehaves there, install the 32-bit mode (see [Install](#install)) and please report it.

The 64-bit mode runs the game as a 64-bit process, free of the 32-bit memory limit, with all the fixes:

- **Graphics, sound and input** run on [FNA](https://fna-xna.github.io/), an open-source replacement for Microsoft's 32-bit-only XNA (the game already runs on FNA under Proton), using Direct3D 11.
- **Music** uses a built-in Ogg decoder ([NVorbis](https://github.com/NVorbis/NVorbis)).
- **Steam and GOG Galaxy** go through a small 32-bit helper (`UnderrailSteamHost.exe`) that the game starts and stops by itself.
- **No Proton launch options needed:** it fixes the Advanced-shader flicker by itself and didn't have GE-Proton's exit hang in testing.

Besides `underrail.exe` it changes a header flag in `sfmlnet-audio-2.dll` and `sfmlnet-window-2.dll` (both backed up). The 32-bit mode or uninstalling puts them back. Windows needs 64-bit Windows with .NET Framework 4; on 32-bit Windows use the 32-bit mode.

## Compatibility

- **Game:** built for **UnderRail 1.3.1.2** (Steam build 21973456). On other versions the patcher checks every change first and stops without changing anything if the code moved.
- **Tested** on Linux with Proton, at 5120x1440 and simulated 4K and 8K.
- **Not tested yet:** Windows (the patchers, display scaling, the 64-bit mode), the GOG version, the Expedition and Heavy Duty expansions, and Occluded characters in the 32-bit mode.
- **Windows (32-bit mode):** the game's graphics profile limits off-screen images to 2048 pixels, so on screens larger than 2048x2048 Occluded characters' fills and outlines switch themselves off. The keyhole works at any size (it splits its off-screen image into smaller tiles there).
- **Linux patcher:** any 64-bit x86 CPU, glibc 2.29 or later (Ubuntu 20.04+, Debian 11+, SteamOS).

## Linux / Proton notes

> None of these launch options are needed with the [64-bit mode](#64-bit-mode) (the default).

- **Floor tiles flicker black with the Advanced shader:** add `FNA3D_FORCE_DRIVER=D3D11 %command%` to the launch options ([FNA3D#274](https://github.com/FNA-XNA/FNA3D/issues/274)).
- **The game keeps running after you quit (GE-Proton only):** add `WINEDLLOVERRIDES="winepulse.drv=d" %command%`, which makes Wine use its ALSA audio driver instead of PulseAudio.

## Reporting problems

Set `Log=1` in `UnderrailFix.ini`, reproduce the problem, and attach `UnderrailFix.log` (game folder) with your game version, resolution and platform.

## Building from source

See [AGENTS.md](AGENTS.md): how the patcher works, how to build and test it, and what's known about the game's internals.

## License

MIT, see [LICENSE](LICENSE). The download includes Mono.Cecil and the Mono runtime (MIT), FNA and NVorbis (Ms-PL), and FNA3D, FAudio and SDL3 (zlib). Full texts are in `THIRD-PARTY-NOTICES.txt`.
