using System.Text.Json;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;

namespace Mima.AI.Prompt.Providers;

/// <summary>
/// Provider adapter for Anthropic Claude API.
/// System/developer text is lifted to a top-level <c>system</c> field; user/assistant use parts-aware content.
/// </summary>
public sealed class AnthropicAdapter : IProviderAdapter
{
    /// <inheritdoc />
    public string ProviderName => "anthropic";

    /// <inheritdoc />
    public IReadOnlyList<MessageRole> SupportedRoles => new[]
    {
        MessageRole.System, MessageRole.User, MessageRole.Assistant
    };

    /// <inheritdoc />
    public object ToProviderFormat(IPrompt prompt)
    {
        // Anthropic: system is top-level (text projection of system/developer parts)
        var systemParts = prompt.Messages
            .Where(m => m.Role == MessageRole.System || m.Role == MessageRole.Developer)
            .Select(m => m.Content);

        var system = string.Join("\n\n", systemParts.Where(s => !string.IsNullOrEmpty(s)));

        var messages = prompt.Messages
            .Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant)
            .Select(m => new { role = m.Role.Name, content = ProviderContentMapper.MapContent(m) })
            .ToList();

        if (!string.IsNullOrEmpty(system))
            return new { system, messages };

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
        var warnings = new List<string>();
        var errors = new List<string>();

        var conversationMessages = prompt.Messages
            .Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant)
            .ToList();

        for (int i = 1; i < conversationMessages.Count; i++)
        {
            if (conversationMessages[i].Role == conversationMessages[i - 1].Role)
                warnings.Add($"Anthropic prefers alternating user/assistant messages. Found consecutive '{conversationMessages[i].Role.Name}' messages at position {i}.");
        }

        if (conversationMessages.Count > 0 && conversationMessages[0].Role != MessageRole.User)
            warnings.Add("Anthropic expects the first conversation message to be a user message.");

        if (prompt.Messages.Any(m => m.Role == MessageRole.Tool || m.Role == MessageRole.Function))
            warnings.Add("Tool/Function messages will be excluded; use Anthropic's tool_use format instead.");

        var customRoles = prompt.Messages.Where(m => !m.Role.IsBuiltIn).Select(m => m.Role.Name).Distinct().ToList();
        if (customRoles.Count > 0)
            warnings.Add($"Custom role(s) '{string.Join("', '", customRoles)}' are not included in Anthropic's messages array (only user/assistant are); map them before sending.");

        if (prompt.Messages.Any(m =>
                (m.Role == MessageRole.System || m.Role == MessageRole.Developer)
                && ProviderContentMapper.HasStructuredParts(m)))
            warnings.Add("Non-text parts on system/developer messages are flattened to text in Anthropic's top-level system field.");

        if (prompt.Messages.Any(m => m.Parts.OfType<ThinkingPart>().Any() || (m is AssistantMessage a && a.Reasoning is not null)))
            warnings.Add("Reasoning/thinking parts are omitted from the Anthropic content payload by default.");

        return new ProviderValidationResult(errors.Count == 0, warnings, errors);
    }
}
