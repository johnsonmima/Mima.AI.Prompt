namespace Mima.AI.Prompt.Models;

/// <summary>
/// Represents a variable placeholder discovered in a template.
/// Carries metadata about the variable for validation and documentation.
/// </summary>
public sealed class PromptVariable
{
    /// <summary>Gets the variable name (as it appears in the template without braces).</summary>
    public string Name { get; }

    /// <summary>Gets the description of what this variable represents.</summary>
    public string? Description { get; }

    /// <summary>Gets the default value for this variable (null means required).</summary>
    public object? DefaultValue { get; }

    /// <summary>Gets whether this variable is required (has no default value).</summary>
    public bool IsRequired { get; }

    /// <summary>Gets the expected type hint for the variable value.</summary>
    public string? TypeHint { get; }

    /// <summary>Gets example values for documentation.</summary>
    public IReadOnlyList<string> Examples { get; }

    /// <summary>Creates a new prompt variable definition.</summary>
    public PromptVariable(
        string name,
        string? description = null,
        object? defaultValue = null,
        bool isRequired = true,
        string? typeHint = null,
        IEnumerable<string>? examples = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

        Name = name;
        Description = description;
        DefaultValue = defaultValue;
        IsRequired = isRequired;
        TypeHint = typeHint;
        Examples = examples?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Creates a required variable with a name.</summary>
    public static PromptVariable Required(string name, string? description = null) =>
        new(name, description, isRequired: true);

    /// <summary>Creates an optional variable with a default value.</summary>
    public static PromptVariable Optional(string name, object defaultValue, string? description = null) =>
        new(name, description, defaultValue, isRequired: false);
}
