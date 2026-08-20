using System.Text.Json;
using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Serialization;

namespace Mima.AI.Prompt.Tests;

/// <summary>
/// <see cref="IMessage.Parts"/> is the body. <see cref="IMessage.Content"/> is concatenated text parts.
/// JSON stores <c>parts</c> only. A <c>content</c> string is not a valid wire body.
/// </summary>
public class PartsAndContentContractTests
{
    private readonly PromptSerializer _serializer = new();

    [Fact]
    public void Content_IsConcatenatedTextParts_NotASecondBody()
    {
        var message = UserMessage.Create(new IContentPart[]
        {
            TextPart.Create("Look at "),
            ImagePart.FromUrl("https://example.com/a.png"),
            TextPart.Create("this.")
        });

        message.Parts.Should().HaveCount(3);
        message.Content.Should().Be("Look at this.");
    }

    [Fact]
    public void Serialize_WritesPartsOnly_DoesNotRepeatTextUnderContent()
    {
        var prompt = PromptBuilder
            .System("You are a support agent for Billing. Be concise. Never invent policy.")
            .AddUser("Why was I charged twice?")
            .Build();

        var json = _serializer.Serialize(prompt);
        using var doc = JsonDocument.Parse(json);
        var messages = doc.RootElement.GetProperty("messages");

        foreach (var message in messages.EnumerateArray())
        {
            message.TryGetProperty("content", out _).Should().BeFalse(
                "canonical JSON must not duplicate parts as a content string");
            message.TryGetProperty("parts", out var parts).Should().BeTrue();
            parts.GetArrayLength().Should().BeGreaterThan(0);
            parts[0].GetProperty("type").GetString().Should().Be("text");
            parts[0].TryGetProperty("text", out _).Should().BeTrue();
        }

        json.Should().Contain("You are a support agent for Billing");
        json.Should().Contain("Why was I charged twice?");
    }

    [Fact]
    public void RoundTrip_RestoresParts_AndRecomputesContent()
    {
        var original = UserMessage.Create(new IContentPart[]
        {
            TextPart.Create("Caption"),
            ImagePart.FromUrl("https://example.com/a.png")
        });

        var json = _serializer.SerializeMessage(original);
        json.Should().NotContain("\"content\"");

        var restored = _serializer.DeserializeMessage(json);
        restored.Parts.OfType<TextPart>().Single().Text.Should().Be("Caption");
        restored.Parts.OfType<ImagePart>().Single().Url.Should().Be("https://example.com/a.png");
        restored.Content.Should().Be("Caption");
    }

    [Fact]
    public void Deserialize_ContentStringWithoutParts_Throws()
    {
        const string json = """
            { "role": "user", "content": "Why was I charged twice?", "id": "invalid" }
            """;

        FluentActions.Invoking(() => _serializer.DeserializeMessage(json))
            .Should().Throw<JsonException>()
            .WithMessage("*parts*");
    }
}
