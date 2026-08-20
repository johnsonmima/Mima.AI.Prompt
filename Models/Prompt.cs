using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Models;

/// <summary>
/// Immutable prompt composed of ordered messages, optional metadata, and optional response format.
/// </summary>
public sealed class Prompt : IPrompt
{
    /// <inheritdoc />
    public IReadOnlyList<IMessage> Messages { get; }

    /// <inheritdoc />
    public MessageMetadata Metadata { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public int MessageCount => Messages.Count;

    /// <inheritdoc />
    public OutputFormat? ResponseFormat { get; }

    internal Prompt(
        IEnumerable<IMessage> messages,
        MessageMetadata? metadata = null,
        string? id = null,
        OutputFormat? responseFormat = null)
    {
        var messageList = messages?.ToList() ?? throw new ArgumentNullException(nameof(messages));
        if (messageList.Count == 0)
            throw new Exceptions.PromptValidationException("A prompt must contain at least one message.");

        Messages = messageList.AsReadOnly();
        Metadata = metadata ?? MessageMetadata.Empty;
        Id = id ?? Guid.NewGuid().ToString("N");
        ResponseFormat = responseFormat;
    }

    /// <summary>Gets the first system message, if any.</summary>
    public IMessage? SystemMessage => Messages.FirstOrDefault(m => m.Role == Roles.MessageRole.System);

    /// <summary>Gets the last user message, if any.</summary>
    public IMessage? LastUserMessage => Messages.LastOrDefault(m => m.Role == Roles.MessageRole.User);

    /// <summary>Gets all messages for a specific role.</summary>
    public IReadOnlyList<IMessage> GetMessages(Roles.MessageRole role) =>
        Messages.Where(m => m.Role == role).ToList().AsReadOnly();

    /// <inheritdoc />
    public override string ToString() => $"Prompt [{MessageCount} messages] {Metadata.Name ?? Id}";
}
