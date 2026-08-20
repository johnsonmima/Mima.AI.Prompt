using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Content;

/// <summary>Plain text content part.</summary>
public sealed class TextPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "text";

    /// <summary>Gets the text value.</summary>
    public string Text { get; }

    /// <summary>Creates a text part.</summary>
    public TextPart(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Creates a text part.</summary>
    public static TextPart Create(string text) => new(text);
}
