using FluentAssertions;
using SaaFarr.AI.Prompt.Exceptions;

namespace SaaFarr.AI.Prompt.Tests;

public class ExceptionTests
{
    [Fact]
    public void Ctor_SingleMessage_SetsErrorsAndMessage()
    {
        var ex = new PromptValidationException("Something went wrong.");

        ex.Message.Should().Be("Something went wrong.");
        ex.Errors.Should().ContainSingle().Which.Should().Be("Something went wrong.");
        ex.MissingVariables.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_MultipleErrors_JoinsMessageAndSetsErrors()
    {
        var errors = new[] { "Error 1", "Error 2" };
        var ex = new PromptValidationException((IEnumerable<string>)errors);

        ex.Message.Should().Be("Error 1; Error 2");
        ex.Errors.Should().BeEquivalentTo(errors);
        ex.MissingVariables.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_MissingVariablesWithTemplateName_SetsMessageAndCollections()
    {
        var missing = new[] { "name", "topic" };
        var ex = new PromptValidationException(missing, "MyTemplate");

        ex.Message.Should().Contain("MyTemplate").And.Contain("name").And.Contain("topic");
        ex.MissingVariables.Should().BeEquivalentTo(missing);
        ex.Errors.Should().HaveCount(2);
        ex.Errors[0].Should().Contain("name");
    }

    [Fact]
    public void Exception_IsExceptionSubtype()
    {
        var ex = new PromptValidationException("error");
        ex.Should().BeAssignableTo<Exception>();
    }
}
