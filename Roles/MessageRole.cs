namespace Mima.AI.Prompt.Roles;

/// <summary>
/// Abstract base class representing the role of a message in a conversation with an LLM.
/// Roles are a closed set aligned with chat APIs: system, developer, user, assistant, tool, function.
/// </summary>
/// <remarks>
/// Built-in hierarchy: System &gt; Developer &gt; User &gt; Assistant &gt; Tool &gt; Function.
/// </remarks>
public abstract class MessageRole : IEquatable<MessageRole>
{
    /// <summary>System role - defines the AI's identity, behavior, and permanent rules.</summary>
    public static readonly MessageRole System = SystemRole.Instance;

    /// <summary>Developer role - provides additional context between system and user.</summary>
    public static readonly MessageRole Developer = DeveloperRole.Instance;

    /// <summary>User role - represents the human user's input and intent.</summary>
    public static readonly MessageRole User = UserRole.Instance;

    /// <summary>Assistant role - represents previous AI responses.</summary>
    public static readonly MessageRole Assistant = AssistantRole.Instance;

    /// <summary>Tool role - represents output from a tool invocation.</summary>
    public static readonly MessageRole Tool = ToolRole.Instance;

    /// <summary>Function role - represents a function call request or response.</summary>
    public static readonly MessageRole Function = FunctionRole.Instance;

    /// <summary>
    /// Gets the canonical string name of this role (e.g., "system", "user", "assistant").
    /// Used for serialization and provider mapping.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the priority of this role in the prompt hierarchy.
    /// Lower values indicate higher priority (System = 0 is highest).
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    /// Gets a human-readable description of this role's purpose.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>Gets whether this role is a built-in chat role. Always <c>true</c>.</summary>
    public virtual bool IsBuiltIn => true;

    /// <summary>
    /// Parses a built-in role name. Unknown names throw.
    /// </summary>
    /// <param name="roleName">The role name to parse (case-insensitive).</param>
    /// <returns>The matching built-in <see cref="MessageRole"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the role name is not a built-in role.</exception>
    public static MessageRole Parse(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));
        }

        return roleName.Trim().ToLowerInvariant() switch
        {
            "system" => System,
            "developer" => Developer,
            "user" => User,
            "assistant" => Assistant,
            "tool" => Tool,
            "function" => Function,
            _ => throw new ArgumentException(
                $"Unknown message role: '{roleName}'. Supported: system, developer, user, assistant, tool, function.",
                nameof(roleName))
        };
    }

    /// <summary>
    /// Attempts to parse a built-in role name. Returns false for unknown names.
    /// </summary>
    public static bool TryParse(string? roleName, out MessageRole? role)
    {
        role = null;
        if (roleName is not { } name || string.IsNullOrWhiteSpace(name))
            return false;

        try
        {
            role = Parse(name);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Returns all built-in message roles (does not include custom roles).</summary>
    public static IReadOnlyList<MessageRole> All =>
    [
        System, Developer, User, Assistant, Tool, Function
    ];

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    public bool Equals(MessageRole? other) => other is not null && Name == other.Name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MessageRole role && Equals(role);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();

    /// <summary>Equality operator.</summary>
    public static bool operator ==(MessageRole? left, MessageRole? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(MessageRole? left, MessageRole? right) => !(left == right);

    /// <summary>
    /// Restricts subclassing to this assembly.
    /// </summary>
    private protected MessageRole() { }
}
