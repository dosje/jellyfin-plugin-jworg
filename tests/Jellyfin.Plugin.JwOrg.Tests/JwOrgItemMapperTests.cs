using Jellyfin.Plugin.JwOrg.Configuration;
using Jellyfin.Plugin.JwOrg.Services;
using MediaBrowser.Controller.Channels;
using Xunit;

namespace Jellyfin.Plugin.JwOrg.Tests;

public sealed class JwOrgItemMapperTests
{
    private readonly JwOrgItemMapper _mapper = new();

    [Fact]
    public void MapLanguagesSetsNameAndIdFromTuple()
    {
        var result = _mapper.MapLanguages([(Code: "E", Name: "English")]);

        Assert.Single(result.Items);
        Assert.Equal("lang:E", result.Items[0].Id);
        Assert.Equal("English", result.Items[0].Name);
    }

    [Fact]
    public void MapCategoryIncludesSubcategoriesAndPlayableMedia()
    {
        var category = new JwOrgCategory(
            "E",
            "VODStudio",
            "JW Broadcasting",
            null,
            null,
            [
                new JwOrgCategory("E", "VODStudioMonthly", "Monthly Programs", null, null, [], [], null)
            ],
            [
                new JwOrgMediaItem(
                    "E",
                    "pub-jwb-202605_1_VIDEO",
                    "May 2026 Broadcast",
                    "Monthly program.",
                    "https://example.test/thumb.jpg",
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    TimeSpan.FromMinutes(60),
                    [
                        new JwOrgMediaFile("https://cdn.example.test/video-720.mp4", "720p", "mp4", 720, 1000, 2500000),
                        new JwOrgMediaFile("https://cdn.example.test/video-1080.mp4", "1080p", "mp4", 1080, 2000, 5000000)
                    ])
            ],
            2);

        var result = _mapper.MapCategory(category, new PluginConfiguration { MaxVideoHeight = 720 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(ChannelItemType.Folder, result.Items[0].Type);
        Assert.Equal(ChannelItemType.Media, result.Items[1].Type);
        Assert.Equal("https://cdn.example.test/video-720.mp4", result.Items[1].MediaSources[0].Path);
    }

    [Fact]
    public void MapCategorySkipsMediaWithoutPlayableUrl()
    {
        var category = new JwOrgCategory(
            "E",
            "VOD",
            "Videos",
            null,
            null,
            [],
            [new JwOrgMediaItem("E", "missing", "Missing", null, null, null, null, [])],
            null);

        var result = _mapper.MapCategory(category, new PluginConfiguration());

        Assert.Empty(result.Items);
    }
}
