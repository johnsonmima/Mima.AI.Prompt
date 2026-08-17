using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Messages;

/// <summary>
/// Message with any role, including custom roles. String factories are shorthand for a single text part.
/// </summary>
public sealed class CustomMessage : Message
{
    /// <summary>Creates a text custom-role message.</summary>
    public CustomMessage(MessageRole role, string content, MessageMetadata? metadata = null, string? id = null)
        : this(role, content, null, metadata, id, null, null, null) { }

    private CustomMessage(
        MessageRole role,
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations,
        CacheControl? cacheControl)
        : base(role ?? throw new ArgumentNullException(nameof(role)), content, parts, metadata, id, name, annotations, cacheControl) { }

    /// <summary>Creates a text custom-role message (single text part via content shorthand).</summary>
    public static CustomMessage Create(MessageRole role, string content, MessageMetadata? metadata = null) =>
        new(role, content, metadata);

    /// <summary>Creates a text message for a custom role name.</summary>
    public static CustomMessage Create(string roleName, string content, MessageMetadata? metadata = null) =>
        Create(MessageRole.Custom(roleName), content, metadata);

    /// <summary>Creates a custom-role message from content parts (canonical path).</summary>
    public static CustomMessage Create(
        MessageRole role,
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        CacheControl? cacheControl = null) =>
        new(role ?? throw new ArgumentNullException(nameof(role)), null, parts, metadata, id, name, annotations, cacheControl);
}
