using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Interfaces;

/// <summary>A single unit of communication sent to an LLM.</summary>
/// <remarks>
/// <see cref="Parts"/> is the canonical body. <see cref="Content"/> is the concatenated
/// text of all <c>text</c> parts.
/// </remarks>
public interface IMessage
{
    /// <summary>Gets the role of this message in the conversation.</summary>
    MessageRole Role { get; }

    /// <summary>
    /// Gets the text projection (concatenated <c>text</c> parts).
    /// Empty when the body is image-only, tool-call-only, or refusal-only.
    /// </summary>
    string Content { get; }

    /// <summary>Gets structured content parts — the source of truth for the message body.</summary>
    IReadOnlyList<IContentPart> Parts { get; }

    /// <summary>Optional speaker name (OpenAI <c>name</c> field).</summary>
    string? Name { get; }

    /// <summary>Citations / grounding annotations.</summary>
    IReadOnlyList<MessageAnnotation> Annotations { get; }

    /// <summary>Gets the metadata associated with this message.</summary>
    MessageMetadata Metadata { get; }

    /// <summary>Gets the unique identifier for this message instance.</summary>
    string Id { get; }
}
