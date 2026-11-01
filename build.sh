#!/usr/bin/env bash
# Builds the UnderrailFix release into dist/:
#   UnderrailFix.dll               runtime fixes, loaded by the patched game (all platforms)
#   UnderrailFix-Patcher.exe       Windows patcher (.NET Framework 4, built into Windows) + Mono.Cecil.dll
#   underrailfix-patcher           Linux patcher (single binary with the Mono runtime bundled)
# ./build.sh install   also patches the local Steam copy.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
GAME="$HOME/.local/share/Steam/steamapps/common/Underrail"
CECIL=/usr/lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756/Mono.Cecil.dll
OUT="$HERE/dist/UnderrailFix"
rm -rf "$HERE/dist" && mkdir -p "$OUT" "$HERE/build"

# Compile the runtime against the stock game exe (the backup when the local copy is already patched).
REF="$GAME/underrail.exe"
[ -f "$GAME/underrail.exe.backup" ] && REF="$GAME/underrail.exe.backup"
# XNA types come from FNA at compile time; Retarget then points them at the XNA 4.0 assemblies the game uses.
FNA="$(ls -d "$HOME"/.local/share/Steam/compatibilitytools.d/*/files/share/wine/mono/wine-mono-*/lib/mono/gac/WineMono.FNA/*/WineMono.FNA.dll \
    /usr/share/steam/compatibilitytools.d/*/files/share/wine/mono/wine-mono-*/lib/mono/gac/WineMono.FNA/*/WineMono.FNA.dll 2>/dev/null | sort -V | tail -1)"
[ -n "$FNA" ] || { echo "WineMono.FNA.dll not found (comes with any Proton install)" >&2; exit 1; }
mcs -nologo -optimize+ -target:library -out:"$OUT/UnderrailFix.dll" -r:"$REF" -r:"$FNA" -r:System.Core.dll -r:System.Drawing.dll "$HERE"/src/Runtime/*.cs
mcs -nologo -out:"$HERE/build/Retarget.exe" -r:"$CECIL" -r:System.Core.dll "$HERE/src/Build/Retarget.cs"
MONO_PATH="$(dirname "$CECIL")" mono "$HERE/build/Retarget.exe" "$OUT/UnderrailFix.dll" "$REF"
mcs -nologo -optimize+ -out:"$OUT/UnderrailFix-Patcher.exe" -r:"$CECIL" -r:System.Core.dll "$HERE"/src/Installer/*.cs
cp "$CECIL" "$OUT/"

# Linux: the patcher, Cecil and the Mono runtime bundled into one executable (no Mono install needed), plus
# libmono-native.so (System.Native: file and console I/O), which mkbundle --simple can't load from inside the bundle.
# The launch script puts its own folder on the library path; src/Bundle/mono.config maps System.Native to the file.
mkdir -p "$OUT/linux"
mkbundle --simple --deps --machine-config /etc/mono/4.5/machine.config --config "$HERE/src/Bundle/mono.config" \
    -L "$(dirname "$CECIL")" -o "$OUT/linux/underrailfix-patcher.bin" "$OUT/UnderrailFix-Patcher.exe" "$OUT/Mono.Cecil.dll" >/dev/null
cp -L /usr/lib/libmono-native.so "$OUT/linux/libmono-native.so"
cat > "$OUT/underrailfix-patcher" <<'SH'
#!/bin/sh
# Linux patcher for UnderrailFix. Usage: ./underrailfix-patcher [game folder] [--uninstall]
here="$(cd "$(dirname "$0")" && pwd)"
# No folder given: the folder this script is in (it's meant to be copied into the Underrail folder).
case " $* " in *" /"*|*" ."*|*" ~"*) ;; *) [ -f "$here/underrail.exe" ] && set -- "$here" "$@" ;; esac
LD_LIBRARY_PATH="$here/linux${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}" exec "$here/linux/underrailfix-patcher.bin" "$@"
SH
chmod +x "$OUT/underrailfix-patcher" "$OUT/linux/underrailfix-patcher.bin"
cp "$HERE/README.md" "$OUT/"
echo "Built $OUT"

if [ "${1:-}" = install ]; then
    if pgrep -ix 'underrail.exe' >/dev/null; then echo "Close Underrail first." >&2; exit 1; fi
    cp "$OUT/UnderrailFix.dll" "$GAME/"
    "$OUT/underrailfix-patcher" "$GAME"
fi
