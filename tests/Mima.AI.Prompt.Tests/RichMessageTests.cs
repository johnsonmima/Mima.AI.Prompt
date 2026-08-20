using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Serialization;

namespace Mima.AI.Prompt.Tests;

public class RichMessageTests
{
    [Fact]
    public void UserMessage_TextCreate_HasSingleTextPart()
    {
        var message = UserMessage.Create("Hello");
        message.Parts.Should().ContainSingle().Which.Should().BeOfType<TextPart>();
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void ImagePart_FromBase64_RequiresMediaType()
    {
        var part = ImagePart.FromBase64("abc123", "image/png", detail: "low");
        part.Base64Data.Should().Be("abc123");
        FluentActions.Invoking(() => ImagePart.FromBase64("abc", "  ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImagePart_RequiresUrlOrBase64()
    {
        FluentActions.Invoking(() => ImagePart.FromUrl("  ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => ImagePart.FromUrl(TestNull.Ref<string>())).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Builder_AddUserWithImage_And_Parts()
    {
        var prompt = PromptBuilder.Create()
            .AddUserWithImage("Look", "https://example.com/a.png")
            .Build();
        Must.Be(prompt.LastUserMessage).Parts.OfType<ImagePart>().Should().ContainSingle();
    }

    [Fact]
    public void Assistant_Refusal_AllowsEmptyText()
    {
        var refusal = AssistantMessage.CreateRefusal("I can't help with that.");
        refusal.Refusal.Should().Contain("can't");
        refusal.Content.Should().BeEmpty();
    }

    [Fact]
    public void Serializer_RoundTrips_ImageBase64_And_ToolCalls()
    {
        var prompt = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("See"),
                ImagePart.FromBase64("abc123", "image/png")
            })
            .AddAssistantToolCalls(new[] { ToolCall.Create("call_1", "get_weather", "{}") })
            .AddTool("call_1", "{}")
            .Build();
        var serializer = new PromptSerializer();
        var json = serializer.Serialize(prompt);
        json.Should().Contain("abc123").And.Contain("call_1");
        var restored = (Models.Prompt)serializer.DeserializePrompt(json);
        restored.Messages.OfType<ToolMessage>().Should().ContainSingle();
    }

    [Fact]
    public void Serializer_RoundTrips_ImageAndName()
    {
        var message = UserMessage.Create(
            new IContentPart[] { TextPart.Create("Hi"), ImagePart.FromUrl("https://example.com/a.png") },
            name: "alice");
        var serializer = new PromptSerializer();
        var restored = serializer.DeserializeMessage(serializer.SerializeMessage(message));
        restored.Name.Should().Be("alice");
        restored.Parts.OfType<ImagePart>().Single().Url.Should().Contain("example.com");
    }
}
