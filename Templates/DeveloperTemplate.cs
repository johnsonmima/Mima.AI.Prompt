using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Templates;

/// <summary>
/// A reusable developer message template with variable support.
/// Used for framework-level instructions that can be parameterized.
/// </summary>
/// <example>
/// <code>
/// var template = DeveloperTemplate.Create("""
///     Always respond in {{language}}.
///     Use {{framework}} for code examples.
///     """);
/// </code>
/// </example>
public sealed class DeveloperTemplate : MessageTemplate
{
    private DeveloperTemplate(string templateContent, MessageMetadata? metadata)
        : base(MessageRole.Developer, templateContent, metadata) { }

    /// <summary>Creates a new developer template.</summary>
    /// <param name="templateContent">The template content with {{variable}} placeholders.</param>
    /// <param name="metadata">Optional metadata.</param>
    public static DeveloperTemplate Create(string templateContent, MessageMetadata? metadata = null) =>
        new(templateContent, metadata);

    /// <summary>Creates a named developer template.</summary>
    public static DeveloperTemplate Create(string name, string templateContent) =>
        new(templateContent, MessageMetadata.WithName(name));

    /// <inheritdoc />
    protected override IMessage CreateMessage(string renderedContent) =>
        new DeveloperMessage(renderedContent, Metadata);
}
