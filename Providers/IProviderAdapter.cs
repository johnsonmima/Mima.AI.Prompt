using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Providers;

/// <summary>
/// Interface for provider-specific prompt adapters.
/// Converts the domain model into the format expected by a specific LLM provider.
/// </summary>
/// <remarks>
/// <para>
/// The core library stays free of HTTP clients and SDK dependencies.
/// In-box adapters (<c>OpenAiAdapter</c>, <c>AnthropicAdapter</c>, <c>OllamaAdapter</c>)
/// convert the domain model into provider-shaped JSON. Future packages may wrap
/// official SDKs for network I/O while reusing these format adapters.
/// </para>
/// <para>
/// Provider differences include:
/// - Message format (OpenAI uses role/content, Anthropic uses a different structure)
/// - Role support (not all providers support "developer" role)
/// - System message handling (some providers use a separate field)
/// - Token counting algorithms
/// </para>
/// </remarks>
public interface IProviderAdapter
{
    /// <summary>Gets the provider name (e.g., "openai", "anthropic", "ollama").</summary>
    string ProviderName { get; }

    /// <summary>Gets the supported roles for this provider.</summary>
    IReadOnlyList<Roles.MessageRole> SupportedRoles { get; }

    /// <summary>
    /// Converts a prompt to the provider-specific request format.
    /// </summary>
    /// <param name="prompt">The prompt to convert.</param>
    /// <returns>A provider-specific object ready for serialization and sending.</returns>
    object ToProviderFormat(IPrompt prompt);

    /// <summary>
    /// Converts a prompt to the provider-specific JSON string.
    /// </summary>
    string ToJson(IPrompt prompt);

    /// <summary>
    /// Estimates the token count for a prompt using this provider's tokenizer.
    /// </summary>
    /// <param name="prompt">The prompt to estimate.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokens(IPrompt prompt);

    /// <summary>
    /// Validates that all messages in the prompt are supported by this provider.
    /// </summary>
    ProviderValidationResult Validate(IPrompt prompt);
}

/// <summary>Result of provider-specific validation.</summary>
public sealed class ProviderValidationResult
{
    /// <summary>Gets whether the prompt is valid for this provider.</summary>
    public bool IsValid { get; }

    /// <summary>Gets warnings (non-blocking issues).</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Gets errors (blocking issues).</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Creates a new provider validation result.</summary>
    public ProviderValidationResult(bool isValid, IEnumerable<string>? warnings = null, IEnumerable<string>? errors = null)
    {
        IsValid = isValid;
        Warnings = warnings?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        Errors = errors?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Valid result.</summary>
    public static ProviderValidationResult Success() => new(true);
}
