using Jellyfin.Plugin.JwOrg.Configuration;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// JW.ORG media API client.
/// </summary>
public interface IJwOrgClient
{
    /// <summary>
    /// Gets top-level video categories for a language.
    /// </summary>
    Task<IReadOnlyList<JwOrgCategory>> GetTopCategoriesAsync(string languageCode, PluginConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a category with subcategories and media.
    /// </summary>
    Task<JwOrgCategory> GetCategoryAsync(string languageCode, string categoryKey, PluginConfiguration configuration, int startIndex, int? limit, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single media item by key.
    /// </summary>
    Task<JwOrgMediaItem?> GetMediaItemAsync(string languageCode, string mediaKey, PluginConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the vernacular display name for a language code, falling back to the code itself.
    /// </summary>
    Task<string> GetLanguageNameAsync(string languageCode, CancellationToken cancellationToken);
}
