using System.Text.RegularExpressions;
using SaaFarr.AI.Prompt.Exceptions;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;
using SaaFarr.AI.Prompt.Messages;

namespace SaaFarr.AI.Prompt.Templates;

/// <summary>
/// Base class for message templates with variable placeholder support.
/// Templates use the <c>{{variableName}}</c> syntax and support automatic variable discovery.
/// </summary>
/// <remarks>
/// <para>
/// Templates separate the structure of a message from its specific values,
/// enabling reuse across applications without duplication.
/// </para>
/// <para>
/// Variable discovery is automatic - the framework scans the template content for
/// <c>{{variableName}}</c> patterns and makes them available via the <see cref="Variables"/> property.
/// </para>
/// </remarks>
public abstract class MessageTemplate : IMessageTemplate
{
    private static readonly Regex VariablePattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
    private readonly Lazy<IReadOnlyList<string>> _variables;

    /// <inheritdoc />
    public MessageRole Role { get; }

    /// <inheritdoc />
    public string TemplateContent { get; }

    /// <inheritdoc />
    public MessageMetadata Metadata { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Variables => _variables.Value;

    /// <summary>
    /// Creates a new message template.
    /// </summary>
    /// <param name="role">The role of messages produced by this template.</param>
    /// <param name="templateContent">The template content with {{variable}} placeholders.</param>
    /// <param name="metadata">Optional metadata.</param>
    protected MessageTemplate(MessageRole role, string templateContent, MessageMetadata? metadata = null)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));
        if (string.IsNullOrWhiteSpace(templateContent))
            throw new PromptValidationException("Template content cannot be null or empty.");

        Role = role;
        TemplateContent = templateContent;
        Metadata = metadata ?? MessageMetadata.Empty;
        _variables = new Lazy<IReadOnlyList<string>>(DiscoverVariables);
    }

    /// <inheritdoc />
    public IMessage Render(IDictionary<string, object> variables)
    {
        TemplateValidationResult validation = Validate(variables);
        if (!validation.IsValid)
            throw new PromptValidationException(validation.Errors);

        string rendered = RenderContent(variables);
        return CreateMessage(rendered);
    }

    /// <inheritdoc />
    public IMessage Render(object variables)
    {
        var dict = ObjectToDictionary(variables);
        return Render(dict);
    }

    /// <inheritdoc />
    public TemplateValidationResult Validate(IDictionary<string, object> variables)
    {
        var missing = Variables
            .Where(v => !variables.ContainsKey(v) || variables[v] is null)
            .ToList();

        var extra = variables.Keys
            .Where(k => !Variables.Contains(k))
            .ToList();

        if (missing.Count == 0)
            return TemplateValidationResult.Success();

        return new TemplateValidationResult(false, missing, extra,
            missing.Select(v => $"Missing required variable: '{v}'"));
    }

    /// <summary>
    /// Creates the concrete message type for this template's role.
    /// </summary>
    protected abstract IMessage CreateMessage(string renderedContent);

    /// <summary>
    /// Renders the template content by replacing variable placeholders.
    /// </summary>
    private string RenderContent(IDictionary<string, object> variables)
    {
        return VariablePattern.Replace(TemplateContent, match =>
        {
            var variableName = match.Groups[1].Value;
            if (variables.TryGetValue(variableName, out var value) && value is not null)
                return value.ToString() ?? string.Empty;
            return match.Value; // Leave unreplaced if not found (shouldn't happen after validation)
        });
    }

    /// <summary>
    /// Discovers all variable placeholders in the template content.
    /// </summary>
    private IReadOnlyList<string> DiscoverVariables()
    {
        return VariablePattern.Matches(TemplateContent)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>Converts an anonymous object to a dictionary.</summary>
    private static IDictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj is IDictionary<string, object> dict)
            return dict;

        return obj.GetType()
            .GetProperties()
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(obj)!);
    }
}
