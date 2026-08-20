using System.Text.Json;
using System.Text.Json.Serialization;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Serialization;

/// <summary>
/// Serializes prompts and messages. Message <c>parts</c> are the only persisted body.
/// <see cref="IMessage.Content"/> is concatenated text parts in memory and is not written.
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
            Id = message.Id,
            Name = message.Name,
            Metadata = ToMetadataDto(message.Metadata),
            Parts = message.Parts.Count == 0 ? null : message.Parts.Select(ToPartDto).ToList(),
            Annotations = message.Annotations.Count == 0 ? null : message.Annotations.Select(a => new AnnotationDto
            {
                Kind = a.Kind,
                Url = a.Url,
                FileId = a.FileId,
                Title = a.Title,
                Quote = a.Quote,
                StartIndex = a.StartIndex,
                EndIndex = a.EndIndex
            }).ToList()
        };

        if (message is ToolMessage tool)
            dto.ToolCallId = tool.ToolCallId;
        else if (message is FunctionMessage function)
            dto.FunctionName = function.FunctionName;
        else if (message is AssistantMessage assistant)
        {
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
        TextPart t => new PartDto { Type = "text", Text = t.Text },
        ImagePart i => new PartDto { Type = "image", Url = i.Url, Base64Data = i.Base64Data, MediaType = i.MediaType, Detail = i.Detail },
        _ => new PartDto { Type = part.Type }
    };

    private static IContentPart FromPartDto(PartDto dto) => dto.Type switch
    {
        "image" when dto.Url is { Length: > 0 } url => ImagePart.FromUrl(url, dto.Detail),
        "image" => ImagePart.FromBase64(dto.Base64Data ?? string.Empty, dto.MediaType ?? "image/png", dto.Detail),
        _ => new TextPart(dto.Text ?? string.Empty)
    };

    private static IMessage FromDto(MessageDto dto)
    {
        var role = MessageRole.Parse(dto.Role);
        var metadata = FromMetadataDto(dto.Metadata);
        var annotations = dto.Annotations?.Select(a =>
            new MessageAnnotation(a.Kind, a.Url, a.FileId, a.Title, a.Quote, a.StartIndex, a.EndIndex)).ToList();

        IReadOnlyList<IContentPart>? parts = dto.Parts is { Count: > 0 }
            ? dto.Parts.Select(FromPartDto).ToList()
            : null;

        var toolCalls = dto.ToolCalls?.Select(t => new ToolCall(t.Id, t.Name, t.ArgumentsJson ?? "{}")).ToList();
        var allowEmptyParts = role == MessageRole.Assistant
            && ((toolCalls is { Count: > 0 }) || !string.IsNullOrWhiteSpace(dto.Refusal));

        if (parts is null && !allowEmptyParts)
            throw new JsonException("Message JSON requires a non-empty parts array.");

        if (role == MessageRole.System)
            return SystemMessage.Create(
                parts ?? throw new JsonException("Message JSON requires a non-empty parts array."),
                metadata, dto.Id, dto.Name, annotations);

        if (role == MessageRole.Developer)
            return DeveloperMessage.Create(
                parts ?? throw new JsonException("Message JSON requires a non-empty parts array."),
                metadata, dto.Id);

        if (role == MessageRole.User)
            return UserMessage.Create(
                parts ?? throw new JsonException("Message JSON requires a non-empty parts array."),
                metadata, dto.Name, dto.Id, annotations);

        if (role == MessageRole.Assistant)
            return AssistantMessage.CreateDetailed(
                parts: parts,
                toolCalls: toolCalls,
                refusal: dto.Refusal,
                metadata: metadata,
                id: dto.Id,
                name: dto.Name,
                annotations: annotations);

        var textBody = string.Concat((parts ?? Array.Empty<IContentPart>()).OfType<TextPart>().Select(p => p.Text));

        if (role == MessageRole.Tool)
            return new ToolMessage(dto.ToolCallId ?? dto.Id ?? "unknown", textBody, metadata, dto.Id);

        return new FunctionMessage(dto.FunctionName ?? dto.Id ?? "unknown", textBody, metadata, dto.Id);
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

        if (schema is { Length: > 0 } schemaText)
        {
            if (string.Equals(type, "json", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.JsonWithSchema(schemaText);
            if (string.Equals(type, "yaml", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.YamlWithSchema(schemaText);
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
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("toolCallId")] public string? ToolCallId { get; set; }
        [JsonPropertyName("functionName")] public string? FunctionName { get; set; }
        [JsonPropertyName("refusal")] public string? Refusal { get; set; }
        [JsonPropertyName("toolCalls")] public List<ToolCallDto>? ToolCalls { get; set; }
        [JsonPropertyName("parts")] public List<PartDto>? Parts { get; set; }
        [JsonPropertyName("annotations")] public List<AnnotationDto>? Annotations { get; set; }
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
