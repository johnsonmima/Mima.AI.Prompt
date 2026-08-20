using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>Tool result message paired with an assistant <see cref="ToolCall"/> via <see cref="ToolCallId"/>.</summary>
public sealed class ToolMessage : Message
{
    /// <summary>Gets the ID of the tool call this message responds to.</summary>
    public string ToolCallId { get; }

    /// <summary>Creates a tool result message.</summary>
    /// <param name="toolCallId">Matching tool call id from the assistant turn.</param>
    /// <param name="content">Tool output (often JSON).</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="id">Optional unique identifier.</param>
    public ToolMessage(string toolCallId, string content, MessageMetadata? metadata = null, string? id = null)
        : base(MessageRole.Tool, content, metadata: metadata, id: id)
    {
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or empty.", nameof(toolCallId));
        ToolCallId = toolCallId;
    }

    /// <summary>Creates a tool result message.</summary>
    /// <param name="toolCallId">Matching tool call id.</param>
    /// <param name="content">Tool output.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new <see cref="ToolMessage"/>.</returns>
    public static ToolMessage Create(string toolCallId, string content, MessageMetadata? metadata = null) =>
        new(toolCallId, content, metadata);
}
