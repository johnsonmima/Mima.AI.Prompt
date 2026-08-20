namespace Mima.AI.Prompt.Interfaces;

/// <summary>A single unit of message content (text or image).</summary>
public interface IContentPart
{
    /// <summary>Discriminator used in serialization (e.g. "text", "image").</summary>
    string Type { get; }
}
