using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Content;

/// <summary>File / document content part (PDF, etc.).</summary>
public sealed class FilePart : IContentPart
{
    /// <inheritdoc />
    public string Type => "file";

    /// <summary>Gets a provider file identifier, when applicable.</summary>
    public string? FileId { get; }

    /// <summary>Gets a remote file URL, when applicable.</summary>
    public string? Url { get; }

    /// <summary>Gets base64-encoded file bytes, when inlined.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type.</summary>
    public string? MediaType { get; }

    /// <summary>Gets an optional display filename.</summary>
    public string? Filename { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    private FilePart(string? fileId, string? url, string? base64Data, string? mediaType, string? filename, CacheControl? cacheControl)
    {
        if (string.IsNullOrWhiteSpace(fileId) && string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("File part requires a file id, URL, or base64 data.");

        FileId = fileId;
        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType;
        Filename = filename;
        CacheControl = cacheControl;
    }

    /// <summary>Creates a file part from a provider file id.</summary>
    /// <param name="fileId">Provider file identifier.</param>
    /// <param name="filename">Optional display name.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="FilePart"/>.</returns>
    public static FilePart FromId(string fileId, string? filename = null, CacheControl? cacheControl = null) =>
        new(fileId, null, null, null, filename, cacheControl);

    /// <summary>Creates a file part from a URL.</summary>
    /// <param name="url">File URL.</param>
    /// <param name="mediaType">Optional MIME type.</param>
    /// <param name="filename">Optional display name.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="FilePart"/>.</returns>
    public static FilePart FromUrl(string url, string? mediaType = null, string? filename = null, CacheControl? cacheControl = null) =>
        new(null, url, null, mediaType, filename, cacheControl);

    /// <summary>Creates a file part from base64 data.</summary>
    /// <param name="base64Data">Base64-encoded bytes.</param>
    /// <param name="mediaType">MIME type.</param>
    /// <param name="filename">Optional display name.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="FilePart"/>.</returns>
    public static FilePart FromBase64(string base64Data, string mediaType, string? filename = null, CacheControl? cacheControl = null) =>
        new(null, null, base64Data, mediaType, filename, cacheControl);
}
