using Jellyfin.Plugin.JwOrg.Services;
using Xunit;

namespace Jellyfin.Plugin.JwOrg.Tests;

public sealed class JwOrgFolderIdTests
{
    [Fact]
    public void ParseEmptyFolderIdReturnsRoot()
    {
        var folder = JwOrgFolderId.Parse(null);

        Assert.Equal(JwOrgFolderKind.Root, folder.Kind);
    }

    [Fact]
    public void ParseLanguageFolderIdNormalizesLanguageCode()
    {
        var folder = JwOrgFolderId.Parse("lang:e");

        Assert.Equal(JwOrgFolderKind.LanguageRoot, folder.Kind);
        Assert.Equal("E", folder.LanguageCode);
    }

    [Fact]
    public void ParseCategoryFolderIdKeepsCategoryKey()
    {
        var folder = JwOrgFolderId.Parse("cat:d:VODStudio");

        Assert.Equal(JwOrgFolderKind.Category, folder.Kind);
        Assert.Equal("D", folder.LanguageCode);
        Assert.Equal("VODStudio", folder.CategoryKey);
    }
}
