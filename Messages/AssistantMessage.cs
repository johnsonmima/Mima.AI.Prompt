using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Messages;

/// <summary>
/// Assistant response with optional outbound tool calls, reasoning, and refusal.
/// </summary>
/// <remarks>
/// <para>
/// Body is always represented as <see cref="Message.Parts"/>. String <c>Create</c> is shorthand
/// for a single <see cref="TextPart"/>. Prefer <see cref="Create(IEnumerable{IContentPart}, MessageMetadata?)"/>
/// or <see cref="CreateDetailed"/> when composing multimodal / tool / citation turns.
/// </para>
/// <para>
/// <see cref="Message.Content"/> is the user-visible text projection (concatenated text parts).
/// <see cref="Reasoning"/> mirrors a <see cref="ThinkingPart"/> and is omitted from OpenAI payloads by default.
/// </para>
/// </remarks>
public sealed class AssistantMessage : Message
{
    /// <summary>Gets outbound tool calls (provider <c>tool_calls</c> / <c>tool_use</c>).</summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; }

    /// <summary>Gets extended thinking / reasoning text, when present.</summary>
    public string? Reasoning { get; }

    /// <summary>Gets refusal text when the model declines to answer.</summary>
    public string? Refusal { get; }

    /// <summary>Creates a text assistant message.</summary>
    public AssistantMessage(string content, MessageMetadata? metadata = null, string? id = null)
        : this(content, null, null, null, null, metadata, id, null, null, null) { }

    private AssistantMessage(
        string? content,
        IEnumerable<IContentPart>? parts,
        IEnumerable<ToolCall>? toolCalls,
        string? reasoning,
        string? refusal,
        MessageMetadata? metadata,
        string? id,
        string? name,
        IEnumerable<MessageAnnotation>? annotations,
        CacheControl? cacheControl)
        : base(
            MessageRole.Assistant,
            content,
            MergeThinking(parts, reasoning),
            metadata,
            id,
            name,
            annotations,
            cacheControl,
            allowEmptyBody: (toolCalls is not null && toolCalls.Any())
                || !string.IsNullOrWhiteSpace(refusal)
                || (parts is not null && parts.Any())
                || !string.IsNullOrWhiteSpace(reasoning))
    {
        ToolCalls = toolCalls?.ToList().AsReadOnly() ?? (IReadOnlyList<ToolCall>)Array.Empty<ToolCall>();
        Reasoning = string.IsNullOrWhiteSpace(reasoning) ? null : reasoning;
        Refusal = string.IsNullOrWhiteSpace(refusal) ? null : refusal;
    }

    /// <summary>
    /// Ensures reasoning is also present as a <see cref="ThinkingPart"/> in the parts list.
    /// </summary>
    private static IEnumerable<IContentPart>? MergeThinking(IEnumerable<IContentPart>? parts, string? reasoning)
    {
        if (string.IsNullOrWhiteSpace(reasoning))
            return parts;

        var list = parts?.ToList() ?? new List<IContentPart>();
        if (!list.OfType<ThinkingPart>().Any())
            list.Insert(0, ThinkingPart.Create(reasoning!));
        return list;
    }

    /// <summary>Creates a text assistant message (single text part).</summary>
    public static AssistantMessage Create(string content, MessageMetadata? metadata = null) =>
        CreateDetailed(content: content, metadata: metadata);

    /// <summary>Creates an assistant message from content parts (canonical body path).</summary>
    public static AssistantMessage Create(IEnumerable<IContentPart> parts, MessageMetadata? metadata = null) =>
        CreateDetailed(parts: parts, metadata: metadata);

    /// <summary>Creates an assistant message that requests tool calls.</summary>
    public static AssistantMessage CreateWithToolCalls(
        IEnumerable<ToolCall> toolCalls,
        string? content = null,
        string? reasoning = null,
        MessageMetadata? metadata = null) =>
        CreateDetailed(content: content, toolCalls: toolCalls, reasoning: reasoning, metadata: metadata);

    /// <summary>Creates a refusal assistant message.</summary>
    public static AssistantMessage CreateRefusal(string refusal, MessageMetadata? metadata = null) =>
        CreateDetailed(refusal: refusal, metadata: metadata);

    /// <summary>
    /// Unified factory for assistant turns: text and/or parts, tools, reasoning, refusal, citations, cache.
    /// </summary>
    public static AssistantMessage CreateDetailed(
        string? content = null,
        IEnumerable<IContentPart>? parts = null,
        IEnumerable<ToolCall>? toolCalls = null,
        string? reasoning = null,
        string? refusal = null,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        CacheControl? cacheControl = null) =>
        new(content, parts, toolCalls, reasoning, refusal, metadata, id, name, annotations, cacheControl);
}
