#!/usr/bin/env bash
# Makes a GitHub release of UnderRailFixes from this machine, the same way every time:
#   ./release.sh 1.2.0 "What changed, in a sentence or a paragraph."
#   ./release.sh 1.2.0 -F notes.md          (description from a file)
# It refuses to run unless the tree is clean, on main or a release/* branch, in sync with origin, and the version
# is new. Then it stamps
# the version, builds from scratch, runs tools/check-release.sh, packages the zip with the licence notices, writes
# the release notes (your description + game version read from the exe + install steps + checksum), tags, pushes
# and publishes with gh. Any failure stops it before anything is pushed.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE"
GAME="${GAME:-$HOME/.local/share/Steam/steamapps/common/Underrail}"
CECIL_DIR=/usr/lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756
REPO=Rhiyo/UnderRailFixes

VERSION="${1:-}"; shift || true
case "${1:-}" in
    -F) DESCRIPTION="$(cat "${2:?-F needs a file}")";;
    "") echo "usage: ./release.sh X.Y.Z \"description\"  |  ./release.sh X.Y.Z -F notes.md" >&2; exit 1;;
    *) DESCRIPTION="$1";;
esac
die() { echo "release: $*" >&2; exit 1; }

# 1. Preconditions.
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || die "version must look like 1.2.3"
# main, or a release/* branch (fixes for an older release line without what's on main since)
BRANCH="$(git branch --show-current)"
case "$BRANCH" in main|release/*) ;; *) die "not on main or a release/* branch";; esac
[ -z "$(git status --porcelain)" ] || die "working tree not clean (commit or stash first)"
git fetch -q origin
git rev-parse -q --verify "origin/$BRANCH" >/dev/null || die "$BRANCH isn't on GitHub yet (git push -u origin $BRANCH)"
[ "$(git rev-parse HEAD)" = "$(git rev-parse "origin/$BRANCH")" ] || die "$BRANCH is not in sync with origin/$BRANCH (pull/push first)"
git rev-parse -q --verify "refs/tags/v$VERSION" >/dev/null && die "tag v$VERSION already exists"
git ls-remote --exit-code --tags origin "refs/tags/v$VERSION" >/dev/null && die "tag v$VERSION already exists on GitHub"
gh auth status >/dev/null 2>&1 || die "gh is not logged in"
if pgrep -ix 'underrail.exe' >/dev/null; then die "close UnderRail first"; fi

# 2. Version stamp (the patcher writes it into the exe; DLL/exe mismatches are caught at launch). It is committed
# only once everything has passed; on any failure before that the file is put back.
VFILE=src/Runtime/Version.cs
WORK="$(mktemp -d)"
committed=0
cleanup() { [ "$committed" = 1 ] || git checkout -q -- "$VFILE"; rm -rf "$WORK"; }
trap cleanup EXIT
sed -i -E "s/(public const string Value = \")[^\"]*(\";)/\1$VERSION\2/" "$VFILE"
grep -q "Value = \"$VERSION\"" "$VFILE" || die "couldn't set the version in $VFILE"

# 3-4. Build from scratch and check.
./build.sh
tools/check-release.sh

# 5. Package.
ORIG="$GAME/underrail.exe"; [ -f "$GAME/underrail.exe.backup" ] && ORIG="$GAME/underrail.exe.backup"
mcs -nologo -out:"$WORK/GameVersion.exe" -r:"$CECIL_DIR/Mono.Cecil.dll" -r:System.Core.dll src/Build/GameVersion.cs
GAME_VERSION="$(MONO_PATH="$CECIL_DIR" mono "$WORK/GameVersion.exe" "$ORIG")" || die "couldn't read the game version"
STEAM_BUILD="$(grep -oP '"buildid"\s+"\K[0-9]+' "$GAME/../../appmanifest_250520.acf" 2>/dev/null || echo unknown)"

NAME="UnderRailFixes-$VERSION"
PKG="$WORK/$NAME"
cp -r dist/UnderrailFix "$PKG"
cp LICENSE "$PKG/LICENSE.txt"
{
    echo "UnderRailFixes includes the following third-party software."
    echo
    echo "=== Mono.Cecil (Mono.Cecil.dll, and inside the Linux patcher) ==="
    echo "https://github.com/jbevain/cecil"
    echo
    cat src/Release/cecil.txt
    echo
    echo "=== Mono runtime and class libraries (inside the Linux patcher: linux/underrailfix-patcher.bin, linux/libmono-native.so) ==="
    echo "https://github.com/mono/mono"
    echo
    cat src/Release/mono.txt
} > "$PKG/THIRD-PARTY-NOTICES.txt"
mkdir -p dist/release
ZIP="dist/release/$NAME.zip"
rm -f "$ZIP"
(cd "$WORK" && zip -qr -y "$HERE/$ZIP" "$NAME")
SHA="$(sha256sum "$ZIP" | cut -d' ' -f1)"

# 6. Release notes.
NOTES="$WORK/notes.md"
cat > "$NOTES" <<EOF
$DESCRIPTION

---

**Built against UnderRail $GAME_VERSION** (Steam build $STEAM_BUILD). On another game version the patcher checks every place it changes first, and stops without changing anything if the game's code moved.

### Install
1. Close the game, extract the zip and copy the contents of \`$NAME\` into the UnderRail folder.
2. Windows: run \`UnderrailFix-Patcher.exe\`. Linux / Steam Deck: run \`./underrailfix-patcher\`.
3. Start the game; settings are in **Options -> Fixes** and \`UnderrailFix.ini\`.

After a game update, run the patcher again. To uninstall, run it with \`--uninstall\`. Details in the [README](https://github.com/$REPO#readme).

SHA-256 (\`$NAME.zip\`): \`$SHA\`
EOF

# 7. Commit the version (if it changed), tag, push, publish.
if [ -n "$(git status --porcelain -- "$VFILE")" ]; then git commit -q -m "Release v$VERSION" -- "$VFILE"; fi
committed=1
git tag -a "v$VERSION" -m "UnderRailFixes $VERSION (UnderRail $GAME_VERSION)"
git push -q origin "$BRANCH" "v$VERSION"
gh release create "v$VERSION" "$ZIP" --repo "$REPO" --title "UnderRailFixes $VERSION" --notes-file "$NOTES"
echo "Released v$VERSION: https://github.com/$REPO/releases/tag/v$VERSION"
