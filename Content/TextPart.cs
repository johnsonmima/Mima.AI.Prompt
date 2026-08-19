using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Content;

/// <summary>Plain text content part.</summary>
public sealed class TextPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "text";

    /// <summary>Gets the text value.</summary>
    public string Text { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    /// <summary>Creates a text part.</summary>
    /// <param name="text">The text content (may be empty).</param>
    /// <param name="cacheControl">Optional cache control for this part.</param>
    public TextPart(string text, CacheControl? cacheControl = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        CacheControl = cacheControl;
    }

    /// <summary>Creates a text part.</summary>
    /// <param name="text">The text content.</param>
    /// <param name="cacheControl">Optional cache control for this part.</param>
    /// <returns>A new <see cref="TextPart"/>.</returns>
    public static TextPart Create(string text, CacheControl? cacheControl = null) => new(text, cacheControl);
}
