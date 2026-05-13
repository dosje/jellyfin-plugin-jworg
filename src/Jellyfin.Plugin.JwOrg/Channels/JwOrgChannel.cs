using Jellyfin.Plugin.JwOrg.Configuration;
using Jellyfin.Plugin.JwOrg.Services;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;

namespace Jellyfin.Plugin.JwOrg.Channels;

/// <summary>
/// Jellyfin Channel implementation for public JW.ORG video content.
/// </summary>
public sealed class JwOrgChannel : IChannel
{
    private readonly IJwOrgClient _client;
    private readonly IJwOrgItemMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwOrgChannel"/> class.
    /// </summary>
    /// <param name="client">JW.ORG API client.</param>
    /// <param name="mapper">Channel item mapper.</param>
    public JwOrgChannel(IJwOrgClient client, IJwOrgItemMapper mapper)
    {
        _client = client;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public string Name => "JW.ORG";

    /// <inheritdoc />
    public string Description => "Browse and stream public JW.ORG videos.";

    /// <inheritdoc />
    public string DataVersion => "1";

    /// <inheritdoc />
    public string HomePageUrl => "https://www.jw.org/";

    /// <inheritdoc />
    public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

    /// <inheritdoc />
    public InternalChannelFeatures GetChannelFeatures()
    {
        return new InternalChannelFeatures
        {
            MediaTypes = [ChannelMediaType.Video],
            ContentTypes = [ChannelMediaContentType.Clip],
            MaxPageSize = 100,
            SupportsContentDownloading = false,
            AutoRefreshLevels = 2
        };
    }

    /// <inheritdoc />
    public bool IsEnabledFor(string userId) => true;

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DynamicImageResponse());
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedChannelImages()
    {
        return [];
    }

    /// <inheritdoc />
    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var folder = JwOrgFolderId.Parse(query.FolderId);

        if (folder.Kind == JwOrgFolderKind.Root)
        {
            return _mapper.MapLanguages(configuration.LanguageCodes);
        }

        if (folder.Kind == JwOrgFolderKind.LanguageRoot)
        {
            var topCategories = await _client.GetTopCategoriesAsync(folder.LanguageCode, configuration, cancellationToken).ConfigureAwait(false);
            return _mapper.MapCategories(topCategories);
        }

        var category = await _client
            .GetCategoryAsync(folder.LanguageCode, folder.CategoryKey, configuration, query.StartIndex ?? 0, query.Limit, cancellationToken)
            .ConfigureAwait(false);

        return _mapper.MapCategory(category, configuration);
    }
}
