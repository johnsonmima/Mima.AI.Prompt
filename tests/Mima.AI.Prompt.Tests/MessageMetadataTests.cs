using FluentAssertions;
using Mima.AI.Prompt.Models;

namespace Mima.AI.Prompt.Tests;

public class MessageMetadataTests
{
    [Fact]
    public void Empty_HasAllNullOrDefaultProperties()
    {
        var metadata = MessageMetadata.Empty;

        metadata.Name.Should().BeNull();
        metadata.Description.Should().BeNull();
        metadata.Category.Should().BeNull();
        metadata.Version.Should().BeNull();
        metadata.Author.Should().BeNull();
        metadata.Tags.Should().BeEmpty();
        metadata.Language.Should().BeNull();
        metadata.Provider.Should().BeNull();
        metadata.Model.Should().BeNull();
        metadata.MinCompatibleVersion.Should().BeNull();
        metadata.SemanticVersion.Should().BeNull();
    }

    [Fact]
    public void WithName_CreatesMetadataWithOnlyName()
    {
        var metadata = MessageMetadata.WithName("MyName");
        metadata.Name.Should().Be("MyName");
    }

    [Fact]
    public void Ctor_SetsAllProvidedValues()
    {
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var modified = DateTimeOffset.UtcNow;
        var metadata = new MessageMetadata(
            name: "n",
            description: "d",
            category: "c",
            version: "1.0.0",
            author: "a",
            tags: new[] { "t1", "t2" },
            language: "en",
            provider: "openai",
            model: "gpt-4",
            created: created,
            modified: modified,
            minCompatibleVersion: "0.9.0");

        metadata.Name.Should().Be("n");
        metadata.Description.Should().Be("d");
        metadata.Category.Should().Be("c");
        metadata.Version.Should().Be("1.0.0");
        metadata.Author.Should().Be("a");
        metadata.Tags.Should().BeEquivalentTo(new[] { "t1", "t2" });
        metadata.Language.Should().Be("en");
        metadata.Provider.Should().Be("openai");
        metadata.Model.Should().Be("gpt-4");
        metadata.Created.Should().Be(created);
        metadata.Modified.Should().Be(modified);
        metadata.MinCompatibleVersion.Should().Be("0.9.0");
    }

    [Fact]
    public void Ctor_DefaultsCreatedAndModified_ToUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var metadata = new MessageMetadata();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        metadata.Created.Should().BeAfter(before).And.BeBefore(after);
        metadata.Modified.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void SetName_ReturnsNewInstanceWithUpdatedName()
    {
        var original = MessageMetadata.Empty;
        var updated = original.SetName("NewName");

        updated.Name.Should().Be("NewName");
        original.Name.Should().BeNull();
    }

    [Fact]
    public void SetDescription_ReturnsNewInstanceWithUpdatedDescription()
    {
        var updated = MessageMetadata.Empty.SetDescription("desc");
        updated.Description.Should().Be("desc");
    }

    [Fact]
    public void SetVersion_String_ReturnsNewInstanceWithUpdatedVersion()
    {
        var updated = MessageMetadata.Empty.SetVersion("2.0.0");
        updated.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void SetVersion_PromptVersion_ReturnsNewInstanceWithVersionString()
    {
        var updated = MessageMetadata.Empty.SetVersion(PromptVersion.Create(1, 2, 3));
        updated.Version.Should().Be("1.2.3");
    }

    [Fact]
    public void SetVersion_NullPromptVersion_Throws()
    {
        var act = () => MessageMetadata.Empty.SetVersion((PromptVersion)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SemanticVersion_ValidVersionString_ReturnsParsedVersion()
    {
        var metadata = MessageMetadata.Empty.SetVersion("1.2.3");
        metadata.SemanticVersion.Should().NotBeNull();
        metadata.SemanticVersion!.Major.Should().Be(1);
    }

    [Fact]
    public void SemanticVersion_InvalidVersionString_ReturnsNull()
    {
        var metadata = MessageMetadata.Empty.SetVersion("not-a-version!!!");
        metadata.SemanticVersion.Should().BeNull();
    }

    [Fact]
    public void SemanticVersion_NoVersionSet_ReturnsNull()
    {
        MessageMetadata.Empty.SemanticVersion.Should().BeNull();
    }

    [Fact]
    public void SetTags_ReturnsNewInstanceWithTags()
    {
        var updated = MessageMetadata.Empty.SetTags(new[] { "a", "b" });
        updated.Tags.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void AddTag_AppendsToExistingTags()
    {
        var metadata = MessageMetadata.Empty.SetTags(new[] { "a" }).AddTag("b");
        metadata.Tags.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void SetAuthor_ReturnsNewInstanceWithUpdatedAuthor()
    {
        var updated = MessageMetadata.Empty.SetAuthor("Jane");
        updated.Author.Should().Be("Jane");
    }

    [Fact]
    public void SetCategory_ReturnsNewInstanceWithUpdatedCategory()
    {
        var updated = MessageMetadata.Empty.SetCategory("coding");
        updated.Category.Should().Be("coding");
    }

    [Fact]
    public void SetLanguage_ReturnsNewInstanceWithUpdatedLanguage()
    {
        var updated = MessageMetadata.Empty.SetLanguage("fr");
        updated.Language.Should().Be("fr");
    }

    [Fact]
    public void SetProvider_ReturnsNewInstanceWithUpdatedProvider()
    {
        var updated = MessageMetadata.Empty.SetProvider("anthropic");
        updated.Provider.Should().Be("anthropic");
    }

    [Fact]
    public void SetModel_ReturnsNewInstanceWithUpdatedModel()
    {
        var updated = MessageMetadata.Empty.SetModel("claude-3");
        updated.Model.Should().Be("claude-3");
    }

    [Fact]
    public void SetMinCompatibleVersion_String_ReturnsNewInstance()
    {
        var updated = MessageMetadata.Empty.SetMinCompatibleVersion("1.0.0");
        updated.MinCompatibleVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void SetMinCompatibleVersion_PromptVersion_ReturnsNewInstance()
    {
        var updated = MessageMetadata.Empty.SetMinCompatibleVersion(PromptVersion.Create(1, 0, 0));
        updated.MinCompatibleVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void SetMinCompatibleVersion_NullPromptVersion_Throws()
    {
        var act = () => MessageMetadata.Empty.SetMinCompatibleVersion((PromptVersion)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FluentChaining_ProducesFullyConfiguredMetadata()
    {
        var metadata = MessageMetadata.Empty
            .SetName("Chain")
            .SetDescription("desc")
            .SetCategory("cat")
            .SetVersion("1.0.0")
            .SetAuthor("author")
            .AddTag("tag1")
            .SetLanguage("en")
            .SetProvider("openai")
            .SetModel("gpt-4")
            .SetMinCompatibleVersion("0.5.0");

        metadata.Name.Should().Be("Chain");
        metadata.Description.Should().Be("desc");
        metadata.Category.Should().Be("cat");
        metadata.Version.Should().Be("1.0.0");
        metadata.Author.Should().Be("author");
        metadata.Tags.Should().Contain("tag1");
        metadata.Language.Should().Be("en");
        metadata.Provider.Should().Be("openai");
        metadata.Model.Should().Be("gpt-4");
        metadata.MinCompatibleVersion.Should().Be("0.5.0");
    }
}
