namespace Mima.AI.Prompt.Roles;

/// <summary>
/// Developer role - sits between system and user, providing additional context or constraints.
/// Vendor mappers may fold this role into a system message if the target API has no developer role.
/// </summary>
/// <example>
/// <code>
/// var message = new DeveloperMessage("Always respond using Markdown. Use C# for code examples.");
/// </code>
/// </example>
public sealed class DeveloperRole : MessageRole
{
    internal static readonly DeveloperRole Instance = new();

    /// <inheritdoc />
    public override string Name => "developer";

    /// <inheritdoc />
    public override int Priority => 1;

    /// <inheritdoc />
    public override string Description =>
        "Provides additional context between system and user. Some providers merge this into system.";

    private DeveloperRole() { }
}
