namespace SaaFarr.AI.Prompt.Roles;

/// <summary>
/// Assistant role - represents previous AI responses.
/// Useful for chat history, memory, few-shot examples, testing, and conversation replay.
/// </summary>
/// <example>
/// <code>
/// var message = new AssistantMessage("Dependency injection is a design pattern where...");
/// </code>
/// </example>
public sealed class AssistantRole : MessageRole
{
    internal static readonly AssistantRole Instance = new();

    /// <inheritdoc />
    public override string Name => "assistant";

    /// <inheritdoc />
    public override int Priority => 3;

    /// <inheritdoc />
    public override string Description =>
        "Represents previous AI responses. Used for chat history, memory, and few-shot examples.";

    private AssistantRole() { }
}
