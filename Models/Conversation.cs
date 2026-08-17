using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;

namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// Represents an ongoing conversation with history support.
/// Conversations track message exchanges and can be converted to prompts.
/// </summary>
/// <remarks>
/// Unlike <see cref="Prompt"/> which is immutable, a <see cref="Conversation"/>
/// grows over time as new messages are exchanged. It becomes the foundation
/// for chat applications and memory-based AI interactions.
/// </remarks>
/// <example>
/// <code>
/// var conversation = Conversation.Create("Coding Help")
///     .WithSystem("You are a helpful coding assistant.")
///     .AddUser("How do I read a file in C#?")
///     .AddAssistant("You can use File.ReadAllText() or StreamReader...");
///
/// var prompt = conversation.ToPrompt();
/// </code>
/// </example>
public sealed class Conversation
{
    private readonly List<IMessage> _messages = new();
    private IMessage? _systemMessage;

    /// <summary>Gets the conversation name.</summary>
    public string Name { get; }

    /// <summary>Gets the conversation ID.</summary>
    public string Id { get; }

    /// <summary>Gets all messages in the conversation (including system).</summary>
    public IReadOnlyList<IMessage> Messages => _messages.AsReadOnly();

    /// <summary>Gets the system message, if set.</summary>
    public IMessage? SystemMessage => _systemMessage;

    /// <summary>Gets the number of messages in the conversation.</summary>
    public int MessageCount => _messages.Count;

    /// <summary>Gets when the conversation was created.</summary>
    public DateTimeOffset Created { get; }

    private Conversation(string name, string? id = null)
    {
        Name = name;
        Id = id ?? Guid.NewGuid().ToString("N");
        Created = DateTimeOffset.UtcNow;
    }

    /// <summary>Creates a new conversation.</summary>
    /// <param name="name">The conversation name.</param>
    /// <returns>A new conversation instance.</returns>
    public static Conversation Create(string name) => new(name);

    /// <summary>Sets the system message for this conversation.</summary>
    /// <param name="content">The system instruction content.</param>
    /// <returns>This conversation for chaining.</returns>
    public Conversation WithSystem(string content)
    {
        _systemMessage = new SystemMessage(content);
        return this;
    }

    /// <summary>Adds a user message.</summary>
    /// <param name="content">The user's input.</param>
    /// <returns>This conversation for chaining.</returns>
    public Conversation AddUser(string content)
    {
        _messages.Add(new UserMessage(content));
        return this;
    }

    /// <summary>Adds an assistant message.</summary>
    /// <param name="content">The assistant's response.</param>
    /// <returns>This conversation for chaining.</returns>
    public Conversation AddAssistant(string content)
    {
        _messages.Add(new AssistantMessage(content));
        return this;
    }

    /// <summary>Adds any message to the conversation.</summary>
    /// <param name="message">The message to add.</param>
    /// <returns>This conversation for chaining.</returns>
    public Conversation AddMessage(IMessage message)
    {
        _messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
        return this;
    }

    /// <summary>
    /// Converts this conversation to an immutable prompt.
    /// Includes the system message (if set) followed by all conversation messages.
    /// </summary>
    /// <returns>An immutable prompt representing this conversation.</returns>
    public Prompt ToPrompt()
    {
        List<IMessage> allMessages = [];

        if (_systemMessage is not null)
            allMessages.Add(_systemMessage);

        allMessages.AddRange(_messages);

        return new Prompt(allMessages, new MessageMetadata(name: Name));
    }

    /// <summary>
    /// Gets the last N messages for windowed context.
    /// Useful for keeping context within token limits.
    /// </summary>
    /// <param name="count">Maximum number of recent messages to include.</param>
    /// <returns>A prompt with the system message and last N conversation messages.</returns>
    public Prompt ToPromptWithWindow(int count)
    {
        var allMessages = new List<IMessage>();

        if (_systemMessage is not null)
            allMessages.Add(_systemMessage);

        // TakeLast is net6+; use Skip for netstandard2.0 compatibility
        var take = Math.Max(0, count);
        var recentMessages = take >= _messages.Count
            ? _messages
            : _messages.Skip(_messages.Count - take);
        allMessages.AddRange(recentMessages);

        return new Prompt(allMessages, new MessageMetadata(name: Name));
    }
}
