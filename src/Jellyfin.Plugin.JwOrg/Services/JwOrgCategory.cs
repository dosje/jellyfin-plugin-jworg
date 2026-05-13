namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// JW.ORG category data.
/// </summary>
public sealed record JwOrgCategory(
    string LanguageCode,
    string Key,
    string Name,
    string? Description,
    string? ImageUrl,
    IReadOnlyList<JwOrgCategory> Subcategories,
    IReadOnlyList<JwOrgMediaItem> Media,
    int? TotalRecordCount);
