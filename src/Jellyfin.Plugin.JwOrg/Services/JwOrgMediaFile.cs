namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// JW.ORG playable media file.
/// </summary>
public sealed record JwOrgMediaFile(
    string Url,
    string? Label,
    string? Format,
    int? Height,
    long? SizeBytes,
    int? Bitrate);
