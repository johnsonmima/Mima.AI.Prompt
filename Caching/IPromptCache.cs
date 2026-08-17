using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Caching;

/// <summary>
/// Interface for caching rendered prompts to avoid repeated template rendering
/// and reduce latency for frequently used prompts.
/// </summary>
public interface IPromptCache
{
    /// <summary>Gets a cached prompt by its key.</summary>
    IPrompt? Get(string key);

    /// <summary>Stores a prompt in the cache with a TTL.</summary>
    void Set(string key, IPrompt prompt, TimeSpan ttl);

    /// <summary>Stores a prompt in the cache with the default TTL.</summary>
    void Set(string key, IPrompt prompt);

    /// <summary>Removes a specific entry from the cache.</summary>
    bool Remove(string key);

    /// <summary>Clears all cached prompts.</summary>
    void Clear();

    /// <summary>Checks if a key exists and has not expired.</summary>
    bool Contains(string key);
}
