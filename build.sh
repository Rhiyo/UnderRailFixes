#!/usr/bin/env bash
# Builds the UnderrailFix release into dist/:
#   UnderrailFix.dll               runtime fixes, loaded by the patched game (all platforms)
#   UnderrailFix-Patcher.exe       Windows patcher (.NET Framework 4, built into Windows) + Mono.Cecil.dll
#   underrailfix-patcher           Linux patcher (single binary with the Mono runtime bundled)
# and the experimental 64-bit download into dist/UnderrailFix-64bit/ (UnderrailFix-Patcher-64bit.exe, FNA, ...).
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
# NVorbis 0.8.6 (Ms-PL, no dependencies): the Ogg Vorbis decoder for 64-bit music (Music.cs). Fetched from NuGet
# once and checked against a known hash, so the repo needn't hold a binary.
NVORBIS="$HERE/lib/NVorbis.dll"
NVORBIS_SHA=dd049b810f39240a5405ff9eda8525c6b74065f1a032a1fb8350762bd1be9c3b
if [ ! -f "$NVORBIS" ]; then
    mkdir -p "$HERE/lib" "$HERE/build/nvorbis"
    curl -fsSL https://www.nuget.org/api/v2/package/NVorbis/0.8.6 -o "$HERE/build/nvorbis/pkg.zip"
    unzip -qo "$HERE/build/nvorbis/pkg.zip" lib/net35/NVorbis.dll -d "$HERE/build/nvorbis"
    cp "$HERE/build/nvorbis/lib/net35/NVorbis.dll" "$NVORBIS"
fi
echo "$NVORBIS_SHA  $NVORBIS" | sha256sum -c --quiet - || { echo "NVorbis.dll doesn't match the expected hash" >&2; rm -f "$NVORBIS"; exit 1; }
# The game's SFML.Net bindings, for the 32-bit music path (the backup when the local copy was patched for 64-bit).
SFML=""
for lib in sfmlnet-audio-2 sfmlnet-window-2; do
    f="$GAME/$lib.dll"; [ -f "$f.backup" ] && f="$f.backup"; SFML="$SFML -r:$f"
done
mcs -nologo -optimize+ -target:library -out:"$OUT/UnderrailFix.dll" -r:"$REF" -r:"$FNA" -r:"$NVORBIS" $SFML -r:System.Core.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:System.Configuration.dll "$HERE"/src/Runtime/*.cs
mcs -nologo -out:"$HERE/build/Retarget.exe" -r:"$CECIL" -r:System.Core.dll "$HERE/src/Build/Retarget.cs"
MONO_PATH="$(dirname "$CECIL")" mono "$HERE/build/Retarget.exe" "$OUT/UnderrailFix.dll" "$REF"
cp "$CECIL" "$OUT/"

# Experimental 64-bit (its own download, dist/UnderrailFix-64bit): the runtime plus a copy pointed at FNA, FNA.dll
# with its native libraries in x64/ (tools/fetch-fna.sh fills lib/fna once), the music decoder and the 32-bit Steam helper.
OUT64="$HERE/dist/UnderrailFix-64bit"
mkdir -p "$OUT64/x64"
cp "$OUT/UnderrailFix.dll" "$CECIL" "$NVORBIS" "$OUT64/"
# The helper also runs GOG Galaxy for the GOG version: compiled against the game's GalaxyCSharp.dll (loaded from the game
# folder at run time, only on GOG).
mcs -nologo -optimize+ -platform:x86 -out:"$OUT64/UnderrailSteamHost.exe" -r:"$GAME/GalaxyCSharp.dll" "$HERE/src/SteamHost/SteamHost.cs"
[ -f "$HERE/lib/fna/FNA.dll" ] || "$HERE/tools/fetch-fna.sh"
cp "$HERE/lib/fna/FNA.dll" "$OUT64/"
cp "$HERE"/lib/fna/x64/*.dll "$OUT64/x64/"
MONO_PATH="$(dirname "$CECIL")" mono "$HERE/build/Retarget.exe" --fna "$OUT/UnderrailFix.dll" "$OUT64/FNA.dll" "$OUT64/UnderrailFix64.dll"

# The patchers: Windows (.NET Framework 4) and Linux. The Linux one is the patcher, Cecil and the Mono runtime bundled
# into one executable (no Mono install needed), plus libmono-native.so (System.Native: file and console I/O), which
# mkbundle --simple can't load from inside the bundle. The launch script puts its own folder on the library path;
# src/Bundle/mono.config maps System.Native to the file.
patchers() {     # <dist folder> <Windows exe name> <Linux name> [mcs flags]
    local dir="$1" exe="$2" name="$3"; shift 3
    mcs -nologo -optimize+ "$@" -out:"$dir/$exe" -r:"$CECIL" -r:System.Core.dll "$HERE"/src/Installer/*.cs
    mkdir -p "$dir/linux"
    mkbundle --simple --deps --machine-config /etc/mono/4.5/machine.config --config "$HERE/src/Bundle/mono.config" \
        -L "$(dirname "$CECIL")" -o "$dir/linux/$name.bin" "$dir/$exe" "$dir/Mono.Cecil.dll" >/dev/null
    cp -L /usr/lib/libmono-native.so "$dir/linux/libmono-native.so"
    cat > "$dir/$name" <<SH
#!/bin/sh
# Linux patcher for UnderRailFixes. Usage: ./$name [game folder] [--uninstall]
here="\$(cd "\$(dirname "\$0")" && pwd)"
# No folder given: the folder this script is in (it's meant to be copied into the Underrail folder).
case " \$* " in *" /"*|*" ."*|*" ~"*) ;; *) [ -f "\$here/underrail.exe" ] && set -- "\$here" "\$@" ;; esac
LD_LIBRARY_PATH="\$here/linux\${LD_LIBRARY_PATH:+:\$LD_LIBRARY_PATH}" exec "\$here/linux/$name.bin" "\$@"
SH
    chmod +x "$dir/$name" "$dir/linux/$name.bin"
    cp "$HERE/README.md" "$dir/"
}
patchers "$OUT" UnderrailFix-Patcher.exe underrailfix-patcher
patchers "$OUT64" UnderrailFix-Patcher-64bit.exe underrailfix-patcher-64bit -define:EXPERIMENTAL_64BIT
echo "Built $OUT and $OUT64"

if [ "${1:-}" = install ]; then
    if pgrep -ix 'underrail.exe' >/dev/null; then echo "Close Underrail first." >&2; exit 1; fi
    cp "$OUT/UnderrailFix.dll" "$GAME/"
    "$OUT/underrailfix-patcher" "$GAME"
fi
