using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Exceptions;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void System_CreatesBuilderWithSystemMessage()
    {
        var prompt = PromptBuilder.System("You are helpful.").Build();

        prompt.Messages.Should().HaveCount(1);
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void Use_Template_RendersOnBuild()
    {
        var template = SystemTemplate.Create("You are a {{profession}}.");
        var prompt = PromptBuilder.Use(template).With("profession", "teacher").Build();

        prompt.Messages[0].Content.Should().Be("You are a teacher.");
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void Use_Template_Null_Throws()
    {
        var act = () => PromptBuilder.Use((Mima.AI.Prompt.Interfaces.IMessageTemplate)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Use_Message_AddsMessage()
    {
        var message = new UserMessage("Hi");
        var prompt = PromptBuilder.Use(message).Build();

        prompt.Messages[0].Should().Be(message);
    }

    [Fact]
    public void Use_Message_Null_Throws()
    {
        var act = () => PromptBuilder.Use((Mima.AI.Prompt.Interfaces.IMessage)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ReturnsEmptyBuilder()
    {
        var builder = PromptBuilder.Create();
        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddSystem_AddsSystemMessage()
    {
        var prompt = PromptBuilder.Create().AddSystem("sys").Build();
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
        prompt.Messages[0].Content.Should().Be("sys");
    }

    [Fact]
    public void AddDeveloper_AddsDeveloperMessage()
    {
        var prompt = PromptBuilder.Create().AddDeveloper("dev").Build();
        prompt.Messages[0].Role.Should().Be(MessageRole.Developer);
    }

    [Fact]
    public void AddUser_AddsUserMessage()
    {
        var prompt = PromptBuilder.Create().AddUser("hello").Build();
        prompt.Messages[0].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void AddAssistant_AddsAssistantMessage()
    {
        var prompt = PromptBuilder.Create().AddUser("hi").AddAssistant("hello").Build();
        prompt.Messages[1].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void AddTool_AddsToolMessage()
    {
        var prompt = PromptBuilder.Create().AddUser("hi").AddTool("call-1", "result").Build();
        prompt.Messages[1].Role.Should().Be(MessageRole.Tool);
        ((ToolMessage)prompt.Messages[1]).ToolCallId.Should().Be("call-1");
    }

    [Fact]
    public void AddFunction_AddsFunctionMessage()
    {
        var prompt = PromptBuilder.Create().AddUser("hi").AddFunction("fn", "result").Build();
        prompt.Messages[1].Role.Should().Be(MessageRole.Function);
        ((FunctionMessage)prompt.Messages[1]).FunctionName.Should().Be("fn");
    }

    [Fact]
    public void AddMessage_AddsGivenMessage()
    {
        var message = new UserMessage("custom");
        var prompt = PromptBuilder.Create().AddMessage(message).Build();
        prompt.Messages[0].Should().Be(message);
    }

    [Fact]
    public void AddMessage_Null_Throws()
    {
        var act = () => PromptBuilder.Create().AddMessage(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTemplate_RendersOnBuild()
    {
        var template = UserTemplate.Create("Hello {{name}}");
        var prompt = PromptBuilder.Create().AddSystem("sys").AddTemplate(template).With("name", "World").Build();

        prompt.Messages.Should().HaveCount(2);
        prompt.Messages[0].Content.Should().Be("Hello World");
    }

    [Fact]
    public void AddTemplate_Null_Throws()
    {
        var act = () => PromptBuilder.Create().AddTemplate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void With_NullValue_Throws()
    {
        var act = () => PromptBuilder.Create().With("name", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void With_EmptyName_Throws(string? name)
    {
        var act = () => PromptBuilder.Create().With(name!, "value");
        act.Should().Throw<ArgumentException>().WithMessage("*Variable name cannot be null or empty*");
    }

    [Fact]
    public void WithMetadata_SetsMetadataOnPrompt()
    {
        var metadata = MessageMetadata.WithName("MyPrompt");
        var prompt = PromptBuilder.Create().AddUser("hi").WithMetadata(metadata).Build();

        prompt.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void WithName_SetsNameMetadata()
    {
        var prompt = PromptBuilder.Create().AddUser("hi").WithName("Named").Build();
        prompt.Metadata.Name.Should().Be("Named");
    }

    [Fact]
    public void AddExample_AddsUserAndAssistantPair()
    {
        var prompt = PromptBuilder.Create().AddExample("What is DI?", "It's a design pattern.").Build();

        prompt.Messages.Should().HaveCount(2);
        prompt.Messages[0].Role.Should().Be(MessageRole.User);
        prompt.Messages[1].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void AddHistory_AddsAlternatingMessages()
    {
        var history = new List<(string User, string Assistant)>
        {
            ("Hi", "Hello!"),
            ("How are you?", "I'm good.")
        };

        var prompt = PromptBuilder.Create().AddHistory(history).Build();

        prompt.Messages.Should().HaveCount(4);
        prompt.Messages[0].Role.Should().Be(MessageRole.User);
        prompt.Messages[1].Role.Should().Be(MessageRole.Assistant);
        prompt.Messages[2].Role.Should().Be(MessageRole.User);
        prompt.Messages[3].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void Build_WithVariables_RendersTemplate()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var prompt = PromptBuilder.Use(template).With("name", "Bob").AddUser("question").Build();

        prompt.Messages.Should().HaveCount(2);
        prompt.Messages[0].Content.Should().Be("Hi Bob");
    }

    [Fact]
    public void Build_TemplateWithNoVariables_RendersDirectly()
    {
        var template = SystemTemplate.Create("Static content, no vars.");
        var prompt = PromptBuilder.Use(template).AddUser("hi").Build();

        prompt.Messages[0].Content.Should().Be("Static content, no vars.");
    }

    [Fact]
    public void Build_TemplateWithMissingVariables_Throws()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var act = () => PromptBuilder.Use(template).AddUser("hi").Build();

        act.Should().Throw<PromptValidationException>().WithMessage("*variable(s) but no values were provided*");
    }

    [Fact]
    public void Quick_CreatesSystemAndUserPrompt()
    {
        var prompt = PromptBuilder.Quick("You are helpful.", "Explain DI.");

        prompt.Messages.Should().HaveCount(2);
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
        prompt.Messages[1].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void UserOnly_CreatesUserOnlyPrompt()
    {
        var prompt = PromptBuilder.UserOnly("Explain DI.");

        prompt.Messages.Should().HaveCount(1);
        prompt.Messages[0].Role.Should().Be(MessageRole.User);
    }
}
