namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// JW.ORG media item (video or audio).
/// </summary>
public sealed record JwOrgMediaItem(
    string LanguageCode,
    string Key,
    string Title,
    string? Description,
    string? ImageUrl,
    DateTimeOffset? PublishedAt,
    TimeSpan? Duration,
    IReadOnlyList<JwOrgMediaFile> Files,
    string MediaType = "video");
