using FluentAssertions;
using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Tests;

public class PromptVersionTests
{
    [Fact]
    public void Create_SetsComponents()
    {
        var version = PromptVersion.Create(1, 2, 3);
        version.Major.Should().Be(1);
        version.Minor.Should().Be(2);
        version.Patch.Should().Be(3);
    }

    [Fact]
    public void Create_DefaultsMinorAndPatchToZero()
    {
        var version = PromptVersion.Create(1);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
    }

    [Fact]
    public void Initial_IsOneZeroZero()
    {
        var version = PromptVersion.Initial;
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
        version.IsStable.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Ctor_NegativeComponent_Throws(int major, int minor, int patch)
    {
        var act = () => new PromptVersion(major, minor, patch);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Parse_FullVersion_ParsesAllComponents()
    {
        var version = PromptVersion.Parse("1.2.3");
        version.Major.Should().Be(1);
        version.Minor.Should().Be(2);
        version.Patch.Should().Be(3);
        version.PreRelease.Should().BeNull();
        version.BuildMetadata.Should().BeNull();
    }

    [Fact]
    public void Parse_MajorOnly_DefaultsMinorAndPatch()
    {
        var version = PromptVersion.Parse("5");
        version.Major.Should().Be(5);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
    }

    [Fact]
    public void Parse_MajorMinor_DefaultsPatch()
    {
        var version = PromptVersion.Parse("2.3");
        version.Major.Should().Be(2);
        version.Minor.Should().Be(3);
        version.Patch.Should().Be(0);
    }

    [Fact]
    public void Parse_WithPreRelease_ParsesPreRelease()
    {
        var version = PromptVersion.Parse("1.2.3-beta");
        version.PreRelease.Should().Be("beta");
        version.IsPreRelease.Should().BeTrue();
        version.IsStable.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithBuildMetadata_ParsesBuildMetadata()
    {
        var version = PromptVersion.Parse("1.0.0+build.123");
        version.BuildMetadata.Should().Be("build.123");
        version.PreRelease.Should().BeNull();
    }

    [Fact]
    public void Parse_WithPreReleaseAndBuildMetadata_ParsesBoth()
    {
        var version = PromptVersion.Parse("1.0.0-rc.1+build.456");
        version.PreRelease.Should().Be("rc.1");
        version.BuildMetadata.Should().Be("build.456");
        version.Major.Should().Be(1);
    }

    [Fact]
    public void Parse_TrimsWhitespace()
    {
        var version = PromptVersion.Parse("  1.0.0  ");
        version.Major.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrEmpty_Throws(string? input)
    {
        var act = () => PromptVersion.Parse(input!);
        act.Should().Throw<ArgumentException>().WithMessage("*Version string cannot be null or empty*");
    }

    [Fact]
    public void Parse_TooManyParts_Throws()
    {
        var act = () => PromptVersion.Parse("1.2.3.4");
        act.Should().Throw<FormatException>().WithMessage("*Invalid version format*");
    }

    [Fact]
    public void Parse_InvalidMajor_Throws()
    {
        var act = () => PromptVersion.Parse("abc.2.3");
        act.Should().Throw<FormatException>().WithMessage("*Invalid major version*");
    }

    [Fact]
    public void Parse_InvalidMinor_DefaultsToZero()
    {
        var version = PromptVersion.Parse("1.abc.3");
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(3);
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        var result = PromptVersion.TryParse("1.0.0", out var version);
        result.Should().BeTrue();
        version.Should().NotBeNull();
    }

    [Fact]
    public void TryParse_Invalid_ReturnsFalse()
    {
        var result = PromptVersion.TryParse("1.2.3.4", out var version);
        result.Should().BeFalse();
        version.Should().BeNull();
    }

    [Fact]
    public void TryParse_NullOrEmpty_ReturnsFalse()
    {
        PromptVersion.TryParse(null, out var v1).Should().BeFalse();
        PromptVersion.TryParse("", out var v2).Should().BeFalse();
        v1.Should().BeNull();
        v2.Should().BeNull();
    }

    [Fact]
    public void NextMajor_IncrementsMajorAndResetsOthers()
    {
        var version = PromptVersion.Create(1, 5, 3).NextMajor();
        version.Major.Should().Be(2);
        version.Minor.Should().Be(0);
        version.Patch.Should().Be(0);
    }

    [Fact]
    public void NextMinor_IncrementsMinorAndResetsPatch()
    {
        var version = PromptVersion.Create(1, 5, 3).NextMinor();
        version.Major.Should().Be(1);
        version.Minor.Should().Be(6);
        version.Patch.Should().Be(0);
    }

    [Fact]
    public void NextPatch_IncrementsPatchOnly()
    {
        var version = PromptVersion.Create(1, 5, 3).NextPatch();
        version.Major.Should().Be(1);
        version.Minor.Should().Be(5);
        version.Patch.Should().Be(4);
    }

    [Fact]
    public void WithPreRelease_SetsPreReleaseLabel()
    {
        var version = PromptVersion.Create(1, 0, 0).WithPreRelease("beta");
        version.PreRelease.Should().Be("beta");
        version.IsPreRelease.Should().BeTrue();
    }

    [Fact]
    public void WithBuildMetadata_SetsBuildMetadata()
    {
        var version = PromptVersion.Create(1, 0, 0).WithBuildMetadata("sha.123");
        version.BuildMetadata.Should().Be("sha.123");
    }

    [Fact]
    public void ToStable_StripsPreReleaseAndBuildMetadata()
    {
        var version = PromptVersion.Parse("1.2.3-beta+build.1").ToStable();
        version.PreRelease.Should().BeNull();
        version.BuildMetadata.Should().BeNull();
        version.ToString().Should().Be("1.2.3");
    }

    [Fact]
    public void IsCompatibleWith_SameMajorHigherMinor_ReturnsTrue()
    {
        var v1 = PromptVersion.Create(1, 2, 0);
        var v2 = PromptVersion.Create(1, 1, 0);
        v1.IsCompatibleWith(v2).Should().BeTrue();
    }

    [Fact]
    public void IsCompatibleWith_SameMajorMinorHigherPatch_ReturnsTrue()
    {
        var v1 = PromptVersion.Create(1, 1, 5);
        var v2 = PromptVersion.Create(1, 1, 2);
        v1.IsCompatibleWith(v2).Should().BeTrue();
    }

    [Fact]
    public void IsCompatibleWith_DifferentMajor_ReturnsFalse()
    {
        var v1 = PromptVersion.Create(2, 0, 0);
        var v2 = PromptVersion.Create(1, 0, 0);
        v1.IsCompatibleWith(v2).Should().BeFalse();
    }

    [Fact]
    public void IsCompatibleWith_LowerMinor_ReturnsFalse()
    {
        var v1 = PromptVersion.Create(1, 0, 0);
        var v2 = PromptVersion.Create(1, 1, 0);
        v1.IsCompatibleWith(v2).Should().BeFalse();
    }

    [Fact]
    public void IsCompatibleWith_Null_ReturnsFalse()
    {
        var v1 = PromptVersion.Create(1, 0, 0);
        v1.IsCompatibleWith(null!).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var version = PromptVersion.Create(1, 0, 0);
        version.CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void CompareTo_HigherMajor_ReturnsNegative()
    {
        PromptVersion.Create(1, 0, 0).CompareTo(PromptVersion.Create(2, 0, 0)).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_HigherMinor_ReturnsNegative()
    {
        PromptVersion.Create(1, 0, 0).CompareTo(PromptVersion.Create(1, 1, 0)).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_HigherPatch_ReturnsNegative()
    {
        PromptVersion.Create(1, 0, 0).CompareTo(PromptVersion.Create(1, 0, 1)).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_PreReleaseVsStable_PreReleaseIsLower()
    {
        var stable = PromptVersion.Create(1, 0, 0);
        var preRelease = PromptVersion.Create(1, 0, 0, "beta");

        preRelease.CompareTo(stable).Should().BeNegative();
        stable.CompareTo(preRelease).Should().BePositive();
    }

    [Fact]
    public void CompareTo_BothPreRelease_ComparesLabelsOrdinally()
    {
        var alpha = PromptVersion.Create(1, 0, 0, "alpha");
        var beta = PromptVersion.Create(1, 0, 0, "beta");

        alpha.CompareTo(beta).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_Equal_ReturnsZero()
    {
        var v1 = PromptVersion.Create(1, 2, 3);
        var v2 = PromptVersion.Create(1, 2, 3);
        v1.CompareTo(v2).Should().Be(0);
    }

    [Fact]
    public void Equals_SameMajorMinorPatchAndPreRelease_ReturnsTrue()
    {
        var v1 = PromptVersion.Create(1, 0, 0, "beta");
        var v2 = PromptVersion.Create(1, 0, 0, "beta");
        v1.Equals(v2).Should().BeTrue();
        v1.Equals((object)v2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentPreRelease_ReturnsFalse()
    {
        var v1 = PromptVersion.Create(1, 0, 0, "beta");
        var v2 = PromptVersion.Create(1, 0, 0, "alpha");
        v1.Equals(v2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var v1 = PromptVersion.Create(1, 0, 0);
        PromptVersion? nullVersion = null;
        object? nullObject = null;
        object notAVersion = "1.0.0";

        v1.Equals(nullVersion).Should().BeFalse();
        v1.Equals(nullObject).Should().BeFalse();
        v1.Equals(notAVersion).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EqualVersions_HaveSameHashCode()
    {
        var v1 = PromptVersion.Create(1, 2, 3, "beta");
        var v2 = PromptVersion.Create(1, 2, 3, "beta");
        v1.GetHashCode().Should().Be(v2.GetHashCode());
    }

    [Fact]
    public void ToString_FullVersion_FormatsCorrectly()
    {
        var version = new PromptVersion(1, 2, 3, "beta", "build.1");
        version.ToString().Should().Be("1.2.3-beta+build.1");
    }

    [Fact]
    public void ToString_SimpleVersion_FormatsCorrectly()
    {
        PromptVersion.Create(1, 0, 0).ToString().Should().Be("1.0.0");
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        var v1 = PromptVersion.Create(1, 0, 0);
        var v2 = PromptVersion.Create(1, 0, 0);
        var v3 = PromptVersion.Create(2, 0, 0);

        (v1 == v2).Should().BeTrue();
        (v1 != v3).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        PromptVersion? a = null;
        PromptVersion? b = null;
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        PromptVersion? a = PromptVersion.Create(1, 0, 0);
        PromptVersion? b = null;
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void ComparisonOperators_Work()
    {
        var lower = PromptVersion.Create(1, 0, 0);
        var lowerCopy = PromptVersion.Create(1, 0, 0);
        var higher = PromptVersion.Create(2, 0, 0);
        var higherCopy = PromptVersion.Create(2, 0, 0);

        (lower < higher).Should().BeTrue();
        (higher > lower).Should().BeTrue();
        (lower <= lowerCopy).Should().BeTrue();
        (higher >= higherCopy).Should().BeTrue();
        (lower <= higher).Should().BeTrue();
        (higher >= lower).Should().BeTrue();
    }
}
