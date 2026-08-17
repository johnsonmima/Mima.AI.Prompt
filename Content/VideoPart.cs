using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Content;

/// <summary>Video content part.</summary>
public sealed class VideoPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "video";

    /// <summary>Gets the video URL, when sourced remotely.</summary>
    public string? Url { get; }

    /// <summary>Gets base64-encoded video bytes, when inlined.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type (e.g. <c>video/mp4</c>).</summary>
    public string? MediaType { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    private VideoPart(string? url, string? base64Data, string? mediaType, CacheControl? cacheControl)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Video part requires a URL or base64 data.");
        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType;
        CacheControl = cacheControl;
    }

    /// <summary>Creates a video part from a URL.</summary>
    /// <param name="url">Video URL.</param>
    /// <param name="mediaType">Optional MIME type.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="VideoPart"/>.</returns>
    public static VideoPart FromUrl(string url, string? mediaType = null, CacheControl? cacheControl = null) =>
        new(url, null, mediaType, cacheControl);

    /// <summary>Creates a video part from base64 data.</summary>
    /// <param name="base64Data">Base64-encoded bytes.</param>
    /// <param name="mediaType">MIME type.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="VideoPart"/>.</returns>
    public static VideoPart FromBase64(string base64Data, string mediaType, CacheControl? cacheControl = null) =>
        new(null, base64Data, mediaType, cacheControl);
}
