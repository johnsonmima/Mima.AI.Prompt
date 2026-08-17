namespace SaaFarr.AI.Prompt.Interfaces;

/// <summary>
/// A single unit of message content (text, image, file, thinking, computer-use, etc.).
/// </summary>
public interface IContentPart
{
    /// <summary>Discriminator used in serialization (e.g. "text", "image", "file").</summary>
    string Type { get; }

    /// <summary>Optional prompt-cache hint for providers that support it (e.g. Anthropic).</summary>
    Models.CacheControl? CacheControl { get; }
}
