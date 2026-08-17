namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// A citation or grounding annotation attached to a message (typically assistant).
/// </summary>
public sealed class MessageAnnotation
{
    /// <summary>Gets the annotation kind (e.g. <c>url_citation</c>, <c>file_citation</c>, <c>internal_ref</c>).</summary>
    public string Kind { get; }

    /// <summary>Gets an optional URL for web citations.</summary>
    public string? Url { get; }

    /// <summary>Gets an optional file / document identifier.</summary>
    public string? FileId { get; }

    /// <summary>Gets an optional human title.</summary>
    public string? Title { get; }

    /// <summary>Gets an optional quoted or summarized excerpt.</summary>
    public string? Quote { get; }

    /// <summary>Gets the start index into text content, if applicable.</summary>
    public int? StartIndex { get; }

    /// <summary>Gets the end index into text content, if applicable.</summary>
    public int? EndIndex { get; }

    /// <summary>Creates an annotation.</summary>
    /// <param name="kind">Annotation kind.</param>
    /// <param name="url">Optional URL.</param>
    /// <param name="fileId">Optional file id.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="quote">Optional quote.</param>
    /// <param name="startIndex">Optional start index.</param>
    /// <param name="endIndex">Optional end index.</param>
    public MessageAnnotation(
        string kind,
        string? url = null,
        string? fileId = null,
        string? title = null,
        string? quote = null,
        int? startIndex = null,
        int? endIndex = null)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Annotation kind cannot be empty.", nameof(kind));

        Kind = kind.Trim().ToLowerInvariant();
        Url = url;
        FileId = fileId;
        Title = title;
        Quote = quote;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    /// <summary>Creates a URL citation annotation.</summary>
    /// <param name="url">Cited URL.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="quote">Optional excerpt.</param>
    /// <param name="startIndex">Optional start index.</param>
    /// <param name="endIndex">Optional end index.</param>
    /// <returns>A new <see cref="MessageAnnotation"/>.</returns>
    public static MessageAnnotation UrlCitation(string url, string? title = null, string? quote = null, int? startIndex = null, int? endIndex = null) =>
        new("url_citation", url: url, title: title, quote: quote, startIndex: startIndex, endIndex: endIndex);

    /// <summary>Creates a file citation annotation.</summary>
    /// <param name="fileId">File identifier.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="quote">Optional excerpt.</param>
    /// <returns>A new <see cref="MessageAnnotation"/>.</returns>
    public static MessageAnnotation FileCitation(string fileId, string? title = null, string? quote = null) =>
        new("file_citation", fileId: fileId, title: title, quote: quote);

    /// <summary>Creates an internal reference annotation.</summary>
    /// <param name="reference">Internal reference id.</param>
    /// <param name="title">Optional title.</param>
    /// <returns>A new <see cref="MessageAnnotation"/>.</returns>
    public static MessageAnnotation InternalRef(string reference, string? title = null) =>
        new("internal_ref", fileId: reference, title: title);
}
