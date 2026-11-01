#!/usr/bin/env bash
# Automated checks on a built dist/UnderrailFix, run by release.sh (and fine to run by hand after ./build.sh).
# Works on a temporary copy of the game folder; the real install is only read.
#   - Linux patcher, with Mono hidden (it must be self-contained): install, re-run, simulated game update, uninstall
#   - the original exe is restored byte-identical and the backup is removed
#   - the patched exe calls exactly the expected set of hooks
#   - the Windows patcher (run with mono) installs and uninstalls too
set -euo pipefail
HERE="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${GAME:-$HOME/.local/share/Steam/steamapps/common/Underrail}"
DIST="$HERE/dist/UnderrailFix"
CECIL_DIR=/usr/lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756
[ -d "$DIST" ] || { echo "no $DIST: run ./build.sh first" >&2; exit 1; }

# The stock exe: the backup if the local copy is patched.
ORIG="$GAME/underrail.exe"; [ -f "$GAME/underrail.exe.backup" ] && ORIG="$GAME/underrail.exe.backup"

WORK="$(mktemp -d)"; trap 'rm -rf "$WORK"' EXIT
mcs -nologo -out:"$WORK/ListHooks.exe" -r:"$CECIL_DIR/Mono.Cecil.dll" -r:System.Core.dll "$HERE/src/Build/ListHooks.cs"
hooks() { MONO_PATH="$CECIL_DIR" mono "$WORK/ListHooks.exe" "$1"; }
fail() { echo "CHECK FAILED: $*" >&2; exit 1; }
md5() { md5sum "$1" | cut -d' ' -f1; }

# The game's managed and native DLLs (the patcher resolves references next to the exe), plus the stock exe.
game_copy() {
    mkdir -p "$1"
    cp "$ORIG" "$1/underrail.exe"
    for f in "$GAME"/*.dll; do case "$(basename "$f")" in UnderrailFix.dll|Mono.Cecil.dll) ;; *) cp "$f" "$1/";; esac; done
    cp -r "$DIST"/. "$1/"
}
STOCK=$(md5 "$ORIG")

EXPECTED="FixStartup.Run
ResolutionStore.Default
ResolutionStore.Load
ResolutionStore.Save
UiScaleFix.BackgroundDraw
UiScaleFix.BackgroundPlace
UiScaleFix.BackgroundSize
UiScaleFix.Begin5
UiScaleFix.Begin6
UiScaleFix.DimRect
UiScaleFix.FloorOrigin
UiScaleFix.FullScreen
UiScaleFix.GetMouse
UiScaleFix.GetScissor
UiScaleFix.InScreen
UiScaleFix.SetScissor
UiScaleFix.ToScreen
UiScaleFix.Transform
UiScaleFix.Virtual
UiScaleFix.VirtualHeight
UiScaleFix.VirtualWidth
UiScaleFix.WorldLocation
UiScaleFix.WorldLocationGet
UnderrailDisplayModes.Get
UnderrailDisplayModes.Label
UnderrailDisplayModes.List
UnderrailDisplayModes.MaxZoom
UnderrailDisplayModes.MinZoom
UnderrailDisplayModes.ShownZoom
UnderrailDisplayModes.ZoomStep
UnderrailDropdownScroll.Closed
UnderrailDropdownScroll.Opened
UnderrailOptions.AddTab
UnderrailOptions.Applied
UnderrailOptions.Save"

echo "== Linux patcher (Mono hidden)"
L="$WORK/linux"; game_copy "$L"
nomono() { bwrap --ro-bind / / --bind "$WORK" "$WORK" --tmpfs /usr/lib/mono --tmpfs /etc/mono --dev /dev --proc /proc "$@" </dev/null; }
nomono "$L/underrailfix-patcher" "$L" >/dev/null || fail "install"
[ "$(md5 "$L/underrail.exe.backup")" = "$STOCK" ] || fail "backup is not the original exe"
diff <(echo "$EXPECTED") <(hooks "$L/underrail.exe") >/dev/null || { diff <(echo "$EXPECTED") <(hooks "$L/underrail.exe") >&2; fail "patched exe hooks differ from the expected set"; }
FIRST=$(md5 "$L/underrail.exe")
nomono "$L/underrailfix-patcher" "$L" >/dev/null || fail "re-run"
[ "$(md5 "$L/underrail.exe")" = "$FIRST" ] || fail "re-running the patcher changed the result (patches stacking?)"
cp "$L/underrail.exe.backup" "$L/underrail.exe"          # a game update / Verify files puts an original back
nomono "$L/underrailfix-patcher" "$L" >/dev/null || fail "patch after game update"
[ "$(md5 "$L/underrail.exe.backup")" = "$STOCK" ] || fail "backup after game update is not the original"
nomono "$L/underrailfix-patcher" "$L" --uninstall >/dev/null || fail "uninstall"
[ "$(md5 "$L/underrail.exe")" = "$STOCK" ] || fail "uninstall didn't restore the original exe"
[ ! -e "$L/underrail.exe.backup" ] || fail "uninstall left the backup"
echo "   ok: install, re-run, game update, uninstall; $(echo "$EXPECTED" | wc -l) hooks"

echo "== Windows patcher (run with mono)"
W="$WORK/windows"; game_copy "$W"
(cd "$W" && mono UnderrailFix-Patcher.exe "$W" </dev/null >/dev/null) || fail "Windows patcher install"
diff <(echo "$EXPECTED") <(hooks "$W/underrail.exe") >/dev/null || fail "Windows patcher: hooks differ"
[ "$(md5 "$W/underrail.exe")" = "$FIRST" ] || fail "Windows and Linux patchers produce different exes"
(cd "$W" && mono UnderrailFix-Patcher.exe "$W" --uninstall </dev/null >/dev/null) || fail "Windows patcher uninstall"
[ "$(md5 "$W/underrail.exe")" = "$STOCK" ] || fail "Windows patcher uninstall didn't restore the original"
echo "   ok: same patched exe as Linux, uninstall restores the original"
echo "All checks passed."
