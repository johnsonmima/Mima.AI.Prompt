using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>Assistant response with optional outbound tool calls and refusal.</summary>
public sealed class AssistantMessage : Message
{
    /// <summary>Gets outbound tool calls (OpenAI <c>tool_calls</c> / Anthropic <c>tool_use</c>).</summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; }

    /// <summary>Gets refusal text when the model declines to answer.</summary>
    public string? Refusal { get; }

    /// <summary>Creates a text assistant message.</summary>
    public AssistantMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, null, null, metadata, id, null, null) { }

    private AssistantMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        IEnumerable<ToolCall>? toolCalls,
        string? refusal,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations)
        : base(
            MessageRole.Assistant,
            content,
            parts,
            metadata,
            id,
            name,
            annotations,
            allowEmptyBody: (toolCalls is not null && toolCalls.Any())
                || !string.IsNullOrWhiteSpace(refusal)
                || (parts is not null && parts.Any()))
    {
        ToolCalls = toolCalls?.ToList().AsReadOnly() ?? (IReadOnlyList<ToolCall>)Array.Empty<ToolCall>();
        Refusal = string.IsNullOrWhiteSpace(refusal) ? null : refusal;
    }

    /// <summary>Creates a text assistant message.</summary>
    public static AssistantMessage Create(string content, MessageMetadata? metadata = null) =>
        CreateDetailed(content: content, metadata: metadata);

    /// <summary>Creates an assistant message from content parts.</summary>
    public static AssistantMessage Create(IEnumerable<IContentPart> parts, MessageMetadata? metadata = null) =>
        CreateDetailed(parts: parts, metadata: metadata);

    /// <summary>Creates an assistant message that requests tool calls.</summary>
    public static AssistantMessage CreateWithToolCalls(
        IEnumerable<ToolCall> toolCalls,
        string? content = null,
        MessageMetadata? metadata = null) =>
        CreateDetailed(content: content, toolCalls: toolCalls, metadata: metadata);

    /// <summary>Creates a refusal assistant message.</summary>
    public static AssistantMessage CreateRefusal(string refusal, MessageMetadata? metadata = null) =>
        CreateDetailed(refusal: refusal, metadata: metadata);

    /// <summary>Unified factory for assistant turns.</summary>
    public static AssistantMessage CreateDetailed(
        string? content = null,
        IEnumerable<IContentPart>? parts = null,
        IEnumerable<ToolCall>? toolCalls = null,
        string? refusal = null,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null) =>
        new(content, parts, toolCalls, refusal, metadata, id, name, annotations);
}
