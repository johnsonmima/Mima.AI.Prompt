namespace Mima.AI.Prompt.Interfaces;

/// <summary>
/// Renders a prompt into a provider-specific format.
/// Implementations convert the domain model into the structure expected by a specific LLM provider.
/// </summary>
/// <remarks>
/// The core library ships with a generic JSON renderer.
/// Provider-specific packages (Mima.AI.Prompt.OpenAI, Mima.AI.Prompt.Anthropic, etc.)
/// provide their own implementations.
/// </remarks>
public interface IPromptRenderer
{
    /// <summary>Gets the name of the provider this renderer targets.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Renders a prompt into the provider-specific format as a JSON string.
    /// </summary>
    /// <param name="prompt">The prompt to render.</param>
    /// <returns>A JSON string in the format expected by the target provider.</returns>
    string Render(IPrompt prompt);

    /// <summary>
    /// Renders a prompt into the provider-specific format as a list of message dictionaries.
    /// </summary>
    /// <param name="prompt">The prompt to render.</param>
    /// <returns>A list of dictionaries representing messages in provider format.</returns>
    IReadOnlyList<IDictionary<string, string>> RenderMessages(IPrompt prompt);
}
