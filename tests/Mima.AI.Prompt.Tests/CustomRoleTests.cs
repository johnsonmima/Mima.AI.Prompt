using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Providers;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Tests;

public class CustomRoleTests
{
    [Fact]
    public void Custom_CreatesCustomRole_NormalizedLowercase()
    {
        var role = MessageRole.Custom("Critic");

        role.Should().BeOfType<CustomRole>();
        role.Name.Should().Be("critic");
        role.IsBuiltIn.Should().BeFalse();
        role.Priority.Should().Be(int.MaxValue);
        role.Description.Should().Contain("critic");
    }

    [Fact]
    public void Custom_WithPriorityAndDescription_UsesProvidedValues()
    {
        var role = MessageRole.Custom("agent", priority: 10, description: "Autonomous agent");

        role.Priority.Should().Be(10);
        role.Description.Should().Be("Autonomous agent");
    }

    [Fact]
    public void Custom_BuiltinName_ReturnsBuiltinSingleton()
    {
        MessageRole.Custom("system").Should().BeSameAs(MessageRole.System);
        MessageRole.Custom("USER").Should().BeSameAs(MessageRole.User);
        MessageRole.Custom("system").IsBuiltIn.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Custom_NullOrEmpty_Throws(string? name)
    {
        var act = () => MessageRole.Custom(name!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Custom_SameName_AreEqual()
    {
        var a = MessageRole.Custom("critic");
        var b = MessageRole.Custom("CRITIC");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Parse_StillRejectsCustomNames()
    {
        var act = () => MessageRole.Parse("critic");
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown built-in*");
    }

    [Fact]
    public void TryParse_CustomName_ReturnsFalse()
    {
        MessageRole.TryParse("critic", out var role).Should().BeFalse();
        role.Should().BeNull();
    }

    [Fact]
    public void ParseOrCreate_Builtin_ReturnsBuiltin()
    {
        MessageRole.ParseOrCreate("assistant").Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void ParseOrCreate_Unknown_ReturnsCustom()
    {
        var role = MessageRole.ParseOrCreate("moderator");
        role.Should().BeOfType<CustomRole>();
        role.Name.Should().Be("moderator");
    }

    [Fact]
    public void ParseOrCreate_Empty_Throws()
    {
        var act = () => MessageRole.ParseOrCreate("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuiltInRoles_IsBuiltIn_True()
    {
        foreach (var role in MessageRole.All)
            role.IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public void All_DoesNotIncludeCustomRoles()
    {
        _ = MessageRole.Custom("critic");
        MessageRole.All.Should().HaveCount(6);
        MessageRole.All.Should().NotContain(r => r.Name == "critic");
    }

    [Fact]
    public void CustomMessage_Create_WithRole()
    {
        var role = MessageRole.Custom("critic");
        var message = CustomMessage.Create(role, "Find flaws.");

        message.Role.Should().Be(role);
        message.Content.Should().Be("Find flaws.");
    }

    [Fact]
    public void CustomMessage_Create_WithRoleName()
    {
        var message = CustomMessage.Create("reviewer", "Review this.");
        message.Role.Name.Should().Be("reviewer");
        message.Role.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public void PromptBuilder_Add_CustomRole()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem("You host a debate.")
            .Add(MessageRole.Custom("critic"), "Challenge weak points.")
            .AddUser("Explain DI.")
            .Build();

        prompt.MessageCount.Should().Be(3);
        prompt.Messages[1].Should().BeOfType<CustomMessage>();
        prompt.Messages[1].Role.Name.Should().Be("critic");
    }

    [Fact]
    public void PromptBuilder_AddCustom_ByName()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("agent", "Plan the next step.")
            .AddUser("Go.")
            .Build();

        prompt.Messages[0].Role.Name.Should().Be("agent");
    }

    [Fact]
    public void PromptBuilder_Add_Builtin_DispatchesToConcreteType()
    {
        var prompt = PromptBuilder.Create()
            .Add(MessageRole.System, "Be helpful.")
            .Add(MessageRole.User, "Hi")
            .Build();

        prompt.Messages[0].Should().BeOfType<SystemMessage>();
        prompt.Messages[1].Should().BeOfType<UserMessage>();
    }

    [Fact]
    public void PromptBuilder_Add_ToolRole_Throws()
    {
        var act = () => PromptBuilder.Create().Add(MessageRole.Tool, "result");
        act.Should().Throw<ArgumentException>().WithMessage("*AddTool*");
    }

    [Fact]
    public void PromptBuilder_Add_NullRole_Throws()
    {
        var act = () => PromptBuilder.Create().Add(null!, "x");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Serializer_RoundTrips_CustomRole()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("critic", "Be skeptical.")
            .AddUser("Claim X")
            .Build();

        var serializer = new PromptSerializer();
        var json = serializer.Serialize(prompt);
        var restored = serializer.DeserializePrompt(json);

        restored.Messages[0].Should().BeOfType<CustomMessage>();
        restored.Messages[0].Role.Name.Should().Be("critic");
        restored.Messages[0].Role.IsBuiltIn.Should().BeFalse();
        restored.Messages[0].Content.Should().Be("Be skeptical.");
    }

    [Fact]
    public void Serializer_DeserializeMessage_CustomRole()
    {
        var serializer = new PromptSerializer();
        var original = CustomMessage.Create("oracle", "42");
        var json = serializer.SerializeMessage(original);
        var restored = serializer.DeserializeMessage(json);

        restored.Role.Name.Should().Be("oracle");
        restored.Should().BeOfType<CustomMessage>();
    }

    [Fact]
    public void CustomTemplate_Render_ProducesCustomMessage()
    {
        var template = CustomTemplate.Create("critic", "Review: {{claim}}");
        var message = template.Render(new { claim = "DI is always better" });

        message.Should().BeOfType<CustomMessage>();
        message.Role.Name.Should().Be("critic");
        message.Content.Should().Contain("DI is always better");
    }

    [Fact]
    public void CustomTemplate_WithBuiltinRole_ProducesConcreteMessage()
    {
        var template = CustomTemplate.Create(MessageRole.User, "Hello {{name}}");
        var message = template.Render(new { name = "Ada" });
        message.Should().BeOfType<UserMessage>();
    }

    [Fact]
    public void CustomTemplate_NullRole_Throws()
    {
        var act = () => CustomTemplate.Create((MessageRole)null!, "x");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CustomTemplate_Named_SetsMetadata()
    {
        var template = CustomTemplate.Create(MessageRole.Custom("critic"), "CriticTpl", "Hi {{x}}");
        template.Metadata.Name.Should().Be("CriticTpl");
    }

    [Fact]
    public void OpenAiAdapter_Validate_WarnsOnCustomRole_AndPassesThrough()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("critic", "Be tough.")
            .AddUser("Hi")
            .Build();

        var adapter = new OpenAiAdapter();
        var validation = adapter.Validate(prompt);
        validation.IsValid.Should().BeTrue();
        validation.Warnings.Should().Contain(w => w.Contains("critic"));

        var json = adapter.ToJson(prompt);
        json.Should().Contain("critic");
    }

    [Fact]
    public void AnthropicAdapter_Validate_WarnsOnCustomRole()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("critic", "Be tough.")
            .AddUser("Hi")
            .Build();

        var adapter = new AnthropicAdapter();
        var validation = adapter.Validate(prompt);
        validation.Warnings.Should().Contain(w => w.Contains("critic"));
    }

    [Fact]
    public void OllamaAdapter_PassesCustomRoleThrough_AndWarns()
    {
        var prompt = PromptBuilder.Create()
            .AddCustom("agent", "Plan.")
            .AddUser("Go")
            .Build();

        var adapter = new OllamaAdapter();
        adapter.Validate(prompt).Warnings.Should().Contain(w => w.Contains("agent"));
        adapter.ToJson(prompt).Should().Contain("agent");
    }
}
