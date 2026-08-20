using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Content;

/// <summary>Image content part (HTTPS URL or base64 string + MIME type).</summary>
public sealed class ImagePart : IContentPart
{
    /// <inheritdoc />
    public string Type => "image";

    /// <summary>Gets the image URL, when sourced from a remote location.</summary>
    public string? Url { get; }

    /// <summary>Gets the base64-encoded image payload, when inlined. Host encodes bytes.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type for base64 images (e.g. <c>image/png</c>).</summary>
    public string? MediaType { get; }

    /// <summary>Gets an optional detail hint (OpenAI <c>low</c>/<c>high</c>/<c>auto</c>).</summary>
    public string? Detail { get; }

    private ImagePart(string? url, string? base64Data, string? mediaType, string? detail)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Image part requires a URL or base64 data.");

        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType;
        Detail = detail;
    }

    /// <summary>Creates an image part from a URL.</summary>
    public static ImagePart FromUrl(string url, string? detail = null) =>
        new(url ?? throw new ArgumentNullException(nameof(url)), null, null, detail);

    /// <summary>Creates an image part from a base64 string (not raw bytes).</summary>
    public static ImagePart FromBase64(string base64Data, string mediaType, string? detail = null)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type is required for base64 images.", nameof(mediaType));
        return new(null, base64Data, mediaType, detail);
    }
}
