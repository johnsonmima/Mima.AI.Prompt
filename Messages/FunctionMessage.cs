using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Messages;

/// <summary>
/// Legacy function-result message. Prefer <see cref="ToolMessage"/> + assistant <see cref="ToolCall"/> for new code.
/// </summary>
public sealed class FunctionMessage : Message
{
    /// <summary>Gets the function name.</summary>
    public string FunctionName { get; }

    /// <summary>Creates a function result message.</summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="content">Function output.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="id">Optional unique identifier.</param>
    public FunctionMessage(string functionName, string content, MessageMetadata? metadata = null, string? id = null)
        : base(MessageRole.Function, content, metadata: metadata, id: id)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or empty.", nameof(functionName));
        FunctionName = functionName;
    }

    /// <summary>Creates a function result message.</summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="content">Function output.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new <see cref="FunctionMessage"/>.</returns>
    public static FunctionMessage Create(string functionName, string content, MessageMetadata? metadata = null) =>
        new(functionName, content, metadata);
}
