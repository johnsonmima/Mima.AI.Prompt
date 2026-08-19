using System.Text.Json;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;

namespace Mima.AI.Prompt.Providers;

/// <summary>
/// Provider adapter for Ollama local LLM API.
/// Uses the same parts-aware content mapping as OpenAI-style chat APIs.
/// </summary>
public sealed class OllamaAdapter : IProviderAdapter
{
    /// <inheritdoc />
    public string ProviderName => "ollama";

    /// <inheritdoc />
    public IReadOnlyList<MessageRole> SupportedRoles => new[]
    {
        MessageRole.System, MessageRole.User, MessageRole.Assistant
    };

    /// <inheritdoc />
    public object ToProviderFormat(IPrompt prompt)
    {
        var messages = prompt.Messages
            .Where(m => m.Role != MessageRole.Tool && m.Role != MessageRole.Function)
            .Select(m =>
            {
                var role = m.Role == MessageRole.Developer ? "system" : m.Role.Name;
                return new { role, content = ProviderContentMapper.MapContent(m) };
            }).ToList();

        return new { messages };
    }

    /// <inheritdoc />
    public string ToJson(IPrompt prompt) =>
        JsonSerializer.Serialize(ToProviderFormat(prompt), PromptJsonOptions.IndentedCamelCase);

    /// <inheritdoc />
    public int EstimateTokens(IPrompt prompt)
    {
        var totalChars = prompt.Messages.Sum(m =>
            m.Content.Length + 10 + m.Parts.OfType<ImagePart>().Count() * 1000);
        return (int)Math.Ceiling(totalChars / 4.0);
    }

    /// <inheritdoc />
    public ProviderValidationResult Validate(IPrompt prompt)
    {
        List<string> warnings = new List<string>();

        if (prompt.Messages.Any(m => m.Role == MessageRole.Tool || m.Role == MessageRole.Function))
            warnings.Add("Tool/Function messages are not supported by most Ollama models and will be excluded.");

        foreach (var name in prompt.Messages.Where(m => !m.Role.IsBuiltIn).Select(m => m.Role.Name).Distinct())
            warnings.Add($"Custom role '{name}' will be sent as-is; most Ollama models only accept system, user, and assistant.");

        if (prompt.Messages.Any(ProviderContentMapper.HasStructuredParts))
            warnings.Add("Multimodal / structured parts are emitted in the payload; not all Ollama models support them.");

        if (prompt.Messages.Any(m => m.Parts.OfType<ThinkingPart>().Any() || (m is AssistantMessage a && a.Reasoning is not null)))
            warnings.Add("Reasoning/thinking parts are omitted from the Ollama content payload by default.");

        return new ProviderValidationResult(true, warnings);
    }
}
