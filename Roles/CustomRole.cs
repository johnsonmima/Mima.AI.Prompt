namespace SaaFarr.AI.Prompt.Roles;

/// <summary>
/// A developer-invented message role that is not part of the built-in LLM wire vocabulary.
/// Created via <see cref="MessageRole.Custom"/>.
/// </summary>
/// <remarks>
/// Custom roles are first-class in the domain model and serialize by name.
/// Many LLM providers only accept built-in roles (system, user, assistant, tool, …);
/// provider adapters emit warnings when custom roles are present but still pass the name through.
/// </remarks>
public sealed class CustomRole : MessageRole
{
    private readonly string _name;
    private readonly int _priority;
    private readonly string _description;

    internal CustomRole(string name, int priority, string description)
    {
        _name = name;
        _priority = priority;
        _description = description;
    }

    /// <inheritdoc />
    public override string Name => _name;

    /// <inheritdoc />
    public override int Priority => _priority;

    /// <inheritdoc />
    public override string Description => _description;

    /// <inheritdoc />
    public override bool IsBuiltIn => false;
}
