using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>
/// System message — AI identity and permanent rules.
/// String factories are shorthand for a single <see cref="Content.TextPart"/>.
/// </summary>
public sealed class SystemMessage : Message
{
    /// <summary>Creates a text system message.</summary>
    public SystemMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id, null, null, null) { }

    private SystemMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations,
        CacheControl? cacheControl)
        : base(MessageRole.System, content, parts, metadata, id, name, annotations, cacheControl) { }

    /// <summary>Creates a text system message (single text part via content shorthand).</summary>
    public static SystemMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a system message from content parts (canonical path).</summary>
    /// <param name="parts">Content parts.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="id">Optional id.</param>
    /// <param name="name">Optional speaker name.</param>
    /// <param name="annotations">Optional citations.</param>
    /// <param name="cacheControl">Optional message-level cache control.</param>
    public static SystemMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        CacheControl? cacheControl = null) =>
        new(null, parts, metadata, id, name, annotations, cacheControl);
}
