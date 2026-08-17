using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Templates;

/// <summary>
/// A reusable user message template with variable support.
/// Use user templates for common request patterns like summarization, translation, etc.
/// </summary>
/// <example>
/// <code>
/// var template = UserTemplate.Create("""
///     Summarize the following document in {{style}} style:
///     
///     {{document}}
///     """);
/// 
/// var message = template.Render(new { style = "bullet points", document = articleText });
/// </code>
/// </example>
public sealed class UserTemplate : MessageTemplate
{
    private UserTemplate(string templateContent, MessageMetadata? metadata)
        : base(MessageRole.User, templateContent, metadata) { }

    /// <summary>Creates a new user template.</summary>
    /// <param name="templateContent">The template content with {{variable}} placeholders.</param>
    /// <param name="metadata">Optional metadata.</param>
    public static UserTemplate Create(string templateContent, MessageMetadata? metadata = null) =>
        new(templateContent, metadata);

    /// <summary>Creates a named user template.</summary>
    /// <param name="name">The template name.</param>
    /// <param name="templateContent">The template content.</param>
    public static UserTemplate Create(string name, string templateContent) =>
        new(templateContent, MessageMetadata.WithName(name));

    /// <inheritdoc />
    protected override IMessage CreateMessage(string renderedContent) =>
        new UserMessage(renderedContent, Metadata);
}
