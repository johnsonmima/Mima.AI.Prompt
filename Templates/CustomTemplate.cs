using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Templates;

/// <summary>
/// A reusable message template for any role, including custom roles.
/// </summary>
/// <example>
/// <code>
/// var template = CustomTemplate.Create(MessageRole.Custom("critic"), """
///     Review the following claim and find flaws:
///     {{claim}}
///     """);
/// var message = template.Render(new { claim = "DI is always better than service locator." });
/// </code>
/// </example>
public sealed class CustomTemplate : MessageTemplate
{
    private CustomTemplate(MessageRole role, string templateContent, MessageMetadata? metadata)
        : base(role, templateContent, metadata) { }

    /// <summary>Creates a template for the given role.</summary>
    public static CustomTemplate Create(MessageRole role, string templateContent, MessageMetadata? metadata = null)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));
        return new CustomTemplate(role, templateContent, metadata);
    }

    /// <summary>Creates a template for a custom role name.</summary>
    public static CustomTemplate Create(string roleName, string templateContent, MessageMetadata? metadata = null) =>
        Create(MessageRole.Custom(roleName), templateContent, metadata);

    /// <summary>Creates a named template for the given role.</summary>
    public static CustomTemplate Create(MessageRole role, string name, string templateContent) =>
        Create(role, templateContent, MessageMetadata.WithName(name));

    /// <inheritdoc />
    protected override IMessage CreateMessage(string renderedContent)
    {
        // Prefer concrete types for built-ins so adapters/serializers keep specialized fields when applicable
        if (Role == MessageRole.System) return new SystemMessage(renderedContent, Metadata);
        if (Role == MessageRole.Developer) return new DeveloperMessage(renderedContent, Metadata);
        if (Role == MessageRole.User) return new UserMessage(renderedContent, Metadata);
        if (Role == MessageRole.Assistant) return new AssistantMessage(renderedContent, Metadata);
        return new CustomMessage(Role, renderedContent, Metadata);
    }
}
