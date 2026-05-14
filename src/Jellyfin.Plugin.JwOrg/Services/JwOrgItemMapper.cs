using System.Globalization;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <inheritdoc />
public sealed class JwOrgItemMapper : IJwOrgItemMapper
{
    private const string ProviderIdName = "JWORG";

    /// <inheritdoc />
    public ChannelItemResult MapLanguages(IReadOnlyList<(string Code, string Name)> languages)
    {
        var items = languages
            .Select(lang => new ChannelItemInfo
            {
                Id = JwOrgFolderId.Language(lang.Code),
                Name = lang.Name,
                Overview = $"JW.ORG videos in {lang.Name}.",
                Type = ChannelItemType.Folder,
                FolderType = ChannelFolderType.Container,
                ProviderIds = new Dictionary<string, string>
                {
                    [ProviderIdName] = $"language:{lang.Code}"
                }
            })
            .ToArray();

        return new ChannelItemResult
        {
            Items = items,
            TotalRecordCount = items.Length
        };
    }

    /// <inheritdoc />
    public ChannelItemResult MapCategories(IReadOnlyList<JwOrgCategory> categories)
    {
        var items = categories
            .Where(category => !string.IsNullOrWhiteSpace(category.Key))
            .Select(MapCategoryFolder)
            .ToArray();

        return new ChannelItemResult
        {
            Items = items,
            TotalRecordCount = items.Length
        };
    }

    /// <inheritdoc />
    public ChannelItemResult MapCategory(JwOrgCategory category, Configuration.PluginConfiguration configuration)
    {
        var items = new List<ChannelItemInfo>();
        items.AddRange(category.Subcategories.Where(item => !string.IsNullOrWhiteSpace(item.Key)).Select(MapCategoryFolder));
        items.AddRange(category.Media
            .Select(item => MapMediaItem(item, configuration))
            .OfType<ChannelItemInfo>()
            .OrderByDescending(item => item.PremiereDate ?? DateTime.MinValue));

        return new ChannelItemResult
        {
            Items = items,
            TotalRecordCount = category.TotalRecordCount ?? items.Count
        };
    }

    private static ChannelItemInfo MapCategoryFolder(JwOrgCategory category)
    {
        return new ChannelItemInfo
        {
            Id = JwOrgFolderId.Category(category.LanguageCode, category.Key),
            Name = category.Name,
            Overview = category.Description,
            ImageUrl = category.ImageUrl,
            Type = ChannelItemType.Folder,
            FolderType = ChannelFolderType.Container,
            ProviderIds = new Dictionary<string, string>
            {
                [ProviderIdName] = $"category:{category.LanguageCode}:{category.Key}"
            }
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<MediaSourceInfo> MapMediaSources(JwOrgMediaItem mediaItem, Configuration.PluginConfiguration configuration)
    {
        var selectedFile = SelectFile(mediaItem.Files, configuration.MaxVideoHeight);
        if (selectedFile is null)
        {
            return [];
        }

        return [BuildMediaSource(mediaItem, selectedFile)];
    }

    private static ChannelItemInfo? MapMediaItem(JwOrgMediaItem mediaItem, Configuration.PluginConfiguration configuration)
    {
        var selectedFile = SelectFile(mediaItem.Files, configuration.MaxVideoHeight);
        if (selectedFile is null)
        {
            return null;
        }

        var durationTicks = mediaItem.Duration is { Ticks: > 0 } duration ? duration.Ticks : (long?)null;
        var source = BuildMediaSource(mediaItem, selectedFile);

        return new ChannelItemInfo
        {
            Id = StableItemId(mediaItem.LanguageCode, mediaItem.Key),
            Name = mediaItem.Title,
            OriginalTitle = mediaItem.Title,
            Overview = mediaItem.Description,
            ImageUrl = mediaItem.ImageUrl,
            Type = ChannelItemType.Media,
            MediaType = ChannelMediaType.Video,
            ContentType = ChannelMediaContentType.Clip,
            RunTimeTicks = durationTicks,
            PremiereDate = mediaItem.PublishedAt?.UtcDateTime,
            DateCreated = mediaItem.PublishedAt?.UtcDateTime,
            DateModified = mediaItem.PublishedAt?.UtcDateTime ?? DateTime.UtcNow,
            HomePageUrl = $"https://www.jw.org/finder?wtlocale={Uri.EscapeDataString(mediaItem.LanguageCode)}&lank={Uri.EscapeDataString(mediaItem.Key)}",
            OfficialRating = "Unrated",
            Studios = ["JW.ORG"],
            ProviderIds = new Dictionary<string, string>
            {
                [ProviderIdName] = $"media:{mediaItem.LanguageCode}:{mediaItem.Key}"
            },
            MediaSources = [source]
        };
    }

    private static MediaSourceInfo BuildMediaSource(JwOrgMediaItem mediaItem, JwOrgMediaFile selectedFile)
    {
        var durationTicks = mediaItem.Duration is { Ticks: > 0 } duration ? duration.Ticks : (long?)null;
        return new MediaSourceInfo
        {
            Id = StableItemId(mediaItem.LanguageCode, mediaItem.Key),
            Name = selectedFile.Label ?? selectedFile.Height?.ToString(CultureInfo.InvariantCulture) ?? "MP4",
            Path = selectedFile.Url,
            Protocol = MediaProtocol.Http,
            Type = MediaSourceType.Default,
            Container = DeriveContainer(selectedFile),
            IsRemote = true,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
            SupportsTranscoding = true,
            RunTimeTicks = durationTicks,
            Size = selectedFile.SizeBytes,
            Bitrate = selectedFile.Bitrate,
            ETag = mediaItem.Key,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Codec = "h264", Index = 0, IsDefault = true },
                new MediaStream { Type = MediaStreamType.Audio, Codec = "aac",  Index = 1, IsDefault = true }
            ]
        };
    }

    private static JwOrgMediaFile? SelectFile(IReadOnlyList<JwOrgMediaFile> files, int? maxVideoHeight)
    {
        var candidates = files
            .Where(file => Uri.TryCreate(file.Url, UriKind.Absolute, out _))
            .Where(file => IsMp4(file))
            .Where(file => maxVideoHeight is null || file.Height is null || file.Height <= maxVideoHeight)
            .OrderByDescending(file => file.Height ?? 0)
            .ThenByDescending(file => file.Bitrate ?? 0)
            .ToArray();

        return candidates.FirstOrDefault();
    }

    private static bool IsMp4(JwOrgMediaFile file)
    {
        return file.Url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(file.Format, "mp4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(file.Format, "video/mp4", StringComparison.OrdinalIgnoreCase);
    }

    private static string StableItemId(string languageCode, string key)
    {
        return $"jworg:{languageCode.ToUpperInvariant()}:{key}";
    }

    private static IReadOnlyList<string> NormalizeLanguageCodes(IEnumerable<string> languageCodes)
    {
        var normalized = languageCodes
            .Select(languageCode => languageCode.Trim().ToUpperInvariant())
            .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? ["E"] : normalized;
    }

    private static string DeriveContainer(JwOrgMediaFile file)
    {
        if (!string.IsNullOrEmpty(file.Format))
        {
            var fmt = file.Format.ToLowerInvariant();
            if (fmt.Contains("mp4", StringComparison.Ordinal))
            {
                return "mp4";
            }

            if (fmt.Contains("webm", StringComparison.Ordinal))
            {
                return "webm";
            }

            if (fmt.Contains("ts", StringComparison.Ordinal))
            {
                return "ts";
            }
        }

        var ext = Path.GetExtension(file.Url).TrimStart('.').ToLowerInvariant();
        return string.IsNullOrEmpty(ext) ? "mp4" : ext;
    }
}
