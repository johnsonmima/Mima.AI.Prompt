using System.Text.Json;
using System.Text.Json.Serialization;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Serialization;

/// <summary>
/// Serializes prompts and messages including multimodal parts, tool calls, annotations, and cache controls.
/// </summary>
public sealed class PromptSerializer : IPromptSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = PromptJsonOptions.IndentedCamelCaseIgnoreNull;

    /// <inheritdoc />
    public string Serialize(IPrompt prompt)
    {
        if (prompt is null)
            throw new ArgumentNullException(nameof(prompt));

        var dto = new PromptDto
        {
            Id = prompt.Id,
            Metadata = ToMetadataDto(prompt.Metadata),
            Messages = prompt.Messages.Select(ToDto).ToList(),
            ResponseFormat = prompt.ResponseFormat is null ? null : new OutputFormatDto
            {
                Type = prompt.ResponseFormat.Type,
                Schema = prompt.ResponseFormat.Schema,
                Instructions = prompt.ResponseFormat.Instructions
            }
        };

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <inheritdoc />
    public IPrompt DeserializePrompt(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty.", nameof(json));

        PromptDto dto = JsonSerializer.Deserialize<PromptDto>(json, JsonOptions)
            ?? throw new JsonException("Failed to deserialize prompt.");

        List<IMessage> messages = dto.Messages.Select(FromDto).ToList();
        return new Models.Prompt(messages, FromMetadataDto(dto.Metadata), dto.Id, FromFormatDto(dto.ResponseFormat));
    }

    /// <inheritdoc />
    public string SerializeMessage(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));
        return JsonSerializer.Serialize(ToDto(message), JsonOptions);
    }

    /// <inheritdoc />
    public IMessage DeserializeMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty.", nameof(json));

        var dto = JsonSerializer.Deserialize<MessageDto>(json, JsonOptions)
            ?? throw new JsonException("Failed to deserialize message.");
        return FromDto(dto);
    }

    /// <inheritdoc />
    public string SerializeTemplate(IMessageTemplate template)
    {
        if (template is null)
            throw new ArgumentNullException(nameof(template));

        var dto = new TemplateDto
        {
            Role = template.Role.Name,
            TemplateContent = template.TemplateContent,
            Variables = template.Variables.ToList(),
            Metadata = ToMetadataDto(template.Metadata)
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static MessageDto ToDto(IMessage message)
    {
        var dto = new MessageDto
        {
            Role = message.Role.Name,
            Content = message.Content,
            Id = message.Id,
            Name = message.Name,
            Metadata = ToMetadataDto(message.Metadata),
            Parts = message.Parts.Select(ToPartDto).ToList(),
            Annotations = message.Annotations.Select(a => new AnnotationDto
            {
                Kind = a.Kind,
                Url = a.Url,
                FileId = a.FileId,
                Title = a.Title,
                Quote = a.Quote,
                StartIndex = a.StartIndex,
                EndIndex = a.EndIndex
            }).ToList(),
            CacheControl = message.CacheControl is null ? null : new CacheControlDto
            {
                Type = message.CacheControl.Type,
                TtlSeconds = message.CacheControl.Ttl?.TotalSeconds
            }
        };

        if (message is ToolMessage tool)
            dto.ToolCallId = tool.ToolCallId;
        else if (message is FunctionMessage function)
            dto.FunctionName = function.FunctionName;
        else if (message is AssistantMessage assistant)
        {
            dto.Reasoning = assistant.Reasoning;
            dto.Refusal = assistant.Refusal;
            dto.ToolCalls = assistant.ToolCalls.Select(t => new ToolCallDto
            {
                Id = t.Id,
                Name = t.Name,
                ArgumentsJson = t.ArgumentsJson
            }).ToList();
        }

        return dto;
    }

    private static PartDto ToPartDto(IContentPart part) => part switch
    {
        TextPart t => new PartDto { Type = "text", Text = t.Text, CacheControl = ToCacheDto(t.CacheControl) },
        ImagePart i => new PartDto { Type = "image", Url = i.Url, Base64Data = i.Base64Data, MediaType = i.MediaType, Detail = i.Detail, CacheControl = ToCacheDto(i.CacheControl) },
        FilePart f => new PartDto { Type = "file", FileId = f.FileId, Url = f.Url, Base64Data = f.Base64Data, MediaType = f.MediaType, Filename = f.Filename, CacheControl = ToCacheDto(f.CacheControl) },
        AudioPart a => new PartDto { Type = "audio", Url = a.Url, Base64Data = a.Base64Data, MediaType = a.MediaType, CacheControl = ToCacheDto(a.CacheControl) },
        VideoPart v => new PartDto { Type = "video", Url = v.Url, Base64Data = v.Base64Data, MediaType = v.MediaType, CacheControl = ToCacheDto(v.CacheControl) },
        ThinkingPart th => new PartDto { Type = "thinking", Text = th.Text, CacheControl = ToCacheDto(th.CacheControl) },
        ScreenshotPart s => new PartDto { Type = "screenshot", Url = s.Url, Base64Data = s.Base64Data, MediaType = s.MediaType, CacheControl = ToCacheDto(s.CacheControl) },
        ComputerActionPart c => new PartDto { Type = "computer_action", Action = c.Action, ArgumentsJson = c.ArgumentsJson, CacheControl = ToCacheDto(c.CacheControl) },
        _ => new PartDto { Type = part.Type }
    };

    private static CacheControlDto? ToCacheDto(CacheControl? c) =>
        c is null ? null : new CacheControlDto { Type = c.Type, TtlSeconds = c.Ttl?.TotalSeconds };

    private static IContentPart FromPartDto(PartDto dto) => dto.Type switch
    {
        "text" => new TextPart(dto.Text ?? string.Empty, FromCacheDto(dto.CacheControl)),
        "image" when !string.IsNullOrEmpty(dto.Url) => ImagePart.FromUrl(dto.Url!, dto.Detail, FromCacheDto(dto.CacheControl)),
        "image" => ImagePart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "image/png", dto.Detail, FromCacheDto(dto.CacheControl)),
        "file" when !string.IsNullOrEmpty(dto.FileId) => FilePart.FromId(dto.FileId!, dto.Filename, FromCacheDto(dto.CacheControl)),
        "file" when !string.IsNullOrEmpty(dto.Url) => FilePart.FromUrl(dto.Url!, dto.MediaType, dto.Filename, FromCacheDto(dto.CacheControl)),
        "file" => FilePart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "application/octet-stream", dto.Filename, FromCacheDto(dto.CacheControl)),
        "audio" when !string.IsNullOrEmpty(dto.Url) => AudioPart.FromUrl(dto.Url!, dto.MediaType, FromCacheDto(dto.CacheControl)),
        "audio" => AudioPart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "audio/mpeg", FromCacheDto(dto.CacheControl)),
        "video" when !string.IsNullOrEmpty(dto.Url) => VideoPart.FromUrl(dto.Url!, dto.MediaType, FromCacheDto(dto.CacheControl)),
        "video" => VideoPart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "video/mp4", FromCacheDto(dto.CacheControl)),
        "thinking" => string.IsNullOrWhiteSpace(dto.Text)
            ? new TextPart(string.Empty, FromCacheDto(dto.CacheControl))
            : ThinkingPart.Create(dto.Text!, FromCacheDto(dto.CacheControl)),
        "screenshot" when !string.IsNullOrEmpty(dto.Url) => ScreenshotPart.FromUrl(dto.Url!, FromCacheDto(dto.CacheControl)),
        "screenshot" => ScreenshotPart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "image/png", FromCacheDto(dto.CacheControl)),
        "computer_action" => ComputerActionPart.Create(dto.Action ?? "unknown", dto.ArgumentsJson, FromCacheDto(dto.CacheControl)),
        _ => new TextPart(dto.Text ?? string.Empty)
    };

    private static CacheControl? FromCacheDto(CacheControlDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Type)) return null;
        TimeSpan? ttl = dto.TtlSeconds is null ? null : TimeSpan.FromSeconds(dto.TtlSeconds.Value);
        return CacheControl.Custom(dto.Type, ttl);
    }

    private static IMessage FromDto(MessageDto dto)
    {
        var role = MessageRole.ParseOrCreate(dto.Role);
        var metadata = FromMetadataDto(dto.Metadata);
        var annotations = dto.Annotations?.Select(a =>
            new MessageAnnotation(a.Kind, a.Url, a.FileId, a.Title, a.Quote, a.StartIndex, a.EndIndex)).ToList();
        var cache = FromCacheDto(dto.CacheControl);

        // Parts are canonical. Content is only used when parts were omitted (older / minimal JSON).
        IReadOnlyList<IContentPart>? parts = dto.Parts is { Count: > 0 }
            ? dto.Parts.Select(FromPartDto).ToList()
            : null;
        var contentShorthand = parts is null ? dto.Content : null;

        if (role == MessageRole.System)
        {
            return parts is not null
                ? SystemMessage.Create(parts, metadata, dto.Id, dto.Name, annotations, cache)
                : new SystemMessage(contentShorthand ?? string.Empty, metadata, dto.Id);
        }

        if (role == MessageRole.Developer)
        {
            return parts is not null
                ? DeveloperMessage.Create(parts, metadata, dto.Id, cache)
                : new DeveloperMessage(contentShorthand ?? string.Empty, metadata, dto.Id);
        }

        if (role == MessageRole.User)
        {
            return parts is not null
                ? UserMessage.Create(parts, metadata, dto.Name, dto.Id, annotations, cache)
                : new UserMessage(contentShorthand ?? string.Empty, metadata, dto.Id);
        }

        if (role == MessageRole.Assistant)
        {
            var toolCalls = dto.ToolCalls?.Select(t => new ToolCall(t.Id, t.Name, t.ArgumentsJson ?? "{}")).ToList();
            return AssistantMessage.CreateDetailed(
                content: contentShorthand,
                parts: parts,
                toolCalls: toolCalls,
                reasoning: dto.Reasoning,
                refusal: dto.Refusal,
                metadata: metadata,
                id: dto.Id,
                name: dto.Name,
                annotations: annotations,
                cacheControl: cache);
        }

        if (role == MessageRole.Tool)
            return new ToolMessage(dto.ToolCallId ?? dto.Id ?? "unknown", dto.Content, metadata, dto.Id);

        if (role == MessageRole.Function)
            return new FunctionMessage(dto.FunctionName ?? dto.Id ?? "unknown", dto.Content, metadata, dto.Id);

        return parts is not null
            ? CustomMessage.Create(role, parts, metadata, dto.Id, dto.Name, annotations, cache)
            : new CustomMessage(role, contentShorthand ?? string.Empty, metadata, dto.Id);
    }

    private static MetadataDto ToMetadataDto(MessageMetadata metadata) => new()
    {
        Name = metadata.Name,
        Description = metadata.Description,
        Category = metadata.Category,
        Version = metadata.Version,
        Author = metadata.Author,
        Tags = metadata.Tags.ToList(),
        Language = metadata.Language,
        Provider = metadata.Provider,
        Model = metadata.Model,
        Created = metadata.Created,
        Modified = metadata.Modified,
        MinCompatibleVersion = metadata.MinCompatibleVersion
    };

    private static OutputFormat? FromFormatDto(OutputFormatDto? dto)
    {
        if (dto is null)
            return null;

        var type = dto.Type ?? string.Empty;
        var instructions = dto.Instructions ?? string.Empty;
        var schema = dto.Schema;

        if (!string.IsNullOrEmpty(schema))
        {
            if (string.Equals(type, "json", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.JsonWithSchema(schema!);
            if (string.Equals(type, "yaml", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.YamlWithSchema(schema!);
        }

        return OutputFormat.Custom(type, instructions, schema);
    }

    private static MessageMetadata FromMetadataDto(MetadataDto? dto)
    {
        if (dto is null)
            return MessageMetadata.Empty;
        return new MessageMetadata(
            dto.Name, dto.Description, dto.Category, dto.Version, dto.Author,
            dto.Tags, dto.Language, dto.Provider, dto.Model, dto.Created, dto.Modified,
            dto.MinCompatibleVersion);
    }

    private sealed class PromptDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("metadata")] public MetadataDto? Metadata { get; set; }
        [JsonPropertyName("messages")] public List<MessageDto> Messages { get; set; } = new();
        [JsonPropertyName("responseFormat")] public OutputFormatDto? ResponseFormat { get; set; }
    }

    private sealed class MessageDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("toolCallId")] public string? ToolCallId { get; set; }
        [JsonPropertyName("functionName")] public string? FunctionName { get; set; }
        [JsonPropertyName("reasoning")] public string? Reasoning { get; set; }
        [JsonPropertyName("refusal")] public string? Refusal { get; set; }
        [JsonPropertyName("toolCalls")] public List<ToolCallDto>? ToolCalls { get; set; }
        [JsonPropertyName("parts")] public List<PartDto>? Parts { get; set; }
        [JsonPropertyName("annotations")] public List<AnnotationDto>? Annotations { get; set; }
        [JsonPropertyName("cacheControl")] public CacheControlDto? CacheControl { get; set; }
        [JsonPropertyName("metadata")] public MetadataDto? Metadata { get; set; }
    }

    private sealed class PartDto
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "text";
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("base64Data")] public string? Base64Data { get; set; }
        [JsonPropertyName("mediaType")] public string? MediaType { get; set; }
        [JsonPropertyName("detail")] public string? Detail { get; set; }
        [JsonPropertyName("fileId")] public string? FileId { get; set; }
        [JsonPropertyName("filename")] public string? Filename { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("argumentsJson")] public string? ArgumentsJson { get; set; }
        [JsonPropertyName("cacheControl")] public CacheControlDto? CacheControl { get; set; }
    }

    private sealed class ToolCallDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("argumentsJson")] public string? ArgumentsJson { get; set; }
    }

    private sealed class AnnotationDto
    {
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("fileId")] public string? FileId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("quote")] public string? Quote { get; set; }
        [JsonPropertyName("startIndex")] public int? StartIndex { get; set; }
        [JsonPropertyName("endIndex")] public int? EndIndex { get; set; }
    }

    private sealed class CacheControlDto
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("ttlSeconds")] public double? TtlSeconds { get; set; }
    }

    private sealed class OutputFormatDto
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("schema")] public string? Schema { get; set; }
        [JsonPropertyName("instructions")] public string? Instructions { get; set; }
    }

    private sealed class TemplateDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("templateContent")] public string TemplateContent { get; set; } = string.Empty;
        [JsonPropertyName("variables")] public List<string> Variables { get; set; } = new();
        [JsonPropertyName("metadata")] public MetadataDto? Metadata { get; set; }
    }

    private sealed class MetadataDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("created")] public DateTimeOffset Created { get; set; }
        [JsonPropertyName("modified")] public DateTimeOffset Modified { get; set; }
        [JsonPropertyName("minCompatibleVersion")] public string? MinCompatibleVersion { get; set; }
    }
}
