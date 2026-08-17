using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Messages;

/// <summary>
/// Developer message — framework-level instructions. String factories are shorthand for a single text part.
/// </summary>
public sealed class DeveloperMessage : Message
{
    /// <summary>Creates a text developer message.</summary>
    public DeveloperMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id, null) { }

    private DeveloperMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id,
        CacheControl? cacheControl)
        : base(MessageRole.Developer, content, parts, metadata, id, null, null, cacheControl) { }

    /// <summary>Creates a text developer message (single text part via content shorthand).</summary>
    public static DeveloperMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a developer message from content parts (canonical path).</summary>
    public static DeveloperMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? id = null,
        CacheControl? cacheControl = null) =>
        new(null, parts, metadata, id, cacheControl);
}
