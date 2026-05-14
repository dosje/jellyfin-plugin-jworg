using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Jellyfin.Plugin.JwOrg.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <inheritdoc />
public sealed class JwOrgClient : IJwOrgClient
{
    private const string ApiBase = "https://b.jw-cdn.org/apis/mediator/v1";
    private readonly HttpClient _httpClient;
    private readonly IJwOrgCache _cache;
    private readonly ILogger<JwOrgClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwOrgClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="cache">API response cache.</param>
    /// <param name="logger">Logger.</param>
    public JwOrgClient(HttpClient httpClient, IJwOrgCache cache, ILogger<JwOrgClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Plugin.JwOrg/0.1");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<JwOrgCategory>> GetTopCategoriesAsync(string languageCode, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var cacheKey = $"top:{normalizedLanguage}";

        return _cache.GetOrCreateAsync<IReadOnlyList<JwOrgCategory>>(cacheKey, CacheDuration(configuration), async ct =>
        {
            using var document = await GetJsonAsync($"{ApiBase}/categories/{Uri.EscapeDataString(normalizedLanguage)}?clientType=www", ct).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("categories", out var categories) || categories.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return categories.EnumerateArray()
                .Select(item => ParseCategory(normalizedLanguage, item, includeChildren: false))
                .Where(category => !string.IsNullOrWhiteSpace(category.Key))
                .ToArray();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<JwOrgCategory> GetCategoryAsync(string languageCode, string categoryKey, PluginConfiguration configuration, int startIndex, int? limit, CancellationToken cancellationToken)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var safeLimit = Math.Clamp(limit ?? 100, 1, 100);
        var safeStartIndex = Math.Max(0, startIndex);
        var cacheKey = $"category:{normalizedLanguage}:{categoryKey}:{safeStartIndex}:{safeLimit}";

        return _cache.GetOrCreateAsync(cacheKey, CacheDuration(configuration), async ct =>
        {
            var url = string.Create(
                CultureInfo.InvariantCulture,
                $"{ApiBase}/categories/{Uri.EscapeDataString(normalizedLanguage)}/{Uri.EscapeDataString(categoryKey)}?clientType=www&detailed=1&offset={safeStartIndex}&limit={safeLimit}&mediaLimit=0");

            using var document = await GetJsonAsync(url, ct).ConfigureAwait(false);
            var categoryElement = document.RootElement.TryGetProperty("category", out var category)
                ? category
                : document.RootElement;

            var parsed = ParseCategory(normalizedLanguage, categoryElement, includeChildren: true);
            var totalRecordCount = ReadInt(document.RootElement, "pagination", "totalCount") ?? parsed.TotalRecordCount;
            var media = await HydrateMediaAsync(normalizedLanguage, parsed.Media, configuration, ct).ConfigureAwait(false);

            return parsed with
            {
                Media = media,
                TotalRecordCount = totalRecordCount
            };
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<JwOrgMediaItem?> GetMediaItemAsync(string languageCode, string mediaKey, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var cacheKey = $"media:{normalizedLanguage}:{mediaKey}";

        return _cache.GetOrCreateAsync<JwOrgMediaItem?>(cacheKey, CacheDuration(configuration), async ct =>
        {
            var url = $"{ApiBase}/media-items/{Uri.EscapeDataString(normalizedLanguage)}/{Uri.EscapeDataString(mediaKey)}?clientType=www";
            using var document = await GetJsonAsync(url, ct).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("media", out var media) || media.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var item = media.EnumerateArray().FirstOrDefault();
            return item.ValueKind == JsonValueKind.Object ? ParseMedia(normalizedLanguage, item) : null;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetLanguageNameAsync(string languageCode, CancellationToken cancellationToken)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        const string cacheKey = "languages:all";
        var names = await _cache.GetOrCreateAsync<IReadOnlyDictionary<string, string>>(
            cacheKey,
            TimeSpan.FromHours(24),
            async ct =>
            {
                using var document = await GetJsonAsync($"{ApiBase}/languages/E/all?clientType=www", ct).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("languages", out var langs) || langs.ValueKind != JsonValueKind.Array)
                {
                    return new Dictionary<string, string>();
                }

                return langs.EnumerateArray()
                    .Where(l => l.TryGetProperty("code", out _))
                    .ToDictionary(
                        l => ReadString(l, "code") ?? string.Empty,
                        l => ReadString(l, "vernacularName") ?? ReadString(l, "name") ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);
            },
            cancellationToken).ConfigureAwait(false);

        return names.TryGetValue(normalized, out var name) && !string.IsNullOrWhiteSpace(name) ? name : normalized;
    }

    private async Task<IReadOnlyList<JwOrgMediaItem>> HydrateMediaAsync(string languageCode, IReadOnlyList<JwOrgMediaItem> media, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var tasks = media.Select(async item =>
        {
            var hydrated = item.Files.Count == 0
                ? await GetMediaItemAsync(languageCode, item.Key, configuration, cancellationToken).ConfigureAwait(false)
                : item;
            return hydrated is not null && hydrated.Files.Count > 0 ? hydrated : null;
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.OfType<JwOrgMediaItem>().ToArray();
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken).ConfigureAwait(false)
                ?? JsonDocument.Parse("{}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "JW.ORG API request failed for {Url}", url);
            throw;
        }
    }

    private static JwOrgCategory ParseCategory(string languageCode, JsonElement element, bool includeChildren)
    {
        var subcategories = includeChildren && element.TryGetProperty("subcategories", out var subs) && subs.ValueKind == JsonValueKind.Array
            ? subs.EnumerateArray().Select(item => ParseCategory(languageCode, item, includeChildren: false)).ToArray()
            : [];

        var media = element.TryGetProperty("media", out var mediaElement) && mediaElement.ValueKind == JsonValueKind.Array
            ? mediaElement.EnumerateArray().Select(item => ParseMedia(languageCode, item)).Where(item => !string.IsNullOrWhiteSpace(item.Key)).ToArray()
            : [];

        return new JwOrgCategory(
            languageCode,
            ReadString(element, "key") ?? string.Empty,
            ReadString(element, "name") ?? ReadString(element, "title") ?? "Untitled",
            ReadString(element, "description"),
            ReadImageUrl(element),
            subcategories,
            media,
            ReadInt(element, "_paginationTotalCount"));
    }

    private static JwOrgMediaItem ParseMedia(string languageCode, JsonElement element)
    {
        return new JwOrgMediaItem(
            languageCode,
            ReadString(element, "key") ?? ReadString(element, "naturalKey") ?? string.Empty,
            ReadString(element, "title") ?? ReadString(element, "name") ?? "Untitled",
            ReadString(element, "description"),
            ReadImageUrl(element),
            ReadDate(element, "firstPublished") ?? ReadDate(element, "published"),
            ReadDuration(element),
            ReadFiles(element));
    }

    private static IReadOnlyList<JwOrgMediaFile> ReadFiles(JsonElement element)
    {
        if (!element.TryGetProperty("files", out var files))
        {
            return [];
        }

        return EnumerateFileElements(files)
            .Select(ReadFile)
            .Where(file => Uri.TryCreate(file.Url, UriKind.Absolute, out _))
            .ToArray();
    }

    private static IEnumerable<JsonElement> EnumerateFileElements(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var file in EnumerateFileElements(item))
                {
                    yield return file;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("progressiveDownloadURL", out _) || element.TryGetProperty("url", out _))
            {
                yield return element;
                yield break;
            }

            if (element.TryGetProperty("file", out var file) && file.ValueKind == JsonValueKind.Object)
            {
                yield return file;
                yield break;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var fileElement in EnumerateFileElements(property.Value))
                {
                    yield return fileElement;
                }
            }
        }
    }

    private static JwOrgMediaFile ReadFile(JsonElement element)
    {
        return new JwOrgMediaFile(
            ReadString(element, "progressiveDownloadURL")
                ?? ReadString(element, "url")
                ?? ReadString(element, "downloadUrl")
                ?? string.Empty,
            ReadString(element, "label"),
            ReadString(element, "format") ?? ReadString(element, "mimetype"),
            ReadInt(element, "height") ?? ReadInt(element, "frameHeight"),
            ReadLong(element, "filesize") ?? ReadLong(element, "size"),
            ReadInt(element, "bitRate") ?? ReadInt(element, "bitrate"));
    }

    private static string? ReadImageUrl(JsonElement element)
    {
        if (!element.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Object)
        {
            return ReadString(element, "imageUrl") ?? ReadString(element, "thumbnailUrl");
        }

        foreach (var name in new[] { "wss", "lsr", "sqr", "pnr", "lss", "wide", "thumbnail" })
        {
            if (images.TryGetProperty(name, out var image))
            {
                if (image.ValueKind == JsonValueKind.String)
                {
                    return image.GetString();
                }

                if (image.ValueKind == JsonValueKind.Object)
                {
                    var directUrl = ReadString(image, "url");
                    if (directUrl is not null) return directUrl;
                    // Nested size variants: lg > md > sm
                    foreach (var size in new[] { "lg", "md", "sm" })
                    {
                        if (image.TryGetProperty(size, out var sizeObj) && sizeObj.ValueKind == JsonValueKind.Object)
                        {
                            var sizeUrl = ReadString(sizeObj, "url");
                            if (sizeUrl is not null) return sizeUrl;
                        }
                    }
                }
            }
        }

        return images.EnumerateObject().Select(property => property.Value).Select(value =>
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Object => ReadString(value, "url"),
                _ => null
            };
        }).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static TimeSpan ReadDuration(JsonElement element)
    {
        var seconds = ReadDouble(element, "duration") ?? ReadDouble(element, "durationSeconds") ?? 0;
        return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "E" : languageCode.Trim().ToUpperInvariant();
    }

    private static TimeSpan CacheDuration(PluginConfiguration configuration)
    {
        return TimeSpan.FromHours(Math.Clamp(configuration.CacheDurationHours, 1, 168));
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static int? ReadInt(JsonElement element, string objectName, string propertyName)
    {
        return element.TryGetProperty(objectName, out var child) && child.ValueKind == JsonValueKind.Object
            ? ReadInt(child, propertyName)
            : null;
    }

    private static long? ReadLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static DateTimeOffset? ReadDate(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }
}
