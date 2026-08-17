namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// Represents a semantic version for prompts, messages, and templates.
/// Follows Semantic Versioning 2.0.0 (https://semver.org):
/// MAJOR.MINOR.PATCH[-prerelease][+buildmetadata]
/// </summary>
/// <remarks>
/// <para>
/// Versioning prompts is critical for production AI systems:
/// - MAJOR: Breaking changes to prompt behavior or output format
/// - MINOR: New capabilities added (new variables, sections) without breaking existing behavior
/// - PATCH: Bug fixes, wording improvements that don't change behavior
/// </para>
/// <para>
/// Examples:
/// - "1.0.0" → initial release
/// - "1.1.0" → added new optional variable
/// - "2.0.0" → changed output format from Markdown to JSON
/// - "1.0.1-beta" → pre-release test version
/// - "1.0.0+exp.sha.5114f85" → build metadata for traceability
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var v1 = PromptVersion.Create(1, 0, 0);
/// var v2 = PromptVersion.Parse("2.1.0-beta+build.123");
/// 
/// if (v2 > v1) { /* migrate prompt */ }
/// 
/// var template = SystemTemplate.Create("You are helpful.")
///     .WithVersion(PromptVersion.Create(1, 0, 0));
/// </code>
/// </example>
public sealed class PromptVersion : IComparable<PromptVersion>, IEquatable<PromptVersion>
{
    /// <summary>Gets the major version number. Incremented for breaking changes.</summary>
    public int Major { get; }

    /// <summary>Gets the minor version number. Incremented for backward-compatible additions.</summary>
    public int Minor { get; }

    /// <summary>Gets the patch version number. Incremented for backward-compatible fixes.</summary>
    public int Patch { get; }

    /// <summary>Gets the pre-release label (e.g., "alpha", "beta", "rc.1"). Null if stable.</summary>
    public string? PreRelease { get; }

    /// <summary>Gets the build metadata (e.g., "build.123", "sha.5114f85"). Does not affect precedence.</summary>
    public string? BuildMetadata { get; }

    /// <summary>Gets whether this is a pre-release version.</summary>
    public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

    /// <summary>Gets whether this is a stable (non-pre-release) version.</summary>
    public bool IsStable => !IsPreRelease;

    /// <summary>
    /// Creates a new semantic version.
    /// </summary>
    /// <param name="major">Major version (breaking changes).</param>
    /// <param name="minor">Minor version (backward-compatible additions).</param>
    /// <param name="patch">Patch version (backward-compatible fixes).</param>
    /// <param name="preRelease">Optional pre-release label.</param>
    /// <param name="buildMetadata">Optional build metadata.</param>
    public PromptVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), "Major version must be non-negative.");
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), "Minor version must be non-negative.");
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), "Patch version must be non-negative.");

        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = string.IsNullOrWhiteSpace(preRelease) ? null : preRelease!.Trim();
        BuildMetadata = string.IsNullOrWhiteSpace(buildMetadata) ? null : buildMetadata!.Trim();
    }

    /// <summary>Factory method to create a version.</summary>
    public static PromptVersion Create(int major, int minor = 0, int patch = 0, string? preRelease = null, string? buildMetadata = null) =>
        new(major, minor, patch, preRelease, buildMetadata);

    /// <summary>The initial version (1.0.0).</summary>
    public static PromptVersion Initial => new(1, 0, 0);

    /// <summary>
    /// Parses a semantic version string.
    /// Supports formats: "1.0.0", "1.2.3-beta", "1.0.0-rc.1+build.456"
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>A parsed PromptVersion instance.</returns>
    /// <exception cref="FormatException">Thrown when the version string is invalid.</exception>
    public static PromptVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version string cannot be null or empty.", nameof(version));

        var input = version.Trim();
        string? buildMetadata = null;
        string? preRelease = null;

        // Extract build metadata (+...)
        var buildIndex = input.IndexOf('+');
        if (buildIndex >= 0)
        {
            buildMetadata = input[(buildIndex + 1)..];
            input = input[..buildIndex];
        }

        // Extract pre-release (-...)
        var preReleaseIndex = input.IndexOf('-');
        if (preReleaseIndex >= 0)
        {
            preRelease = input[(preReleaseIndex + 1)..];
            input = input[..preReleaseIndex];
        }

        // Parse major.minor.patch
        var parts = input.Split('.');
        if (parts.Length < 1 || parts.Length > 3)
            throw new FormatException($"Invalid version format: '{version}'. Expected MAJOR.MINOR.PATCH.");

        if (!int.TryParse(parts[0], out var major))
            throw new FormatException($"Invalid major version in '{version}'.");

        var minor = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;

        return new PromptVersion(major, minor, patch, preRelease, buildMetadata);
    }

    /// <summary>Attempts to parse a version string without throwing.</summary>
    public static bool TryParse(string? version, out PromptVersion? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(version)) return false;

        try
        {
            result = Parse(version!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns the next major version (breaking change).</summary>
    public PromptVersion NextMajor() => new(Major + 1, 0, 0);

    /// <summary>Returns the next minor version (new feature).</summary>
    public PromptVersion NextMinor() => new(Major, Minor + 1, 0);

    /// <summary>Returns the next patch version (bug fix).</summary>
    public PromptVersion NextPatch() => new(Major, Minor, Patch + 1);

    /// <summary>Returns a pre-release version with the given label.</summary>
    public PromptVersion WithPreRelease(string label) => new(Major, Minor, Patch, label, BuildMetadata);

    /// <summary>Returns a version with build metadata attached.</summary>
    public PromptVersion WithBuildMetadata(string metadata) => new(Major, Minor, Patch, PreRelease, metadata);

    /// <summary>Returns the stable version (strips pre-release and build metadata).</summary>
    public PromptVersion ToStable() => new(Major, Minor, Patch);

    /// <summary>
    /// Checks if this version is compatible with another version (same major, >= minor).
    /// </summary>
    /// <param name="other">The version to check compatibility with.</param>
    /// <returns>True if backward-compatible.</returns>
    public bool IsCompatibleWith(PromptVersion other) =>
        other is not null && Major == other.Major && (Minor > other.Minor || (Minor == other.Minor && Patch >= other.Patch));

    /// <inheritdoc />
    public int CompareTo(PromptVersion? other)
    {
        if (other is null) return 1;

        var majorCmp = Major.CompareTo(other.Major);
        if (majorCmp != 0) return majorCmp;

        var minorCmp = Minor.CompareTo(other.Minor);
        if (minorCmp != 0) return minorCmp;

        var patchCmp = Patch.CompareTo(other.Patch);
        if (patchCmp != 0) return patchCmp;

        // Pre-release versions have lower precedence than stable
        if (IsPreRelease && !other.IsPreRelease) return -1;
        if (!IsPreRelease && other.IsPreRelease) return 1;
        if (IsPreRelease && other.IsPreRelease)
            return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);

        return 0;
    }

    /// <inheritdoc />
    public bool Equals(PromptVersion? other) =>
        other is not null && Major == other.Major && Minor == other.Minor && Patch == other.Patch && PreRelease == other.PreRelease;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PromptVersion v && Equals(v);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    /// <summary>Returns the full semantic version string.</summary>
    public override string ToString()
    {
        var result = $"{Major}.{Minor}.{Patch}";
        if (PreRelease is not null) result += $"-{PreRelease}";
        if (BuildMetadata is not null) result += $"+{BuildMetadata}";
        return result;
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(PromptVersion? left, PromptVersion? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(PromptVersion? left, PromptVersion? right) => !(left == right);
    /// <summary>Less-than operator based on version precedence.</summary>
    public static bool operator <(PromptVersion left, PromptVersion right) => left.CompareTo(right) < 0;
    /// <summary>Greater-than operator based on version precedence.</summary>
    public static bool operator >(PromptVersion left, PromptVersion right) => left.CompareTo(right) > 0;
    /// <summary>Less-than-or-equal operator based on version precedence.</summary>
    public static bool operator <=(PromptVersion left, PromptVersion right) => left.CompareTo(right) <= 0;
    /// <summary>Greater-than-or-equal operator based on version precedence.</summary>
    public static bool operator >=(PromptVersion left, PromptVersion right) => left.CompareTo(right) >= 0;
}
