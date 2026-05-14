# Jellyfin JW.ORG Plugin

Unofficial Jellyfin Channel plugin that lets you browse and stream public JW.ORG video media directly from JW CDN URLs — no local downloads, no mirroring.

> This project is not affiliated with, endorsed by, or sponsored by JW.ORG, Watch Tower Bible and Tract Society of Pennsylvania, or any related organisation. Users are responsible for complying with JW.ORG terms of use.

## Features

- Adds a **JW.ORG** Channel to Jellyfin
- Supports multiple language codes (e.g. `E`, `D`, `F`)
- Mirrors JW.ORG video category structure
- Streams public MP4 files directly from JW CDN — nothing is saved locally
- In-memory metadata cache to reduce repeated API calls

## Installation

### Via Jellyfin Plugin Catalog (recommended)

1. Open **Jellyfin Dashboard → Plugins → Repositories**
2. Add the repository URL:
   ```
   https://raw.githubusercontent.com/dosje/jellyfin-plugin-jworg/main/manifest.json
   ```
3. Go to **Catalog**, find **JW.ORG**, and click **Install**
4. Restart Jellyfin

### Manual installation

See [docs/installation.md](docs/installation.md) for step-by-step manual install instructions.

## Configuration

After installing, open **Dashboard → Plugins → JW.ORG**:

| Setting | Description | Default |
|---------|-------------|---------|
| JW language codes | Comma-separated JW language codes, e.g. `E,D,F` | `E` |
| Maximum video height | Optional height cap for MP4 selection, e.g. `720`; leave empty for highest available | *(empty)* |
| Metadata cache duration | How long to cache JW API responses, in hours | `12` |

Save, then browse to the **JW.ORG** Channel in Jellyfin.

## Building from source

Requirements: .NET SDK 9

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

To package the plugin locally:

```bash
dotnet publish src/Jellyfin.Plugin.JwOrg/Jellyfin.Plugin.JwOrg.csproj \
  --configuration Release \
  --output artifacts/plugin
```

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for larger changes.

## License

MIT
