namespace Jellyfin.Plugin.JwOrg.Services;

/// <summary>
/// Small in-memory cache for JW API responses.
/// </summary>
public interface IJwOrgCache
{
    /// <summary>
    /// Gets a cached value or creates and caches a fresh value.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan duration, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken);
}
