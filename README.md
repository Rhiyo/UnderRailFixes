# UnderRailFixes

Display and quality-of-life fixes for UnderRail, for ultrawide and other resolutions the game doesn't list, and more:

- **Resolution fix:** the resolution list comes from the modes your monitor reports, so 5120x1440, 3440x1440, 2560x1080 and the rest are all selectable. Long lists scroll, and they stay inside the options window. The resolution is stored in `UnderrailFix.ini` (default: your desktop resolution), and the game's `config.dat` is left alone. Save screenshots on screens bigger than 2048 pixels show the view instead of a cropped corner.
- **Insets:** the HUD and menus can be moved in from the screen's edges into a centred area, while the world still fills the screen.
- **Interface scale:** the interface can be scaled beyond the game's largest Interface Size, automatically on screens above 4K.
- **Zoom fix:** the world is the same size on every screen, as it is at 1080p, with more room to zoom out. The zoom indicator shows the same numbers as at 1080p.
- **Frame rate (off by default, not tested thoroughly yet):** no more 60 FPS cap; runs at your monitor's refresh rate, with an optional cap.
- **Menu backgrounds:** optionally show the whole menu art instead of the game's crop.
- **Occluded characters (off by default):** characters hidden behind walls and other scenery are shown, with a see-through keyhole, a line or dot pattern, a fill or an outline, chosen separately for you, hostile characters and everyone else. See [below](#occluded-characters).
- **In-game settings:** an **Options → Fixes** tab; changes apply without restarting.
- **Experimental 64-bit** (separate download): runs the game as a 64-bit process. See [below](#experimental-64-bit).

It works by patching your own `underrail.exe`, after backing it up. No launch options and no other game files are changed.

## Compatibility

Built and tested against **UnderRail 1.3.1.2** (the version shown on the main menu; Steam build 21973456, February 2026), on Linux with Proton, at 5120x1440 and at simulated 4K and 8K. The Windows patcher and Windows display scaling haven't been tested on Windows yet.

Other game versions: the patcher checks every place it changes before writing anything. If an update changed that code, it stops with "this game version isn't supported" and leaves the game as it is.

## Install

1. Close the game.
2. Extract this folder anywhere.
3. Run the patcher, pointing it at the UnderRail folder:
   - **Windows:** copy everything into the UnderRail folder and double-click `UnderrailFix-Patcher.exe`.
   - **Linux / Steam Deck:** copy everything into the UnderRail folder and run `./underrailfix-patcher`.
     It also accepts the folder as an argument, e.g. `./underrailfix-patcher ~/.local/share/Steam/steamapps/common/Underrail`.

The patcher saves the original as `underrail.exe.backup`, copies `UnderrailFix.dll` into the game folder, and patches `underrail.exe`.

### Updating

- **The game updated** (or you used Steam's "Verify integrity of game files"): the exe is the original again, so the fixes are off. Run the patcher again. It saves the new original as the backup and patches it. If the update changed the code the fixes rely on, the patcher stops with "this game version isn't supported" before changing anything, and the game keeps working unmodified.
- **New version of UnderRailFixes:** copy all of its files over the old ones and run the patcher. It re-patches from your backup of the original, so patches never stack.

If you copy in a new `UnderrailFix.dll` but forget to run the patcher, the game shows a message saying so and closes, instead of crashing.

## Settings

Most settings are also in the game: **Options → Fixes**, with a section for each fix that's switched on. Changes there apply as soon as you press Accept. After changing the interface scale or insets, the game asks whether to keep them, and puts the old values back after 15 seconds unless you choose Keep.

All settings are in `UnderrailFix.ini` in the game folder, created on the first launch. Edits to the file apply the next time you start the game.

```ini
ResolutionFix=1   ; 1 = resolution list from your monitor, resolution kept here; 0 = the game's list and config.dat
Width=native      ; native = your desktop's width, or a number of pixels (the in-game menu writes these)
Height=native
SaveScreenshots=1 ; save screenshots on screens over 2048 px: 1 = the 1080p view from the screen's centre; 0 = the game's own
ZoomFix=1         ; 1 = zoom as on a 1080p screen, same on every screen; 0 = the game's own zoom
ZoomDefault=150   ; starting zoom, also used with Allow Zoom off (the game's 1080p default)
ZoomMin=75        ; how far Allow Zoom (SHIFT+wheel) can zoom out (the game stops at 100)
ZoomMax=200       ; how far it can zoom in (the game's 1080p limit)
UiScaleFix=1      ; 1 = interface scale, insets and menu backgrounds below; 0 = the game's own interface
UiScale=auto      ; interface scale: auto = whole multiples (100% below 4320p, 200% from 4320p ...); or a percentage
UiInsetX=0        ; space kept clear at the left and right edges (total, split evenly): 0.1 or 10% = 5% each side, 256 or 256px = 128 px each side
UiInsetY=0        ; same for the top and bottom
GameplayFixes=1   ; 1 = the gameplay fixes below (occluded characters); 0 = none of them
ShowCovered=0     ; with GameplayFixes=1: 1 = show characters hidden behind walls and scenery (off by default)
ShowCoveredPlayer=keyhole  ; for you: off, keyhole, lines, dither, fill or outline
ShowCoveredHostile=lines   ; for hostile and unfriendly characters
ShowCoveredOther=lines     ; for everyone else
ShowCoveredTransparency=75 ; how see-through the lines, dots, fill and outline are, in percent
KeyholeTransparency=75     ; how see-through the keyhole is at its centre, in percent
KeyholeSize=100            ; keyhole size, in percent of the default (about a character's height)
DpiAware=1        ; Windows only: stop Windows display scaling from stretching the game
WindowIcon=1      ; give the game window the game's icon (under Proton the task bar otherwise shows a generic one)
BackgroundFit=0   ; with UiScaleFix=1, menu backgrounds: 0 = fill the screen, cropping the art (as the game does); 1 = show the whole image, never stretched
FrameRateFix=0    ; 1 = no 60 FPS cap (off by default: not tested thoroughly yet); 0 = the game's fixed 60
MaxFps=0          ; with FrameRateFix=1: 0 = no cap (with VSync: your refresh rate), or a cap such as 120
Log=0             ; 1 = write UnderrailFix.log (for bug reports)
```

### Save screenshots

Each save has a screenshot, shown in the load menu. On screens bigger than 2048 pixels the game means to shrink the screen to fit 2048, but only the top-left corner ends up in the picture. With `SaveScreenshots=1` (the default, with `ResolutionFix=1`; also **Options → Fixes → 1080p save screenshots**) the screenshot shows what a 1920x1080 screen would show: the 16:9 area at the centre of your screen, at 1920x1080. `0` keeps the game's own. Screens up to 2048 pixels aren't affected either way.

### Zoom

The game's own zoom depends on your resolution, so the world looks bigger or smaller depending on your screen. With `ZoomFix=1`, the zoom works as it does on a 1920x1080 screen, whatever your resolution: the world is the same size on screen, and the game's zoom indicator shows the same numbers as at 1080p. It starts at 150%, Allow Zoom (SHIFT+wheel or the zoom buttons) goes from 75% to 200%, and each step is 25%, as at 1080p.

| Screen | 75% (`ZoomMin`) | 150% (`ZoomDefault`) | 200% (`ZoomMax`) |
|---|---|---|---|
| 1920x1080 | 0.75x | 1.5x (as the game) | 2x (as the game) |
| 2560x1440, 5120x1440 | 1x | 2x | 2.66x |
| 3840x2160 | 1.5x | 3x | 4x |
| 7680x4320 | 3x | 6x | 8x |

(The table shows the actual scale the world is drawn at; the indicator always shows the percentages.)

The shorter side of the screen is what counts, so ultrawide screens see more to the sides (and portrait screens more above and below) at the same size.

The game itself stops zooming out at 100%. The default `ZoomMin=75` goes a bit further. Lower values show even more of the map, but cost more performance, and the game wasn't designed for it.

`ZoomFix=0` restores the game's own behaviour: the zoom depends on your resolution (1.5x at 1080p and 1440p, 2x at 4K), and Allow Zoom goes from 1x to 2x, or 3x on screens wider than 1920.

### Interface scale

The game draws its interface at three fixed sizes (Small, Large, Huge), and Huge was made for 4K. With `UiScaleFix=1`, `UiScale` scales the whole interface on top of the Interface Size option (`UiScaleFix=0` turns this off):

- `auto` (default): a whole multiple based on the screen's shorter side: 100% below 4320p, 200% from 4320p (8K), 300% from 6480p, and so on. A 5K or 6K screen therefore stays at 100% (a slightly smaller interface than on 4K) rather than going to 133% or 150%, which would look uneven.
- A percentage, e.g. `150`, for a bigger interface on any screen.

> **Sharpness:** the interface is pixel art with bitmap fonts. Only whole multiples (`100`, `200`, `300`, ...) keep it crisp. Anything in between (`125`, `150`, `175`, ...) makes some pixels wider than others, so text and borders look slightly uneven. That's why `auto` only uses whole multiples.

The Interface Size choices follow the scaled screen: at 150% a 1440p screen is laid out as a 960p one, so it offers up to Large. The game world stays the same size, because its zoom is adjusted to match.

### Insets

On a wide screen the HUD sits at the far edges. With `UiScaleFix=1`, insets move the interface (HUD, menus, windows) into a centred area, while the game world still fills the whole screen. For example, `UiInsetX=0.5` on a 32:9 screen keeps the interface in the middle 16:9:

- `UiInsetX` / `UiInsetY`: how much to keep clear, in total, split evenly between the two sides. A decimal or `%` is a share of the screen (`0.1` or `10%` = 5% on each side); a whole number or `px` is screen pixels (`256` or `256px` = 128 px on each side).

The main menu, options and loading screens are interface too, so their buttons and windows sit in the inset area. Their background art ignores the insets and fills the whole screen, as without insets. In game, the world still fills the screen and can be clicked right up to the edges. The interface never gets smaller than a quarter of the screen in either direction.

### Menu backgrounds

The game crops its menu art to fill the screen (it ships the art in several shapes and picks the closest). With `UiScaleFix=1` and `BackgroundFit=1` (or **Show whole menu backgrounds** in Options → Fixes) you see the whole picture instead, as large as fits and centred, with black bars where its shape doesn't match the screen. It's never stretched. Either way, the menu's buttons stay where they normally are (inside the inset area, with insets).

### Frame rate

> **Not tested thoroughly yet**, so it's off by default. Turn it on with `FrameRateFix=1` in `UnderrailFix.ini`. If anything in the game runs too fast or too slow with it, please report it.

The game runs at a fixed 60 updates and frames per second (XNA's default), whatever your monitor. With `FrameRateFix=1` that limit is gone: the game's timing is based on elapsed time, so it plays at normal speed at any frame rate (tested at 240 on a 240 Hz monitor). With it on, **Options → Fixes → Frame rate cap** (or `MaxFps`) sets an optional cap: Unlimited, 30, 60, 120, 144, 165 or 240.

| Game's VSync | Cap | Frame rate |
|---|---|---|
| on | Unlimited (default) | your monitor's refresh rate: the smoothest |
| on | below the refresh rate | the cap; best if it divides the refresh rate evenly (e.g. 120 or 60 on 240 Hz), otherwise frame pacing is slightly uneven |
| off | a number | exactly the cap, with possible tearing |
| off | Unlimited | as fast as your GPU can go; wastes power and can make coil whine |

### Occluded characters

In UnderRail's isometric view, walls and other scenery in front of a character hide them. With `ShowCovered=1` (or **Options → Fixes → Gameplay → Occluded Characters → Show occluded characters**), characters hidden that way are shown, only where they're hidden. Off by default. Choose an effect separately for **Player** (you), **Hostile** (characters hostile or unfriendly to you; the game's red and orange) and **Other** (everyone else):

| Effect | What you see where the character is hidden |
|---|---|
| **Keyhole** (default for Player) | A soft round see-through hole in the scenery in front of them, showing them and what's behind them, as Neverwinter Nights: Enhanced Edition's "keyholing". Floors are never cut. `KeyholeSize` and `KeyholeTransparency` set its size and how see-through its centre is. |
| **Line fill** (default for Hostile and Other) | Their shape in horizontal lines, slowly drifting, in the colour the game outlines them with when you hold Tab |
| **Dithered fill** | The same as a pattern of spaced-out dots |
| **Fill** | The same, solid |
| **Outline** | The game's Tab outline, on the hidden part only |
| **Off** | Nothing |

`ShowCoveredTransparency` sets how see-through the fills and outline are. The patterns keep their size against the characters at any zoom. **Reset to recommended** (next to the Occluded Characters heading) puts the effects and sliders back to the defaults.

It follows the game's own rules for outlines: it shows the characters holding Tab would outline, in the same colours, and nothing if the game's **Character Outlines** option is set to Never. Other characters don't count as scenery, and where a keyhole is open, the other effects are cleared inside it. Holding Tab still shows the game's full outlines, with fills drawn on top. The game's own **Unobscuration alpha** option (Interface tab) is separate: it fades certain whole objects.

### Windows display scaling

XNA games don't tell Windows they can handle high-DPI screens, so with display scaling on (e.g. 150% in Windows' display settings) Windows may stretch the game or report a smaller resolution. `DpiAware=1` (the default) fixes that. It has no effect on Linux/Proton, which doesn't need it.

## Experimental 64-bit

> **Largely untested:** played under Proton (Linux) only, not yet on Windows. Keep a backup of your saves, and please report anything that behaves differently.

The game is a 32-bit program, which limits how much memory it can use. The 64-bit download (`UnderRailFixes-64bit-experimental-X.Y.Z.zip`) runs it as a 64-bit process instead, with all the fixes above:

- **Graphics, sound and input** use [FNA](https://fna-xna.github.io/), an open-source reimplementation of XNA (the 32-bit-only framework the game is made with). It's what the game already runs on under Proton. It uses Direct3D 11, which also avoids the shader flicker described under Linux below.
- **No Proton launch options needed:** it sets Direct3D 11 itself, so `FNA3D_FORCE_DRIVER=D3D11` isn't needed, and in testing on GE-Proton it exited cleanly without `WINEDLLOVERRIDES="winepulse.drv=d"` (its music no longer goes through the 32-bit audio library involved in that hang).
- **Music** is played by a built-in Ogg decoder ([NVorbis](https://github.com/NVorbis/NVorbis)), since the game's 32-bit music library can't load in a 64-bit process.
- **Steam and GOG Galaxy** (achievements, the overlay keyboard) go through a small 32-bit helper, `UnderrailSteamHost.exe`, which loads the game's own Steam or Galaxy libraries. The game starts and stops it by itself; there's nothing extra to run. (GOG achievements in 64-bit mode haven't been tested on a GOG copy yet.)

Install it like the standard download, but run `UnderrailFix-Patcher-64bit.exe` (Windows) or `./underrailfix-patcher-64bit` (Linux). Besides `underrail.exe` it changes a header flag in the game's `sfmlnet-audio-2.dll` and `sfmlnet-window-2.dll` (backed up as `.backup`), and adds `FNA.dll`, the `x64` folder, `UnderrailFix64.dll`, `NVorbis.dll` and `UnderrailSteamHost.exe`.

To go back, run the standard patcher (the normal fixes, 32-bit again), or uninstall as below; both put the `sfmlnet` DLLs back. Saves and settings made in 64-bit mode stay readable by the normal game. Windows needs a 64-bit Windows with .NET Framework 4 (built in).

## Uninstall

Run the patcher with `--uninstall` (on Windows, from a command prompt: `UnderrailFix-Patcher.exe --uninstall`), or copy `underrail.exe.backup` over `underrail.exe`. If the game has updated since, the exe is already the original and the patcher just removes the outdated backup. Then delete `UnderrailFix.dll`, `UnderrailFix.ini`, `UnderrailFix.log` and the patcher files (and, after the 64-bit patcher, `UnderrailFix64.dll`, `FNA.dll`, `NVorbis.dll`, `UnderrailSteamHost.exe` and the `x64` folder). `config.dat` only ever contains values the unmodified game understands, so the game keeps working afterwards.

## Notes for Linux (Proton)

> **With the [64-bit download](#experimental-64-bit) none of the launch options below are needed:** it sets Direct3D 11 itself, and the exit hang didn't occur in testing.

- If the floor tiles flicker black with the **Advanced** shader, add `FNA3D_FORCE_DRIVER=D3D11 %command%` to the launch options ([FNA3D#274](https://github.com/FNA-XNA/FNA3D/issues/274)).
- On GE-Proton the game may keep running after you quit: Wine's PulseAudio driver hangs while shutting down (GE's controller-audio patches change it). Official Proton and Proton-CachyOS don't have this. On GE-Proton, `WINEDLLOVERRIDES="winepulse.drv=d" %command%` in the launch options avoids it, by making Wine use its ALSA audio driver instead (PipeWire and PulseAudio both provide ALSA).

## Reporting problems

Set `Log=1` in `UnderrailFix.ini` and start the game: `UnderrailFix.log` in the game folder then records what the fixes did and any errors. Logging is off by default. Please include your game version (main menu), your resolution and the log.

## Building from source

See [AGENTS.md](AGENTS.md) for how the patcher works, how to build and test it, and everything learned about the game's internals along the way.

## License

MIT, see [LICENSE](LICENSE). The release also contains Mono.Cecil (MIT) and, in the Linux patcher, the Mono runtime (MIT). The 64-bit download also contains FNA and NVorbis (Ms-PL), and FNA3D, FAudio and SDL3 (zlib); `THIRD-PARTY-NOTICES.txt` in each download has the full texts.
