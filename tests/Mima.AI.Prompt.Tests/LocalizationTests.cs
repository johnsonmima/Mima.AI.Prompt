using FluentAssertions;
using Mima.AI.Prompt.Localization;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Tests;

public class LocalizationTests
{
    [Fact]
    public void Create_SetsRoleAndDefaultMetadata()
    {
        var template = LocalizedTemplate.Create(MessageRole.System);
        template.Role.Should().Be(MessageRole.System);
        template.Metadata.Should().NotBeNull();
        template.DefaultLocale.Should().Be("en");
        template.AvailableLocales.Should().BeEmpty();
    }

    [Fact]
    public void Create_NullRole_Throws()
    {
        var act = () => LocalizedTemplate.Create(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithMetadata_UsesProvidedMetadata()
    {
        var metadata = MessageMetadata.WithName("Greeting");
        var template = LocalizedTemplate.Create(MessageRole.System, metadata);
        template.Metadata.Name.Should().Be("Greeting");
    }

    [Fact]
    public void AddLocale_AddsContentForLocale()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "You are a helpful assistant.");

        template.AvailableLocales.Should().Contain("en");
        template.HasLocale("en").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLocale_EmptyLocale_Throws(string? locale)
    {
        var template = LocalizedTemplate.Create(MessageRole.System);
        var act = () => template.AddLocale(locale!, "content");
        act.Should().Throw<ArgumentException>().WithMessage("*Locale cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLocale_EmptyContent_Throws(string? content)
    {
        var template = LocalizedTemplate.Create(MessageRole.System);
        var act = () => template.AddLocale("en", content!);
        act.Should().Throw<ArgumentException>().WithMessage("*Content cannot be empty*");
    }

    [Fact]
    public void WithDefault_SetsDefaultLocale()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("fr", "Bonjour")
            .WithDefault("fr");

        template.DefaultLocale.Should().Be("fr");
    }

    [Fact]
    public void WithMetadata_UpdatesMetadata()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .WithMetadata(MessageMetadata.WithName("Updated"));

        template.Metadata.Name.Should().Be("Updated");
    }

    [Fact]
    public void GetContent_ExistingLocale_ReturnsContent()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "Hello")
            .AddLocale("fr", "Bonjour");

        template.GetContent("fr").Should().Be("Bonjour");
    }

    [Fact]
    public void GetContent_MissingLocale_FallsBackToDefault()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "Hello")
            .WithDefault("en");

        template.GetContent("de").Should().Be("Hello");
    }

    [Fact]
    public void GetContent_NoDefaultAndNoMatch_ReturnsFirstAvailable()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("fr", "Bonjour")
            .WithDefault("nonexistent-default");

        template.GetContent("de").Should().Be("Bonjour");
    }

    [Fact]
    public void GetContent_NoLocalesAdded_Throws()
    {
        var template = LocalizedTemplate.Create(MessageRole.System);
        var act = () => template.GetContent("en");
        act.Should().Throw<InvalidOperationException>().WithMessage("*No localized content has been added*");
    }

    [Fact]
    public void Render_SystemRole_ProducesSystemMessage()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "You are a {{profession}}.");

        var message = template.Render("en", new Dictionary<string, object> { ["profession"] = "teacher" });

        message.Role.Should().Be(MessageRole.System);
        message.Content.Should().Be("You are a teacher.");
    }

    [Fact]
    public void Render_UserRole_ProducesUserMessage()
    {
        var template = LocalizedTemplate.Create(MessageRole.User)
            .AddLocale("en", "Hello {{name}}");

        var message = template.Render("en", new Dictionary<string, object> { ["name"] = "Bob" });

        message.Role.Should().Be(MessageRole.User);
        message.Content.Should().Be("Hello Bob");
    }

    [Fact]
    public void Render_AssistantRole_ProducesAssistantMessage()
    {
        var template = LocalizedTemplate.Create(MessageRole.Assistant)
            .AddLocale("en", "Answer: {{value}}");

        var message = template.Render("en", new Dictionary<string, object> { ["value"] = "42" });

        message.Should().BeOfType<AssistantMessage>();
        message.Role.Should().Be(MessageRole.Assistant);
        message.Content.Should().Be("Answer: 42");
    }

    [Fact]
    public void Render_CustomRole_ProducesCustomMessage()
    {
        var critic = MessageRole.Custom("critic");
        var template = LocalizedTemplate.Create(critic)
            .AddLocale("en", "Critique: {{text}}");

        var message = template.Render("en", new Dictionary<string, object> { ["text"] = "claim" });

        message.Should().BeOfType<CustomMessage>();
        message.Role.Name.Should().Be("critic");
        message.Content.Should().Be("Critique: claim");
    }

    [Fact]
    public void HasLocale_UnknownLocale_ReturnsFalse()
    {
        var template = LocalizedTemplate.Create(MessageRole.System).AddLocale("en", "Hello");
        template.HasLocale("fr").Should().BeFalse();
    }

    [Fact]
    public void AddLocale_CaseInsensitiveLookup()
    {
        var template = LocalizedTemplate.Create(MessageRole.System).AddLocale("EN", "Hello");
        template.HasLocale("en").Should().BeTrue();
    }

    [Fact]
    public void AddLocale_UpdatingExistingLocale_OverwritesContent()
    {
        var template = LocalizedTemplate.Create(MessageRole.System)
            .AddLocale("en", "First")
            .AddLocale("en", "Second");

        template.GetContent("en").Should().Be("Second");
    }
}
