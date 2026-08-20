namespace Mima.AI.Prompt.Roles;

/// <summary>
/// User role - represents the human user's input and intent.
/// Contains the actual request or question being asked.
/// </summary>
/// <example>
/// <code>
/// var message = new UserMessage("Explain dependency injection in simple terms.");
/// </code>
/// </example>
public sealed class UserRole : MessageRole
{
    internal static readonly UserRole Instance = new();

    /// <inheritdoc />
    public override string Name => "user";

    /// <inheritdoc />
    public override int Priority => 2;

    /// <inheritdoc />
    public override string Description =>
        "Represents the human user's input and intent. Contains the actual request.";

    private UserRole() { }
}
