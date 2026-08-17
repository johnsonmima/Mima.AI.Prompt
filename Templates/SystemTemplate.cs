using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Templates;

/// <summary>
/// A reusable system message template with variable support.
/// Use system templates to define AI personas that can be parameterized.
/// </summary>
/// <example>
/// <code>
/// var template = SystemTemplate.Create("""
///     You are a {{profession}}.
///     Use a {{tone}} tone.
///     Limit responses to {{maxWords}} words.
///     """);
/// 
/// var message = template.Render(new { profession = "Teacher", tone = "Friendly", maxWords = 200 });
/// </code>
/// </example>
public sealed class SystemTemplate : MessageTemplate
{
    private SystemTemplate(string templateContent, MessageMetadata? metadata)
        : base(MessageRole.System, templateContent, metadata) { }

    /// <summary>Creates a new system template.</summary>
    /// <param name="templateContent">The template content with {{variable}} placeholders.</param>
    /// <param name="metadata">Optional metadata.</param>
    public static SystemTemplate Create(string templateContent, MessageMetadata? metadata = null) =>
        new(templateContent, metadata);

    /// <summary>Creates a named system template.</summary>
    /// <param name="name">The template name.</param>
    /// <param name="templateContent">The template content.</param>
    public static SystemTemplate Create(string name, string templateContent) =>
        new(templateContent, MessageMetadata.WithName(name));

    /// <inheritdoc />
    protected override IMessage CreateMessage(string renderedContent) =>
        new SystemMessage(renderedContent, Metadata);
}
