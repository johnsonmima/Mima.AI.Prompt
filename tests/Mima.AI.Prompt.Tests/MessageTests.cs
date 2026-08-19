using FluentAssertions;
using Mima.AI.Prompt.Exceptions;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Tests;

public class MessageTests
{
    [Fact]
    public void SystemMessage_Create_HasCorrectRoleAndContent()
    {
        var message = SystemMessage.Create("You are helpful.");

        message.Role.Should().Be(MessageRole.System);
        message.Content.Should().Be("You are helpful.");
        message.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SystemMessage_Ctor_WithMetadataAndId_Passthrough()
    {
        var metadata = MessageMetadata.WithName("sys");
        var message = new SystemMessage("content", metadata, "fixed-id");

        message.Metadata.Should().BeSameAs(metadata);
        message.Id.Should().Be("fixed-id");
    }

    [Fact]
    public void UserMessage_Create_HasCorrectRoleAndContent()
    {
        var message = UserMessage.Create("Hello");

        message.Role.Should().Be(MessageRole.User);
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void UserMessage_Ctor_Works()
    {
        var message = new UserMessage("Hi there");
        message.Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void AssistantMessage_Create_HasCorrectRoleAndContent()
    {
        var message = AssistantMessage.Create("Sure, here's the answer.");

        message.Role.Should().Be(MessageRole.Assistant);
        message.Content.Should().Be("Sure, here's the answer.");
    }

    [Fact]
    public void AssistantMessage_Ctor_Works()
    {
        var message = new AssistantMessage("answer");
        message.Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void DeveloperMessage_Create_HasCorrectRoleAndContent()
    {
        var message = DeveloperMessage.Create("Respond in Markdown.");

        message.Role.Should().Be(MessageRole.Developer);
        message.Content.Should().Be("Respond in Markdown.");
    }

    [Fact]
    public void DeveloperMessage_Ctor_Works()
    {
        var message = new DeveloperMessage("dev instructions");
        message.Role.Should().Be(MessageRole.Developer);
    }

    [Fact]
    public void ToolMessage_Create_HasCorrectRoleAndToolCallId()
    {
        var message = ToolMessage.Create("call-1", "{ \"result\": true }");

        message.Role.Should().Be(MessageRole.Tool);
        message.ToolCallId.Should().Be("call-1");
        message.Content.Should().Be("{ \"result\": true }");
    }

    [Fact]
    public void ToolMessage_Ctor_Works()
    {
        var message = new ToolMessage("call-2", "output");
        message.ToolCallId.Should().Be("call-2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToolMessage_EmptyToolCallId_Throws(string? toolCallId)
    {
        var act = () => new ToolMessage(toolCallId!, "content");
        act.Should().Throw<ArgumentException>().WithMessage("*Tool call ID cannot be null or empty*");
    }

    [Fact]
    public void FunctionMessage_Create_HasCorrectRoleAndFunctionName()
    {
        var message = FunctionMessage.Create("get_weather", "{ \"temp\": 72 }");

        message.Role.Should().Be(MessageRole.Function);
        message.FunctionName.Should().Be("get_weather");
        message.Content.Should().Be("{ \"temp\": 72 }");
    }

    [Fact]
    public void FunctionMessage_Ctor_Works()
    {
        var message = new FunctionMessage("fn", "content");
        message.FunctionName.Should().Be("fn");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FunctionMessage_EmptyFunctionName_Throws(string? functionName)
    {
        var act = () => new FunctionMessage(functionName!, "content");
        act.Should().Throw<ArgumentException>().WithMessage("*Function name cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Message_EmptyContent_Throws(string? content)
    {
        var act = () => new SystemMessage(content!);
        act.Should().Throw<PromptValidationException>().WithMessage("*Message content cannot be null or empty*");
    }

    private sealed class TestMessage : Message
    {
        public TestMessage(MessageRole role, string content) : base(role, content) { }
    }

    [Fact]
    public void Message_NullRole_Throws()
    {
        var act = () => new TestMessage(null!, "content");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        var a = new UserMessage("content", id: "same-id");
        var b = new UserMessage("different content", id: "same-id");

        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = new UserMessage("content", id: "id-1");
        var b = new UserMessage("content", id: "id-2");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new UserMessage("content");
        Message? nullMessage = null;
        object? nullObject = null;
        object notAMessage = "not a message";

        a.Equals(nullMessage).Should().BeFalse();
        a.Equals(nullObject).Should().BeFalse();
        a.Equals(notAMessage).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShortContent_ReturnsFullContent()
    {
        var message = new UserMessage("short");
        message.ToString().Should().Be("[user] short");
    }

    [Fact]
    public void ToString_LongContent_TruncatesTo50Characters()
    {
        var longContent = new string('a', 100);
        var message = new UserMessage(longContent);

        var expected = $"[user] {longContent[..50]}";
        message.ToString().Should().Be(expected);
    }

    [Fact]
    public void ToString_ExactlyFiftyCharacters_ReturnsFullContent()
    {
        var content = new string('b', 50);
        var message = new UserMessage(content);

        message.ToString().Should().Be($"[user] {content}");
    }

    [Fact]
    public void Metadata_DefaultsToEmpty_WhenNotProvided()
    {
        var message = new UserMessage("content");
        message.Metadata.Should().NotBeNull();
        message.Metadata.Name.Should().BeNull();
    }

    [Fact]
    public void Id_GeneratedAutomatically_WhenNotProvided()
    {
        var a = new UserMessage("content");
        var b = new UserMessage("content");
        a.Id.Should().NotBe(b.Id);
    }
}
