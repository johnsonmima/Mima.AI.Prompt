using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Content;

/// <summary>Image content part (URL or base64).</summary>
public sealed class ImagePart : IContentPart
{
    /// <inheritdoc />
    public string Type => "image";

    /// <summary>Gets the image URL, when sourced from a remote location.</summary>
    public string? Url { get; }

    /// <summary>Gets the base64-encoded image payload, when inlined.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type for base64 images (e.g. <c>image/png</c>).</summary>
    public string? MediaType { get; }

    /// <summary>Gets an optional detail hint (e.g. OpenAI <c>low</c>/<c>high</c>/<c>auto</c>).</summary>
    public string? Detail { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    private ImagePart(string? url, string? base64Data, string? mediaType, string? detail, CacheControl? cacheControl)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Image part requires a URL or base64 data.");

        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType;
        Detail = detail;
        CacheControl = cacheControl;
    }

    /// <summary>Creates an image part from a URL.</summary>
    /// <param name="url">Image URL.</param>
    /// <param name="detail">Optional detail hint.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ImagePart"/>.</returns>
    public static ImagePart FromUrl(string url, string? detail = null, CacheControl? cacheControl = null) =>
        new(url ?? throw new ArgumentNullException(nameof(url)), null, null, detail, cacheControl);

    /// <summary>Creates an image part from base64 data.</summary>
    /// <param name="base64Data">Base64-encoded bytes.</param>
    /// <param name="mediaType">MIME type (required).</param>
    /// <param name="detail">Optional detail hint.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ImagePart"/>.</returns>
    public static ImagePart FromBase64(string base64Data, string mediaType, string? detail = null, CacheControl? cacheControl = null)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type is required for base64 images.", nameof(mediaType));
        return new(null, base64Data, mediaType, detail, cacheControl);
    }
}
