namespace Mima.AI.Prompt.Models;

/// <summary>
/// An outbound tool invocation requested by the assistant.
/// Pair with <see cref="Messages.ToolMessage"/> results via <see cref="Id"/>.
/// </summary>
public sealed class ToolCall
{
    /// <summary>Gets the provider tool call id (must match the subsequent tool result message).</summary>
    public string Id { get; }

    /// <summary>Gets the tool / function name.</summary>
    public string Name { get; }

    /// <summary>Gets the JSON arguments payload.</summary>
    public string ArgumentsJson { get; }

    /// <summary>Creates a tool call.</summary>
    /// <param name="id">Tool call id.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="argumentsJson">JSON arguments (defaults to <c>{}</c>).</param>
    public ToolCall(string id, string name, string argumentsJson = "{}")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Tool call id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool call name cannot be empty.", nameof(name));

        Id = id.Trim();
        Name = name.Trim();
        ArgumentsJson = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;
    }

    /// <summary>Creates a tool call.</summary>
    /// <param name="id">Tool call id.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="argumentsJson">JSON arguments (defaults to <c>{}</c>).</param>
    /// <returns>A new <see cref="ToolCall"/>.</returns>
    public static ToolCall Create(string id, string name, string argumentsJson = "{}") =>
        new(id, name, argumentsJson);
}
