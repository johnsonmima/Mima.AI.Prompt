using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Tests;

public class SerializationTests
{
    private readonly PromptSerializer _serializer = new();

    [Fact]
    public void Serialize_Prompt_ProducesJson()
    {
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys"),
            new UserMessage("hi")
        }, new MessageMetadata(name: "MyPrompt"), "prompt-id");

        var json = _serializer.Serialize(prompt);

        json.Should().Contain("\"id\"");
        json.Should().Contain("prompt-id");
        json.Should().Contain("sys");
        json.Should().Contain("hi");
    }

    [Fact]
    public void Serialize_NullPrompt_Throws()
    {
        var act = () => _serializer.Serialize((IPrompt)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeserializePrompt_NullOrEmptyJson_Throws(string? json)
    {
        var act = () => _serializer.DeserializePrompt(json!);
        act.Should().Throw<ArgumentException>().WithMessage("*JSON cannot be null or empty*");
    }

    [Fact]
    public void RoundTrip_Prompt_PreservesMessagesAndMetadata()
    {
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("You are helpful."),
            new UserMessage("Hi there")
        }, new MessageMetadata(name: "RoundTrip"));

        var json = _serializer.Serialize(prompt);
        var deserialized = _serializer.DeserializePrompt(json);

        deserialized.Messages.Should().HaveCount(2);
        deserialized.Messages[0].Role.Should().Be(MessageRole.System);
        deserialized.Messages[0].Content.Should().Be("You are helpful.");
        deserialized.Messages[1].Role.Should().Be(MessageRole.User);
        deserialized.Metadata.Name.Should().Be("RoundTrip");
        deserialized.Id.Should().Be(prompt.Id);
    }

    [Fact]
    public void RoundTrip_AllRoles_PreservesEachRole()
    {
        var prompt = new Models.Prompt(new List<IMessage>
        {
            new SystemMessage("sys"),
            new DeveloperMessage("dev"),
            new UserMessage("user"),
            new AssistantMessage("assistant"),
            new ToolMessage("call-1", "tool-output"),
            new FunctionMessage("my_fn", "fn-output"),
        });

        var json = _serializer.Serialize(prompt);
        var deserialized = _serializer.DeserializePrompt(json);

        deserialized.Messages.Should().HaveCount(6);
        deserialized.Messages[0].Should().BeOfType<SystemMessage>();
        deserialized.Messages[1].Should().BeOfType<DeveloperMessage>();
        deserialized.Messages[2].Should().BeOfType<UserMessage>();
        deserialized.Messages[3].Should().BeOfType<AssistantMessage>();
        deserialized.Messages[4].Should().BeOfType<ToolMessage>();
        deserialized.Messages[5].Should().BeOfType<FunctionMessage>();

        ((ToolMessage)deserialized.Messages[4]).ToolCallId.Should().Be("call-1");
        ((FunctionMessage)deserialized.Messages[5]).FunctionName.Should().Be("my_fn");
    }

    [Fact]
    public void SerializeMessage_ProducesJson()
    {
        var message = new UserMessage("hello");
        var json = _serializer.SerializeMessage(message);

        json.Should().Contain("\"role\"");
        json.Should().Contain("hello");
    }

    [Fact]
    public void SerializeMessage_Null_Throws()
    {
        var act = () => _serializer.SerializeMessage((IMessage)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeMessage_ToolMessage_IncludesToolCallId()
    {
        var message = new ToolMessage("call-42", "result");
        var json = _serializer.SerializeMessage(message);

        json.Should().Contain("toolCallId");
        json.Should().Contain("call-42");
    }

    [Fact]
    public void SerializeMessage_FunctionMessage_IncludesFunctionName()
    {
        var message = new FunctionMessage("get_weather", "result");
        var json = _serializer.SerializeMessage(message);

        json.Should().Contain("functionName");
        json.Should().Contain("get_weather");
    }

    [Fact]
    public void DeserializeMessage_RoundTrips()
    {
        var message = new AssistantMessage("Here's your answer.");
        var json = _serializer.SerializeMessage(message);
        var deserialized = _serializer.DeserializeMessage(json);

        deserialized.Should().BeOfType<AssistantMessage>();
        deserialized.Content.Should().Be("Here's your answer.");
    }

    [Fact]
    public void DeserializeMessage_ToolMessage_PreservesToolCallId()
    {
        var message = new ToolMessage("call-99", "output");
        var json = _serializer.SerializeMessage(message);
        var deserialized = (ToolMessage)_serializer.DeserializeMessage(json);

        deserialized.ToolCallId.Should().Be("call-99");
    }

    [Fact]
    public void DeserializeMessage_FunctionMessage_PreservesFunctionName()
    {
        var message = new FunctionMessage("compute", "output");
        var json = _serializer.SerializeMessage(message);
        var deserialized = (FunctionMessage)_serializer.DeserializeMessage(json);

        deserialized.FunctionName.Should().Be("compute");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeserializeMessage_NullOrEmptyJson_Throws(string? json)
    {
        var act = () => _serializer.DeserializeMessage(json!);
        act.Should().Throw<ArgumentException>().WithMessage("*JSON cannot be null or empty*");
    }

    [Fact]
    public void SerializeTemplate_ProducesJson()
    {
        var template = SystemTemplate.Create("You are a {{profession}}.");
        var json = _serializer.SerializeTemplate(template);

        json.Should().Contain("templateContent");
        json.Should().Contain("profession");
        json.Should().Contain("\"role\"");
    }

    [Fact]
    public void SerializeTemplate_Null_Throws()
    {
        var act = () => _serializer.SerializeTemplate((IMessageTemplate)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeTemplate_IncludesVariablesList()
    {
        var template = UserTemplate.Create("{{a}} {{b}}");
        var json = _serializer.SerializeTemplate(template);

        json.Should().Contain("variables");
        json.Should().Contain("\"a\"");
        json.Should().Contain("\"b\"");
    }

    [Fact]
    public void DeserializeMessage_MissingMetadataField_DefaultsToEmptyMetadata()
    {
        const string json = """
        {
          "role": "user",
          "content": "hello",
          "id": "abc"
        }
        """;

        var message = _serializer.DeserializeMessage(json);

        message.Metadata.Should().NotBeNull();
        message.Metadata.Name.Should().BeNull();
        message.Metadata.Tags.Should().BeEmpty();
    }

    [Fact]
    public void DeserializePrompt_MissingMetadataField_DefaultsToEmptyMetadata()
    {
        const string json = """
        {
          "id": "prompt-1",
          "messages": [
            { "role": "user", "content": "hi", "id": "m1" }
          ]
        }
        """;

        var prompt = _serializer.DeserializePrompt(json);

        prompt.Metadata.Should().NotBeNull();
        prompt.Metadata.Name.Should().BeNull();
    }

    [Fact]
    public void PromptJsonOptions_SharedCamelCaseSettings()
    {
        PromptJsonOptions.IndentedCamelCase.WriteIndented.Should().BeTrue();
        PromptJsonOptions.IndentedCamelCase.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        PromptJsonOptions.IndentedCamelCase.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.Never);

        PromptJsonOptions.IndentedCamelCaseIgnoreNull.WriteIndented.Should().BeTrue();
        PromptJsonOptions.IndentedCamelCaseIgnoreNull.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        PromptJsonOptions.IndentedCamelCaseIgnoreNull.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
    }
}
