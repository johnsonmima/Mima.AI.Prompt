using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Content;

/// <summary>Audio content part.</summary>
public sealed class AudioPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "audio";

    /// <summary>Gets the audio URL, when sourced remotely.</summary>
    public string? Url { get; }

    /// <summary>Gets base64-encoded audio bytes, when inlined.</summary>
    public string? Base64Data { get; }

    /// <summary>Gets the MIME type (e.g. <c>audio/mpeg</c>).</summary>
    public string? MediaType { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    private AudioPart(string? url, string? base64Data, string? mediaType, CacheControl? cacheControl)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Audio part requires a URL or base64 data.");
        Url = url;
        Base64Data = base64Data;
        MediaType = mediaType;
        CacheControl = cacheControl;
    }

    /// <summary>Creates an audio part from a URL.</summary>
    /// <param name="url">Audio URL.</param>
    /// <param name="mediaType">Optional MIME type.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="AudioPart"/>.</returns>
    public static AudioPart FromUrl(string url, string? mediaType = null, CacheControl? cacheControl = null) =>
        new(url, null, mediaType, cacheControl);

    /// <summary>Creates an audio part from base64 data.</summary>
    /// <param name="base64Data">Base64-encoded bytes.</param>
    /// <param name="mediaType">MIME type.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="AudioPart"/>.</returns>
    public static AudioPart FromBase64(string base64Data, string mediaType, CacheControl? cacheControl = null) =>
        new(null, base64Data, mediaType, cacheControl);
}
