using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Templates;

/// <summary>
/// A reusable assistant message template with variable support.
/// Used for few-shot examples where the expected AI response format is templated.
/// </summary>
/// <example>
/// <code>
/// var template = AssistantTemplate.Create("""
///     Here is the summary in {{format}} format:
///     {{content}}
///     """);
/// </code>
/// </example>
public sealed class AssistantTemplate : MessageTemplate
{
    private AssistantTemplate(string templateContent, MessageMetadata? metadata)
        : base(MessageRole.Assistant, templateContent, metadata) { }

    /// <summary>Creates a new assistant template.</summary>
    /// <param name="templateContent">The template content with {{variable}} placeholders.</param>
    /// <param name="metadata">Optional metadata.</param>
    public static AssistantTemplate Create(string templateContent, MessageMetadata? metadata = null) =>
        new(templateContent, metadata);

    /// <summary>Creates a named assistant template.</summary>
    public static AssistantTemplate Create(string name, string templateContent) =>
        new(templateContent, MessageMetadata.WithName(name));

    /// <inheritdoc />
    protected override IMessage CreateMessage(string renderedContent) =>
        new AssistantMessage(renderedContent, Metadata);
}
