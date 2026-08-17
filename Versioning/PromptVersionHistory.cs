using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Versioning;

/// <summary>
/// Tracks version history for a prompt asset, enabling rollback and audit trails.
/// </summary>
/// <remarks>
/// <para>
/// Version history is crucial for:
/// - Auditing: know which prompt version produced a given AI output
/// - Rollback: revert to a previous version when a new one underperforms
/// - A/B testing: compare multiple versions side-by-side
/// - Migration: gradually roll out prompt changes
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var history = PromptVersionHistory&lt;IPrompt&gt;.Create("SupportBot");
///
/// history.Add(PromptVersion.Create(1, 0, 0), promptV1, "Initial release");
/// history.Add(PromptVersion.Create(1, 1, 0), promptV2, "Added tone parameter");
/// history.Add(PromptVersion.Create(2, 0, 0), promptV3, "Changed to JSON output");
///
/// var latest = history.Latest;       // v2.0.0
/// var stable = history.LatestStable;  // v2.0.0
/// var v1 = history.Get(PromptVersion.Create(1, 0, 0)); // rollback reference
/// </code>
/// </example>
/// <typeparam name="T">The type of content being versioned.</typeparam>
public sealed class PromptVersionHistory<T> where T : class
{
    private readonly SortedList<PromptVersion, VersionEntry<T>> _versions = new();

    /// <summary>Gets the asset name this history tracks.</summary>
    public string AssetName { get; }

    /// <summary>Gets all version entries in ascending order.</summary>
    public IReadOnlyList<VersionEntry<T>> All => _versions.Values.ToList().AsReadOnly();

    /// <summary>Gets the total number of versions.</summary>
    public int Count => _versions.Count;

    /// <summary>Gets the latest (highest) version.</summary>
    // Index from-end (^1) requires PolySharp on netstandard2.0; prefer explicit index
    public VersionEntry<T>? Latest => _versions.Count > 0 ? _versions.Values[_versions.Count - 1] : null;

    /// <summary>Gets the latest stable (non-pre-release) version.</summary>
    public VersionEntry<T>? LatestStable => _versions.Values
        .LastOrDefault(v => v.Version.IsStable);

    private PromptVersionHistory(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            throw new ArgumentException("Asset name cannot be null or empty.", nameof(assetName));
        AssetName = assetName;
    }

    /// <summary>Creates a new version history tracker.</summary>
    public static PromptVersionHistory<T> Create(string assetName) => new(assetName);

    /// <summary>Adds a version to the history.</summary>
    /// <param name="version">The semantic version.</param>
    /// <param name="content">The content at this version.</param>
    /// <param name="changeLog">Description of what changed.</param>
    /// <param name="author">Who made the change.</param>
    /// <returns>This history for chaining.</returns>
    public PromptVersionHistory<T> Add(PromptVersion version, T content, string? changeLog = null, string? author = null)
    {
        if (version is null) throw new ArgumentNullException(nameof(version));
        if (content is null) throw new ArgumentNullException(nameof(content));

        if (_versions.ContainsKey(version))
            throw new InvalidOperationException($"Version {version} already exists in history for '{AssetName}'.");

        _versions[version] = new VersionEntry<T>(version, content, changeLog, author);
        return this;
    }

    /// <summary>Gets a specific version by its version number.</summary>
    /// <param name="version">The version to retrieve.</param>
    /// <returns>The version entry, or null if not found.</returns>
    public VersionEntry<T>? Get(PromptVersion version) =>
        _versions.TryGetValue(version, out var entry) ? entry : null;

    /// <summary>Gets the content at a specific version.</summary>
    public T? GetContent(PromptVersion version) => Get(version)?.Content;

    /// <summary>Checks if a specific version exists.</summary>
    public bool Contains(PromptVersion version) => _versions.ContainsKey(version);

    /// <summary>Gets all versions between two version numbers (inclusive).</summary>
    public IReadOnlyList<VersionEntry<T>> GetRange(PromptVersion from, PromptVersion to) =>
        _versions.Where(kv => kv.Key >= from && kv.Key <= to)
            .Select(kv => kv.Value)
            .ToList()
            .AsReadOnly();

    /// <summary>Gets the changelog between two versions.</summary>
    public IReadOnlyList<string> GetChangeLog(PromptVersion from, PromptVersion to) =>
        GetRange(from, to)
            .Where(e => e.ChangeLog is not null)
            .Select(e => $"v{e.Version}: {e.ChangeLog}")
            .ToList()
            .AsReadOnly();
}

/// <summary>
/// A single entry in a version history, containing the version, content, and metadata.
/// </summary>
/// <typeparam name="T">The type of content.</typeparam>
public sealed class VersionEntry<T> where T : class
{
    /// <summary>Gets the semantic version of this entry.</summary>
    public PromptVersion Version { get; }

    /// <summary>Gets the content at this version.</summary>
    public T Content { get; }

    /// <summary>Gets what changed in this version.</summary>
    public string? ChangeLog { get; }

    /// <summary>Gets who made this change.</summary>
    public string? Author { get; }

    /// <summary>Gets when this version was recorded.</summary>
    public DateTimeOffset Timestamp { get; }

    internal VersionEntry(PromptVersion version, T content, string? changeLog, string? author)
    {
        Version = version;
        Content = content;
        ChangeLog = changeLog;
        Author = author;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public override string ToString() => $"v{Version} ({Timestamp:yyyy-MM-dd}){(ChangeLog is not null ? $": {ChangeLog}" : "")}";
}
