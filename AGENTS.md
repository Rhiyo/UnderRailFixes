# AGENTS.md: working on UnderRailFixes

Notes for whoever (person or AI agent) works on this next. The game's title is stylized **UnderRail**; this mod is
**UnderRailFixes**. File and assembly names still say `UnderrailFix` (see "Naming" below).

## What it is

Display fixes for UnderRail (Styg, Steam app 250520), distributed as a **patcher**: it backs up the player's own
`underrail.exe` to `underrail.exe.backup` and patches it in place (Mono.Cecil) to call `UnderrailFix.dll`, which
holds all the fix logic. No launch options, no other game files touched, `--uninstall` restores the backup.
Every fix is switched at runtime from `UnderrailFix.ini`, so toggling never needs a re-patch.

Fixes (README.md has the player-facing description):

| Fix | ini | What |
|---|---|---|
| Resolution | `ResolutionFix`, `Width`, `Height` | resolution list from the monitor's modes; resolution kept in the ini (default native), config.dat keeps a vanilla value; long dropdowns scroll; no odd aspect labels |
| Zoom | `ZoomFix`, `ZoomDefault`, `ZoomMin`, `ZoomMax` | zoom as a 1080p screen would need it for the same view (numbers match the stock 1080p indicator: start 150, 100-200), wider limits, steps scaled |
| Interface | `UiScaleFix`, `UiScale`, `UiInsetX`, `UiInsetY` | whole-UI scale (auto = whole multiples of the shorter side / 2160), insets that centre the UI while the world stays full-screen |
| Menu backgrounds | `BackgroundFit` | 0 = the game's crop; 1 = whole image, letterboxed (backgrounds ignore insets either way) |
| DPI | `DpiAware` | Windows only: per-monitor DPI awareness before the window exists |
| Frame rate | `FrameRateFix`, `MaxFps` | no fixed 60 Hz timestep (0) or a fixed one at MaxFps |
| Log | `Log` | 0 (default) = no log file; 1 = `UnderrailFix.log` |

In game: **Options -> Fixes** tab with a group per enabled fix; applies on Accept; scale/inset changes get a
15 s keep/revert countdown (the game's own dialog).

### Why a patcher (alternatives that were tried and fail)

Goal was "drop in, no launch options, delete to remove". Under Proton every no-patch route fails:
- Proxy DLLs (version/dinput8/winmm): need `WINEDLLOVERRIDES`. The exe only imports mscoree.
- `underrail.exe.config` `appDomainManager*` and `APPDOMAIN_MANAGER_*` env vars: ignored by Mono and by Wine Mono (tested).
- Image File Execution Options `Debugger`: ignored by Wine (tested).
- `dllmap` on the game's native `SteamHelper.dll`: Steam-only, and first called after the loading screen is up.
- A launcher exe that patches in memory worked (an earlier version) but needs a Steam launch option.
- Shipping a patched exe = redistributing the game. Don't.

## Repository

```
build.sh                 build into dist/ (and `./build.sh install` patches the local Steam copy)
release.sh               the only way to make a release (see Releasing)
src/Release/             licence texts shipped in the release zip
tools/check-release.sh   automated patcher checks (release.sh runs them)
src/Runtime/*.cs         UnderrailFix.dll: everything the patched game calls
src/Installer/*.cs       UnderrailFix-Patcher.exe: backup + Cecil patches + uninstall
src/Build/Retarget.cs    build step, see "XNA type identity"
src/Build/GameVersion.cs, ListHooks.cs   release helpers
src/Bundle/mono.config   dllmap for the bundled Linux patcher
tools/gamescope-test.sh  run the game at a virtual resolution (8K, portrait...)
```

Never commit decompiled game code or game files (`.gitignore` blocks exe/dll/xnb).

### Building (Linux)

Needs: `mono` + `mcs` + `mkbundle` (Arch: `mono`), Mono.Cecil 0.11 in the GAC
(`/usr/lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756/`), a stock `underrail.exe` (the build compiles against
the Steam copy, or its `.backup`), and any Proton install (for `WineMono.FNA.dll`). Paths are at the top of build.sh.

Output `dist/UnderrailFix/`: `UnderrailFix.dll`, `UnderrailFix-Patcher.exe` + `Mono.Cecil.dll` (Windows, .NET
Framework 4), `underrailfix-patcher` + `linux/` (Linux: mkbundle'd Mono, needs glibc >= 2.38), README.md.

### Releasing

Always through `./release.sh X.Y.Z "description"` (or `-F notes.md`), never by hand, so every release is made the
same way. It refuses to run unless on `main`, clean, in sync with origin, with a new version and UnderRail closed.
Then it stamps `FixVersion.Value` (src/Runtime/Version.cs; committed only after everything passed), builds from
scratch, runs `tools/check-release.sh`, packages `UnderRailFixes-X.Y.Z.zip` (dist + LICENSE.txt +
THIRD-PARTY-NOTICES.txt from src/Release/), writes the notes (description + game version read from the exe by
src/Build/GameVersion.cs + Steam build + install steps + SHA-256), tags `vX.Y.Z`, pushes and runs `gh release create`.

`tools/check-release.sh` (also usable after any `./build.sh`) works on a temporary copy of the game folder: the Linux
patcher with Mono hidden (install, re-run gives the same exe, simulated game update, uninstall restores the original
byte for byte), the exact set of hooks in the patched exe (src/Build/ListHooks.cs; update its EXPECTED list when hooks
are added or removed), and the Windows patcher producing the identical exe. No CI: building needs the real game.

If a setting's **meaning** changed in the release, bump `FixSettings.Version` and add the key to `Reset`
(Settings.cs) first: older ini files are then regenerated with current comments, other values kept, those keys reset.

## How the game is put together (what we learned)

**Built against UnderRail 1.3.1.2** (`dm9.d`, shown on the main menu), Steam build 21973456 (Feb 2026), original
`underrail.exe` MD5 `0046fd2c81955dff092f2cf00d1f60f8`. All obfuscated names below are from that build. After a game
update, run the patcher on a copy first: an `Expect` failure names the patch point that moved.

- .NET 4 / XNA 4.0, 32-bit with LARGE_ADDRESS_AWARE. Obfuscated: types and members are names like `t1`, `efw`,
  `a`, `b`; they can change between game versions. Decompile with `ilspycmd` (into a scratch folder, never the repo).
- **Under Proton it runs on Wine Mono 11.x, which replaces XNA with FNA/FNA3D/SDL3.** On Windows it's Microsoft XNA.
- Native DLLs it P/Invokes: `SteamHelper.dll` (9 functions, Styg's Steam wrapper; also `IsRunningOnWine`), user32.
- Every patch point is checked with `Expect(what, found, expected)` in the patcher; a different game version fails
  before anything is written. Current counts: zoom limits 3, loading-screen size reads 2, SpriteBatch.Begin 2,
  scissor reads/writes 11, Mouse.GetState 7, world projection 1, zoom label 1, zoom wheel step 1, zoom buttons 2,
  world view sizing 1, shake moves 2, shake check 1, floor at() 1.

### Resolutions and settings

- `TimelapseVertigo.Common.Settings.DisplayResolution` enum (`r1024x768=5` ... `r3840x2160=200`, all < 1000).
  `efw`'s static ctor parses the names into `Dictionary<DisplayResolution, egd>` (`efw.m_a`); `efw.a()` lists,
  `efw.a(key)` looks up (stock throws on unknown keys).
- `egd`: `a` width, `b` height, `c` InterfaceElementSize2, `d` key, `e` System.Drawing.Point aspect, `f` Auto's
  UiSize, `g` largest allowed UiSize, `h` fixed zoom. Tiers by height: UiSize Huge > 1200, Small <= 800 or w < 1200,
  else Large; zoom 2 above 1440, 1.5 for 1080-1440, else 1. `egd.ToString` shows the ratio only if it reduces to <= 20:x.
- Our extra modes use synthetic keys `width << 16 | height` (in memory only).
- config.dat: 24-byte header + gzip'd BinaryFormatter. Video settings class `cnt` (keys `VS:R` resolution, `VS:D`
  mode, `VS:S` shader, `VS:M` sprite target mask...). `cnt`'s default ctor runs on every settings **copy**, so the
  resolution hook there is `ResolutionStore.Default`, and only the `VS:R` unbox in the deserializing ctor is `Load`.
- `ls.a()` **re-reads config.dat from disk**. Never call it per frame; the live settings are
  `bpo.GetService<cnk>().aq1()` (cnk is implemented by d6f).
- Options screen `t1`: tabs built once in `t1.h()`; per tab `s = page; rows...; f(); g();` (`s` current page,
  `u` tab control `dpg`, `t` dropdowns added last). Row builder `a(string label, euz control, euz, bool, bool, int,
  bool)`, heading `a(string)`, separator `a(bool)`, styled dropdown `CreateComboBoxFromEnum<T>`. Accept:
  `t1.a(object, b25)` -> `d()` (writes options) -> `cnk.arb(ls)` -> `d6f.a(ls)` applies; when resolution/UI size
  changed the game re-lays-out with `bl9.b(); bpo.GetService<ps>().anc();` (in game: new `ci3`, map kept).
- Keep/revert dialog: `bnr.a(esj size, eyp callback, title, text, null, defaultButton, Color?, Color?, ms, cpk[] buttons, owner, owner)`;
  `eyp.g0(string button)`; on timeout the default button is passed.
- Sprite Target Mask (`VS:M`): pixel hit-testing from mask files instead of GPU readbacks; the game only turns it on
  under Wine for fresh configs. Keep it on.

### Drawing, input, layout

- One draw context `cp9` (SpriteBatch `Begin(..., PointClamp, ...)`), drawing calls through `ob` (`bc2` wraps
  SpriteBatch, `drl` = the world's `UrGenericSpriteBatchBase`, which builds its own pixel-to-clip `MatrixTransform`
  from the viewport and passes `TargetSize`).
- **The world view is a UI control**: `mv : a7m : dnn : euz`, a child of the in-game surface `ci3` (`daj`), sized by
  `a2x.c(m_t)`. Screen shake moves it in `ci3.iv` (and resets it when `Location != Point.Zero`, every frame).
- UI screen size comes from `bef.k0()` (back buffer) / `bef.k4()` (requested), implemented by `tb`.
- Hit-testing is hierarchical (`euz.a(MouseState)`): children are only tried if the parent's `iz()` accepts;
  `daj.iz` accepts 0,0 + k4.
- `a7m.b(int,int)` (mouse -> floor tile) subtracts `at()` from the mouse but compares with tiles projected in screen
  coordinates (`m_b` includes `at()`): a stock bug that is harmless while the view is at 0,0.
- Cursor warps: `bso.abb(Point)` -> Win32 `SetCursorPos` (right-drag camera re-centres the cursor every frame).
- Menu backgrounds: `czo.a(esj size)` picks the concept art closest to the size's aspect (several ship), crops it
  from the top-left (`bg1.d`), places it at 0,0 (main menu `a8k`, options `t1`, `iw`, `c9v` pass k0).
- Popups dim the screen with `d0p.a(cp9, Color, Rectangle(0,0,k0), ...)` (Escape menu `cef`, `bnr`, `tg`, `e4u`, `pz`).
- Off-screen render targets exist (e.g. `by8` lights); the screenshot path renders the UI into a target with a
  screen-sized viewport. Hence UI mapping applies only while the viewport is back-buffer-sized.
- Interface sizes are hand-made per tier: atlases `gui/ui_s|l|h_gen1`, cursors `cursors_s|l|h`, bitmap fonts in
  `data/fonts`, ~330 lines of pixel layout per tier in `wj`. No continuous scale exists in the game.

### Timing

- The game never sets `IsFixedTimeStep`/`TargetElapsedTime`, so XNA's default fixed 1/60 s caps it at 60 FPS. Its
  systems take elapsed time from `GameTime` (passed down via `e6d`; e.g. `d6f.d1` -> `es6` world ticks), nothing reads
  `TargetElapsedTime`, and in game it plays at normal speed at 240 FPS (frame-counting logic would have run 4x fast), so a
  variable timestep is safe. Not yet tried: a 30 FPS cap (would show frame-bound logic as half speed).
- VSync is the game's option `VS:V` -> `GraphicsDeviceManager.SynchronizeWithVerticalRetrace` (`tb.k7`).

### Zoom

- `dka.b` (per frame) sets the camera limits: `ud(1f)` min, `uf(2f)` and `uf(3f)` when the view is wider than 1920.
  Camera `a3u`: `h` zoom, `d` min, `e` max; `uq` clamps.
- Current zoom stored in `d6f` (`aq8` get, `aq9` set, `ar1(float, bool)`), started from `egd.h`.
- Indicator: `b5r` shows `aq8() * 100 + "%"`. SHIFT+wheel: `mv` calls `an9().uo(notches * e.b)` with `e.b = 0.25`
  (set by ci3); buttons: `ci3` `aq9(aq8() -/+ 0.25f)`. Stock 1080p: starts 150%, 100-200%, 25 per step.
- The visible tile range accounts for zoom (`a7m`: Size / (tile * 2 * zoom)), so zooming out below 1x just draws more.

## Our hooks (src/Installer/Installer.cs)

| Game | Hook | Purpose |
|---|---|---|
| Game subclass ctor (`enu`, first `newobj` in Main) | `FrameRate.Apply(object)` | IsFixedTimeStep / TargetElapsedTime |
| entry `Main` (`b7i.a`) | `FixStartup.Run(version)` | version check, DPI awareness. **Signature must never change.** |
| `cnt` ctors / `GetObjectData` | `ResolutionStore.Default/Load/Save` | resolution via ini; config.dat stays vanilla-valid |
| `efw.a()` / `efw.a(key)` | `UnderrailDisplayModes.List/Get` | monitor modes, nearest-size fallback |
| `egd.ToString` | `UnderrailDisplayModes.Label` | no aspect suffix (stock when off) |
| `aw9.a()` / `aw9.b()` (dropdown open/close) | `UnderrailDropdownScroll.Opened/Closed` | fit list in tab page, game's `dg8` scrollbar |
| `dka.b` `ud(1f)/uf(2f)/uf(3f)` | `MinZoom/MaxZoom` | zoom limits |
| `b5r` `aq8()` | `ShownZoom` | indicator in 1080p-equivalent % |
| `mv` `uo(step)`, `ci3` `aq8() -/+ 0.25` | `ZoomStep` | 25 points per step on any screen |
| `tb.k0/k4` | `UiScaleFix.Virtual` | layout size = interface area / scale |
| `tb.Draw` PreferredBackBuffer reads | `VirtualWidth/Height` | loading screen |
| `SpriteBatch.Begin` (cp9, bc2) | `Begin5/Begin6` | UI-to-pixel transform |
| world batch `MatrixTransform` | `Transform` | same for the world renderer |
| `GraphicsDevice.ScissorRectangle` get/set | `GetScissor/SetScissor` | clip rects |
| `Mouse.GetState()` | `GetMouse` | mouse in UI coordinates |
| `bso.abb` | `ToScreen` | cursor warp in pixels |
| `ci3.a` `a2x.c(m_t)` | `FullScreen` | world view stays full-screen with insets |
| `ci3.iv` Location set / get | `WorldLocation` / `WorldLocationGet` | shake relative to the screen |
| `daj.iz` | `InScreen` | world clickable in inset margins |
| `a7m.b` `at()` | `FloorOrigin` | floor clicks with insets (the stock bug above) |
| `d0p.a(cp9, Color, Rectangle, bool, bool)` | `DimRect` | full-screen dims with insets |
| `czo.a(esj)` -> `bg1` (start / return) | `BackgroundSize` / `BackgroundPlace` | menu backgrounds ignore insets; `BackgroundFit=1` shows the whole image (letterboxed) |
| `bg1.iv` first Rectangle (draw target) | `BackgroundDraw` | the picture moves, not the control: the main menu panel is a **child** of the background |
| end of `t1.h` / `t1.d` / `d6f.a(ls)` | `UnderrailOptions.AddTab/Save/Applied` | Fixes tab, save, live apply + confirm |

## Traps (each of these cost time)

- **Obfuscated property accessors**: `euz.Location`'s setter is named `g`. Find accessors through the
  `PropertyDefinition` (Cecil) or `GetProperty` (reflection), never by `set_Name`.
- **Field/method name clashes**: `esj` (size) has fields `c`/`d` and methods `c()`/`d(...)`; C# can't pick, so the
  runtime reads those fields by reflection.
- **XNA type identity**: the runtime DLL is compiled against FNA (`WineMono.FNA`), then `Retarget.cs` points each
  XNA type reference at the XNA 4.0 assembly the game references for it, so it binds like the game does (FNA facades
  under Wine Mono, real XNA on Windows). Calling *game* members whose signatures use XNA types (e.g. setting
  `euz.Location`, `bnr.a` with `Color?`) doesn't compile (CS0012); use reflection for those.
- Cecil drops IMAGE_FILE_LARGE_ADDRESS_AWARE; the patcher copies the original COFF characteristics.
- An exe-loading launcher must not touch game types before loading the patched copy (the stock exe would load).
- `mkbundle --simple` can't load embedded native libraries: `libmono-native.so` ships next to the binary, with a
  wrapper script setting `LD_LIBRARY_PATH` and `src/Bundle/mono.config` mapping `System.Native`.
- Under zsh, `$VAR` with spaces doesn't word-split; use bash for test loops.
- **Never `pkill -f` a pattern that appears in your own command line**: it kills your shell. Kill by PID.
- Process names are cut to 15 chars (`underrailfix.ex`).

## Testing

- Offline: build, then run the patcher on a **copy** of the game folder (install / re-run / game update / uninstall
  -> byte-identical). Runtime rules without XNA (ScaleRules, ResolutionStore, settings migration) can be called by
  reflection from a tiny mono program in that folder. Hide Mono (`bwrap --tmpfs /usr/lib/mono --tmpfs /etc/mono`)
  to prove the Linux patcher is self-contained.
- In game, with `Log=1` in the ini (off by default; `Log.Always` is only used for the version-mismatch exit):
  `UnderrailFix.log` logs resolution decisions, display modes, "interface: ..." lines once scaling/insets
  are active, option changes, and errors.
- Other resolutions: `tools/gamescope-test.sh 7680 4320` (8K), `3840 2160`, `2160 3840` (portrait). Compare
  screenshots: 4K and 8K should look identical.
- Done: 5120x1440 native, 4K and 8K (UI 200%, right-drag fixed), insets (floor clicks, edge clicks, Escape dim),
  options tab basics, keep/revert countdown, zoom steps (wheel and buttons equal).
- Not yet: the Frame rate cap dropdown in game, the latest background change in game (picture drawn full-screen/letterboxed while the main menu's
  panel stays inset), portrait, the Windows patcher and `DpiAware` on real Windows, the keep/revert countdown in all paths,
  older glibc distros for the Linux patcher.

## Open ideas

- 64-bit: the exe is x86 + 32BITREQUIRED; clearing that runs the managed code as 64-bit (Wine starts IL-only exes as
  64-bit), but every native DLL is 32-bit: CSFML audio/window (old SFML 2.0/2.1, VC9 build, matching the sfmlnet
  bindings), OpenAL Soft, libsndfile 1.0.24, steam_api.dll (02.01.77.37) and Styg's closed SteamHelper.dll (9
  functions). 64-bit SFML/OpenAL/libsndfile could be shipped; SteamHelper would need a rewrite; steam_api64.dll may
  only be redistributed by Steamworks partners, and without it there is no Steam integration or achievements.
  The exe is LAA (4 GB), so the gain is small unless out-of-memory crashes appear. Parked.
- Sharper scaled text: re-render the bitmap fonts from their original TTFs at the scaled size; UI art would need
  upscaling (it's painted, not vector).
- Naming: rename files/assembly to UnderRailFixes. The patcher detects a patched exe by its reference to the
  `UnderrailFix` assembly, so a rename must keep recognising old patches (and bump the version).

## Linux / Proton notes (not part of the mod)

- Advanced shader tiles flicker black on FNA3D's SDLGPU (Vulkan) and OpenGL backends; `FNA3D_FORCE_DRIVER=D3D11`
  fixes it. Reported upstream: https://github.com/FNA-XNA/FNA3D/issues/274
- On GE-Proton the game can hang on exit in `winepulse.so: pulse_stop` (GE's proton-ds5-haptic patches change
  winepulse); official Proton and Proton-CachyOS exit fine. Suggested (untested) workaround on GE:
  `WINEDLLOVERRIDES="winepulse.drv=d"` (Wine falls back to winealsa).
- The game's own logger (`aa5`) only calls `Trace.WriteLine`, which no listener writes anywhere in release builds,
  so there is no game log to add to; `UnderrailFix.log` is the only log.
