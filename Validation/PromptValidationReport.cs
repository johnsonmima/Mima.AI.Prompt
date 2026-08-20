namespace Mima.AI.Prompt.Validation;

/// <summary>
/// The result of prompt validation, containing errors and warnings.
/// </summary>
public sealed class PromptValidationReport
{
    /// <summary>Gets whether the prompt passed validation (no errors).</summary>
    public bool IsValid { get; }

    /// <summary>Gets validation errors (failures that prevent the prompt from being used).</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Gets validation warnings (non-blocking best practice suggestions).</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Gets whether there are any warnings.</summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>Creates a new validation report.</summary>
    public PromptValidationReport(bool isValid, IEnumerable<string>? errors = null, IEnumerable<string>? warnings = null)
    {
        IsValid = isValid;
        Errors = errors?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        Warnings = warnings?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Creates a successful validation report.</summary>
    public static PromptValidationReport Success() => new(true);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid
            ? $"Valid{(HasWarnings ? $" ({Warnings.Count} warning(s))" : "")}"
            : $"Invalid ({Errors.Count} error(s), {Warnings.Count} warning(s))";
}
