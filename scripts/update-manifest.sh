#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 3 ]; then
  echo "Usage: scripts/update-manifest.sh <github-owner> <version> <sha256>" >&2
  echo "Example: scripts/update-manifest.sh my-user 0.1.0 abc123" >&2
  exit 1
fi

owner="$1"
version="$2"
checksum="$3"
zip_name="Jellyfin.Plugin.JwOrg_${version}.zip"
source_url="https://github.com/${owner}/jellyfin-plugin-jworg/releases/download/v${version}/${zip_name}"

python3 - "$owner" "$version" "$checksum" "$source_url" <<'PY'
import json
import sys
from pathlib import Path

owner, version, checksum, source_url = sys.argv[1:]
path = Path("manifest.json")
manifest = json.loads(path.read_text())
manifest[0]["owner"] = owner
manifest[0]["versions"][0]["version"] = f"{version}.0" if version.count(".") == 2 else version
manifest[0]["versions"][0]["sourceUrl"] = source_url
manifest[0]["versions"][0]["checksum"] = checksum
path.write_text(json.dumps(manifest, indent=2) + "\n")
PY

sed -i.bak "s/owner: \"<owner>\"/owner: \"${owner}\"/" build.yaml
rm -f build.yaml.bak
