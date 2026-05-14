# Configuration

Open **Jellyfin Dashboard → Plugins → JW.ORG** after installing the plugin.

## Settings

### JW language codes

Comma-separated list of [JW.ORG language codes](https://www.jw.org/en/languages/). The plugin creates one folder per language in the JW.ORG Channel.

**Examples**

| Value | Languages shown |
|-------|----------------|
| `E` | English only |
| `E,D` | English and German |
| `E,D,F,S` | English, German, French, Spanish |

Leave this at `E` if you only need English. Changes take effect after saving; you may need to refresh the Channel.

### Maximum video height

Optional. Limits the MP4 quality the plugin requests from JW CDN.

| Value | Effect |
|-------|--------|
| *(empty)* | Highest available resolution |
| `720` | Up to 720p |
| `480` | Up to 480p |
| `360` | Up to 360p |

Lower values reduce bandwidth and transcode load on your Jellyfin server.

### Metadata cache duration

How many hours the plugin caches category and video metadata fetched from the JW API.

| Value | Effect |
|-------|--------|
| `0` | No caching — every browse hits the JW API (not recommended) |
| `12` | Default; suitable for most use cases |
| `24` | Reduces API calls further; content updates appear next day |

The cache is stored in memory and cleared on Jellyfin restart.

## How the plugin works

1. On first browse, the plugin calls the public JW.ORG API to fetch video categories and items for each configured language
2. Results are cached in memory for the configured duration
3. When you select a video, Jellyfin receives the direct MP4 URL from JW CDN and handles playback — no video data passes through the plugin or is saved to disk
4. The plugin does not download, mirror, or redistribute any JW.ORG media

## Troubleshooting

**Channel does not appear after install**  
Restart Jellyfin. If it still does not appear, check Dashboard → Plugins that the plugin status is *Active*.

**No videos or categories load**  
Check your Jellyfin server's internet access. The plugin requires outbound HTTPS to `b.jw-cdn.org` and `data.jw-cdn.org`.

**Videos fail to play**  
Your Jellyfin client needs to be able to reach JW CDN directly (or via Jellyfin's streaming proxy). The plugin itself only supplies the MP4 URL; actual playback is handled by Jellyfin.

**Content is stale**  
Reduce or set the cache duration to `0` temporarily, browse the channel once, then restore your preferred value.
