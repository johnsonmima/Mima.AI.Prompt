using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>System message — AI identity and permanent rules.</summary>
public sealed class SystemMessage : Message
{
    /// <summary>Creates a text system message.</summary>
    public SystemMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id, null, null) { }

    private SystemMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations)
        : base(MessageRole.System, content, parts, metadata, id, name, annotations) { }

    /// <summary>Creates a text system message.</summary>
    public static SystemMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a system message from content parts.</summary>
    public static SystemMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null) =>
        new(null, parts, metadata, id, name, annotations);
}
