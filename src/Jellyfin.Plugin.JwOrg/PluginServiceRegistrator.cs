using Jellyfin.Plugin.JwOrg.Channels;
using Jellyfin.Plugin.JwOrg.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JwOrg;

/// <summary>
/// Registers plugin services with Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<IJwOrgClient, JwOrgClient>();
        serviceCollection.AddSingleton<IJwOrgCache, JwOrgCache>();
        serviceCollection.AddSingleton<IJwOrgItemMapper, JwOrgItemMapper>();
        serviceCollection.AddSingleton<IChannel, JwOrgChannel>();
    }
}
