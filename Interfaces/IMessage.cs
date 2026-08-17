using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Interfaces;

/// <summary>
/// Represents a single unit of communication sent to an LLM.
/// </summary>
/// <remarks>
/// <see cref="Parts"/> is the canonical body. <see cref="Content"/> is the concatenated
/// text of all <c>text</c> parts (convenient for templates, logging, and string-only providers).
/// </remarks>
public interface IMessage
{
    /// <summary>Gets the role of this message in the conversation.</summary>
    MessageRole Role { get; }

    /// <summary>
    /// Gets the text projection of this message (concatenated <c>text</c> parts).
    /// Empty when the body is image-only, tool-call-only, or refusal-only.
    /// </summary>
    string Content { get; }

    /// <summary>Gets structured content parts — the source of truth for the message body.</summary>
    IReadOnlyList<IContentPart> Parts { get; }

    /// <summary>Optional speaker / agent name (multi-agent transcripts; OpenAI <c>name</c> field).</summary>
    string? Name { get; }

    /// <summary>Citations / grounding annotations.</summary>
    IReadOnlyList<MessageAnnotation> Annotations { get; }

    /// <summary>Optional prompt-cache control for the whole message.</summary>
    CacheControl? CacheControl { get; }

    /// <summary>Gets the metadata associated with this message.</summary>
    MessageMetadata Metadata { get; }

    /// <summary>Gets the unique identifier for this message instance.</summary>
    string Id { get; }
}
