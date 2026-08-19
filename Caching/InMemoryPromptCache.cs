using System.Collections.Concurrent;
using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Caching;

/// <summary>
/// Thread-safe in-memory prompt cache with TTL support.
/// For distributed scenarios, implement <see cref="IPromptCache"/> with Redis or similar.
/// </summary>
public sealed class InMemoryPromptCache : IPromptCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;

    /// <summary>Creates a new in-memory cache with the specified default TTL.</summary>
    public InMemoryPromptCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>Gets the number of entries currently in the cache.</summary>
    public int Count => _cache.Count;

    /// <inheritdoc />
    public IPrompt? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired) { _cache.TryRemove(key, out _); return null; }
            entry.IncrementHits();
            return entry.Prompt;
        }
        return null;
    }

    /// <inheritdoc />
    public void Set(string key, IPrompt prompt, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache key cannot be empty.", nameof(key));
        if (prompt is null) throw new ArgumentNullException(nameof(prompt));
        _cache[key] = new CacheEntry(prompt, ttl);
    }

    /// <inheritdoc />
    public void Set(string key, IPrompt prompt) => Set(key, prompt, _defaultTtl);

    /// <inheritdoc />
    public bool Remove(string key) => _cache.TryRemove(key, out _);

    /// <inheritdoc />
    public void Clear() => _cache.Clear();

    /// <inheritdoc />
    public bool Contains(string key)
    {
        if (!_cache.TryGetValue(key, out var entry)) return false;
        if (entry.IsExpired) { _cache.TryRemove(key, out _); return false; }
        return true;
    }

    /// <summary>Removes all expired entries.</summary>
    public void Evict()
    {
        foreach (var key in _cache.Where(kv => kv.Value.IsExpired).Select(kv => kv.Key).ToList())
            _cache.TryRemove(key, out _);
    }

    /// <summary>Gets cache statistics.</summary>
    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();
        return new CacheStatistics(entries.Count, entries.Count(e => e.IsExpired), entries.Sum(e => e.Hits));
    }

    private sealed class CacheEntry
    {
        public IPrompt Prompt { get; }
        public DateTimeOffset ExpiresAt { get; }
        public int Hits { get; private set; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

        public CacheEntry(IPrompt prompt, TimeSpan ttl)
        {
            Prompt = prompt;
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl);
        }

        public void IncrementHits() => Hits++;
    }
}

/// <summary>Cache statistics for monitoring.</summary>
public sealed record CacheStatistics(int TotalEntries, int ExpiredEntries, int TotalHits)
{
    /// <summary>Active (non-expired) entries.</summary>
    public int ActiveEntries => TotalEntries - ExpiredEntries;
}
