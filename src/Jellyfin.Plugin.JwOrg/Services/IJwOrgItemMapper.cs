using Jellyfin.Plugin.JwOrg.Configuration;
using MediaBrowser.Controller.Channels;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// Maps JW.ORG domain objects to Jellyfin channel items.
/// </summary>
public interface IJwOrgItemMapper
{
    /// <summary>
    /// Maps configured languages to channel folders using display names.
    /// Key = language code, Value = vernacular display name.
    /// </summary>
    ChannelItemResult MapLanguages(IReadOnlyList<(string Code, string Name)> languages);

    /// <summary>
    /// Maps categories to channel folders.
    /// </summary>
    ChannelItemResult MapCategories(IReadOnlyList<JwOrgCategory> categories);

    /// <summary>
    /// Maps a category response to mixed category folders and video items.
    /// </summary>
    ChannelItemResult MapCategory(JwOrgCategory category, PluginConfiguration configuration);
}
