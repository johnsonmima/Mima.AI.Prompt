using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Content;

/// <summary>Model reasoning / extended-thinking block (often omitted when sending to providers).</summary>
public sealed class ThinkingPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "thinking";

    /// <summary>Gets the thinking / reasoning text.</summary>
    public string Text { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    /// <summary>Creates a thinking part.</summary>
    /// <param name="text">Non-empty reasoning text.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    public ThinkingPart(string text, CacheControl? cacheControl = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Thinking text cannot be empty.", nameof(text));
        Text = text;
        CacheControl = cacheControl;
    }

    /// <summary>Creates a thinking part.</summary>
    /// <param name="text">Non-empty reasoning text.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ThinkingPart"/>.</returns>
    public static ThinkingPart Create(string text, CacheControl? cacheControl = null) => new(text, cacheControl);
}
