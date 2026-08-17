using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Providers;

/// <summary>
/// Shared content mapping for provider adapters.
/// Emits a plain string for single-text messages; otherwise a list of part objects.
/// Thinking parts are omitted from the wire payload by default.
/// </summary>
internal static class ProviderContentMapper
{
    /// <summary>
    /// Maps message body for chat APIs. Returns <see cref="string"/> or a list of anonymous part objects.
    /// </summary>
    public static object MapContent(IMessage message, bool includeThinking = false)
    {
        var parts = includeThinking
            ? message.Parts.ToList()
            : message.Parts.Where(p => p is not ThinkingPart).ToList();

        if (parts.Count == 0)
            return message.Content;

        if (parts.Count == 1 && parts[0] is TextPart onlyText)
            return onlyText.Text;

        return parts.Select(MapPart).ToList();
    }

    /// <summary>True when the message has non-text (or multi-part) content that string-only APIs would lose.</summary>
    public static bool HasStructuredParts(IMessage message) =>
        message.Parts.Any(p => p is not TextPart and not ThinkingPart)
        || message.Parts.OfType<TextPart>().Count() > 1
        || message.Parts.OfType<ThinkingPart>().Any();

    private static object MapPart(IContentPart part) => part switch
    {
        TextPart t => new { type = "text", text = t.Text },
        ThinkingPart th => new { type = "thinking", thinking = th.Text },
        ImagePart i when i.Url is not null => new { type = "image_url", image_url = new { url = i.Url, detail = i.Detail } },
        ImagePart i => new
        {
            type = "image_url",
            image_url = new { url = $"data:{i.MediaType};base64,{i.Base64Data}", detail = i.Detail }
        },
        FilePart f => new { type = "file", file = new { file_id = f.FileId, url = f.Url, filename = f.Filename } },
        AudioPart a when a.Url is not null => new { type = "audio_url", audio_url = new { url = a.Url } },
        AudioPart a => new { type = "input_audio", input_audio = new { data = a.Base64Data, format = a.MediaType } },
        VideoPart v when v.Url is not null => new { type = "video_url", video_url = new { url = v.Url } },
        VideoPart v => new { type = "video", video = new { data = v.Base64Data, media_type = v.MediaType } },
        ScreenshotPart s when s.Url is not null => new { type = "image_url", image_url = new { url = s.Url } },
        ScreenshotPart s => new
        {
            type = "image_url",
            image_url = new { url = $"data:{s.MediaType};base64,{s.Base64Data}" }
        },
        ComputerActionPart c => new { type = "computer_action", action = c.Action, arguments = c.ArgumentsJson },
        _ => new { type = part.Type }
    };
}
