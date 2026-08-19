using FluentAssertions;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Providers;
using Mima.AI.Prompt.Rendering;

namespace Mima.AI.Prompt.Tests;

public class RenderingAndProviderTests
{
    private static Models.Prompt MakePrompt(params IMessage[] messages) => new(messages.ToList());

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    // --- GenericPromptRenderer ---

    [Fact]
    public void GenericRenderer_ProviderName_IsGeneric()
    {
        new GenericPromptRenderer().ProviderName.Should().Be("generic");
    }

    [Fact]
    public void GenericRenderer_Render_ProducesJsonWithMessages()
    {
        var renderer = new GenericPromptRenderer();
        var prompt = MakePrompt(new SystemMessage("sys"), new UserMessage("hi"));

        var json = renderer.Render(prompt);

        json.Should().Contain("messages");
        json.Should().Contain("sys");
        json.Should().Contain("hi");
    }

    [Fact]
    public void GenericRenderer_Render_NullPrompt_Throws()
    {
        var renderer = new GenericPromptRenderer();
        var act = () => renderer.Render(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenericRenderer_RenderMessages_ProducesRoleContentDictionaries()
    {
        var renderer = new GenericPromptRenderer();
        var prompt = MakePrompt(new UserMessage("hi"));

        var messages = renderer.RenderMessages(prompt);

        messages.Should().ContainSingle();
        messages[0]["role"].Should().Be("user");
        messages[0]["content"].Should().Be("hi");
    }

    [Fact]
    public void GenericRenderer_RenderMessages_NullPrompt_Throws()
    {
        var renderer = new GenericPromptRenderer();
        var act = () => renderer.RenderMessages(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- ProviderValidationResult ---

    [Fact]
    public void ProviderValidationResult_Success_IsValidWithNoIssues()
    {
        var result = ProviderValidationResult.Success();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ProviderValidationResult_Ctor_DefaultsToEmptyCollections()
    {
        var result = new ProviderValidationResult(true);
        result.Warnings.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    // --- OpenAiAdapter ---

    [Fact]
    public void OpenAiAdapter_ProviderName_IsOpenAi()
    {
        new OpenAiAdapter().ProviderName.Should().Be("openai");
    }

    [Fact]
    public void OpenAiAdapter_SupportedRoles_IncludesExpectedRoles()
    {
        var adapter = new OpenAiAdapter();
        adapter.SupportedRoles.Should().Contain(new[]
        {
            Mima.AI.Prompt.Roles.MessageRole.System,
            Mima.AI.Prompt.Roles.MessageRole.User,
            Mima.AI.Prompt.Roles.MessageRole.Assistant,
            Mima.AI.Prompt.Roles.MessageRole.Tool,
            Mima.AI.Prompt.Roles.MessageRole.Function
        });
    }

    [Fact]
    public void OpenAiAdapter_ToProviderFormat_MergesDeveloperIntoSystem()
    {
        var adapter = new OpenAiAdapter();
        var prompt = MakePrompt(new DeveloperMessage("dev"), new UserMessage("hi"));

        var format = adapter.ToProviderFormat(prompt);
        var messages = ((IEnumerable<object>)GetProp(format, "messages")!).ToList();
        var role = GetProp(messages[0], "role");

        role.Should().Be("system");
    }

    [Fact]
    public void OpenAiAdapter_ToJson_ProducesValidJson()
    {
        var adapter = new OpenAiAdapter();
        var prompt = MakePrompt(new UserMessage("hi"));

        var json = adapter.ToJson(prompt);
        json.Should().Contain("hi");
    }

    [Fact]
    public void OpenAiAdapter_EstimateTokens_ReturnsPositiveNumber()
    {
        var adapter = new OpenAiAdapter();
        var prompt = MakePrompt(new UserMessage("Hello world, this is a test message."));

        adapter.EstimateTokens(prompt).Should().BeGreaterThan(0);
    }

    [Fact]
    public void OpenAiAdapter_Validate_MultipleSystemMessages_Warns()
    {
        var adapter = new OpenAiAdapter();
        var prompt = MakePrompt(new SystemMessage("sys1"), new DeveloperMessage("dev1"), new UserMessage("hi"));

        var result = adapter.Validate(prompt);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle(w => w.Contains("single system message"));
    }

    [Fact]
    public void OpenAiAdapter_Validate_SingleSystem_NoWarning()
    {
        var adapter = new OpenAiAdapter();
        var prompt = MakePrompt(new SystemMessage("sys"), new UserMessage("hi"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().BeEmpty();
    }

    // --- AnthropicAdapter ---

    [Fact]
    public void AnthropicAdapter_ProviderName_IsAnthropic()
    {
        new AnthropicAdapter().ProviderName.Should().Be("anthropic");
    }

    [Fact]
    public void AnthropicAdapter_SupportedRoles_ExcludesToolAndFunction()
    {
        var adapter = new AnthropicAdapter();
        adapter.SupportedRoles.Should().NotContain(Mima.AI.Prompt.Roles.MessageRole.Tool);
        adapter.SupportedRoles.Should().NotContain(Mima.AI.Prompt.Roles.MessageRole.Function);
    }

    [Fact]
    public void AnthropicAdapter_ToProviderFormat_SeparatesSystemFromMessages()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new SystemMessage("sys"), new DeveloperMessage("dev"), new UserMessage("hi"), new AssistantMessage("hello"));

        var format = adapter.ToProviderFormat(prompt);
        var system = (string)GetProp(format, "system")!;

        system.Should().Contain("sys").And.Contain("dev");
    }

    [Fact]
    public void AnthropicAdapter_ToProviderFormat_NoSystemMessages_OmitsSystemField()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("hi"));

        object format = adapter.ToProviderFormat(prompt);
        format.GetType().GetProperty("system").Should().BeNull();
    }

    [Fact]
    public void AnthropicAdapter_ToJson_ProducesValidJson()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("hi"));

        var json = adapter.ToJson(prompt);
        json.Should().Contain("hi");
    }

    [Fact]
    public void AnthropicAdapter_EstimateTokens_ReturnsPositiveNumber()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("Some content for estimation"));

        adapter.EstimateTokens(prompt).Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnthropicAdapter_Validate_ConsecutiveRoles_Warns()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("u1"), new UserMessage("u2"), new AssistantMessage("a1"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().Contain(w => w.Contains("consecutive"));
    }

    [Fact]
    public void AnthropicAdapter_Validate_FirstMessageNotUser_Warns()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new AssistantMessage("a1"), new UserMessage("u1"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().Contain(w => w.Contains("first conversation message"));
    }

    [Fact]
    public void AnthropicAdapter_Validate_ToolOrFunctionMessages_Warns()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("u1"), new ToolMessage("call-1", "result"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().Contain(w => w.Contains("tool_use"));
    }

    [Fact]
    public void AnthropicAdapter_Validate_CleanConversation_NoWarnings()
    {
        var adapter = new AnthropicAdapter();
        var prompt = MakePrompt(new UserMessage("u1"), new AssistantMessage("a1"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().BeEmpty();
        result.IsValid.Should().BeTrue();
    }

    // --- OllamaAdapter ---

    [Fact]
    public void OllamaAdapter_ProviderName_IsOllama()
    {
        new OllamaAdapter().ProviderName.Should().Be("ollama");
    }

    [Fact]
    public void OllamaAdapter_SupportedRoles_ExcludesToolAndFunction()
    {
        var adapter = new OllamaAdapter();
        adapter.SupportedRoles.Should().NotContain(Mima.AI.Prompt.Roles.MessageRole.Tool);
    }

    [Fact]
    public void OllamaAdapter_ToProviderFormat_MergesDeveloperIntoSystem()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new DeveloperMessage("dev"), new UserMessage("hi"));

        var format = adapter.ToProviderFormat(prompt);
        var messages = ((IEnumerable<object>)GetProp(format, "messages")!).ToList();
        var role = GetProp(messages[0], "role");

        role.Should().Be("system");
    }

    [Fact]
    public void OllamaAdapter_ToProviderFormat_ExcludesToolAndFunctionMessages()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new UserMessage("hi"), new ToolMessage("call-1", "result"));

        var format = adapter.ToProviderFormat(prompt);
        var messages = ((IEnumerable<object>)GetProp(format, "messages")!).ToList();

        messages.Should().HaveCount(1);
    }

    [Fact]
    public void OllamaAdapter_ToJson_ProducesValidJson()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new UserMessage("hi"));

        var json = adapter.ToJson(prompt);
        json.Should().Contain("hi");
    }

    [Fact]
    public void OllamaAdapter_EstimateTokens_ReturnsPositiveNumber()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new UserMessage("Some text"));

        adapter.EstimateTokens(prompt).Should().BeGreaterThan(0);
    }

    [Fact]
    public void OllamaAdapter_Validate_ToolMessages_Warns()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new UserMessage("hi"), new FunctionMessage("fn", "result"));

        var result = adapter.Validate(prompt);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("not supported by most Ollama models"));
    }

    [Fact]
    public void OllamaAdapter_Validate_NoToolMessages_NoWarnings()
    {
        var adapter = new OllamaAdapter();
        var prompt = MakePrompt(new UserMessage("hi"));

        var result = adapter.Validate(prompt);

        result.Warnings.Should().BeEmpty();
    }
}
