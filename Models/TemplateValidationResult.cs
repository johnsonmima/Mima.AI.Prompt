namespace Mima.AI.Prompt.Models;

/// <summary>
/// Represents the result of validating a template's variables.
/// </summary>
public sealed class TemplateValidationResult
{
    /// <summary>Gets whether all required variables were provided.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the list of missing variable names.</summary>
    public IReadOnlyList<string> MissingVariables { get; }

    /// <summary>Gets the list of extra variables provided that aren't in the template.</summary>
    public IReadOnlyList<string> ExtraVariables { get; }

    /// <summary>Gets validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Creates a new validation result.</summary>
    public TemplateValidationResult(
        bool isValid,
        IEnumerable<string>? missingVariables = null,
        IEnumerable<string>? extraVariables = null,
        IEnumerable<string>? errors = null)
    {
        IsValid = isValid;
        MissingVariables = missingVariables?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        ExtraVariables = extraVariables?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        Errors = errors?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Creates a successful validation result.</summary>
    public static TemplateValidationResult Success() => new(true);

    /// <summary>Creates a failed validation result with missing variables.</summary>
    public static TemplateValidationResult Failure(IEnumerable<string> missingVariables) =>
        new(false, missingVariables, errors: missingVariables.Select(v => $"Missing required variable: '{v}'"));

    /// <summary>Creates a failed validation result with errors.</summary>
    public static TemplateValidationResult FromErrors(IEnumerable<string> errors) =>
        new(false, errors: errors);
}
