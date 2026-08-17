using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Versioning;

/// <summary>
/// A versioned prompt asset that tracks version history and supports migration.
/// Wraps any prompt, template, or message with semantic versioning metadata.
/// </summary>
/// <remarks>
/// <para>
/// Versioning prompts is essential in production AI systems because:
/// - Prompt changes can dramatically alter AI behavior
/// - Teams need to rollback to known-good prompt versions
/// - A/B testing requires running different prompt versions simultaneously
/// - Audit trails require knowing which prompt version produced a given output
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var asset = VersionedPromptAsset.Create("CustomerSupportPrompt", PromptVersion.Create(1, 0, 0))
///     .WithDescription("Main customer support prompt")
///     .WithAuthor("Johnson Olusegun")
///     .WithTags("support", "customer", "v1");
/// 
/// // Bump version
/// var v2 = asset.BumpMinor("Added tone variable");
/// </code>
/// </example>
/// <typeparam name="T">The type of the prompt asset (IPrompt, IMessage, or IMessageTemplate).</typeparam>
public sealed class VersionedPromptAsset<T> where T : class
{
    /// <summary>Gets the unique identifier for this asset.</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable name of this asset.</summary>
    public string Name { get; }

    /// <summary>Gets the current semantic version.</summary>
    public PromptVersion Version { get; }

    /// <summary>Gets the actual prompt/template/message content.</summary>
    public T Content { get; }

    /// <summary>Gets the description of this version.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the author who created or last modified this version.</summary>
    public string? Author { get; private set; }

    /// <summary>Gets tags for categorization and discovery.</summary>
    public IReadOnlyList<string> Tags { get; private set; }

    /// <summary>Gets when this version was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the change log entry for this version.</summary>
    public string? ChangeLog { get; private set; }

    /// <summary>Gets whether this version is deprecated.</summary>
    public bool IsDeprecated { get; private set; }

    /// <summary>Gets the deprecation message if deprecated.</summary>
    public string? DeprecationMessage { get; private set; }

    private VersionedPromptAsset(string name, PromptVersion version, T content, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name cannot be null or empty.", nameof(name));

        Id = id ?? Guid.NewGuid().ToString("N");
        Name = name;
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Tags = Array.Empty<string>();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Creates a new versioned prompt asset.</summary>
    public static VersionedPromptAsset<T> Create(string name, PromptVersion version, T content) =>
        new(name, version, content);

    /// <summary>Sets the description.</summary>
    public VersionedPromptAsset<T> WithDescription(string description)
    {
        Description = description;
        return this;
    }

    /// <summary>Sets the author.</summary>
    public VersionedPromptAsset<T> WithAuthor(string author)
    {
        Author = author;
        return this;
    }

    /// <summary>Sets the tags.</summary>
    public VersionedPromptAsset<T> WithTags(params string[] tags)
    {
        Tags = tags.ToList().AsReadOnly();
        return this;
    }

    /// <summary>Sets the changelog entry.</summary>
    public VersionedPromptAsset<T> WithChangeLog(string changeLog)
    {
        ChangeLog = changeLog;
        return this;
    }

    /// <summary>Marks this version as deprecated.</summary>
    public VersionedPromptAsset<T> Deprecate(string reason)
    {
        IsDeprecated = true;
        DeprecationMessage = reason;
        return this;
    }

    /// <summary>Creates a new version with bumped major (breaking change).</summary>
    public VersionedPromptAsset<T> BumpMajor(T newContent, string? changeLog = null) =>
        new VersionedPromptAsset<T>(Name, Version.NextMajor(), newContent, Id)
            .WithChangeLog(changeLog ?? "Breaking change");

    /// <summary>Creates a new version with bumped minor (new feature).</summary>
    public VersionedPromptAsset<T> BumpMinor(T newContent, string? changeLog = null) =>
        new VersionedPromptAsset<T>(Name, Version.NextMinor(), newContent, Id)
            .WithChangeLog(changeLog ?? "New feature");

    /// <summary>Creates a new version with bumped patch (fix).</summary>
    public VersionedPromptAsset<T> BumpPatch(T newContent, string? changeLog = null) =>
        new VersionedPromptAsset<T>(Name, Version.NextPatch(), newContent, Id)
            .WithChangeLog(changeLog ?? "Bug fix");

    /// <inheritdoc />
    public override string ToString() => $"{Name} v{Version}{(IsDeprecated ? " [DEPRECATED]" : "")}";
}
