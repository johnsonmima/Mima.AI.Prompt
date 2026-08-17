namespace SaaFarr.AI.Prompt.Roles;

/// <summary>
/// Abstract base class representing the role of a message in a conversation with an LLM.
/// Built-in roles are a closed set; invent additional roles with <see cref="Custom"/>.
/// </summary>
/// <remarks>
/// <para>
/// Built-in hierarchy: System &gt; Developer &gt; User &gt; Assistant &gt; Tool &gt; Function.
/// </para>
/// <para>
/// Use <see cref="Custom"/> for application-specific roles (e.g. "critic", "agent").
/// <see cref="Parse"/> only accepts built-in names; use <see cref="ParseOrCreate"/> when
/// reconstructing roles from persisted JSON that may include customs.
/// </para>
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

    /// <summary>
    /// Gets whether this role is one of the six built-in provider-aligned roles.
    /// Custom roles return <c>false</c>.
    /// </summary>
    public virtual bool IsBuiltIn => true;

    /// <summary>
    /// Creates a developer-invented role with the given wire name.
    /// If <paramref name="name"/> matches a built-in role (case-insensitive), the built-in singleton is returned.
    /// </summary>
    /// <param name="name">Role name written to JSON / provider payloads (normalized to lowercase).</param>
    /// <param name="priority">Optional sort priority (default: after all built-ins).</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <returns>A custom role, or a built-in role when the name collides with a known role.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public static MessageRole Custom(string name, int priority = int.MaxValue, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty.", nameof(name));

        var normalized = name.Trim().ToLowerInvariant();

        if (TryParse(normalized, out var builtIn) && builtIn is not null)
            return builtIn;

        return new CustomRole(
            normalized,
            priority,
            string.IsNullOrWhiteSpace(description)
                ? $"Custom role '{normalized}'."
                : description!.Trim());
    }

    /// <summary>
    /// Parses a built-in role name. Does not create custom roles — unknown names throw.
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
                $"Unknown built-in message role: '{roleName}'. Use {nameof(Custom)} or {nameof(ParseOrCreate)} for invented roles.",
                nameof(roleName))
        };
    }

    /// <summary>
    /// Parses a built-in role, or creates a custom role when the name is not built-in.
    /// Prefer this when deserializing persisted prompts that may contain custom roles.
    /// </summary>
    public static MessageRole ParseOrCreate(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));
        }

        return TryParse(roleName, out var builtIn) && builtIn is not null ? builtIn : Custom(roleName);
    }

    /// <summary>
    /// Attempts to parse a built-in role name. Returns false for custom / unknown names.
    /// </summary>
    public static bool TryParse(string? roleName, out MessageRole? role)
    {
        role = null;
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        try
        {
            role = Parse(roleName!);
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
    /// Restricts subclassing to this assembly. Invent roles with <see cref="Custom"/> instead of inheriting.
    /// </summary>
    private protected MessageRole() { }
}
