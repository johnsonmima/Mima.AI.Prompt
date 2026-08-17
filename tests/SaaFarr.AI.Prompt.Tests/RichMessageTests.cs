using FluentAssertions;
using SaaFarr.AI.Prompt.Builder;
using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Providers;
using SaaFarr.AI.Prompt.Roles;
using SaaFarr.AI.Prompt.Serialization;
using SaaFarr.AI.Prompt.Validation;

namespace SaaFarr.AI.Prompt.Tests;

/// <summary>
/// Usage-oriented coverage for multimodal parts, tool calls, reasoning, refusal,
/// annotations, cache control, speaker name, computer-use, and response format.
/// Prefer <see cref="EndToEndUsageTests"/> for full README-style pipelines.
/// </summary>
public class RichMessageTests
{
    private readonly PromptSerializer _serializer = new();

    // --- Text / multimodal parts ---

    /// <summary>
    /// Plain-text factory still produces a single TextPart; Content mirrors that text.
    /// </summary>
    [Fact]
    public void TextMessage_ExposesSingleTextPart_AndContentProjection()
    {
        var message = UserMessage.Create("Hello");

        message.Parts.Should().ContainSingle()
            .Which.Should().BeOfType<TextPart>()
            .Which.Text.Should().Be("Hello");
        message.Content.Should().Be("Hello");
        message.Name.Should().BeNull();
        message.Annotations.Should().BeEmpty();
        message.CacheControl.Should().BeNull();
    }

    /// <summary>
    /// Content is the text projection only — images live in Parts and do not append to Content.
    /// </summary>
    [Fact]
    public void UserMessage_MultimodalParts_ProjectsOnlyTextIntoContent()
    {
        var message = UserMessage.Create(new IContentPart[]
        {
            TextPart.Create("Describe this"),
            ImagePart.FromUrl("https://example.com/a.png", detail: "high")
        }, name: "alice");

        message.Parts.Should().HaveCount(2);
        message.Content.Should().Be("Describe this");
        message.Name.Should().Be("alice");
        message.Parts[1].Should().BeOfType<ImagePart>()
            .Which.Detail.Should().Be("high");
    }

    /// <summary>
    /// Multiple TextParts concatenate into Content (no separator — callers own spacing).
    /// </summary>
    [Fact]
    public void MultipleTextParts_ConcatenateIntoContent()
    {
        var message = UserMessage.Create(new IContentPart[]
        {
            TextPart.Create("Hello "),
            TextPart.Create("world"),
            ImagePart.FromUrl("https://example.com/x.png")
        });

        // Projection concatenates TextPart.Text values; non-text parts are ignored for Content
        message.Content.Should().Be("Hello world");
        message.Parts.Should().HaveCount(3);
    }

    [Fact]
    public void ImagePart_FromBase64_RequiresMediaType()
    {
        var part = ImagePart.FromBase64("abc123", "image/png", detail: "low");
        part.Base64Data.Should().Be("abc123");
        part.MediaType.Should().Be("image/png");

        var act = () => ImagePart.FromBase64("abc", "  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImagePart_RequiresUrlOrBase64()
    {
        // Whitespace-only URL is treated as missing; ctor requires url or base64.
        var act = () => ImagePart.FromUrl("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FileAudioVideo_Factories_RequireSource()
    {
        FilePart.FromId("file_1", filename: "a.pdf").FileId.Should().Be("file_1");
        FilePart.FromUrl("https://example.com/a.pdf").Url.Should().Contain("a.pdf");
        AudioPart.FromUrl("https://example.com/a.mp3").Type.Should().Be("audio");
        VideoPart.FromUrl("https://example.com/v.mp4").Type.Should().Be("video");

        FluentActions.Invoking(() => FilePart.FromId("  ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AudioPart.FromUrl("")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => VideoPart.FromUrl("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThinkingPart_And_ComputerParts_ExposeTypeAndPayload()
    {
        ThinkingPart.Create("step 1").Text.Should().Be("step 1");
        ScreenshotPart.FromUrl("https://example.com/s.png").Type.Should().Be("screenshot");
        ComputerActionPart.Create("scroll", "{\"dy\":100}").ArgumentsJson.Should().Contain("dy");

        FluentActions.Invoking(() => ComputerActionPart.Create("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Builder_AddUserWithImage_And_AddUserParts()
    {
        var withImage = PromptBuilder.Create()
            .AddSystem("You are a vision assistant.")
            .AddUserWithImage("What is this?", "https://example.com/cat.jpg")
            .Build();

        withImage.LastUserMessage!.Parts.Should().HaveCount(2);
        withImage.LastUserMessage.Parts.OfType<ImagePart>().Should().ContainSingle();

        var withParts = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("Read this PDF"),
                FilePart.FromId("file_abc", "spec.pdf")
            })
            .Build();

        withParts.Messages[0].Parts.OfType<FilePart>().Single().Filename.Should().Be("spec.pdf");
    }

    [Fact]
    public void SystemMessage_FromParts_WithPartLevelCacheControl()
    {
        var message = SystemMessage.Create(new IContentPart[]
        {
            TextPart.Create("Stable policy", CacheControl.Ephemeral(TimeSpan.FromHours(1)))
        });

        var cache = message.Parts[0].CacheControl;
        cache.Should().NotBeNull();
        cache!.Type.Should().Be("ephemeral");
        cache.Ttl.Should().Be(TimeSpan.FromHours(1));
        message.Content.Should().Be("Stable policy");
    }

    [Fact]
    public void FileAudioVideo_FromUrlAndBase64()
    {
        FilePart.FromUrl("https://example.com/a.pdf", "application/pdf", "a.pdf").Url.Should().Contain("a.pdf");
        FilePart.FromBase64("Zm9v", "application/pdf", "a.pdf").Base64Data.Should().Be("Zm9v");

        AudioPart.FromUrl("https://example.com/a.mp3", "audio/mpeg").MediaType.Should().Be("audio/mpeg");
        AudioPart.FromBase64("YQ==", "audio/wav").MediaType.Should().Be("audio/wav");

        VideoPart.FromUrl("https://example.com/v.mp4").Type.Should().Be("video");
        VideoPart.FromBase64("YQ==", "video/mp4").MediaType.Should().Be("video/mp4");
    }

    [Fact]
    public void ComputerUse_ScreenshotAndAction_Parts()
    {
        var shot = ScreenshotPart.FromUrl("https://example.com/screen.png");
        shot.Type.Should().Be("screenshot");
        shot.MediaType.Should().Be("image/png");

        var shotB64 = ScreenshotPart.FromBase64("aaa=", "image/jpeg");
        shotB64.MediaType.Should().Be("image/jpeg");

        var action = ComputerActionPart.Create("click", "{\"x\":10,\"y\":20}");
        action.Action.Should().Be("click");
        action.ArgumentsJson.Should().Contain("x");

        var act = () => ComputerActionPart.Create("  ");
        act.Should().Throw<ArgumentException>();
    }

    // --- Tool calls ---

    [Fact]
    public void ToolCall_Create_ValidatesIdAndName()
    {
        var call = ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}");
        call.Id.Should().Be("call_1");
        call.Name.Should().Be("get_weather");
        call.ArgumentsJson.Should().Contain("NYC");

        FluentActions.Invoking(() => ToolCall.Create("", "x")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => ToolCall.Create("id", "")).Should().Throw<ArgumentException>();
        ToolCall.Create("id", "name", "   ").ArgumentsJson.Should().Be("{}");
        ToolCall.Create("id", "name").ArgumentsJson.Should().Be("{}");
    }

    [Fact]
    public void Assistant_ToolCalls_RoundTrip_WithToolResult()
    {
        var call = ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}");
        var prompt = PromptBuilder.Create()
            .AddUser("Weather?")
            .AddAssistantToolCalls(new[] { call }, content: "Checking…")
            .AddTool("call_1", "{\"temp\":72}")
            .Build();

        var assistant = prompt.Messages[1].Should().BeOfType<AssistantMessage>().Subject;
        assistant.ToolCalls.Should().ContainSingle();
        assistant.Content.Should().Be("Checking…");

        var restored = _serializer.DeserializePrompt(_serializer.Serialize(prompt));
        restored.Messages[1].Should().BeOfType<AssistantMessage>()
            .Which.ToolCalls[0].Name.Should().Be("get_weather");
        restored.Messages[2].Should().BeOfType<ToolMessage>()
            .Which.ToolCallId.Should().Be("call_1");
    }

    [Fact]
    public void Assistant_ToolCallsOnly_AllowsEmptyContent()
    {
        var message = AssistantMessage.CreateWithToolCalls(new[]
        {
            ToolCall.Create("c1", "search")
        });

        message.Content.Should().BeEmpty();
        message.ToolCalls.Should().ContainSingle();
        message.Parts.Should().BeEmpty();
    }

    // --- Reasoning / refusal ---

    [Fact]
    public void Assistant_Reasoning_AddsThinkingPart()
    {
        var withReasoning = AssistantMessage.CreateDetailed(content: "Answer", reasoning: "Step by step...");
        withReasoning.Reasoning.Should().Be("Step by step...");
        withReasoning.Parts.OfType<ThinkingPart>().Should().ContainSingle()
            .Which.Text.Should().Be("Step by step...");
        // Content is text projection only — thinking is not concatenated into Content
        withReasoning.Content.Should().Be("Answer");
    }

    [Fact]
    public void Assistant_Refusal_AllowsEmptyContent()
    {
        var refusal = AssistantMessage.CreateRefusal("I can't help with that.");
        refusal.Refusal.Should().Be("I can't help with that.");
        refusal.Content.Should().BeEmpty();
        refusal.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void ThinkingPart_EmptyText_Throws()
    {
        var act = () => ThinkingPart.Create("  ");
        act.Should().Throw<ArgumentException>();
    }

    // --- Annotations / cache / name ---

    [Fact]
    public void Annotations_CacheControl_And_Name_RoundTrip()
    {
        var message = AssistantMessage.CreateDetailed(
            content: "Paris is the capital.",
            annotations: new[]
            {
                MessageAnnotation.UrlCitation("https://example.com", title: "Wiki", quote: "Paris…", startIndex: 0, endIndex: 5),
                MessageAnnotation.FileCitation("file_1", title: "Notes"),
                MessageAnnotation.InternalRef("doc:42", title: "Internal")
            },
            cacheControl: CacheControl.Ephemeral(TimeSpan.FromMinutes(5)),
            name: "researcher-agent");

        message.Annotations.Should().HaveCount(3);
        message.Annotations[0].Kind.Should().Be("url_citation");
        message.Annotations[1].Kind.Should().Be("file_citation");
        message.Annotations[2].Kind.Should().Be("internal_ref");
        message.CacheControl!.Type.Should().Be("ephemeral");
        message.Name.Should().Be("researcher-agent");

        var prompt = PromptBuilder.Create().AddMessage(message).Build();
        var restored = (AssistantMessage)_serializer.DeserializePrompt(_serializer.Serialize(prompt)).Messages[0];
        restored.Name.Should().Be("researcher-agent");
        restored.Annotations.Should().HaveCount(3);
        restored.CacheControl!.Type.Should().Be("ephemeral");
    }

    [Fact]
    public void CacheControl_Custom_And_Ephemeral()
    {
        CacheControl.Ephemeral().Type.Should().Be("ephemeral");
        CacheControl.Custom("persistent", TimeSpan.FromSeconds(30)).Ttl.Should().Be(TimeSpan.FromSeconds(30));
        FluentActions.Invoking(() => CacheControl.Custom("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MessageAnnotation_EmptyKind_Throws()
    {
        var act = () => new MessageAnnotation("  ");
        act.Should().Throw<ArgumentException>();
    }

    // --- Response format ---

    [Fact]
    public void Prompt_ResponseFormat_RoundTrip()
    {
        var prompt = PromptBuilder.Create()
            .AddUser("Return JSON")
            .WithResponseFormat(OutputFormat.JsonWithSchema("{\"type\":\"object\"}"))
            .Build();

        prompt.ResponseFormat.Should().NotBeNull();
        prompt.ResponseFormat!.Schema.Should().Contain("object");

        var restored = _serializer.DeserializePrompt(_serializer.Serialize(prompt));
        restored.ResponseFormat.Should().NotBeNull();
        restored.ResponseFormat!.Schema.Should().Contain("object");
        restored.ResponseFormat.Type.Should().Be("json");
    }

    [Fact]
    public void Prompt_YamlResponseFormat_RoundTrip_PreservesTypeAndSchema()
    {
        var prompt = PromptBuilder.Create()
            .AddUser("Return YAML")
            .WithResponseFormat(OutputFormat.YamlWithSchema("name: string"))
            .Build();

        var restored = _serializer.DeserializePrompt(_serializer.Serialize(prompt));
        restored.ResponseFormat.Should().NotBeNull();
        restored.ResponseFormat!.Type.Should().Be("yaml");
        restored.ResponseFormat.Schema.Should().Be("name: string");
        restored.ResponseFormat.Instructions.Should().Contain("valid YAML");
    }

    [Fact]
    public void Builder_WithResponseFormat_Null_Throws()
    {
        var act = () => PromptBuilder.Create().WithResponseFormat(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Validation ---

    [Fact]
    public void Validator_AllowsEmptyText_WhenToolCallsOrRefusalPresent()
    {
        var validator = new PromptValidator();

        var withTools = PromptBuilder.Create()
            .AddAssistantToolCalls(new[] { ToolCall.Create("c1", "search") })
            .Build();
        validator.Validate(withTools).IsValid.Should().BeTrue();

        var withRefusal = PromptBuilder.Create()
            .AddMessage(AssistantMessage.CreateRefusal("No."))
            .Build();
        validator.Validate(withRefusal).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_AllowsEmptyText_WhenNonTextPartsPresent()
    {
        var prompt = PromptBuilder.Create()
            .AddUser(new IContentPart[] { ImagePart.FromUrl("https://example.com/x.png") })
            .Build();

        new PromptValidator().Validate(prompt).IsValid.Should().BeTrue();
        prompt.Messages[0].Content.Should().BeEmpty();
        prompt.Messages[0].Parts.Should().ContainSingle().Which.Should().BeOfType<ImagePart>();
    }

    // --- Providers ---

    [Fact]
    public void OpenAiAdapter_EmitsToolCalls_Multimodal_Name_And_ResponseFormat()
    {
        var prompt = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("See"),
                ImagePart.FromUrl("https://example.com/i.png")
            }, name: "user-1")
            .AddAssistantToolCalls(new[] { ToolCall.Create("c1", "search", "{}") })
            .WithResponseFormat(OutputFormat.Json())
            .Build();

        var json = new OpenAiAdapter().ToJson(prompt);
        json.Should().Contain("image_url");
        json.Should().Contain("tool_calls");
        json.Should().Contain("response_format");
        json.Should().Contain("user-1");
        json.Should().Contain("c1");
    }

    [Fact]
    public void OpenAiAdapter_YamlResponseFormat_EmitsTextNotJsonSchema()
    {
        var prompt = PromptBuilder.Create()
            .AddUser("Return YAML")
            .WithResponseFormat(OutputFormat.YamlWithSchema("name: string"))
            .Build();

        var json = new OpenAiAdapter().ToJson(prompt);
        json.Should().Contain("response_format");
        json.Should().Contain("\"type\": \"text\"");
        json.Should().NotContain("json_schema");
        json.Should().NotContain("json_object");
    }

    [Fact]
    public void OpenAiAdapter_Validate_WarnsOnThinkingAndCache()
    {
        var prompt = PromptBuilder.Create()
            .AddMessage(AssistantMessage.CreateDetailed(content: "Hi", reasoning: "think", cacheControl: CacheControl.Ephemeral()))
            .Build();

        var result = new OpenAiAdapter().Validate(prompt);
        result.Warnings.Should().Contain(w => w.Contains("Reasoning"));
        result.Warnings.Should().Contain(w => w.Contains("CacheControl"));
    }

    [Fact]
    public void OpenAiAdapter_OmitsThinkingFromContentPayload()
    {
        var prompt = PromptBuilder.Create()
            .AddMessage(AssistantMessage.CreateDetailed(content: "Final", reasoning: "secret thoughts"))
            .Build();

        var json = new OpenAiAdapter().ToJson(prompt);
        json.Should().Contain("Final");
        json.Should().NotContain("secret thoughts");
    }

    // --- Serialization edge cases ---

    [Fact]
    public void Multimodal_Serializer_RoundTrip_AllPartKinds()
    {
        var prompt = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("hi"),
                ImagePart.FromUrl("https://example.com/x.png", detail: "high"),
                FilePart.FromId("f1"),
                AudioPart.FromUrl("https://example.com/a.mp3"),
                VideoPart.FromUrl("https://example.com/v.mp4"),
                ScreenshotPart.FromUrl("https://example.com/s.png"),
                ComputerActionPart.Create("type", "{\"text\":\"hello\"}")
            })
            .Build();

        var restored = _serializer.DeserializePrompt(_serializer.Serialize(prompt));
        restored.Messages[0].Parts.Should().HaveCount(7);
        restored.Messages[0].Parts[1].Should().BeOfType<ImagePart>().Which.Detail.Should().Be("high");
        restored.Messages[0].Parts[6].Should().BeOfType<ComputerActionPart>().Which.Action.Should().Be("type");
    }

    /// <summary>
    /// Parts-first serialize always rebuilds from parts and preserves id / name / cache.
    /// </summary>
    [Fact]
    public void Serializer_PartsFirst_PreservesIdNameAndCache_OnPlainTextToo()
    {
        var original = SystemMessage.Create(
            parts: new IContentPart[] { TextPart.Create("policy") },
            id: "sys-fixed-id",
            cacheControl: CacheControl.Ephemeral(TimeSpan.FromMinutes(2)));

        var prompt = PromptBuilder.Create().AddMessage(original).AddUser("ok").Build();
        var restored = (Models.Prompt)_serializer.DeserializePrompt(_serializer.Serialize(prompt));

        restored.Messages[0].Id.Should().Be("sys-fixed-id");
        restored.Messages[0].CacheControl!.Type.Should().Be("ephemeral");
        restored.Messages[0].Parts.Should().ContainSingle().Which.Should().BeOfType<TextPart>();
        restored.Messages[0].Content.Should().Be("policy");
    }

    [Fact]
    public void AnthropicAndOllama_EmitStructuredContent_ForVision()
    {
        var prompt = PromptBuilder.Create()
            .AddUserWithImage("What is this?", "https://example.com/cat.png")
            .Build();

        new AnthropicAdapter().ToJson(prompt).Should().Contain("image_url");
        new OllamaAdapter().ToJson(prompt).Should().Contain("image_url");
    }

    [Fact]
    public void Builder_AddSystemAssistantDeveloper_PartsOverloads()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem(new IContentPart[] { TextPart.Create("sys") }, CacheControl.Ephemeral())
            .AddDeveloper(new IContentPart[] { TextPart.Create("dev") })
            .AddAssistant(new IContentPart[] { TextPart.Create("asst") })
            .Build();

        prompt.Messages.Should().HaveCount(3);
        prompt.Messages[0].CacheControl!.Type.Should().Be("ephemeral");
        prompt.Messages[1].Content.Should().Be("dev");
        prompt.Messages[2].Content.Should().Be("asst");
    }

    [Fact]
    public void CustomMessage_FromParts_Works()
    {
        var role = MessageRole.Custom("critic");
        var message = CustomMessage.Create(role, new IContentPart[] { TextPart.Create("Be skeptical.") });
        message.Role.Name.Should().Be("critic");
        message.Content.Should().Be("Be skeptical.");
    }

    [Fact]
    public void DeveloperAndSystem_FromParts_Factories()
    {
        DeveloperMessage.Create(new IContentPart[] { TextPart.Create("Use Markdown") })
            .Content.Should().Be("Use Markdown");
        SystemMessage.Create(new IContentPart[] { TextPart.Create("You are helpful") })
            .Role.Should().Be(MessageRole.System);
    }

    /// <summary>
    /// Message-level cache control is distinct from part-level CacheControl on TextPart.
    /// </summary>
    [Fact]
    public void MessageLevel_CacheControl_IsIndependentOfPartCache()
    {
        var message = AssistantMessage.CreateDetailed(
            content: "Short-lived answer",
            cacheControl: CacheControl.Ephemeral(TimeSpan.FromMinutes(1)));

        message.CacheControl!.Ttl.Should().Be(TimeSpan.FromMinutes(1));
        // TextPart created from content does not inherit message-level cache
        message.Parts[0].CacheControl.Should().BeNull();
    }

    [Fact]
    public void Assistant_CreateWithToolCalls_AllowsEmptyContent()
    {
        var message = AssistantMessage.CreateWithToolCalls(
            new[] { ToolCall.Create("c1", "search", "{}") });

        message.Content.Should().BeEmpty();
        message.ToolCalls.Should().ContainSingle();
        new PromptValidator().Validate(
            PromptBuilder.Create().AddMessage(message).Build()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Assistant_CreateDetailed_AttachesThinkingPart_WhenReasoningSet()
    {
        var message = AssistantMessage.CreateDetailed(
            content: "Answer",
            reasoning: "Because reasons");

        message.Reasoning.Should().Be("Because reasons");
        message.Parts.OfType<ThinkingPart>().Should().ContainSingle(p => p.Text == "Because reasons");
        // Content stays user-visible only
        message.Content.Should().Be("Answer");
    }

    [Fact]
    public void Annotations_UrlFileAndInternal_RoundTrip()
    {
        var annotations = new[]
        {
            MessageAnnotation.UrlCitation("https://example.com", "Title", "quote", 0, 4),
            MessageAnnotation.FileCitation("file_1", "Doc"),
            MessageAnnotation.InternalRef("memory://item-9", "Prior turn")
        };

        var message = AssistantMessage.CreateDetailed("Cited answer", annotations: annotations);
        var prompt = PromptBuilder.Create().AddMessage(message).Build();
        var restored = _serializer.DeserializePrompt(_serializer.Serialize(prompt));

        restored.Messages[0].Annotations.Should().HaveCount(3);
        restored.Messages[0].Annotations[0].Kind.Should().Be("url_citation");
        restored.Messages[0].Annotations[1].Kind.Should().Be("file_citation");
        restored.Messages[0].Annotations[2].Kind.Should().Be("internal_ref");
    }

    [Fact]
    public void AnthropicAndOllama_Validate_WarnOnCustomRoles()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("moderator", "Keep it civil.")
            .AddUser("Hello")
            .Build();

        new AnthropicAdapter().Validate(prompt).Warnings
            .Should().Contain(w => w.Contains("moderator") || w.Contains("Custom"));
        new OllamaAdapter().Validate(prompt).Warnings
            .Should().Contain(w => w.Contains("moderator") || w.Contains("Custom"));
    }

    [Fact]
    public void OpenAiAdapter_ImageBase64_EmitsDataUrlStylePayload()
    {
        var prompt = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("See"),
                ImagePart.FromBase64("abc123", "image/png")
            })
            .Build();

        var json = new OpenAiAdapter().ToJson(prompt);
        json.Should().Contain("image_url");
        json.Should().Contain("base64");
    }

    [Fact]
    public void Builder_AddAssistantToolCalls_Null_Throws()
    {
        // Null toolCalls yields an empty body (no content / tools) → validation exception
        FluentActions.Invoking(() =>
                PromptBuilder.Create().AddAssistantToolCalls(null!))
            .Should().Throw<Exception>();
    }
}