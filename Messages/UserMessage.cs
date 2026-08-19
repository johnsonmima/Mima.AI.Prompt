using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>
/// User message — human input. String factories are shorthand for a single <see cref="Content.TextPart"/>.
/// </summary>
public sealed class UserMessage : Message
{
    /// <summary>Creates a text user message.</summary>
    public UserMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id, null, null, null) { }

    private UserMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations,
        CacheControl? cacheControl)
        : base(MessageRole.User, content, parts, metadata, id, name, annotations, cacheControl) { }

    /// <summary>Creates a text user message (single text part via content shorthand).</summary>
    public static UserMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a user message from content parts (canonical path).</summary>
    /// <param name="parts">Content parts (text, image, file, …).</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="name">Optional speaker name.</param>
    /// <param name="id">Optional id.</param>
    /// <param name="annotations">Optional citations.</param>
    /// <param name="cacheControl">Optional message-level cache control.</param>
    public static UserMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? name = null,
        string? id = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        CacheControl? cacheControl = null) =>
        new(null, parts, metadata, id, name, annotations, cacheControl);
}
