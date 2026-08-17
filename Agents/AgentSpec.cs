using System.Text;
using SaaFarr.AI.Prompt.Builder;
using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Thin syntactic sugar for agent-shaped prompts.
/// Composes identity, tools (by name), memory, retrieval, and response format into an <see cref="IPrompt"/>.
/// Does not call models, invoke tools, or run loops — attach host hooks and use them yourself.
/// </summary>
/// <remarks>
/// See <c>AGENT.md</c> at the repository root for patterns and how to plug in chat clients, tool invokers, and RAG.
/// </remarks>
public sealed class AgentSpec
{
    private readonly List<string> _toolNames = new();
    private string? _instructions;
    private string? _developerInstructions;
    private Conversation? _conversation;
    private int? _memoryWindow;
    private OutputFormat? _responseFormat;
    private MessageMetadata? _metadata;
    private IAgentRetriever? _retriever;
    private IAgentMemory? _externalMemory;
    private IAgentToolCatalog? _toolCatalog;
    private IAgentChatClient? _chatClient;
    private IAgentToolInvoker? _toolInvoker;
    private IAgentLoop? _loop;

    /// <summary>Gets the agent name (also used as prompt metadata name when unset).</summary>
    public string Name { get; }

    /// <summary>Gets system instructions, if set.</summary>
    public string? Instructions => _instructions;

    /// <summary>Gets tool names advertised to the model (names only).</summary>
    public IReadOnlyList<string> ToolNames => _toolNames.AsReadOnly();

    /// <summary>Gets in-process conversation memory, if attached.</summary>
    public Conversation? Memory => _conversation;

    /// <summary>Gets host chat client hook, if attached (never called by this type).</summary>
    public IAgentChatClient? ChatClient => _chatClient;

    /// <summary>Gets host tool invoker hook, if attached (never called by this type).</summary>
    public IAgentToolInvoker? ToolInvoker => _toolInvoker;

    /// <summary>Gets host loop hook, if attached (never called by this type).</summary>
    public IAgentLoop? Loop => _loop;

    /// <summary>
    /// Returns the attached chat client, or throws if <see cref="WithChatClient"/> was not called.
    /// Use this in your loop instead of <c>ChatClient!</c>.
    /// </summary>
    public IAgentChatClient RequireChatClient() =>
        _chatClient ?? throw new InvalidOperationException(
            "No chat client attached. Call WithChatClient(...) before completing a prompt.");

    /// <summary>
    /// Returns the attached tool invoker, or throws if <see cref="WithToolInvoker"/> was not called.
    /// Use this in your loop instead of <c>ToolInvoker!</c>.
    /// </summary>
    public IAgentToolInvoker RequireToolInvoker() =>
        _toolInvoker ?? throw new InvalidOperationException(
            "No tool invoker attached. Call WithToolInvoker(...) before running tools.");

    /// <summary>
    /// Returns the attached loop, or throws if <see cref="WithLoop"/> was not called.
    /// Use this instead of <c>Loop!</c>.
    /// </summary>
    public IAgentLoop RequireLoop() =>
        _loop ?? throw new InvalidOperationException(
            "No agent loop attached. Call WithLoop(...) before RunAsync.");

    /// <summary>Gets host retriever, if attached (called from <see cref="BuildPrompt(string)"/>).</summary>
    public IAgentRetriever? Retriever => _retriever;

    /// <summary>Gets host external memory, if attached.</summary>
    public IAgentMemory? ExternalMemory => _externalMemory;

    /// <summary>Gets host tool catalog, if attached.</summary>
    public IAgentToolCatalog? ToolCatalog => _toolCatalog;

    /// <summary>Gets requested response format, if set.</summary>
    public OutputFormat? ResponseFormat => _responseFormat;

    private AgentSpec(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Agent name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    /// <summary>Creates a new agent spec.</summary>
    public static AgentSpec Create(string name) => new(name);

    /// <summary>Sets system instructions (agent identity / policy).</summary>
    public AgentSpec WithInstructions(string instructions)
    {
        _instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        return this;
    }

    /// <summary>Sets optional developer / framework instructions.</summary>
    public AgentSpec WithDeveloperInstructions(string instructions)
    {
        _developerInstructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        return this;
    }

    /// <summary>Advertises tool names in the system prompt (host still owns schemas and execution).</summary>
    public AgentSpec WithTools(params string[] toolNames)
    {
        if (toolNames is null)
            throw new ArgumentNullException(nameof(toolNames));

        foreach (var name in toolNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var trimmed = name.Trim();
            if (!_toolNames.Contains(trimmed))
                _toolNames.Add(trimmed);
        }

        return this;
    }

    /// <summary>Attaches optional richer tool descriptions for the system prompt.</summary>
    public AgentSpec WithToolCatalog(IAgentToolCatalog catalog)
    {
        _toolCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        foreach (var tool in catalog.GetTools())
        {
            if (!_toolNames.Contains(tool.Name))
                _toolNames.Add(tool.Name);
        }

        return this;
    }

    /// <summary>Attaches in-process conversation memory.</summary>
    /// <param name="conversation">Conversation history.</param>
    /// <param name="window">Optional sliding window (last N history messages, excluding system).</param>
    public AgentSpec WithMemory(Conversation conversation, int? window = null)
    {
        _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
        _memoryWindow = window;
        return this;
    }

    /// <summary>Attaches host-provided memory (messages incorporated into <see cref="BuildPrompt(string)"/>).</summary>
    public AgentSpec WithExternalMemory(IAgentMemory memory)
    {
        _externalMemory = memory ?? throw new ArgumentNullException(nameof(memory));
        return this;
    }

    /// <summary>Attaches host RAG. Results are incorporated into the built prompt.</summary>
    public AgentSpec WithRetriever(IAgentRetriever retriever)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        return this;
    }

    /// <summary>Requests a structured response format on the built prompt.</summary>
    public AgentSpec WithResponseFormat(OutputFormat format)
    {
        _responseFormat = format ?? throw new ArgumentNullException(nameof(format));
        return this;
    }

    /// <summary>Sets prompt metadata (name defaults to the agent name).</summary>
    public AgentSpec WithMetadata(MessageMetadata metadata)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        return this;
    }

    /// <summary>Attaches a host chat client for your loop (not invoked by BuildPrompt).</summary>
    public AgentSpec WithChatClient(IAgentChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        return this;
    }

    /// <summary>Attaches a host tool invoker for your loop (not invoked by BuildPrompt).</summary>
    public AgentSpec WithToolInvoker(IAgentToolInvoker toolInvoker)
    {
        _toolInvoker = toolInvoker ?? throw new ArgumentNullException(nameof(toolInvoker));
        return this;
    }

    /// <summary>Attaches a host agent loop for your app to call (not invoked by BuildPrompt).</summary>
    public AgentSpec WithLoop(IAgentLoop loop)
    {
        _loop = loop ?? throw new ArgumentNullException(nameof(loop));
        return this;
    }

    /// <summary>Builds a <see cref="Models.Prompt"/> for a text user turn.</summary>
    public Models.Prompt BuildPrompt(string userTurn)
    {
        if (string.IsNullOrWhiteSpace(userTurn))
            throw new ArgumentException("User turn cannot be empty.", nameof(userTurn));

        return BuildPromptCore(userTurn, userParts: null, extraMessages: null);
    }

    /// <summary>Builds a <see cref="Models.Prompt"/> for a multimodal user turn.</summary>
    public Models.Prompt BuildPrompt(IEnumerable<IContentPart> userParts)
    {
        if (userParts is null)
            throw new ArgumentNullException(nameof(userParts));

        var list = userParts.ToList();
        if (list.Count == 0)
            throw new ArgumentException("User parts cannot be empty.", nameof(userParts));

        var query = string.Concat(list.OfType<TextPart>().Select(p => p.Text));
        return BuildPromptCore(query, list, extraMessages: null);
    }

    /// <summary>
    /// Builds a follow-up prompt after the model requested tools and the host executed them.
    /// Appends the assistant tool-call turn and matching tool results before the next model call.
    /// </summary>
    public Models.Prompt BuildPromptWithToolResults(
        string userTurn,
        AssistantMessage assistantToolCallTurn,
        IEnumerable<(string ToolCallId, string Result)> toolResults)
    {
        if (assistantToolCallTurn is null)
            throw new ArgumentNullException(nameof(assistantToolCallTurn));
        if (toolResults is null)
            throw new ArgumentNullException(nameof(toolResults));

        var extras = new List<IMessage> { assistantToolCallTurn };
        foreach (var (id, result) in toolResults)
            extras.Add(new ToolMessage(id, result));

        return BuildPromptCore(userTurn, userParts: null, extras);
    }

    private Models.Prompt BuildPromptCore(string userQuery, List<IContentPart>? userParts, List<IMessage>? extraMessages)
    {
        var builder = PromptBuilder.Create();

        var system = ComposeSystemInstructions();
        if (!string.IsNullOrWhiteSpace(system))
            builder.AddSystem(system!);

        if (!string.IsNullOrWhiteSpace(_developerInstructions))
            builder.AddDeveloper(_developerInstructions!);

        foreach (var message in GetHistoryMessages())
            builder.AddMessage(message);

        if (_externalMemory is not null)
        {
            foreach (var message in _externalMemory.GetHistory())
                builder.AddMessage(message);
        }

        AgentRetrievalResult? retrieval = null;
        if (_retriever is not null && !string.IsNullOrWhiteSpace(userQuery))
            retrieval = _retriever.Retrieve(userQuery);

        if (retrieval is { IsEmpty: false })
        {
            if (retrieval.Text is not null)
                builder.AddDeveloper("Retrieved context:\n" + retrieval.Text);

            if (userParts is null && retrieval.Parts is not null)
            {
                var combined = new List<IContentPart> { TextPart.Create(userQuery) };
                combined.AddRange(retrieval.Parts);
                userParts = combined;
            }
            else if (userParts is not null && retrieval.Parts is not null)
            {
                userParts = userParts.Concat(retrieval.Parts).ToList();
            }
        }

        if (extraMessages is not null)
        {
            // Tool follow-up: history already has the original user turn when using Conversation;
            // when building a one-shot, include user then tool turns.
            if (!HistoryContainsUserTurn(userQuery, userParts))
            {
                if (userParts is not null)
                    builder.AddUser(userParts);
                else
                    builder.AddUser(userQuery);
            }

            foreach (var message in extraMessages)
                builder.AddMessage(message);
        }
        else
        {
            if (userParts is not null)
                builder.AddUser(userParts);
            else
                builder.AddUser(userQuery);
        }

        if (_metadata is not null)
            builder.WithMetadata(_metadata);
        else
            builder.WithName(Name);

        if (_responseFormat is not null)
            builder.WithResponseFormat(_responseFormat);

        return builder.Build();
    }

    private bool HistoryContainsUserTurn(string userQuery, List<IContentPart>? userParts)
    {
        foreach (var message in GetHistoryMessages())
        {
            if (message.Role != MessageRole.User)
                continue;
            if (userParts is null && message.Content == userQuery)
                return true;
        }

        return false;
    }

    private IEnumerable<IMessage> GetHistoryMessages()
    {
        if (_conversation is null)
            yield break;

        var history = _conversation.Messages;
        if (_memoryWindow is int window && window >= 0 && window < history.Count)
            history = history.Skip(history.Count - window).ToList();

        foreach (var message in history)
            yield return message;
    }

    private string? ComposeSystemInstructions()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(_instructions))
            sb.Append(_instructions!.Trim());

        var catalogTools = _toolCatalog?.GetTools() ?? Array.Empty<AgentToolDescriptor>();
        if (_toolNames.Count > 0 || catalogTools.Count > 0)
        {
            if (sb.Length > 0)
                sb.Append("\n\n");
            sb.Append("Available tools:");
            var names = _toolNames.Count > 0
                ? _toolNames
                : catalogTools.Select(t => t.Name).ToList();

            foreach (var name in names)
            {
                var descriptor = catalogTools.FirstOrDefault(t => t.Name == name);
                sb.Append("\n- ").Append(name);
                if (descriptor?.Description is not null)
                    sb.Append(": ").Append(descriptor.Description);
                if (descriptor?.ParametersSchemaJson is not null)
                    sb.Append("\n  Parameters schema: ").Append(descriptor.ParametersSchemaJson);
            }

            sb.Append("\nUse tools via tool calls when needed. The host executes tools and returns results.");
        }

        return sb.Length == 0 ? null : sb.ToString();
    }
}
