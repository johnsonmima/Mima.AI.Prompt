using System.Text.Json;
using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Roles;
using SaaFarr.AI.Prompt.Serialization;

namespace SaaFarr.AI.Prompt.Providers;

/// <summary>
/// OpenAI / Azure OpenAI chat-completions adapter.
/// Emits multimodal content arrays, tool_calls, name, and response_format when present.
/// </summary>
public sealed class OpenAiAdapter : IProviderAdapter
{
    /// <inheritdoc />
    public string ProviderName => "openai";

    /// <inheritdoc />
    public IReadOnlyList<MessageRole> SupportedRoles => new[]
    {
        MessageRole.System, MessageRole.User, MessageRole.Assistant,
        MessageRole.Tool, MessageRole.Function
    };

    /// <inheritdoc />
    public object ToProviderFormat(IPrompt prompt)
    {
        var messages = prompt.Messages.Select(MapMessage).ToList();

        if (prompt.ResponseFormat is not null)
        {
            // Native structured output is JSON-only on chat completions.
            // YAML / Markdown / custom formats stay prompt instructions; wire type is "text".
            var isJson = string.Equals(prompt.ResponseFormat.Type, "json", StringComparison.OrdinalIgnoreCase);
            var responseFormat = isJson && prompt.ResponseFormat.Schema is not null
                ? (object)new
                {
                    type = "json_schema",
                    json_schema = new { name = "response", schema = prompt.ResponseFormat.Schema }
                }
                : new { type = isJson ? "json_object" : "text" };

            return new { messages, response_format = responseFormat };
        }

        return new { messages };
    }

    private static object MapMessage(IMessage m)
    {
        var role = m.Role == MessageRole.Developer ? "system" : m.Role.Name;
        var content = ProviderContentMapper.MapContent(m);

        if (m is AssistantMessage assistant && assistant.ToolCalls.Count > 0)
        {
            var toolCalls = assistant.ToolCalls.Select(t => new
            {
                id = t.Id,
                type = "function",
                function = new { name = t.Name, arguments = t.ArgumentsJson }
            }).ToList();

            return string.IsNullOrEmpty(m.Name)
                ? (object)new { role, content, tool_calls = toolCalls }
                : new { role, content, name = m.Name, tool_calls = toolCalls };
        }

        if (m is ToolMessage tool)
        {
            return new { role, content, tool_call_id = tool.ToolCallId };
        }

        if (!string.IsNullOrEmpty(m.Name))
            return new { role, content, name = m.Name };

        return new { role, content };
    }

    /// <inheritdoc />
    public string ToJson(IPrompt prompt) =>
        JsonSerializer.Serialize(ToProviderFormat(prompt), PromptJsonOptions.IndentedCamelCase);

    /// <inheritdoc />
    public int EstimateTokens(IPrompt prompt)
    {
        var totalChars = prompt.Messages.Sum(m =>
            m.Content.Length + m.Role.Name.Length + 4 +
            m.Parts.OfType<ImagePart>().Count() * 1000);
        return (int)Math.Ceiling(totalChars / 4.0);
    }

    /// <inheritdoc />
    public ProviderValidationResult Validate(IPrompt prompt)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        var systemCount = prompt.Messages.Count(m => m.Role == MessageRole.System || m.Role == MessageRole.Developer);
        if (systemCount > 1)
            warnings.Add("OpenAI works best with a single system message. Multiple system/developer messages will be sent separately.");

        foreach (var name in prompt.Messages.Where(m => !m.Role.IsBuiltIn).Select(m => m.Role.Name).Distinct())
            warnings.Add($"Custom role '{name}' will be sent as-is; OpenAI typically only accepts system, user, assistant, tool, and function.");

        if (prompt.Messages.Any(m => m.Parts.OfType<ThinkingPart>().Any() || (m is AssistantMessage a && a.Reasoning is not null)))
            warnings.Add("Reasoning/thinking parts are omitted from the OpenAI content payload by default.");

        if (prompt.Messages.Any(m => m.CacheControl is not null || m.Parts.Any(p => p.CacheControl is not null)))
            warnings.Add("CacheControl is not native to OpenAI chat completions; it is ignored in the payload.");

        return new ProviderValidationResult(errors.Count == 0, warnings, errors);
    }
}
