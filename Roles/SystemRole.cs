namespace Mima.AI.Prompt.Roles;

/// <summary>
/// System role - defines the AI's identity, behavior, and permanent rules.
/// This is the highest priority instruction and should contain rules
/// rather than user-specific information.
/// </summary>
/// <example>
/// <code>
/// var message = new SystemMessage("You are a helpful assistant. Always be concise.");
/// </code>
/// </example>
public sealed class SystemRole : MessageRole
{
    internal static readonly SystemRole Instance = new();

    /// <inheritdoc />
    public override string Name => "system";

    /// <inheritdoc />
    public override int Priority => 0;

    /// <inheritdoc />
    public override string Description =>
        "Defines the AI's identity, behavior, and permanent rules. Highest priority instruction.";

    private SystemRole() { }
}
