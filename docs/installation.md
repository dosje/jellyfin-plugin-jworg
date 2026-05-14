# Installation

## Option A — Jellyfin Plugin Catalog (recommended)

This is the easiest method and keeps the plugin up to date automatically.

1. Open **Jellyfin Dashboard → Plugins → Repositories**
2. Click **+** and add the repository URL:
   ```
   https://raw.githubusercontent.com/dosje/jellyfin-plugin-jworg/main/manifest.json
   ```
3. Go to **Catalog**, search for **JW.ORG**, and click **Install**
4. Restart Jellyfin when prompted
5. The **JW.ORG** Channel will appear in your Channels list

## Option B — Manual ZIP install

Use this if you cannot reach the plugin repository from your Jellyfin server, or if you want to install a specific build.

### 1. Download the release ZIP

Download `Jellyfin.Plugin.JwOrg_<version>.zip` from the [Releases](https://github.com/dosje/jellyfin-plugin-jworg/releases) page.

### 2. Locate your Jellyfin plugin directory

| Platform | Path |
|----------|------|
| Linux (package) | `/var/lib/jellyfin/plugins` |
| Docker | `/config/plugins` |
| Windows | `C:\ProgramData\Jellyfin\Server\plugins` |
| macOS | `~/.local/share/jellyfin/plugins` |

### 3. Install the plugin

1. Stop Jellyfin
2. Create a folder named `JW.ORG_<version>` inside the plugin directory  
   Example: `JW.ORG_0.1.0.0`
3. Extract the ZIP contents into that folder
4. Start Jellyfin
5. Open **Dashboard → Plugins** and confirm **JW.ORG** is listed

## Compatibility

| Plugin version | Jellyfin |
|----------------|----------|
| 0.1.x | 10.11.x |

## Uninstalling

- **Catalog install:** Dashboard → Plugins → JW.ORG → Uninstall
- **Manual install:** stop Jellyfin, delete the `JW.ORG_*` folder from the plugin directory, restart Jellyfin
