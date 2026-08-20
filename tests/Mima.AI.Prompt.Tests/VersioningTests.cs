using FluentAssertions;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Versioning;

namespace Mima.AI.Prompt.Tests;

public class VersioningTests
{
    private static IPrompt MakePrompt(string content) => Mima.AI.Prompt.Builder.PromptBuilder.UserOnly(content);

    // --- VersionedPromptAsset ---

    [Fact]
    public void Create_SetsNameVersionAndContent()
    {
        var prompt = MakePrompt("v1");
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), prompt);

        asset.Name.Should().Be("MyAsset");
        asset.Version.Should().Be(PromptVersion.Create(1, 0, 0));
        asset.Content.Should().Be(prompt);
        asset.Id.Should().NotBeNullOrEmpty();
        asset.IsDeprecated.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string? name)
    {
        var act = () => VersionedPromptAsset<IPrompt>.Create(name ?? TestNull.Ref<string>(), PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        act.Should().Throw<ArgumentException>().WithMessage("*Asset name cannot be null or empty*");
    }

    [Fact]
    public void Create_NullVersion_Throws()
    {
        var act = () => VersionedPromptAsset<IPrompt>.Create("MyAsset", TestNull.Ref<PromptVersion>(), MakePrompt("v1"));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullContent_Throws()
    {
        var act = () => VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), TestNull.Ref<IPrompt>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .WithDescription("desc");

        asset.Description.Should().Be("desc");
    }

    [Fact]
    public void WithAuthor_SetsAuthor()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .WithAuthor("Jane");

        asset.Author.Should().Be("Jane");
    }

    [Fact]
    public void WithTags_SetsTags()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .WithTags("a", "b");

        asset.Tags.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void WithChangeLog_SetsChangeLog()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .WithChangeLog("Initial release");

        asset.ChangeLog.Should().Be("Initial release");
    }

    [Fact]
    public void Deprecate_MarksAsDeprecatedWithReason()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .Deprecate("Use v2 instead");

        asset.IsDeprecated.Should().BeTrue();
        asset.DeprecationMessage.Should().Be("Use v2 instead");
    }

    [Fact]
    public void BumpMajor_CreatesNewVersionWithSameId()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 5, 3), MakePrompt("v1"));
        var bumped = asset.BumpMajor(MakePrompt("v2"), "Breaking change note");

        bumped.Version.Should().Be(PromptVersion.Create(2, 0, 0));
        bumped.Id.Should().Be(asset.Id);
        bumped.ChangeLog.Should().Be("Breaking change note");
    }

    [Fact]
    public void BumpMajor_DefaultChangeLog_WhenNotProvided()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        var bumped = asset.BumpMajor(MakePrompt("v2"));

        bumped.ChangeLog.Should().Be("Breaking change");
    }

    [Fact]
    public void BumpMinor_CreatesNewVersion()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 5, 3), MakePrompt("v1"));
        var bumped = asset.BumpMinor(MakePrompt("v2"));

        bumped.Version.Should().Be(PromptVersion.Create(1, 6, 0));
        bumped.ChangeLog.Should().Be("New feature");
    }

    [Fact]
    public void BumpPatch_CreatesNewVersion()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 5, 3), MakePrompt("v1"));
        var bumped = asset.BumpPatch(MakePrompt("v2"));

        bumped.Version.Should().Be(PromptVersion.Create(1, 5, 4));
        bumped.ChangeLog.Should().Be("Bug fix");
    }

    [Fact]
    public void ToString_IncludesNameAndVersion()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        asset.ToString().Should().Be("MyAsset v1.0.0");
    }

    [Fact]
    public void ToString_Deprecated_IncludesDeprecatedTag()
    {
        var asset = VersionedPromptAsset<IPrompt>.Create("MyAsset", PromptVersion.Create(1, 0, 0), MakePrompt("v1"))
            .Deprecate("old");

        asset.ToString().Should().Be("MyAsset v1.0.0 [DEPRECATED]");
    }

    // --- PromptVersionHistory ---

    [Fact]
    public void Create_SetsAssetName()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.AssetName.Should().Be("SupportBot");
        history.Count.Should().Be(0);
        history.Latest.Should().BeNull();
        history.LatestStable.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAssetName_Throws(string? name)
    {
        var act = () => PromptVersionHistory<IPrompt>.Create(name ?? TestNull.Ref<string>());
        act.Should().Throw<ArgumentException>().WithMessage("*Asset name cannot be null or empty*");
    }

    [Fact]
    public void Add_AddsVersionEntry()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"), "Initial release", "Jane");

        history.Count.Should().Be(1);
        var entry = Must.Be(history.Get(PromptVersion.Create(1, 0, 0)));
        entry.ChangeLog.Should().Be("Initial release");
        entry.Author.Should().Be("Jane");
    }

    [Fact]
    public void Add_NullVersion_Throws()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        var act = () => history.Add(TestNull.Ref<PromptVersion>(), MakePrompt("v1"));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_NullContent_Throws()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        var act = () => history.Add(PromptVersion.Create(1, 0, 0), TestNull.Ref<IPrompt>());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_DuplicateVersion_Throws()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));

        var act = () => history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v2"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void Get_UnknownVersion_ReturnsNull()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Get(PromptVersion.Create(9, 9, 9)).Should().BeNull();
    }

    [Fact]
    public void GetContent_ReturnsContentForVersion()
    {
        var prompt = MakePrompt("v1");
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), prompt);

        history.GetContent(PromptVersion.Create(1, 0, 0)).Should().Be(prompt);
    }

    [Fact]
    public void GetContent_UnknownVersion_ReturnsNull()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.GetContent(PromptVersion.Create(1, 0, 0)).Should().BeNull();
    }

    [Fact]
    public void Contains_ExistingVersion_ReturnsTrue()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));

        history.Contains(PromptVersion.Create(1, 0, 0)).Should().BeTrue();
        history.Contains(PromptVersion.Create(2, 0, 0)).Should().BeFalse();
    }

    [Fact]
    public void Latest_ReturnsHighestVersion()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        history.Add(PromptVersion.Create(2, 0, 0), MakePrompt("v2"));
        history.Add(PromptVersion.Create(1, 5, 0), MakePrompt("v1.5"));

        Must.Be(history.Latest).Version.Should().Be(PromptVersion.Create(2, 0, 0));
    }

    [Fact]
    public void LatestStable_IgnoresPreReleaseVersions()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        history.Add(PromptVersion.Create(2, 0, 0, "beta"), MakePrompt("v2-beta"));

        Must.Be(history.LatestStable).Version.Should().Be(PromptVersion.Create(1, 0, 0));
        Must.Be(history.Latest).Version.Should().Be(PromptVersion.Create(2, 0, 0, "beta"));
    }

    [Fact]
    public void GetRange_ReturnsInclusiveRange()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        history.Add(PromptVersion.Create(1, 1, 0), MakePrompt("v1.1"));
        history.Add(PromptVersion.Create(2, 0, 0), MakePrompt("v2"));

        var range = history.GetRange(PromptVersion.Create(1, 0, 0), PromptVersion.Create(1, 1, 0));

        range.Should().HaveCount(2);
    }

    [Fact]
    public void GetChangeLog_ReturnsFormattedEntries()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"), "Initial release");
        history.Add(PromptVersion.Create(1, 1, 0), MakePrompt("v1.1"), "Added tone parameter");

        var changelog = history.GetChangeLog(PromptVersion.Create(1, 0, 0), PromptVersion.Create(1, 1, 0));

        changelog.Should().HaveCount(2);
        changelog[0].Should().Contain("v1.0.0").And.Contain("Initial release");
        changelog[1].Should().Contain("v1.1.0").And.Contain("Added tone parameter");
    }

    [Fact]
    public void GetChangeLog_SkipsEntriesWithoutChangeLog()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        history.Add(PromptVersion.Create(1, 1, 0), MakePrompt("v1.1"), "Added feature");

        var changelog = history.GetChangeLog(PromptVersion.Create(1, 0, 0), PromptVersion.Create(1, 1, 0));

        changelog.Should().ContainSingle();
    }

    [Fact]
    public void All_ReturnsAllVersionsInAscendingOrder()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(2, 0, 0), MakePrompt("v2"));
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));

        history.All.Should().HaveCount(2);
        history.All[0].Version.Should().Be(PromptVersion.Create(1, 0, 0));
        history.All[1].Version.Should().Be(PromptVersion.Create(2, 0, 0));
    }

    [Fact]
    public void VersionEntry_ToString_IncludesVersionAndDate()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"), "Initial release");

        var entry = Must.Be(history.Get(PromptVersion.Create(1, 0, 0)));
        entry.ToString().Should().Contain("v1.0.0").And.Contain("Initial release");
    }

    [Fact]
    public void VersionEntry_ToString_NoChangeLog_OmitsColon()
    {
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));

        var entry = Must.Be(history.Get(PromptVersion.Create(1, 0, 0)));
        entry.ToString().Should().NotContain(":");
    }

    [Fact]
    public void VersionEntry_TimestampIsSetOnCreation()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var history = PromptVersionHistory<IPrompt>.Create("SupportBot");
        history.Add(PromptVersion.Create(1, 0, 0), MakePrompt("v1"));
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var entry = Must.Be(history.Get(PromptVersion.Create(1, 0, 0)));
        entry.Timestamp.Should().BeAfter(before).And.BeBefore(after);
    }
}
