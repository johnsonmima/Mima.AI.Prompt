using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Validation;

/// <summary>
/// Validates prompts for correctness, completeness, and best practices.
/// </summary>
/// <remarks>
/// Validation catches common mistakes before prompts are sent to an LLM provider.
/// This includes: missing messages, incorrect ordering, duplicate system messages, etc.
/// </remarks>
public sealed class PromptValidator
{
    /// <summary>
    /// Validates a prompt for correctness.
    /// </summary>
    /// <param name="prompt">The prompt to validate.</param>
    /// <returns>A validation result with any errors or warnings.</returns>
    public PromptValidationReport Validate(IPrompt prompt)
    {
        if (prompt is null)
            throw new ArgumentNullException(nameof(prompt));

        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        // Must have at least one message
        if (prompt.Messages.Count == 0)
        {
            errors.Add("Prompt must contain at least one message.");
            return new PromptValidationReport(false, errors, warnings);
        }

        // Empty text is OK when multimodal parts, tool calls, or refusal are present
        foreach (var msg in prompt.Messages)
        {
            bool hasParts = msg.Parts.Count > 0;
            bool assistantOk = msg is Messages.AssistantMessage am &&
                (am.ToolCalls.Count > 0 || !string.IsNullOrWhiteSpace(am.Refusal));

            if (string.IsNullOrWhiteSpace(msg.Content) && !hasParts && !assistantOk)
                errors.Add($"Message with role '{msg.Role.Name}' has empty content.");
        }

        // System message should come first if present
        // Avoid FirstOrDefault(defaultValue) — that overload is net6+ only
        List<int> systemIndexes = prompt.Messages
            .Select((m, i) => (m, i))
            .Where(x => x.m.Role == MessageRole.System)
            .Select(x => x.i)
            .ToList();

        int firstSystemIndex = systemIndexes.Count > 0 ? systemIndexes[0] : -1;

        if (firstSystemIndex > 0)
            warnings.Add("System message should typically be the first message in a prompt.");

        // Multiple system messages
        int systemCount = prompt.Messages.Count(m => m.Role == MessageRole.System);
        if (systemCount > 1)
            warnings.Add($"Prompt contains {systemCount} system messages. Most providers expect at most one.");

        // Must contain at least one user message for most use cases
        bool hasUser = prompt.Messages.Any(m => m.Role == MessageRole.User);
        if (!hasUser)
            warnings.Add("Prompt does not contain a user message. Most providers expect at least one.");

        // Check for reasonable message count
        if (prompt.Messages.Count > 100)
            warnings.Add($"Prompt contains {prompt.Messages.Count} messages. Consider reducing for performance.");

        return new PromptValidationReport(errors.Count == 0, errors, warnings);
    }
}
