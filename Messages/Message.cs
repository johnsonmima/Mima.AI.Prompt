using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Messages;

/// <summary>
/// Abstract base for all messages. <see cref="Parts"/> is the source of truth;
/// <see cref="Content"/> is the concatenated text projection of <see cref="TextPart"/> values.
/// </summary>
/// <remarks>
/// Construction always goes through <c>NormalizeParts</c> so string factories
/// (<c>Create("hi")</c>) and multimodal factories (<c>Create(parts)</c>) share one body model.
/// </remarks>
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
    public CacheControl? CacheControl { get; }

    /// <inheritdoc />
    public MessageMetadata Metadata { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <summary>
    /// Creates a message whose body is defined by <paramref name="parts"/> (and optional text shorthand).
    /// </summary>
    /// <param name="role">Message role.</param>
    /// <param name="content">
    /// Text shorthand. Used only when <paramref name="parts"/> is null/empty, or when parts exist
    /// but contain no <see cref="TextPart"/> (then prepended). Ignored when parts already include text.
    /// </param>
    /// <param name="parts">Structured content parts — preferred representation.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="id">Optional id; generated when omitted.</param>
    /// <param name="name">Optional speaker / agent name.</param>
    /// <param name="annotations">Optional citations.</param>
    /// <param name="cacheControl">Optional message-level cache control.</param>
    /// <param name="allowEmptyBody">Allows empty parts (tool-call-only / refusal-only).</param>
    protected Message(
        MessageRole role,
        string? content = null,
        IEnumerable<IContentPart>? parts = null,
        MessageMetadata? metadata = null,
        string? id = null,
        string? name = null,
        IEnumerable<MessageAnnotation>? annotations = null,
        CacheControl? cacheControl = null,
        bool allowEmptyBody = false)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        // Unify string shorthand + structured parts into one Parts list (source of truth).
        var partList = NormalizeParts(content, parts, allowEmptyBody);

        Role = role;
        Parts = partList;
        // Content is derived — never stored separately from TextParts.
        Content = string.Concat(partList.OfType<TextPart>().Select(p => p.Text));
        Name = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
        Annotations = annotations?.ToList().AsReadOnly() ?? (IReadOnlyList<MessageAnnotation>)Array.Empty<MessageAnnotation>();
        CacheControl = cacheControl;
        Metadata = metadata ?? MessageMetadata.Empty;
        Id = id ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Builds the canonical <see cref="Parts"/> list from optional string shorthand and/or explicit parts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why this exists: callers use two convenient entry points that must end as one representation —
    /// <c>UserMessage.Create("hi")</c> (string) and <c>UserMessage.Create(parts)</c> (multimodal).
    /// <see cref="Parts"/> is always the source of truth; <see cref="Content"/> is computed afterward
    /// by concatenating every <see cref="TextPart"/>.
    /// </para>
    /// <para>
    /// Rules:
    /// <list type="number">
    /// <item><description>No parts + non-empty <paramref name="content"/> → one <see cref="TextPart"/>.</description></item>
    /// <item><description>No parts + empty content → throw, unless <paramref name="allowEmptyBody"/> (tool-call / refusal).</description></item>
    /// <item><description>Parts present with no <see cref="TextPart"/> + non-empty content → prepend a text part.</description></item>
    /// <item><description>Parts already include text → ignore <paramref name="content"/> (parts win; avoid duplicating text).</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<IContentPart> NormalizeParts(
        string? content,
        IEnumerable<IContentPart>? parts,
        bool allowEmptyBody)
    {
        // Drop null entries so Create(new IContentPart[] { text, null }) stays safe.
        var list = parts?.Where(p => p is not null).ToList() ?? new List<IContentPart>();

        if (list.Count == 0)
        {
            // Case A — string-only API, e.g. UserMessage.Create("Hello")
            //   content = "Hello", parts = null  →  Parts = [ TextPart("Hello") ]
            if (!string.IsNullOrWhiteSpace(content))
            {
                list.Add(new TextPart(content!));
            }
            // Case B — intentionally empty body, e.g. assistant tool_calls-only or refusal
            //   content = null, parts = empty, allowEmptyBody = true  →  Parts = []
            // Case C — invalid empty message
            //   content = null/"  ", parts = empty, allowEmptyBody = false  →  throw
            else if (!allowEmptyBody)
            {
                throw new Exceptions.PromptValidationException("Message content cannot be null or empty.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(content) && !list.OfType<TextPart>().Any())
        {
            // Case D — multimodal parts without text, plus a content shorthand:
            //   content = "Describe this", parts = [ ImagePart(...) ]
            //   → Parts = [ TextPart("Describe this"), ImagePart(...) ]
            // If parts already contain a TextPart, content is ignored so we don't double the text.
            list.Insert(0, new TextPart(content!));
        }
        // Case E — parts already carry text (and maybe images/files):
        //   content = "ignored", parts = [ TextPart("See"), ImagePart(...) ]
        //   → Parts unchanged; Content projection becomes "See"

        return list.AsReadOnly();
    }

    /// <inheritdoc />
    public bool Equals(Message? other) =>
        other is not null && Id == other.Id;

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
