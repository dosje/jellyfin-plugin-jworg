namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// Channel folder id type.
/// </summary>
public enum JwOrgFolderKind
{
    /// <summary>
    /// The channel root.
    /// </summary>
    Root,

    /// <summary>
    /// A configured language root.
    /// </summary>
    LanguageRoot,

    /// <summary>
    /// A JW.ORG category.
    /// </summary>
    Category
}
