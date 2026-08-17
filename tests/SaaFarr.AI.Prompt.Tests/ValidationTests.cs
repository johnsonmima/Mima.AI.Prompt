using FluentAssertions;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;
using SaaFarr.AI.Prompt.Validation;

namespace SaaFarr.AI.Prompt.Tests;

public class ValidationTests
{
    private sealed class FakeMessage : IMessage
    {
        public MessageRole Role { get; }
        public string Content { get; }
        public IReadOnlyList<IContentPart> Parts { get; } = Array.Empty<IContentPart>();
        public string? Name => null;
        public IReadOnlyList<MessageAnnotation> Annotations { get; } = Array.Empty<MessageAnnotation>();
        public CacheControl? CacheControl => null;
        public MessageMetadata Metadata { get; } = MessageMetadata.Empty;
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public FakeMessage(MessageRole role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    // The concrete Prompt type cannot itself hold zero messages (its constructor throws),
    // so an empty-message IPrompt implementation is needed to exercise this defensive path.
    private sealed class FakeEmptyPrompt : IPrompt
    {
        public IReadOnlyList<IMessage> Messages { get; } = Array.Empty<IMessage>();
        public MessageMetadata Metadata { get; } = MessageMetadata.Empty;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public int MessageCount => 0;
        public OutputFormat? ResponseFormat => null;
    }

    [Fact]
    public void Validate_EmptyMessageList_ReturnsErrorAndShortCircuits()
    {
        var validator = new PromptValidator();
        var report = validator.Validate(new FakeEmptyPrompt());

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle().Which.Should().Contain("must contain at least one message");
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NullPrompt_Throws()
    {
        var validator = new PromptValidator();
        var act = () => validator.Validate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_EmptyContentMessage_AddsError()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage> { new FakeMessage(MessageRole.User, "   ") });

        var report = validator.Validate(prompt);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Contains("empty content"));
    }

    [Fact]
    public void Validate_SystemNotFirst_AddsWarning()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new UserMessage("hi"),
            new SystemMessage("sys")
        });

        var report = validator.Validate(prompt);

        report.Warnings.Should().Contain(w => w.Contains("System message should typically be the first"));
    }

    [Fact]
    public void Validate_SystemFirst_NoWarningAboutOrder()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys"),
            new UserMessage("hi")
        });

        var report = validator.Validate(prompt);

        report.Warnings.Should().NotContain(w => w.Contains("should typically be the first"));
    }

    [Fact]
    public void Validate_MultipleSystemMessages_AddsWarning()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys1"),
            new SystemMessage("sys2"),
            new UserMessage("hi")
        });

        var report = validator.Validate(prompt);

        report.Warnings.Should().Contain(w => w.Contains("2 system messages"));
    }

    [Fact]
    public void Validate_NoUserMessage_AddsWarning()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage> { new SystemMessage("sys") });

        var report = validator.Validate(prompt);

        report.Warnings.Should().Contain(w => w.Contains("does not contain a user message"));
    }

    [Fact]
    public void Validate_HasUserMessage_NoUserWarning()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") });

        var report = validator.Validate(prompt);

        report.Warnings.Should().NotContain(w => w.Contains("does not contain a user message"));
    }

    [Fact]
    public void Validate_MoreThan100Messages_AddsWarning()
    {
        var validator = new PromptValidator();
        var messages = new List<IMessage>();
        for (var i = 0; i < 101; i++)
            messages.Add(new UserMessage($"message {i}"));

        var prompt = new Models.Prompt(messages);
        var report = validator.Validate(prompt);

        report.Warnings.Should().Contain(w => w.Contains("101 messages"));
    }

    [Fact]
    public void Validate_ValidPrompt_ReturnsSuccessWithNoWarnings()
    {
        var validator = new PromptValidator();
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys"),
            new UserMessage("hi")
        });

        var report = validator.Validate(prompt);

        report.IsValid.Should().BeTrue();
        report.Errors.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void PromptValidationReport_Success_IsValidWithNoIssues()
    {
        var report = PromptValidationReport.Success();
        report.IsValid.Should().BeTrue();
        report.HasWarnings.Should().BeFalse();
        report.Errors.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void PromptValidationReport_ToString_ValidNoWarnings()
    {
        var report = PromptValidationReport.Success();
        report.ToString().Should().Be("Valid");
    }

    [Fact]
    public void PromptValidationReport_ToString_ValidWithWarnings()
    {
        var report = new PromptValidationReport(true, warnings: new[] { "warn1", "warn2" });
        report.ToString().Should().Be("Valid (2 warning(s))");
        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void PromptValidationReport_ToString_Invalid()
    {
        var report = new PromptValidationReport(false, errors: new[] { "err1" }, warnings: new[] { "warn1" });
        report.ToString().Should().Be("Invalid (1 error(s), 1 warning(s))");
    }

    [Fact]
    public void PromptValidationReport_Ctor_DefaultsToEmptyCollections()
    {
        var report = new PromptValidationReport(true);
        report.Errors.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
    }
}
