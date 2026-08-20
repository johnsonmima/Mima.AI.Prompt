using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Exceptions;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Localization;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Tests;

public class EndToEndUsageTests
{
    [Fact]
    public void Sample_MissingPlaceholder_ValidateThenBuildThrows()
    {
        var template = SystemTemplate.Create("You are a {{profession}}. Product: {{product}}.");

        var check = template.Validate(new Dictionary<string, object>
        {
            ["profession"] = "Teacher"
        });

        check.IsValid.Should().BeFalse();
        check.MissingVariables.Should().Equal("product");
        check.Errors.Should().Contain("Missing required variable: 'product'");

        var act = () => PromptBuilder
            .Use(template)
            .With("profession", "Teacher")
            .AddUser("Hello")
            .Build();

        act.Should().Throw<PromptValidationException>()
            .WithMessage("*Missing required variable: 'product'*");
    }

    [Fact]
    public void Sample_LocalizedTemplate_RendersLocaleAndFallsBack()
    {
        var support = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "You are a support agent for {{product}}. Be concise.")
            .AddLocale("fr", "Vous êtes un agent de support pour {{product}}. Soyez concis.")
            .WithDefault("en");

        var french = support.Render("fr", new Dictionary<string, object>
        {
            ["product"] = "Billing"
        });

        french.Role.Should().Be(MessageRole.System);
        french.Content.Should().Be("Vous êtes un agent de support pour Billing. Soyez concis.");

        var prompt = PromptBuilder.Create()
            .AddMessage(french)
            .AddUser("Pourquoi ai-je été facturé deux fois ?")
            .Build();

        prompt.Messages.Should().HaveCount(2);
        Must.Be(prompt.SystemMessage).Content.Should().Contain("Billing");

        support.HasLocale("fr").Should().BeTrue();
        support.GetContent("fr").Should().Contain("{{product}}");

        var germanFallback = support.Render("de", new Dictionary<string, object>
        {
            ["product"] = "Billing"
        });
        germanFallback.Content.Should().Be("You are a support agent for Billing. Be concise.");
    }

    [Fact]
    public void Sample_TextPrompt_Serializes()
    {
        var prompt = PromptBuilder
            .System("You are a helpful assistant.")
            .AddUser("Explain dependency injection.")
            .Build();

        new PromptSerializer().Serialize(prompt).Should().Contain("dependency injection");
    }

    [Fact]
    public void Sample_VisionUrl_And_Base64()
    {
        var vision = PromptBuilder.Create()
            .AddSystem("You are a vision assistant.")
            .AddUserWithImage("What objects are in this photo?", "https://example.com/photo.png")
            .Build();

        var json = new PromptSerializer().Serialize(vision);
        json.Should().Contain("https://example.com/photo.png");

        var inline = PromptBuilder.Create()
            .AddUser(new IContentPart[]
            {
                TextPart.Create("Describe"),
                ImagePart.FromBase64("abc", "image/png", detail: "high")
            })
            .Build();

        new PromptSerializer().Serialize(inline).Should().Contain("abc");
    }

    [Fact]
    public void Sample_Conversation_ToPrompt()
    {
        var conversation = Conversation.Create("session")
            .WithSystem("Be brief.")
            .AddUser("Hi")
            .AddAssistant("Hello");

        conversation.ToPrompt().Messages.Should().HaveCount(3);
    }

    [Fact]
    public void Sample_ToolFollowUp_Transcript()
    {
        var first = PromptBuilder.Create()
            .AddSystem("Weather agent.")
            .AddUser("Weather in NYC?")
            .Build();
        var assistant = AssistantMessage.CreateWithToolCalls(
            new[] { ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}") });
        var second = PromptBuilder.Create()
            .AddSystem("Weather agent.")
            .AddUser("Weather in NYC?")
            .AddMessage(assistant)
            .AddTool("call_1", "{\"temp_f\":72}")
            .Build();

        var json = new PromptSerializer().Serialize(second);
        json.Should().Contain("call_1");
        var restored = (Models.Prompt)new PromptSerializer().DeserializePrompt(new PromptSerializer().Serialize(first));
        Must.Be(restored.LastUserMessage).Content.Should().Contain("NYC");
    }
}
