namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// Prompt-cache / ephemeral control for a message or content part.
/// Maps conceptually to Anthropic <c>cache_control</c> and similar provider features.
/// </summary>
public sealed class CacheControl
{
    /// <summary>Cache type (e.g. "ephemeral").</summary>
    public string Type { get; }

    /// <summary>Optional TTL hint (provider-specific).</summary>
    public TimeSpan? Ttl { get; }

    private CacheControl(string type, TimeSpan? ttl)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Cache control type cannot be empty.", nameof(type));
        Type = type.Trim().ToLowerInvariant();
        Ttl = ttl;
    }

    /// <summary>Ephemeral cache breakpoint (Anthropic-style).</summary>
    public static CacheControl Ephemeral(TimeSpan? ttl = null) => new("ephemeral", ttl);

    /// <summary>Custom cache control type.</summary>
    public static CacheControl Custom(string type, TimeSpan? ttl = null) => new(type, ttl);
}
