using FluentAssertions;
using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Tests;

public class PromptConstraintsTests
{
    [Fact]
    public void Create_ReturnsEmptyConstraints()
    {
        var constraints = PromptConstraints.Create();

        constraints.MaxWordCount.Should().BeNull();
        constraints.MinWordCount.Should().BeNull();
        constraints.Format.Should().BeNull();
        constraints.MustDo.Should().BeEmpty();
        constraints.MustNot.Should().BeEmpty();
    }

    [Fact]
    public void MaxWords_SetsMaxWordCount()
    {
        var constraints = PromptConstraints.Create().MaxWords(200);
        constraints.MaxWordCount.Should().Be(200);
    }

    [Fact]
    public void MinWords_SetsMinWordCount()
    {
        var constraints = PromptConstraints.Create().MinWords(50);
        constraints.MinWordCount.Should().Be(50);
    }

    [Fact]
    public void NoEmojis_AddsNegativeConstraint()
    {
        var constraints = PromptConstraints.Create().NoEmojis();
        constraints.MustNot.Should().ContainSingle().Which.Should().Be("Do not use emojis.");
    }

    [Fact]
    public void NoTables_AddsNegativeConstraint()
    {
        var constraints = PromptConstraints.Create().NoTables();
        constraints.MustNot.Should().Contain("Do not use tables.");
    }

    [Fact]
    public void NoCode_AddsNegativeConstraint()
    {
        var constraints = PromptConstraints.Create().NoCode();
        constraints.MustNot.Should().Contain("Do not include code blocks.");
    }

    [Fact]
    public void NoLinks_AddsNegativeConstraint()
    {
        var constraints = PromptConstraints.Create().NoLinks();
        constraints.MustNot.Should().Contain("Do not include URLs or links.");
    }

    [Fact]
    public void MustInclude_AddsPositiveConstraint()
    {
        var constraints = PromptConstraints.Create().MustInclude("Conclusion");
        constraints.MustDo.Should().Contain("Conclusion");
    }

    [Fact]
    public void MustAvoid_AddsNegativeConstraintWithPrefix()
    {
        var constraints = PromptConstraints.Create().MustAvoid("jargon");
        constraints.MustNot.Should().Contain("Avoid: jargon");
    }

    [Fact]
    public void WithFormat_SetsFormat()
    {
        var format = OutputFormat.Json();
        var constraints = PromptConstraints.Create().WithFormat(format);
        constraints.Format.Should().Be(format);
    }

    [Fact]
    public void Must_AddsCustomPositiveConstraint()
    {
        var constraints = PromptConstraints.Create().Must("Be polite");
        constraints.MustDo.Should().Contain("Be polite");
    }

    [Fact]
    public void Not_AddsCustomNegativeConstraint()
    {
        var constraints = PromptConstraints.Create().Not("Be rude");
        constraints.MustNot.Should().Contain("Be rude");
    }

    [Fact]
    public void Render_EmptyConstraints_ReturnsEmptyString()
    {
        var constraints = PromptConstraints.Create();
        constraints.Render().Should().BeEmpty();
    }

    [Fact]
    public void Render_MaxWords_IncludesLimitLine()
    {
        var constraints = PromptConstraints.Create().MaxWords(200);
        constraints.Render().Should().Contain("Limit your response to 200 words maximum.");
    }

    [Fact]
    public void Render_MinWords_IncludesMinimumLine()
    {
        var constraints = PromptConstraints.Create().MinWords(50);
        constraints.Render().Should().Contain("Your response must be at least 50 words.");
    }

    [Fact]
    public void Render_Format_IncludesFormatInstructions()
    {
        var constraints = PromptConstraints.Create().WithFormat(OutputFormat.Markdown());
        constraints.Render().Should().Contain("Markdown");
    }

    [Fact]
    public void Render_PositiveConstraints_IncludesRequirementsSection()
    {
        var constraints = PromptConstraints.Create().MustInclude("Conclusion").MustInclude("Summary");
        var rendered = constraints.Render();

        rendered.Should().Contain("Requirements:");
        rendered.Should().Contain("- Conclusion");
        rendered.Should().Contain("- Summary");
    }

    [Fact]
    public void Render_NegativeConstraints_IncludesRestrictionsSection()
    {
        var constraints = PromptConstraints.Create().NoEmojis().NoTables();
        var rendered = constraints.Render();

        rendered.Should().Contain("Restrictions:");
        rendered.Should().Contain("- Do not use emojis.");
        rendered.Should().Contain("- Do not use tables.");
    }

    [Fact]
    public void Render_FullConstraints_IncludesAllSections()
    {
        var constraints = PromptConstraints.Create()
            .MaxWords(200)
            .MinWords(10)
            .WithFormat(OutputFormat.Json())
            .MustInclude("Conclusion")
            .Must("Be concise")
            .NoEmojis()
            .MustAvoid("jargon")
            .Not("Be rude");

        var rendered = constraints.Render();

        rendered.Should().Contain("200 words maximum");
        rendered.Should().Contain("at least 10 words");
        rendered.Should().Contain("valid JSON");
        rendered.Should().Contain("Requirements:");
        rendered.Should().Contain("- Conclusion");
        rendered.Should().Contain("- Be concise");
        rendered.Should().Contain("Restrictions:");
        rendered.Should().Contain("- Do not use emojis.");
        rendered.Should().Contain("- Avoid: jargon");
        rendered.Should().Contain("- Be rude");
    }

    [Fact]
    public void FluentMethods_AreChainable()
    {
        var constraints = PromptConstraints.Create()
            .MaxWords(100)
            .MinWords(10)
            .NoEmojis()
            .NoTables()
            .NoCode()
            .NoLinks()
            .MustInclude("a")
            .MustAvoid("b")
            .WithFormat(OutputFormat.PlainText())
            .Must("c")
            .Not("d");

        constraints.Should().NotBeNull();
        constraints.MustDo.Should().HaveCount(2);
        constraints.MustNot.Should().HaveCount(6);
    }
}
