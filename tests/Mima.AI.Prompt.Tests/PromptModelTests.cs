using FluentAssertions;
using Mima.AI.Prompt.Exceptions;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Tests;

public class PromptModelTests
{
    [Fact]
    public void Ctor_NullMessages_Throws()
    {
        var act = () => new Models.Prompt(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_EmptyMessages_Throws()
    {
        var act = () => new Models.Prompt(new List<IMessage>());
        act.Should().Throw<PromptValidationException>().WithMessage("*at least one message*");
    }

    [Fact]
    public void Ctor_GeneratesIdWhenNotProvided()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") });
        prompt.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Ctor_UsesProvidedId()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") }, id: "fixed-id");
        prompt.Id.Should().Be("fixed-id");
    }

    [Fact]
    public void MessageCount_ReflectsMessageCount()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("a"), new AssistantMessage("b") });
        prompt.MessageCount.Should().Be(2);
    }

    [Fact]
    public void SystemMessage_ReturnsFirstSystemMessage()
    {
        var sys = new SystemMessage("sys");
        var prompt = new Models.Prompt(new List<IMessage> { sys, new UserMessage("hi") });

        prompt.SystemMessage.Should().Be(sys);
    }

    [Fact]
    public void SystemMessage_NoneExists_ReturnsNull()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") });
        prompt.SystemMessage.Should().BeNull();
    }

    [Fact]
    public void LastUserMessage_ReturnsLastUser()
    {
        var lastUser = new UserMessage("second");
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new UserMessage("first"),
            new AssistantMessage("reply"),
            lastUser
        });

        prompt.LastUserMessage.Should().Be(lastUser);
    }

    [Fact]
    public void LastUserMessage_NoneExists_ReturnsNull()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new SystemMessage("sys") });
        prompt.LastUserMessage.Should().BeNull();
    }

    [Fact]
    public void GetMessages_FiltersByRole()
    {
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys"),
            new UserMessage("u1"),
            new AssistantMessage("a1"),
            new UserMessage("u2")
        });

        var userMessages = prompt.GetMessages(MessageRole.User);
        userMessages.Should().HaveCount(2);
    }

    [Fact]
    public void ToString_IncludesCountAndNameOrId()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") }, new MessageMetadata(name: "MyPrompt"));
        prompt.ToString().Should().Be("Prompt [1 messages] MyPrompt");
    }

    [Fact]
    public void ToString_NoName_UsesId()
    {
        var prompt = new Models.Prompt(new List<IMessage> { new UserMessage("hi") }, id: "abc123");
        prompt.ToString().Should().Be("Prompt [1 messages] abc123");
    }

    // --- Conversation ---

    [Fact]
    public void Conversation_Create_SetsNameAndId()
    {
        var conversation = Conversation.Create("Chat");
        conversation.Name.Should().Be("Chat");
        conversation.Id.Should().NotBeNullOrEmpty();
        conversation.MessageCount.Should().Be(0);
        conversation.SystemMessage.Should().BeNull();
    }

    [Fact]
    public void Conversation_WithSystem_SetsSystemMessage()
    {
        var conversation = Conversation.Create("Chat").WithSystem("Be helpful.");
        conversation.SystemMessage.Should().NotBeNull();
        conversation.SystemMessage!.Content.Should().Be("Be helpful.");
    }

    [Fact]
    public void Conversation_AddUser_AddsUserMessage()
    {
        var conversation = Conversation.Create("Chat").AddUser("Hi");
        conversation.Messages.Should().ContainSingle(m => m.Role == MessageRole.User);
    }

    [Fact]
    public void Conversation_AddAssistant_AddsAssistantMessage()
    {
        var conversation = Conversation.Create("Chat").AddUser("Hi").AddAssistant("Hello");
        conversation.MessageCount.Should().Be(2);
    }

    [Fact]
    public void Conversation_AddMessage_AddsAnyMessage()
    {
        var toolMessage = new ToolMessage("call-1", "result");
        var conversation = Conversation.Create("Chat").AddMessage(toolMessage);
        conversation.Messages.Should().Contain(toolMessage);
    }

    [Fact]
    public void Conversation_AddMessage_Null_Throws()
    {
        var act = () => Conversation.Create("Chat").AddMessage(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Conversation_ToPrompt_IncludesSystemAndMessages()
    {
        var conversation = Conversation.Create("Chat")
            .WithSystem("Be helpful.")
            .AddUser("Hi")
            .AddAssistant("Hello!");

        var prompt = conversation.ToPrompt();

        prompt.Messages.Should().HaveCount(3);
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
        prompt.Metadata.Name.Should().Be("Chat");
    }

    [Fact]
    public void Conversation_ToPrompt_NoSystem_ExcludesSystem()
    {
        var conversation = Conversation.Create("Chat").AddUser("Hi");
        var prompt = conversation.ToPrompt();

        prompt.Messages.Should().HaveCount(1);
        prompt.Messages[0].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void Conversation_Created_IsSetOnCreation()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var conversation = Conversation.Create("Chat");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        conversation.Created.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void ToPromptWithWindow_CountLessThanMessages_ReturnsLastN()
    {
        var conversation = Conversation.Create("Chat").WithSystem("sys");
        for (var i = 0; i < 5; i++)
        {
            conversation.AddUser($"u{i}");
        }

        var prompt = conversation.ToPromptWithWindow(2);

        prompt.Messages.Should().HaveCount(3); // system + 2
        prompt.Messages[1].Content.Should().Be("u3");
        prompt.Messages[2].Content.Should().Be("u4");
    }

    [Fact]
    public void ToPromptWithWindow_CountGreaterThanMessages_ReturnsAll()
    {
        var conversation = Conversation.Create("Chat").WithSystem("sys").AddUser("u0").AddUser("u1");
        var prompt = conversation.ToPromptWithWindow(100);

        prompt.Messages.Should().HaveCount(3);
    }

    [Fact]
    public void ToPromptWithWindow_ZeroCount_ReturnsOnlySystem()
    {
        var conversation = Conversation.Create("Chat").WithSystem("sys").AddUser("u0").AddUser("u1");
        var prompt = conversation.ToPromptWithWindow(0);

        prompt.Messages.Should().HaveCount(1);
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void ToPromptWithWindow_NegativeCount_TreatedAsZero_ThrowsWhenResultingListEmpty()
    {
        var conversation = Conversation.Create("Chat").AddUser("u0").AddUser("u1");
        var act = () => conversation.ToPromptWithWindow(-5);

        act.Should().Throw<PromptValidationException>().WithMessage("*at least one message*");
    }

    [Fact]
    public void ToPromptWithWindow_NegativeCount_WithSystemMessage_ReturnsOnlySystem()
    {
        var conversation = Conversation.Create("Chat").WithSystem("sys").AddUser("u0").AddUser("u1");
        var prompt = conversation.ToPromptWithWindow(-5);

        prompt.Messages.Should().HaveCount(1);
        prompt.Messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void ToPromptWithWindow_NoSystem_WorksWithoutSystem()
    {
        var conversation = Conversation.Create("Chat").AddUser("u0").AddUser("u1").AddUser("u2");
        var prompt = conversation.ToPromptWithWindow(2);

        prompt.Messages.Should().HaveCount(2);
        prompt.Messages[0].Content.Should().Be("u1");
        prompt.Messages[1].Content.Should().Be("u2");
    }

    // --- PromptChain ---

    [Fact]
    public void PromptChain_Create_SetsName()
    {
        var chain = PromptChain.Create("MyChain");
        chain.Name.Should().Be("MyChain");
        chain.StepCount.Should().Be(0);
    }

    [Fact]
    public void PromptChain_Add_AppendsPrompt()
    {
        var prompt1 = Mima.AI.Prompt.Builder.PromptBuilder.UserOnly("first");
        var prompt2 = Mima.AI.Prompt.Builder.PromptBuilder.UserOnly("second");

        var chain = PromptChain.Create("MyChain").Add(prompt1).Add(prompt2);

        chain.StepCount.Should().Be(2);
        chain.Prompts.Should().HaveCount(2);
    }

    [Fact]
    public void PromptChain_Add_Null_Throws()
    {
        var act = () => PromptChain.Create("MyChain").Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PromptChain_Indexer_ReturnsStepAtIndex()
    {
        var prompt1 = Mima.AI.Prompt.Builder.PromptBuilder.UserOnly("first");
        var chain = PromptChain.Create("MyChain").Add(prompt1);

        chain[0].Should().Be(prompt1);
    }

    [Fact]
    public void PromptChain_Indexer_OutOfRange_Throws()
    {
        var chain = PromptChain.Create("MyChain");
        var act = () => chain[0];
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- PromptVariable ---

    [Fact]
    public void PromptVariable_Required_CreatesRequiredVariable()
    {
        var variable = PromptVariable.Required("topic", "The topic to discuss");

        variable.Name.Should().Be("topic");
        variable.Description.Should().Be("The topic to discuss");
        variable.IsRequired.Should().BeTrue();
        variable.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void PromptVariable_Optional_CreatesOptionalVariableWithDefault()
    {
        var variable = PromptVariable.Optional("tone", "friendly", "The tone to use");

        variable.IsRequired.Should().BeFalse();
        variable.DefaultValue.Should().Be("friendly");
        variable.Description.Should().Be("The tone to use");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PromptVariable_EmptyName_Throws(string? name)
    {
        var act = () => new PromptVariable(name!);
        act.Should().Throw<ArgumentException>().WithMessage("*Variable name cannot be null or empty*");
    }

    [Fact]
    public void PromptVariable_Examples_DefaultsToEmpty()
    {
        var variable = PromptVariable.Required("name");
        variable.Examples.Should().BeEmpty();
    }

    [Fact]
    public void PromptVariable_Examples_CanBeProvided()
    {
        var variable = new PromptVariable("name", examples: new[] { "Alice", "Bob" });
        variable.Examples.Should().BeEquivalentTo(new[] { "Alice", "Bob" });
    }

    [Fact]
    public void PromptVariable_TypeHint_CanBeSet()
    {
        var variable = new PromptVariable("count", typeHint: "int");
        variable.TypeHint.Should().Be("int");
    }

    // --- TemplateValidationResult ---

    [Fact]
    public void TemplateValidationResult_Success_IsValid()
    {
        var result = TemplateValidationResult.Success();
        result.IsValid.Should().BeTrue();
        result.MissingVariables.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TemplateValidationResult_Failure_ListsMissingVariablesAndErrors()
    {
        var result = TemplateValidationResult.Failure(new[] { "name", "topic" });

        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().BeEquivalentTo(new[] { "name", "topic" });
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Should().Contain("name");
    }

    [Fact]
    public void TemplateValidationResult_FromErrors_SetsErrorsOnly()
    {
        var result = TemplateValidationResult.FromErrors(new[] { "Some general error." });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Some general error.");
        result.MissingVariables.Should().BeEmpty();
    }

    [Fact]
    public void TemplateValidationResult_Ctor_DefaultsToEmptyCollections()
    {
        var result = new TemplateValidationResult(true);
        result.MissingVariables.Should().BeEmpty();
        result.ExtraVariables.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    // --- OutputFormat ---

    [Fact]
    public void OutputFormat_Json_HasCorrectTypeAndInstructions()
    {
        var format = OutputFormat.Json();
        format.Type.Should().Be("json");
        format.Instructions.Should().Contain("valid JSON");
        format.Schema.Should().BeNull();
    }

    [Fact]
    public void OutputFormat_Json_WithSchema_SetsSchema()
    {
        var format = OutputFormat.Json("{ \"type\": \"object\" }");
        format.Schema.Should().Be("{ \"type\": \"object\" }");
    }

    [Fact]
    public void OutputFormat_Markdown_HasCorrectType()
    {
        var format = OutputFormat.Markdown();
        format.Type.Should().Be("markdown");
        format.Instructions.Should().Contain("Markdown");
    }

    [Fact]
    public void OutputFormat_PlainText_HasCorrectType()
    {
        var format = OutputFormat.PlainText();
        format.Type.Should().Be("plaintext");
        format.Instructions.Should().Contain("plain text");
    }

    [Fact]
    public void OutputFormat_BulletPoints_HasCorrectType()
    {
        var format = OutputFormat.BulletPoints();
        format.Type.Should().Be("bullets");
        format.Instructions.Should().Contain("bullet points");
    }

    [Fact]
    public void OutputFormat_Steps_HasCorrectType()
    {
        var format = OutputFormat.Steps();
        format.Type.Should().Be("steps");
        format.Instructions.Should().Contain("numbered steps");
    }

    [Fact]
    public void OutputFormat_JsonWithSchema_IncludesSchemaInInstructions()
    {
        var format = OutputFormat.JsonWithSchema("{ \"name\": \"string\" }");
        format.Type.Should().Be("json");
        format.Schema.Should().Be("{ \"name\": \"string\" }");
        format.Instructions.Should().Contain("{ \"name\": \"string\" }");
    }

    [Fact]
    public void OutputFormat_Custom_UsesProvidedTypeAndInstructions()
    {
        var format = OutputFormat.Custom("csv", "Respond with CSV only.");
        format.Type.Should().Be("csv");
        format.Instructions.Should().Be("Respond with CSV only.");
        format.Schema.Should().BeNull();
    }

    [Fact]
    public void OutputFormat_Yaml_HasCorrectTypeAndInstructions()
    {
        var format = OutputFormat.Yaml();
        format.Type.Should().Be("yaml");
        format.Instructions.Should().Contain("valid YAML");
        format.Schema.Should().BeNull();
    }

    [Fact]
    public void OutputFormat_Yaml_WithSchema_SetsSchema()
    {
        var format = OutputFormat.Yaml("name: string");
        format.Type.Should().Be("yaml");
        format.Schema.Should().Be("name: string");
    }

    [Fact]
    public void OutputFormat_YamlWithSchema_IncludesSchemaInInstructions()
    {
        var format = OutputFormat.YamlWithSchema("name: string\nage: number");
        format.Type.Should().Be("yaml");
        format.Schema.Should().Contain("name: string");
        format.Instructions.Should().Contain("name: string");
        format.Instructions.Should().Contain("valid YAML");
    }
}
