namespace SaaFarr.AI.Prompt.Exceptions;

/// <summary>
/// Thrown when prompt validation fails (missing variables, invalid content, etc.).
/// </summary>
public sealed class PromptValidationException : Exception
{
    /// <summary>Gets the list of validation errors.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Gets the list of missing variable names, if applicable.</summary>
    public IReadOnlyList<string> MissingVariables { get; }

    /// <summary>Creates a new validation exception with a single error.</summary>
    public PromptValidationException(string message)
        : base(message)
    {
        Errors = new[] { message };
        MissingVariables = Array.Empty<string>();
    }

    /// <summary>Creates a new validation exception with multiple errors.</summary>
    public PromptValidationException(IEnumerable<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors.ToList().AsReadOnly();
        MissingVariables = Array.Empty<string>();
    }

    /// <summary>Creates a new validation exception for missing variables.</summary>
    public PromptValidationException(IEnumerable<string> missingVariables, string templateName)
        : base($"Template '{templateName}' is missing required variables: {string.Join(", ", missingVariables)}")
    {
        MissingVariables = missingVariables.ToList().AsReadOnly();
        Errors = MissingVariables.Select(v => $"Missing required variable: '{v}'").ToList().AsReadOnly();
    }
}
