#!/usr/bin/env bash
# Builds the mod straight into Elin's Package folder and checks the result.
# Usage: ./deploy.sh [Debug|Release]   (default: Debug)
set -euo pipefail

CONFIGURATION="${1:-Debug}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ELIN_DIR="${ElinGamePath:-/mnt/primary-gaming/SteamLibrary/steamapps/common/Elin}"
MOD_DIR="$ELIN_DIR/Package/Mod_NotMyInventory"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
step()  { printf '\033[36m==> %s\033[0m\n' "$*"; }

case "$CONFIGURATION" in
    Debug|Release) ;;
    *) red "Unknown configuration '$CONFIGURATION' (use Debug or Release)"; exit 1 ;;
esac

step "Checking game folder"
if [[ ! -f "$ELIN_DIR/Elin_Data/Managed/Elin.dll" ]]; then
    red "Elin not found at: $ELIN_DIR"
    red "Is the drive mounted? Or set ElinGamePath to your Elin folder."
    exit 1
fi

step "Checking Elin is not running"
if pgrep -fi 'Elin\.exe' >/dev/null; then
    red "Elin is running. Close the game first (it locks the mod DLL and only loads mods at startup)."
    exit 1
fi

step "Building ($CONFIGURATION)"
started=$(date +%s)
if ! dotnet build "$PROJECT_DIR/NotMyInventory.csproj" --no-incremental -c "$CONFIGURATION" -p:ElinGamePath="$ELIN_DIR" -nologo -v:minimal; then
    red "Build failed - nothing was deployed."
    exit 1
fi

step "Verifying deployed files"
for f in NotMyInventory.dll package.xml; do
    if [[ ! -f "$MOD_DIR/$f" ]]; then
        red "Missing $MOD_DIR/$f"
        exit 1
    fi
done
if (( $(stat -c %Y "$MOD_DIR/NotMyInventory.dll") < started - 1 )); then
    red "NotMyInventory.dll in the mod folder was not updated by this build."
    exit 1
fi

version=$(grep -oP '(?<=Version = ")[^"]+' "$PROJECT_DIR/Plugin.cs" || echo "?")
green "Deployed Not My Inventory $version ($CONFIGURATION) to:"
echo "  $MOD_DIR"
ls -l --time-style=+%H:%M:%S "$MOD_DIR" | tail -n +2 | awk '{print "   ", $6, $7}'
echo
echo "Next: launch Elin, load a save, then check the log with:"
echo "  grep -i \"not my inventory\" \"$ELIN_DIR/BepInEx/LogOutput.log\""
