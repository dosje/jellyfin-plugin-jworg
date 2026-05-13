using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JwOrg.Configuration;

/// <summary>
/// Stores JW.ORG plugin settings.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the JW language codes to expose as root channel folders.
    /// </summary>
    public string[] LanguageCodes { get; set; } = ["E"];

    /// <summary>
    /// Gets or sets the maximum preferred MP4 height. Null means highest available.
    /// </summary>
    public int? MaxVideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the JW API metadata cache duration in hours.
    /// </summary>
    public int CacheDurationHours { get; set; } = 12;
}
