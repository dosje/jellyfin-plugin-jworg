using System.Net;
using Jellyfin.Plugin.JwOrg.Configuration;
using Jellyfin.Plugin.JwOrg.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.JwOrg.Tests;

public sealed class JwOrgClientTests
{
    [Fact]
    public async Task GetCategoryHydratesMediaFilesFromMediaItemEndpoint()
    {
        var handler = new FakeHandler(request =>
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/categories/E/VODStudio", StringComparison.Ordinal))
            {
                return """
                {
                  "category": {
                    "key": "VODStudio",
                    "name": "JW Broadcasting",
                    "subcategories": [{ "key": "VODStudioMonthly", "name": "Monthly Programs" }],
                    "media": [{ "key": "pub-jwb-202605_1_VIDEO", "title": "May 2026 Broadcast" }]
                  },
                  "pagination": { "totalCount": 1 }
                }
                """;
            }

            return """
            {
              "media": [{
                "key": "pub-jwb-202605_1_VIDEO",
                "title": "May 2026 Broadcast",
                "duration": 3600,
                "files": {
                  "MP4": [{ "file": { "url": "https://cdn.example.test/video.mp4", "height": 720, "mimetype": "video/mp4" } }]
                }
              }]
            }
            """;
        });

        var client = new JwOrgClient(new HttpClient(handler), new JwOrgCache(), NullLogger<JwOrgClient>.Instance);

        var category = await client.GetCategoryAsync("E", "VODStudio", new PluginConfiguration(), 0, 100, CancellationToken.None);

        Assert.Single(category.Media);
        Assert.Single(category.Media[0].Files);
        Assert.Equal("https://cdn.example.test/video.mp4", category.Media[0].Files[0].Url);
        Assert.Equal(1, category.TotalRecordCount);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _respond;

        public FakeHandler(Func<HttpRequestMessage, string> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_respond(request))
            });
        }
    }
}
