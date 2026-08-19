using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Builder;

/// <summary>
/// Fluent API for composing prompts from messages and templates.
/// The builder assembles messages - it does not create them.
/// </summary>
/// <remarks>
/// <para>
/// The builder is the final layer, not the first. It depends on:
/// Messages → Templates → Prompt → Builder
/// </para>
/// <para>
/// Key principle: the builder only composes; validation, rendering, and
/// serialization are separate responsibilities.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var prompt = PromptBuilder
///     .System("You are a helpful assistant.")
///     .User("Explain dependency injection.")
///     .Build();
///
///  Using templates:
/// var prompt = PromptBuilder
///     .Use(SystemTemplates.Teacher)
///     .With("topic", "C#")
///     .User("Explain generics.")
///     .Build();
/// </code>
/// </example>
public sealed class PromptBuilder
{
    private readonly List<IMessage> _messages = new();
    private readonly Dictionary<string, object> _variables = new();
    private MessageMetadata? _metadata;
    private IMessageTemplate? _pendingTemplate;
    private OutputFormat? _responseFormat;

    private PromptBuilder() { }

    /// <summary>Creates a new prompt builder starting with a system message.</summary>
    /// <param name="content">The system message content.</param>
    /// <returns>A builder instance for chaining.</returns>
    public static PromptBuilder System(string content)
    {
        PromptBuilder builder = new PromptBuilder();
        builder._messages.Add(new SystemMessage(content));
        return builder;
    }

    /// <summary>Creates a new prompt builder starting with a template.</summary>
    /// <param name="template">The message template to use.</param>
    /// <returns>A builder instance for chaining.</returns>
    public static PromptBuilder Use(IMessageTemplate template)
    {
        PromptBuilder builder = new PromptBuilder();
        builder._pendingTemplate = template ?? throw new ArgumentNullException(nameof(template));
        return builder;
    }

    /// <summary>Creates a new prompt builder starting with a message.</summary>
    /// <param name="message">The message to start with.</param>
    /// <returns>A builder instance for chaining.</returns>
    public static PromptBuilder Use(IMessage message)
    {
        PromptBuilder builder = new PromptBuilder();
        builder._messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
        return builder;
    }

    /// <summary>Creates a new empty prompt builder.</summary>
    /// <returns>A builder instance for chaining.</returns>
    public static PromptBuilder Create() => new();

    /// <summary>Adds a system message.</summary>
    /// <param name="content">The system instruction content.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddSystem(string content)
    {
        _messages.Add(new SystemMessage(content));
        return this;
    }

    /// <summary>Adds a system message from content parts (e.g. cached text blocks).</summary>
    public PromptBuilder AddSystem(IEnumerable<IContentPart> parts, CacheControl? cacheControl = null)
    {
        _messages.Add(SystemMessage.Create(parts ?? throw new ArgumentNullException(nameof(parts)), cacheControl: cacheControl));
        return this;
    }

    /// <summary>Adds a developer message.</summary>
    /// <param name="content">The developer instruction content.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddDeveloper(string content)
    {
        _messages.Add(new DeveloperMessage(content));
        return this;
    }

    /// <summary>Adds a developer message from content parts.</summary>
    public PromptBuilder AddDeveloper(IEnumerable<IContentPart> parts)
    {
        _messages.Add(DeveloperMessage.Create(parts ?? throw new ArgumentNullException(nameof(parts))));
        return this;
    }

    /// <summary>Adds a user message.</summary>
    /// <param name="content">User input text.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddUser(string content)
    {
        _messages.Add(new UserMessage(content));
        return this;
    }

    /// <summary>
    /// Adds a multimodal user message from structured content parts.
    /// Prefer this over string content when mixing text with images, files, or computer-use parts.
    /// </summary>
    /// <param name="parts">Content parts (text, image, file, …). Must not be null.</param>
    /// <param name="name">Optional speaker / agent name for multi-agent transcripts.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddUser(IEnumerable<IContentPart> parts, string? name = null)
    {
        _messages.Add(UserMessage.Create(parts ?? throw new ArgumentNullException(nameof(parts)), name: name));
        return this;
    }

    /// <summary>
    /// Shortcut for a user text prompt plus a remote image URL (vision workflows).
    /// Equivalent to <see cref="AddUser(IEnumerable{IContentPart}, string?)"/> with
    /// <see cref="TextPart"/> + <see cref="ImagePart.FromUrl"/>.
    /// </summary>
    /// <param name="text">User text prompt.</param>
    /// <param name="imageUrl">Image URL.</param>
    /// <param name="name">Optional speaker name.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddUserWithImage(string text, string imageUrl, string? name = null)
    {
        _messages.Add(UserMessage.Create(new IContentPart[]
        {
            TextPart.Create(text),
            ImagePart.FromUrl(imageUrl)
        }, name: name));
        return this;
    }

    /// <summary>Adds an assistant message.</summary>
    /// <param name="content">Assistant reply text.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddAssistant(string content)
    {
        _messages.Add(new AssistantMessage(content));
        return this;
    }

    /// <summary>Adds an assistant message from content parts.</summary>
    public PromptBuilder AddAssistant(IEnumerable<IContentPart> parts)
    {
        _messages.Add(AssistantMessage.Create(parts ?? throw new ArgumentNullException(nameof(parts))));
        return this;
    }

    /// <summary>
    /// Adds an assistant turn that requests one or more tools (provider <c>tool_calls</c>).
    /// Pair each call id with a later <see cref="AddTool"/> result message.
    /// </summary>
    /// <param name="toolCalls">Outbound tool invocations.</param>
    /// <param name="content">Optional accompanying text shown beside the tool calls.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddAssistantToolCalls(IEnumerable<ToolCall> toolCalls, string? content = null)
    {
        _messages.Add(AssistantMessage.CreateWithToolCalls(toolCalls, content));
        return this;
    }

    /// <summary>Adds a tool message.</summary>
    /// <param name="toolCallId">The tool call ID.</param>
    /// <param name="content">The tool output.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddTool(string toolCallId, string content)
    {
        _messages.Add(new ToolMessage(toolCallId, content));
        return this;
    }

    /// <summary>Adds a function message.</summary>
    /// <param name="functionName">The function name.</param>
    /// <param name="content">The function response.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddFunction(string functionName, string content)
    {
        _messages.Add(new FunctionMessage(functionName, content));
        return this;
    }

    /// <summary>Adds any pre-created message.</summary>
    /// <param name="message">The message to add.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddMessage(IMessage message)
    {
        _messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
        return this;
    }

    /// <summary>
    /// Adds a message with an explicit role (built-in or custom via <see cref="MessageRole.Custom"/>).
    /// Built-in roles use the concrete message types; custom roles use <see cref="CustomMessage"/>.
    /// </summary>
    public PromptBuilder Add(MessageRole role, string content)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        if (role == MessageRole.System) return AddSystem(content);
        if (role == MessageRole.Developer) return AddDeveloper(content);
        if (role == MessageRole.User) return AddUser(content);
        if (role == MessageRole.Assistant) return AddAssistant(content);
        if (role == MessageRole.Tool || role == MessageRole.Function)
            throw new ArgumentException(
                $"Use {nameof(AddTool)} / {nameof(AddFunction)} for '{role.Name}' (they require an id/name).",
                nameof(role));

        _messages.Add(new CustomMessage(role, content));
        return this;
    }

    /// <summary>Adds a message with a developer-invented role name.</summary>
    public PromptBuilder AddCustom(string roleName, string content) =>
        Add(MessageRole.Custom(roleName), content);

    /// <summary>Adds a template whose variables will be resolved on Build().</summary>
    /// <param name="template">The template to add.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddTemplate(IMessageTemplate template)
    {
        _pendingTemplate = template ?? throw new ArgumentNullException(nameof(template));
        return this;
    }

    /// <summary>Sets a variable value for pending template rendering.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder With(string name, object value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

        _variables[name] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <summary>Sets metadata for the resulting prompt.</summary>
    /// <param name="metadata">The prompt metadata.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder WithMetadata(MessageMetadata metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>Sets a name for the resulting prompt.</summary>
    public PromptBuilder WithName(string name)
    {
        _metadata = (_metadata ?? MessageMetadata.Empty).SetName(name);
        return this;
    }

    /// <summary>
    /// Requests a structured response format from the provider (maps to OpenAI <c>response_format</c>, etc.).
    /// This is prompt-level metadata — independent of individual message content.
    /// </summary>
    /// <param name="format">Desired output format (JSON, schema, Markdown, …).</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder WithResponseFormat(OutputFormat format)
    {
        _responseFormat = format ?? throw new ArgumentNullException(nameof(format));
        return this;
    }

    /// <summary>
    /// Adds a few-shot example (user + assistant pair).
    /// Few-shot prompting provides the model with example input/output pairs.
    /// </summary>
    /// <param name="userContent">Example user input.</param>
    /// <param name="assistantContent">Expected assistant response.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddExample(string userContent, string assistantContent)
    {
        _messages.Add(new UserMessage(userContent));
        _messages.Add(new AssistantMessage(assistantContent));
        return this;
    }

    /// <summary>
    /// Adds conversation history as alternating user/assistant messages.
    /// </summary>
    /// <param name="history">Pairs of (user, assistant) messages.</param>
    /// <returns>This builder for chaining.</returns>
    public PromptBuilder AddHistory(IEnumerable<(string User, string Assistant)> history)
    {
        foreach (var (user, assistant) in history)
        {
            _messages.Add(new UserMessage(user));
            _messages.Add(new AssistantMessage(assistant));
        }
        return this;
    }

    /// <summary>
    /// Builds the final immutable prompt.
    /// Renders any pending templates with the provided variables.
    /// </summary>
    /// <returns>An immutable prompt containing all composed messages.</returns>
    /// <exception cref="Exceptions.PromptValidationException">Thrown if the prompt has no messages or template validation fails.</exception>
    public Models.Prompt Build()
    {
        var allMessages = new List<IMessage>(_messages);

        // Render pending template if variables are provided
        if (_pendingTemplate is not null)
        {
            if (_variables.Count > 0)
            {
                var rendered = _pendingTemplate.Render(_variables);
                allMessages.Insert(0, rendered);
            }
            else if (_pendingTemplate.Variables.Count == 0)
            {
                // Template with no variables - render directly
                var rendered = _pendingTemplate.Render(new Dictionary<string, object>());
                allMessages.Insert(0, rendered);
            }
            else
            {
                throw new Exceptions.PromptValidationException(
                    $"Template has {_pendingTemplate.Variables.Count} variable(s) but no values were provided via With().");
            }
        }

        return new Models.Prompt(allMessages, _metadata, responseFormat: _responseFormat);
    }

    // --- Convenience static methods for quick prompts ---

    /// <summary>Creates a simple system + user prompt in one call.</summary>
    /// <param name="systemContent">The system message.</param>
    /// <param name="userContent">The user message.</param>
    public static Models.Prompt Quick(string systemContent, string userContent)
    {
        return Create()
            .AddSystem(systemContent)
            .AddUser(userContent)
            .Build();
    }

    /// <summary>Creates a simple user-only prompt.</summary>
    /// <param name="userContent">The user message.</param>
    public static Models.Prompt UserOnly(string userContent)
    {
        return Create()
            .AddUser(userContent)
            .Build();
    }
}
