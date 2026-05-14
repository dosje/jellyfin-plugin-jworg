#!/usr/bin/env bash
# Manual helper — the CI pipeline runs this automatically on tag releases.
# Usage: scripts/update-manifest.sh <version> <md5> [changelog]
# Example: scripts/update-manifest.sh 0.1.0 75a055ed5aae28c2890b69f57eb7714f "Initial release."
# Compute MD5: md5sum Jellyfin.Plugin.JwOrg_<version>.zip | awk '{print $1}'
set -euo pipefail

if [ "$#" -lt 2 ]; then
  echo "Usage: scripts/update-manifest.sh <version> <sha256> [changelog]" >&2
  echo "Example: scripts/update-manifest.sh 0.1.0 abc123 \"Initial release.\"" >&2
  exit 1
fi

version="$1"
checksum="$2"
changelog="${3:-See release notes.}"

# Derive repository from git remote
remote_url="$(git remote get-url origin)"
repo_path="$(echo "$remote_url" | sed -E 's|.*github\.com[:/]||; s|\.git$||')"

zip_name="Jellyfin.Plugin.JwOrg_${version}.zip"
source_url="https://github.com/${repo_path}/releases/download/v${version}/${zip_name}"
timestamp="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
target_abi="10.11.0.0"

# Four-part version required by Jellyfin
if [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  version_full="${version}.0"
else
  version_full="$version"
fi

python3 - <<PYEOF
import json, os

path = "manifest.json"
with open(path) as f:
    manifest = json.load(f)

new_version = {
    "version":   "${version_full}",
    "changelog": "${changelog}",
    "targetAbi": "${target_abi}",
    "sourceUrl": "${source_url}",
    "checksum":  "${checksum}",
    "timestamp": "${timestamp}",
}

existing = [
    v for v in manifest[0]["versions"]
    if v.get("checksum", "") not in ("<sha256>", "")
    and v.get("version", "") != "${version_full}"
]
manifest[0]["versions"] = [new_version] + existing

with open(path, "w") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PYEOF

echo "manifest.json updated for v${version} (${version_full})"
echo "  sourceUrl: ${source_url}"
echo "  checksum:  ${checksum}"
