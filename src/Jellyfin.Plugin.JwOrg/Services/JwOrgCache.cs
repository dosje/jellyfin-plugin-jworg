using System.Collections.Concurrent;

namespace Jellyfin.Plugin.JwOrg.Services;

/// <inheritdoc />
public sealed class JwOrgCache : IJwOrgCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan duration, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var existing)
            && existing.ExpiresAtUtc > DateTimeOffset.UtcNow
            && existing.Value is T cached)
        {
            return cached;
        }

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_entries.TryGetValue(key, out existing)
                && existing.ExpiresAtUtc > DateTimeOffset.UtcNow
                && existing.Value is T alreadyCached)
            {
                return alreadyCached;
            }

            var value = await factory(cancellationToken).ConfigureAwait(false);
            _entries[key] = new CacheEntry(DateTimeOffset.UtcNow.Add(duration), value);
            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private sealed record CacheEntry(DateTimeOffset ExpiresAtUtc, object? Value);
}
