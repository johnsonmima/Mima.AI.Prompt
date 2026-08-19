namespace Mima.AI.Prompt.Roles;

/// <summary>
/// Function role - represents a function call request or response.
/// Used by some providers for function calling capabilities.
/// </summary>
/// <example>
/// <code>
/// var message = new FunctionMessage("get_weather", "{ \"location\": \"Seattle\", \"temp\": 72 }");
/// </code>
/// </example>
public sealed class FunctionRole : MessageRole
{
    internal static readonly FunctionRole Instance = new();

    /// <inheritdoc />
    public override string Name => "function";

    /// <inheritdoc />
    public override int Priority => 5;

    /// <inheritdoc />
    public override string Description =>
        "Represents a function call request or response for function calling capabilities.";

    private FunctionRole() { }
}
