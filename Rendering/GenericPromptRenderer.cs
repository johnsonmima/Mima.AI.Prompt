using System.Text.Json;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Serialization;

namespace SaaFarr.AI.Prompt.Rendering;

/// <summary>
/// A generic, provider-agnostic prompt renderer that produces standard JSON output.
/// Most providers (OpenAI, Azure OpenAI, Ollama) accept this format directly.
/// </summary>
/// <remarks>
/// Provider-specific packages can implement <see cref="IPromptRenderer"/> for
/// custom rendering (e.g., Anthropic's different message format).
/// </remarks>
public sealed class GenericPromptRenderer : IPromptRenderer
{
    /// <inheritdoc />
    public string ProviderName => "generic";

    /// <inheritdoc />
    public string Render(IPrompt prompt)
    {
        if (prompt is null)
            throw new ArgumentNullException(nameof(prompt));

        var messages = RenderMessages(prompt);
        return JsonSerializer.Serialize(new { messages }, PromptJsonOptions.IndentedCamelCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<IDictionary<string, string>> RenderMessages(IPrompt prompt)
    {
        if (prompt is null)
            throw new ArgumentNullException(nameof(prompt));

        return prompt.Messages.Select(m => new Dictionary<string, string>
        {
            ["role"] = m.Role.Name,
            ["content"] = m.Content
        } as IDictionary<string, string>).ToList().AsReadOnly();
    }
}
