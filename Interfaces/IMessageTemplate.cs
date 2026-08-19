using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Interfaces;

/// <summary>
/// Represents a reusable message template with variable placeholders.
/// Templates separate the structure of a message from its specific values.
/// </summary>
/// <remarks>
/// Templates use the <c>{{variableName}}</c> syntax for placeholders.
/// Rendering fills in variables and produces a concrete <see cref="IMessage"/>.
/// </remarks>
public interface IMessageTemplate
{
    /// <summary>Gets the role this template produces messages for.</summary>
    MessageRole Role { get; }

    /// <summary>Gets the raw template content with variable placeholders.</summary>
    string TemplateContent { get; }

    /// <summary>Gets the metadata associated with this template.</summary>
    MessageMetadata Metadata { get; }

    /// <summary>Gets the discovered variable names from the template content.</summary>
    IReadOnlyList<string> Variables { get; }

    /// <summary>
    /// Renders the template by replacing variable placeholders with provided values.
    /// </summary>
    /// <param name="variables">Dictionary of variable name to value mappings.</param>
    /// <returns>A concrete message with all variables replaced.</returns>
    /// <exception cref="Exceptions.PromptValidationException">Thrown when required variables are missing.</exception>
    IMessage Render(IDictionary<string, object> variables);

    /// <summary>
    /// Renders the template using an anonymous object for variable values.
    /// </summary>
    /// <param name="variables">An object whose properties are used as variable values.</param>
    /// <returns>A concrete message with all variables replaced.</returns>
    IMessage Render(object variables);

    /// <summary>
    /// Validates that all required variables are provided.
    /// </summary>
    /// <param name="variables">The variable values to validate.</param>
    /// <returns>A validation result indicating success or listing missing variables.</returns>
    TemplateValidationResult Validate(IDictionary<string, object> variables);
}
