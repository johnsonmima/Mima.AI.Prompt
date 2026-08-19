using FluentAssertions;
using Mima.AI.Prompt.Exceptions;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Tests;

public class TemplateTests
{
    [Fact]
    public void SystemTemplate_Create_ContentOnly_Works()
    {
        var template = SystemTemplate.Create("You are a {{profession}}.");

        template.Role.Should().Be(MessageRole.System);
        template.TemplateContent.Should().Be("You are a {{profession}}.");
        template.Variables.Should().ContainSingle().Which.Should().Be("profession");
    }

    [Fact]
    public void SystemTemplate_Create_NamedVariant_SetsMetadataName()
    {
        var template = SystemTemplate.Create("MyTemplate", "Content here.");

        template.Metadata.Name.Should().Be("MyTemplate");
    }

    [Fact]
    public void SystemTemplate_Render_ProducesSystemMessage()
    {
        var template = SystemTemplate.Create("You are a {{profession}}.");
        var message = template.Render(new Dictionary<string, object> { ["profession"] = "teacher" });

        message.Should().BeOfType<SystemMessage>();
        message.Content.Should().Be("You are a teacher.");
    }

    [Fact]
    public void UserTemplate_Create_ContentOnly_Works()
    {
        var template = UserTemplate.Create("Hello {{name}}");
        template.Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void UserTemplate_Create_NamedVariant_Works()
    {
        var template = UserTemplate.Create("MyUserTemplate", "Hi {{name}}");
        template.Metadata.Name.Should().Be("MyUserTemplate");
    }

    [Fact]
    public void UserTemplate_Render_ProducesUserMessage()
    {
        var template = UserTemplate.Create("Hi {{name}}");
        var message = template.Render(new Dictionary<string, object> { ["name"] = "Bob" });

        message.Should().BeOfType<UserMessage>();
        message.Content.Should().Be("Hi Bob");
    }

    [Fact]
    public void AssistantTemplate_Create_ContentOnly_Works()
    {
        var template = AssistantTemplate.Create("Answer: {{answer}}");
        template.Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void AssistantTemplate_Create_NamedVariant_Works()
    {
        var template = AssistantTemplate.Create("MyAssistant", "Answer: {{answer}}");
        template.Metadata.Name.Should().Be("MyAssistant");
    }

    [Fact]
    public void AssistantTemplate_Render_ProducesAssistantMessage()
    {
        var template = AssistantTemplate.Create("Answer: {{answer}}");
        var message = template.Render(new Dictionary<string, object> { ["answer"] = "42" });

        message.Should().BeOfType<AssistantMessage>();
        message.Content.Should().Be("Answer: 42");
    }

    [Fact]
    public void DeveloperTemplate_Create_ContentOnly_Works()
    {
        var template = DeveloperTemplate.Create("Use {{language}}.");
        template.Role.Should().Be(MessageRole.Developer);
    }

    [Fact]
    public void DeveloperTemplate_Create_NamedVariant_Works()
    {
        var template = DeveloperTemplate.Create("MyDev", "Use {{language}}.");
        template.Metadata.Name.Should().Be("MyDev");
    }

    [Fact]
    public void DeveloperTemplate_Render_ProducesDeveloperMessage()
    {
        var template = DeveloperTemplate.Create("Use {{language}}.");
        var message = template.Render(new Dictionary<string, object> { ["language"] = "C#" });

        message.Should().BeOfType<DeveloperMessage>();
        message.Content.Should().Be("Use C#.");
    }

    [Fact]
    public void Variables_DiscoversMultipleDistinctVariables()
    {
        var template = SystemTemplate.Create("{{a}} and {{b}} and {{a}} again");
        template.Variables.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void Variables_NoPlaceholders_ReturnsEmpty()
    {
        var template = SystemTemplate.Create("No variables here.");
        template.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MissingVariable_ReturnsInvalidWithErrors()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var result = template.Validate(new Dictionary<string, object>());

        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("name");
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_NullVariableValue_TreatedAsMissing()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var dict = new Dictionary<string, object> { ["name"] = null! };
        var result = template.Validate(dict);

        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("name");
    }

    [Fact]
    public void Validate_ExtraVariable_ReportedButStillValidIfNoMissing()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var dict = new Dictionary<string, object> { ["name"] = "Bob", ["extra"] = "value" };
        var result = template.Validate(dict);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllVariablesProvided_Succeeds()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var result = template.Validate(new Dictionary<string, object> { ["name"] = "Bob" });

        result.IsValid.Should().BeTrue();
        result.MissingVariables.Should().BeEmpty();
    }

    [Fact]
    public void Render_MissingVariables_Throws()
    {
        var template = SystemTemplate.Create("Hi {{name}}");
        var act = () => template.Render(new Dictionary<string, object>());

        act.Should().Throw<PromptValidationException>();
    }

    [Fact]
    public void Render_ObjectOverload_UsesPropertiesAsVariables()
    {
        var template = SystemTemplate.Create("You are a {{profession}}. Tone: {{tone}}.");
        var message = template.Render(new { profession = "Teacher", tone = "Friendly" });

        message.Content.Should().Be("You are a Teacher. Tone: Friendly.");
    }

    [Fact]
    public void Render_ObjectOverload_WithDictionaryObject_UsesDirectly()
    {
        var template = UserTemplate.Create("Hi {{name}}");
        object variables = new Dictionary<string, object> { ["name"] = "Dict" };
        var message = template.Render(variables);

        message.Content.Should().Be("Hi Dict");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_EmptyContent_Throws(string? content)
    {
        var act = () => SystemTemplate.Create(content!);
        act.Should().Throw<PromptValidationException>().WithMessage("*Template content cannot be null or empty*");
    }

    [Fact]
    public void Ctor_NullRole_Throws()
    {
        var act = () => new TestTemplate(null!, "content");
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestTemplate : Mima.AI.Prompt.Templates.MessageTemplate
    {
        public TestTemplate(MessageRole role, string content) : base(role, content) { }

        protected override Mima.AI.Prompt.Interfaces.IMessage CreateMessage(string renderedContent) =>
            new UserMessage(renderedContent);
    }
}
