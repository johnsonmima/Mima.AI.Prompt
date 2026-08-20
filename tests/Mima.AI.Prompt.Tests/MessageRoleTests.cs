using FluentAssertions;
using Mima.AI.Prompt.Roles;

namespace Mima.AI.Prompt.Tests;

public class MessageRoleTests
{
    [Theory]
    [InlineData("system")]
    [InlineData("SYSTEM")]
    [InlineData("System")]
    [InlineData(" system ")]
    public void Parse_System_ReturnsSystemRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.System);
    }

    [Theory]
    [InlineData("developer")]
    [InlineData("DEVELOPER")]
    [InlineData("Developer")]
    public void Parse_Developer_ReturnsDeveloperRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.Developer);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("USER")]
    [InlineData("User")]
    public void Parse_User_ReturnsUserRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.User);
    }

    [Theory]
    [InlineData("assistant")]
    [InlineData("ASSISTANT")]
    [InlineData("Assistant")]
    public void Parse_Assistant_ReturnsAssistantRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.Assistant);
    }

    [Theory]
    [InlineData("tool")]
    [InlineData("TOOL")]
    [InlineData("Tool")]
    public void Parse_Tool_ReturnsToolRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.Tool);
    }

    [Theory]
    [InlineData("function")]
    [InlineData("FUNCTION")]
    [InlineData("Function")]
    public void Parse_Function_ReturnsFunctionRole(string input)
    {
        MessageRole.Parse(input).Should().Be(MessageRole.Function);
    }

    [Fact]
    public void Parse_UnknownRole_Throws()
    {
        var act = () => MessageRole.Parse("wizard");
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown message role*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrEmpty_Throws(string? input)
    {
        var act = () => MessageRole.Parse(input ?? TestNull.Ref<string>());
        act.Should().Throw<ArgumentException>().WithMessage("*Role name cannot be null or empty*");
    }

    [Fact]
    public void TryParse_ValidRole_ReturnsTrueAndRole()
    {
        bool result = MessageRole.TryParse("user", out var role);

        result.Should().BeTrue();
        role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void TryParse_InvalidRole_ReturnsFalseAndNull()
    {
        bool result = MessageRole.TryParse("notarole", out var role);

        result.Should().BeFalse();
        role.Should().BeNull();
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        bool result = MessageRole.TryParse(null, out var role);

        result.Should().BeFalse();
        role.Should().BeNull();
    }

    [Fact]
    public void TryParse_Empty_ReturnsFalse()
    {
        bool result = MessageRole.TryParse(string.Empty, out var role);

        result.Should().BeFalse();
        role.Should().BeNull();
    }

    [Fact]
    public void All_ReturnsSixRoles()
    {
        MessageRole.All.Should().HaveCount(6);
        MessageRole.All.Should().Contain(new[]
        {
            MessageRole.System, MessageRole.Developer, MessageRole.User,
            MessageRole.Assistant, MessageRole.Tool, MessageRole.Function
        });
    }

    [Fact]
    public void EqualityOperator_SameRole_ReturnsTrue()
    {
        MessageRole system = MessageRole.System;
        (system == MessageRole.System).Should().BeTrue();
        (MessageRole.System != MessageRole.User).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        MessageRole? left = null;
        MessageRole? right = null;
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        MessageRole? left = MessageRole.System;
        MessageRole? right = null;
        (left == right).Should().BeFalse();
        (right == left).Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WorksCorrectly()
    {
        MessageRole.System.Equals((object)MessageRole.System).Should().BeTrue();
        MessageRole.System.Equals((object)MessageRole.User).Should().BeFalse();
        MessageRole.System.Equals((object)"system").Should().BeFalse();
        MessageRole.System.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_MessageRole_NullReturnsFalse()
    {
        MessageRole.System.Equals((MessageRole?)null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_MatchesNameHashCode()
    {
        MessageRole.System.GetHashCode().Should().Be("system".GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        MessageRole.System.ToString().Should().Be("system");
        MessageRole.User.ToString().Should().Be("user");
    }

    [Theory]
    [MemberData(nameof(RolePriorityData))]
    public void Priority_HasExpectedValue(MessageRole role, int expectedPriority)
    {
        role.Priority.Should().Be(expectedPriority);
    }

    public static IEnumerable<object[]> RolePriorityData()
    {
        yield return new object[] { MessageRole.System, 0 };
        yield return new object[] { MessageRole.Developer, 1 };
        yield return new object[] { MessageRole.User, 2 };
        yield return new object[] { MessageRole.Assistant, 3 };
        yield return new object[] { MessageRole.Tool, 4 };
        yield return new object[] { MessageRole.Function, 5 };
    }

    [Theory]
    [MemberData(nameof(RoleNameData))]
    public void Name_HasExpectedValue(MessageRole role, string expectedName)
    {
        role.Name.Should().Be(expectedName);
    }

    public static IEnumerable<object[]> RoleNameData()
    {
        yield return new object[] { MessageRole.System, "system" };
        yield return new object[] { MessageRole.Developer, "developer" };
        yield return new object[] { MessageRole.User, "user" };
        yield return new object[] { MessageRole.Assistant, "assistant" };
        yield return new object[] { MessageRole.Tool, "tool" };
        yield return new object[] { MessageRole.Function, "function" };
    }

    [Fact]
    public void Description_IsNotEmptyForAllRoles()
    {
        foreach (var role in MessageRole.All)
        {
            role.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Description_ForEachRole_IsSpecific()
    {
        MessageRole.System.Description.Should().Contain("identity");
        MessageRole.Developer.Description.Should().Contain("system");
        MessageRole.User.Description.Should().Contain("human");
        MessageRole.Assistant.Description.Should().Contain("AI responses");
        MessageRole.Tool.Description.Should().Contain("tool");
        MessageRole.Function.Description.Should().Contain("function");
    }
}
