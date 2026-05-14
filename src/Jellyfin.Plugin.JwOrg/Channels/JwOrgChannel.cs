using Jellyfin.Plugin.JwOrg.Configuration;
using Jellyfin.Plugin.JwOrg.Services;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JwOrg.Channels;

/// <summary>
/// Jellyfin Channel implementation for public JW.ORG video content.
/// </summary>
public sealed class JwOrgChannel : IChannel, IRequiresMediaInfoCallback
{
    private readonly IJwOrgClient _client;
    private readonly IJwOrgItemMapper _mapper;
    private readonly ILogger<JwOrgChannel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwOrgChannel"/> class.
    /// </summary>
    /// <param name="client">JW.ORG API client.</param>
    /// <param name="mapper">Channel item mapper.</param>
    /// <param name="logger">Logger.</param>
    public JwOrgChannel(IJwOrgClient client, IJwOrgItemMapper mapper, ILogger<JwOrgChannel> logger)
    {
        _client = client;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "JW.ORG";

    /// <inheritdoc />
    public string Description => "Browse and stream public JW.ORG videos.";

    /// <inheritdoc />
    public string DataVersion => "4";

    /// <inheritdoc />
    public string HomePageUrl => "https://www.jw.org/";

    /// <inheritdoc />
    public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

    /// <inheritdoc />
    public InternalChannelFeatures GetChannelFeatures()
    {
        return new InternalChannelFeatures
        {
            MediaTypes = [ChannelMediaType.Video, ChannelMediaType.Audio],
            ContentTypes = [ChannelMediaContentType.Clip, ChannelMediaContentType.Song],
            MaxPageSize = 100,
            SupportsContentDownloading = false,
            AutoRefreshLevels = 2,
            DefaultSortFields = [ChannelItemSortField.DateCreated, ChannelItemSortField.Name]
        };
    }

    /// <inheritdoc />
    public bool IsEnabledFor(string userId) => true;

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    {
        var assembly = GetType().Assembly;
        var resourceName = $"{assembly.GetName().Name}.Images.logo.png";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return Task.FromResult(new DynamicImageResponse { HasImage = false });
        return Task.FromResult(new DynamicImageResponse
        {
            HasImage = true,
            Format = ImageFormat.Png,
            Stream = stream
        });
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedChannelImages()
    {
        return [ImageType.Primary];
    }

    /// <inheritdoc />
    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var folder = JwOrgFolderId.Parse(query.FolderId);

        try
        {
            if (folder.Kind == JwOrgFolderKind.Root)
            {
                var codes = configuration.LanguageCodes;
                if (codes.Length == 1)
                {
                    var singleCode = codes[0];
                    var topCategories = await _client.GetTopCategoriesAsync(singleCode, configuration, cancellationToken).ConfigureAwait(false);
                    if (topCategories.Count == 0)
                    {
                        _logger.LogWarning("No top categories returned for language {Language}", singleCode);
                    }

                    return _mapper.MapCategories(topCategories);
                }

                var nametasks = codes.Select(async code =>
                    (Code: code, Name: await _client.GetLanguageNameAsync(code, cancellationToken).ConfigureAwait(false)));
                var languages = await Task.WhenAll(nametasks).ConfigureAwait(false);
                return _mapper.MapLanguages(languages);
            }

            if (folder.Kind == JwOrgFolderKind.LanguageRoot)
            {
                var topCategories = await _client.GetTopCategoriesAsync(folder.LanguageCode, configuration, cancellationToken).ConfigureAwait(false);
                if (topCategories.Count == 0)
                {
                    _logger.LogWarning("No top categories returned for language {Language}", folder.LanguageCode);
                }

                return _mapper.MapCategories(topCategories);
            }

            var category = await _client
                .GetCategoryAsync(folder.LanguageCode, folder.CategoryKey, configuration, query.StartIndex ?? 0, query.Limit, cancellationToken)
                .ConfigureAwait(false);

            var result = _mapper.MapCategory(category, configuration);
            foreach (var item in result.Items.Where(i => i.Type == ChannelItemType.Media && string.IsNullOrEmpty(i.ImageUrl)))
            {
                _logger.LogWarning("Media item {Id} ({Name}) has no image URL", item.Id, item.Name);
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch channel items for folder {FolderId}", query.FolderId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
    {
        var parsed = ParseStableItemId(id);
        if (parsed is null)
        {
            _logger.LogWarning("Cannot parse channel item id for media info: {Id}", id);
            return [];
        }

        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var mediaItem = await _client.GetMediaItemAsync(parsed.Value.LanguageCode, parsed.Value.Key, configuration, cancellationToken).ConfigureAwait(false);
        if (mediaItem is null)
        {
            _logger.LogWarning("Media item not found for id {Id}", id);
            return [];
        }

        return _mapper.MapMediaSources(mediaItem, configuration);
    }

    private static (string LanguageCode, string Key)? ParseStableItemId(string id)
    {
        // Format: jworg:{LANG}:{key}
        const string prefix = "jworg:";
        if (!id.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var rest = id[prefix.Length..];
        var colon = rest.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0) return null;
        return (rest[..colon], rest[(colon + 1)..]);
    }
}
