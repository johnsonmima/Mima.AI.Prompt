using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>Developer message — framework-level instructions (mapped to system on OpenAI/Ollama).</summary>
public sealed class DeveloperMessage : Message
{
    /// <summary>Creates a text developer message.</summary>
    public DeveloperMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, metadata, id) { }

    private DeveloperMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        MessageMetadata? metadata,
        string? id)
        : base(MessageRole.Developer, content, parts, metadata, id) { }

    /// <summary>Creates a text developer message.</summary>
    public static DeveloperMessage Create(string content, MessageMetadata? metadata = null) =>
        new(content, metadata);

    /// <summary>Creates a developer message from content parts.</summary>
    public static DeveloperMessage Create(
        IEnumerable<IContentPart> parts,
        MessageMetadata? metadata = null,
        string? id = null) =>
        new(null, parts, metadata, id);
}
