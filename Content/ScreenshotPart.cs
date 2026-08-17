using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Content;

/// <summary>Screenshot / frame used in computer-use agent loops.</summary>
public sealed class ScreenshotPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "screenshot";

    /// <summary>Gets the screenshot URL, when sourced remotely.</summary>
    public string? Url { get; }

    /// <summary>Gets base64-encoded screenshot bytes, when inlined.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type (defaults to <c>image/png</c>).</summary>
    public string? MediaType { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    private ScreenshotPart(string? url, string? base64Data, string? mediaType, CacheControl? cacheControl)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Screenshot part requires a URL or base64 data.");
        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType ?? "image/png";
        CacheControl = cacheControl;
    }

    /// <summary>Creates a screenshot part from a URL.</summary>
    /// <param name="url">Image URL.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ScreenshotPart"/>.</returns>
    public static ScreenshotPart FromUrl(string url, CacheControl? cacheControl = null) =>
        new(url, null, "image/png", cacheControl);

    /// <summary>Creates a screenshot part from base64 data.</summary>
    /// <param name="base64Data">Base64-encoded bytes.</param>
    /// <param name="mediaType">MIME type (default <c>image/png</c>).</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ScreenshotPart"/>.</returns>
    public static ScreenshotPart FromBase64(string base64Data, string mediaType = "image/png", CacheControl? cacheControl = null) =>
        new(null, base64Data, mediaType, cacheControl);
}
