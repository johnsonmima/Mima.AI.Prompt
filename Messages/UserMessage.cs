using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>User message — human input. String factories are a single <see cref="TextPart"/>.</summary>
public sealed class UserMessage : Message
{
    /// <summary>Creates a text user message.</summary>
    public UserMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id, null, null) { }

    private UserMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations)
        : base(MessageRole.User, content, parts, metadata, id, name, annotations) { }

    /// <summary>Creates a text user message.</summary>
    public static UserMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a user message from content parts (text and/or images).</summary>
    public static UserMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? name = null,
        string? id = null,
        IEnumerable<MessageAnnotation>? annotations = null) =>
        new(null, parts, metadata, id, name, annotations);
}
