namespace SaaFarr.AI.Prompt.Roles;

/// <summary>
/// Tool role - represents the output of a tool or function call.
/// Used in agentic workflows where the AI can invoke external tools.
/// </summary>
/// <example>
/// <code>
/// var message = new ToolMessage("tool_call_123", "{ \"result\": \"search completed\" }");
/// </code>
/// </example>
public sealed class ToolRole : MessageRole
{
    internal static readonly ToolRole Instance = new();

    /// <inheritdoc />
    public override string Name => "tool";

    /// <inheritdoc />
    public override int Priority => 4;

    /// <inheritdoc />
    public override string Description =>
        "Represents output from a tool invocation in agentic workflows.";

    private ToolRole() { }
}
