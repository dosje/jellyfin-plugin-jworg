using System.Collections.Concurrent;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <inheritdoc />
public sealed class JwOrgCache : IJwOrgCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan duration, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var existing)
            && existing.ExpiresAtUtc > DateTimeOffset.UtcNow
            && existing.Value is T cached)
        {
            return cached;
        }

        var value = await factory(cancellationToken).ConfigureAwait(false);
        _entries[key] = new CacheEntry(DateTimeOffset.UtcNow.Add(duration), value);
        return value;
    }

    private sealed record CacheEntry(DateTimeOffset ExpiresAtUtc, object? Value);
}
