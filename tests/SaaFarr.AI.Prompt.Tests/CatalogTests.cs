using System.Reflection;
using FluentAssertions;
using SaaFarr.AI.Prompt.Catalog;
using SaaFarr.AI.Prompt.Roles;
using SaaFarr.AI.Prompt.Templates;

namespace SaaFarr.AI.Prompt.Tests;

public class CatalogTests
{
    public static IEnumerable<object[]> SystemTemplateProperties() =>
        typeof(SystemTemplates)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(p => new object[] { p.Name, (SystemTemplate)p.GetValue(null)! });

    public static IEnumerable<object[]> UserTemplateProperties() =>
        typeof(UserTemplates)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(p => new object[] { p.Name, (UserTemplate)p.GetValue(null)! });

    [Theory]
    [MemberData(nameof(SystemTemplateProperties))]
    public void SystemTemplates_Property_IsNonNullWithContentAndSystemRole(string name, SystemTemplate template)
    {
        template.Should().NotBeNull($"{name} should not be null");
        template.TemplateContent.Should().NotBeNullOrWhiteSpace($"{name} should have content");
        template.Role.Should().Be(MessageRole.System, $"{name} should be a system role template");
        template.Metadata.Name.Should().NotBeNullOrWhiteSpace($"{name} should have a metadata name");
    }

    [Theory]
    [MemberData(nameof(UserTemplateProperties))]
    public void UserTemplates_Property_IsNonNullWithContentAndUserRole(string name, UserTemplate template)
    {
        template.Should().NotBeNull($"{name} should not be null");
        template.TemplateContent.Should().NotBeNullOrWhiteSpace($"{name} should have content");
        template.Role.Should().Be(MessageRole.User, $"{name} should be a user role template");
        template.Metadata.Name.Should().NotBeNullOrWhiteSpace($"{name} should have a metadata name");
    }

    [Fact]
    public void SystemTemplates_HasAtLeastExpectedNumberOfPersonas()
    {
        var count = typeof(SystemTemplates).GetProperties(BindingFlags.Public | BindingFlags.Static).Length;
        count.Should().BeGreaterOrEqualTo(20);
    }

    [Fact]
    public void UserTemplates_HasAtLeastExpectedNumberOfTemplates()
    {
        var count = typeof(UserTemplates).GetProperties(BindingFlags.Public | BindingFlags.Static).Length;
        count.Should().BeGreaterOrEqualTo(15);
    }

    [Fact]
    public void Configurable_HasProfessionToneMaxWordsVariables()
    {
        SystemTemplates.Configurable.Variables.Should().BeEquivalentTo(new[] { "profession", "tone", "maxWords" });
    }

    [Fact]
    public void Configurable_RendersWithValuesProvided()
    {
        var message = SystemTemplates.Configurable.Render(new Dictionary<string, object>
        {
            ["profession"] = "Teacher",
            ["tone"] = "Friendly",
            ["maxWords"] = "200"
        });

        message.Content.Should().Contain("You are a Teacher.");
        message.Content.Should().Contain("Use a Friendly tone.");
        message.Content.Should().Contain("Limit responses to 200 words.");
    }

    [Theory]
    [MemberData(nameof(UserTemplateProperties))]
    public void UserTemplates_SmokeRender_WithDummyValuesForAllVariables(string name, UserTemplate template)
    {
        var variables = template.Variables.ToDictionary(v => v, v => (object)$"dummy-{v}");

        var message = template.Render(variables);

        message.Should().NotBeNull();
        message.Content.Should().NotBeNullOrWhiteSpace();

        foreach (var variable in template.Variables)
        {
            message.Content.Should().Contain($"dummy-{variable}", $"{name} should render variable '{variable}'");
        }
    }

    [Fact]
    public void HelpfulAssistant_HasNoVariables()
    {
        SystemTemplates.HelpfulAssistant.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Summarize_HasStyleAndContentVariables()
    {
        UserTemplates.Summarize.Variables.Should().BeEquivalentTo(new[] { "style", "content" });
    }
}
