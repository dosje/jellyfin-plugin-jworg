namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// JW.ORG video media data.
/// </summary>
public sealed record JwOrgMediaItem(
    string LanguageCode,
    string Key,
    string Title,
    string? Description,
    string? ImageUrl,
    DateTimeOffset? PublishedAt,
    TimeSpan? Duration,
    IReadOnlyList<JwOrgMediaFile> Files);
