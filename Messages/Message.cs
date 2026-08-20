using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Messages;

/// <summary>
/// Abstract base for all messages. <see cref="Parts"/> is the source of truth;
/// <see cref="Content"/> is the concatenated text projection of <see cref="TextPart"/> values.
/// </summary>
public abstract class Message : IMessage, IEquatable<Message>
{
    /// <inheritdoc />
    public MessageRole Role { get; }

    /// <inheritdoc />
    public string Content { get; }

    /// <inheritdoc />
    public IReadOnlyList<IContentPart> Parts { get; }

    /// <inheritdoc />
    public string? Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<MessageAnnotation> Annotations { get; }

    /// <inheritdoc />
    public MessageMetadata Metadata { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <summary>Creates a message whose body is defined by parts (and optional text shorthand).</summary>
    protected Message(
        MessageRole role,
        string? content = null,
        IEnumerable<IContentPart>? parts = null,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        bool allowEmptyBody = false)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        var partList = NormalizeParts(content, parts, allowEmptyBody);

        Role = role;
        Parts = partList;
        Content = string.Concat(partList.OfType<TextPart>().Select(p => p.Text));
        Name = name is { } speaker && !string.IsNullOrWhiteSpace(speaker) ? speaker.Trim() : null;
        Annotations = annotations?.ToList().AsReadOnly() ?? (IReadOnlyList<MessageAnnotation>)Array.Empty<MessageAnnotation>();
        Metadata = metadata ?? MessageMetadata.Empty;
        Id = id ?? Guid.NewGuid().ToString("N");
    }

    private static IReadOnlyList<IContentPart> NormalizeParts(
        string? content,
        IEnumerable<IContentPart>? parts,
        bool allowEmptyBody)
    {
        var list = parts?.Where(p => p is not null).ToList() ?? new List<IContentPart>();

        if (list.Count == 0)
        {
            if (content is { Length: > 0 } text && !string.IsNullOrWhiteSpace(text))
                list.Add(new TextPart(text));
            else if (!allowEmptyBody)
                throw new Exceptions.PromptValidationException("Message content cannot be null or empty.");
        }
        else if (content is { Length: > 0 } extra && !string.IsNullOrWhiteSpace(extra) && !list.OfType<TextPart>().Any())
        {
            list.Insert(0, new TextPart(extra));
        }

        return list.AsReadOnly();
    }

    /// <inheritdoc />
    public bool Equals(Message? other) => other is not null && Id == other.Id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Message message && Equals(message);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc />
    public override string ToString()
    {
        var preview = Content.Length == 0
            ? $"[{Parts.Count} part(s)]"
            : Content.Substring(0, Math.Min(Content.Length, 50));
        return $"[{Role.Name}] {preview}";
    }
}
