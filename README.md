# UnderRailFixes

Display fixes for UnderRail, for ultrawide and other resolutions the game doesn't list:

- **Resolution fix:** the resolution list comes from the modes your monitor reports, so 5120x1440, 3440x1440, 2560x1080 and the rest are all selectable. Long lists scroll, and they stay inside the options window. The resolution is stored in `UnderrailFix.ini` (default: your desktop resolution), and the game's `config.dat` is left alone.
- **Insets:** the HUD and menus can be moved in from the screen's edges into a centred area, while the world still fills the screen.
- **Interface scale:** the interface can be scaled beyond the game's largest Interface Size, automatically on screens above 4K.
- **Zoom fix:** the world is the same size on every screen, as it is at 1080p, with more room to zoom out. The zoom indicator shows the same numbers as at 1080p.

It works by patching your own `underrail.exe`, after backing it up. No launch options and no other game files are changed.

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
- **New version of UnderrailFix:** copy all of its files over the old ones and run the patcher. It re-patches from your backup of the original, so patches never stack.

If you copy in a new `UnderrailFix.dll` but forget to run the patcher, the game shows a message saying so and closes, instead of crashing.

## Settings

Most settings are also in the game: **Options → Fixes**, with a section for each fix that's switched on. Changes there apply as soon as you press Accept. After changing the interface scale or insets, the game asks whether to keep them, and puts the old values back after 15 seconds unless you choose Keep.

All settings are in `UnderrailFix.ini` in the game folder, created on the first launch. Edits to the file apply the next time you start the game.

```ini
ResolutionFix=1   ; 1 = resolution list from your monitor, resolution kept here; 0 = the game's list and config.dat
Width=native      ; native = your desktop's width, or a number of pixels (the in-game menu writes these)
Height=native
ZoomFix=1         ; 1 = zoom as on a 1080p screen, same on every screen; 0 = the game's own zoom
ZoomDefault=150   ; starting zoom, also used with Allow Zoom off (the game's 1080p default)
ZoomMin=75        ; how far Allow Zoom (SHIFT+wheel) can zoom out (the game stops at 100)
ZoomMax=200       ; how far it can zoom in (the game's 1080p limit)
UiScaleFix=1      ; 1 = interface scale and insets below; 0 = the game's own interface
UiScale=auto      ; interface scale: auto = whole multiples (100% below 4320p, 200% from 4320p ...); or a percentage
UiInsetX=0        ; space kept clear at the left and right edges (total, split evenly): 0.1 or 10% = 5% each side, 256 or 256px = 128 px each side
UiInsetY=0        ; same for the top and bottom
DpiAware=1        ; Windows only: stop Windows display scaling from stretching the game
Log=0             ; 1 = write UnderrailFix.log (for bug reports)
```

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

The main menu, options and loading screens are interface too, so they sit in the inset area with black edges around them. In game, the world still fills the screen and can be clicked right up to the edges. The interface never gets smaller than a quarter of the screen in either direction.

### Windows display scaling

XNA games don't tell Windows they can handle high-DPI screens, so with display scaling on (e.g. 150% in Windows' display settings) Windows may stretch the game or report a smaller resolution. `DpiAware=1` (the default) fixes that. It has no effect on Linux/Proton, which doesn't need it.

## Uninstall

Run the patcher with `--uninstall` (on Windows, from a command prompt: `UnderrailFix-Patcher.exe --uninstall`), or copy `underrail.exe.backup` over `underrail.exe`. If the game has updated since, the exe is already the original and the patcher just removes the outdated backup. Then delete `UnderrailFix.dll`, `UnderrailFix.ini`, `UnderrailFix.log` and the patcher files. `config.dat` only ever contains values the unmodified game understands, so the game keeps working afterwards.

## Notes for Linux (Proton)

- If the floor tiles flicker black with the **Advanced** shader, add `FNA3D_FORCE_DRIVER=D3D11 %command%` to the launch options ([FNA3D#274](https://github.com/FNA-XNA/FNA3D/issues/274)).
- On GE-Proton the game may keep running after you quit: Wine's PulseAudio driver hangs while shutting down (GE's controller-audio patches change it). Official Proton and Proton-CachyOS don't have this. On GE-Proton, `WINEDLLOVERRIDES="winepulse.drv=d" %command%` in the launch options should avoid it, by making Wine use its ALSA audio driver instead (PipeWire and PulseAudio both provide ALSA). This workaround hasn't been confirmed yet.

To report a problem, set `Log=1` in `UnderrailFix.ini` and start the game: `UnderrailFix.log` in the game folder then records what the fixes did and any errors. Logging is off by default.

## Building from source

See [AGENTS.md](AGENTS.md) for how the patcher works, how to build and test it, and everything learned about the game's internals along the way.

## License

MIT, see [LICENSE](LICENSE). The release also contains Mono.Cecil (MIT) and, in the Linux patcher, the Mono runtime (MIT).
