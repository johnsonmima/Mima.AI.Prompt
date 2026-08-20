using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;

namespace Mima.AI.Prompt.Tests;

public class CoverageGapTests
{
    [Fact]
    public void MessageAnnotation_Factories_And_EmptyKind()
    {
        var url = MessageAnnotation.UrlCitation("https://example.com", "Title", "quote", 1, 2);
        url.Kind.Should().Be("url_citation");
        url.Url.Should().Be("https://example.com");
        url.Title.Should().Be("Title");
        url.Quote.Should().Be("quote");
        url.StartIndex.Should().Be(1);
        url.EndIndex.Should().Be(2);

        var file = MessageAnnotation.FileCitation("file-1", "Doc", "excerpt");
        file.Kind.Should().Be("file_citation");
        file.FileId.Should().Be("file-1");

        var internalRef = MessageAnnotation.InternalRef("ref-9", "Internal");
        internalRef.Kind.Should().Be("internal_ref");
        internalRef.FileId.Should().Be("ref-9");

        FluentActions.Invoking(() => new MessageAnnotation("  ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToolCall_WhitespaceArguments_BecomeEmptyObject()
    {
        var call = ToolCall.Create("id-1", "weather", "  ");
        call.ArgumentsJson.Should().Be("{}");
        FluentActions.Invoking(() => new ToolCall(" ", "n")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new ToolCall("id", " ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TextPart_Type_And_NullText()
    {
        var part = TextPart.Create("hi");
        part.Type.Should().Be("text");
        part.Text.Should().Be("hi");
        FluentActions.Invoking(() => new TextPart(TestNull.Ref<string>())).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImagePart_Type_UrlDetail_And_Base64()
    {
        var url = ImagePart.FromUrl("https://example.com/a.png", "high");
        url.Type.Should().Be("image");
        url.Detail.Should().Be("high");
        var b64 = ImagePart.FromBase64("abc", "image/jpeg", "low");
        b64.MediaType.Should().Be("image/jpeg");
        b64.Detail.Should().Be("low");
    }

    [Fact]
    public void Conversation_Parts_AssistantMessage_And_Window()
    {
        var conversation = Conversation.Create("Chat")
            .AddUser(new IContentPart[] { TextPart.Create("Look") })
            .AddAssistant(AssistantMessage.Create("Seen"));

        conversation.ToPrompt().Messages.Should().HaveCount(2);
        conversation.ToPromptWithWindow(1).Messages.Should().HaveCount(1);
        FluentActions.Invoking(() => conversation.AddUser(TestNull.Ref<IEnumerable<IContentPart>>()))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => conversation.AddAssistant(TestNull.Ref<AssistantMessage>()))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PromptBuilder_Parts_Roles_ResponseFormat_And_UseMessage()
    {
        var parts = new IContentPart[] { TextPart.Create("hello") };
        var prompt = PromptBuilder.Create()
            .AddSystem(parts)
            .AddDeveloper(parts)
            .AddUser(parts)
            .AddAssistant(parts)
            .Add(MessageRole.User, "again")
            .WithResponseFormat(OutputFormat.Json())
            .Build();

        Must.Be(prompt.ResponseFormat).Type.Should().Be("json");
        prompt.Messages.Should().HaveCount(5);

        var fromMessage = PromptBuilder.Use(new UserMessage("hi")).Build();
        fromMessage.Messages.Should().ContainSingle();

        FluentActions.Invoking(() => PromptBuilder.Create().Add(MessageRole.Tool, "x"))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => PromptBuilder.Create().Add(TestNull.Ref<MessageRole>(), "x"))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => PromptBuilder.Create().AddSystem(TestNull.Ref<IEnumerable<IContentPart>>()))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => PromptBuilder.Create().AddAssistant(TestNull.Ref<IEnumerable<IContentPart>>()))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => PromptBuilder.Create().AddDeveloper(TestNull.Ref<IEnumerable<IContentPart>>()))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Serializer_RoundTrips_Annotations_And_YamlSchema()
    {
        var annotated = AssistantMessage.CreateDetailed(
            content: "See source",
            annotations: new[] { MessageAnnotation.UrlCitation("https://example.com", "Ex") });
        var prompt = PromptBuilder.Create()
            .AddUser("q")
            .AddMessage(annotated)
            .WithResponseFormat(OutputFormat.YamlWithSchema("title: string"))
            .Build();

        var serializer = new PromptSerializer();
        var restored = (Models.Prompt)serializer.DeserializePrompt(serializer.Serialize(prompt));
        restored.Messages.OfType<AssistantMessage>().Single().Annotations.Should().ContainSingle(a => a.Kind == "url_citation");
        var format = Must.Be(restored.ResponseFormat);
        format.Type.Should().Be("yaml");
        format.Schema.Should().Contain("title");
    }
}
