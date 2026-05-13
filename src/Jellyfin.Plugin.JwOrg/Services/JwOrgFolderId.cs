namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// Parsed Jellyfin channel folder id.
/// </summary>
public sealed record JwOrgFolderId(JwOrgFolderKind Kind, string LanguageCode, string CategoryKey)
{
    /// <summary>
    /// Parses a channel folder id.
    /// </summary>
    /// <param name="folderId">The folder id supplied by Jellyfin.</param>
    /// <returns>A parsed folder id.</returns>
    public static JwOrgFolderId Parse(string? folderId)
    {
        if (string.IsNullOrWhiteSpace(folderId))
        {
            return new JwOrgFolderId(JwOrgFolderKind.Root, string.Empty, string.Empty);
        }

        var parts = folderId.Split(':', 3, StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && string.Equals(parts[0], "lang", StringComparison.OrdinalIgnoreCase))
        {
            return new JwOrgFolderId(JwOrgFolderKind.LanguageRoot, parts[1].ToUpperInvariant(), string.Empty);
        }

        if (parts.Length == 3 && string.Equals(parts[0], "cat", StringComparison.OrdinalIgnoreCase))
        {
            return new JwOrgFolderId(JwOrgFolderKind.Category, parts[1].ToUpperInvariant(), parts[2]);
        }

        return new JwOrgFolderId(JwOrgFolderKind.Root, string.Empty, string.Empty);
    }

    /// <summary>
    /// Builds a language folder id.
    /// </summary>
    /// <param name="languageCode">JW language code.</param>
    /// <returns>Jellyfin folder id.</returns>
    public static string Language(string languageCode) => $"lang:{languageCode.ToUpperInvariant()}";

    /// <summary>
    /// Builds a category folder id.
    /// </summary>
    /// <param name="languageCode">JW language code.</param>
    /// <param name="categoryKey">JW category key.</param>
    /// <returns>Jellyfin folder id.</returns>
    public static string Category(string languageCode, string categoryKey) => $"cat:{languageCode.ToUpperInvariant()}:{categoryKey}";
}
