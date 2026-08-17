using System.Text.Json.Serialization;

namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// Immutable metadata associated with a message, template, or prompt.
/// Provides context for documentation, versioning, categorization, and discoverability.
/// </summary>
/// <remarks>
/// <para>
/// Metadata turns prompts from throw-away strings into managed software assets.
/// Version tracking, authorship, and categorization enable teams to maintain 
/// prompt libraries at scale.
/// </para>
/// </remarks>
public sealed class MessageMetadata
{
    /// <summary>Gets the unique name for this message or template.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; }

    /// <summary>Gets the human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; }

    /// <summary>Gets the category (e.g., "coding", "writing", "analysis").</summary>
    [JsonPropertyName("category")]
    public string? Category { get; }

    /// <summary>Gets the semantic version string (e.g., "1.0.0", "2.1.0-beta").</summary>
    [JsonPropertyName("version")]
    public string? Version { get; }

    /// <summary>Gets the parsed semantic version. Null if version string is not set or invalid.</summary>
    [JsonIgnore]
    public PromptVersion? SemanticVersion => PromptVersion.TryParse(Version, out var v) ? v : null;

    /// <summary>Gets the author name.</summary>
    [JsonPropertyName("author")]
    public string? Author { get; }

    /// <summary>Gets the collection of tags for discoverability.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets the language this message targets (e.g., "en", "fr").</summary>
    [JsonPropertyName("language")]
    public string? Language { get; }

    /// <summary>Gets the provider this message is optimized for (null means provider-agnostic).</summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; }

    /// <summary>Gets the model this message is optimized for (null means model-agnostic).</summary>
    [JsonPropertyName("model")]
    public string? Model { get; }

    /// <summary>Gets the UTC timestamp when this metadata was created.</summary>
    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; }

    /// <summary>Gets the UTC timestamp when this metadata was last modified.</summary>
    [JsonPropertyName("modified")]
    public DateTimeOffset Modified { get; }

    /// <summary>
    /// Gets the minimum compatible version required by consumers. 
    /// Useful for ensuring prompt consumers can handle this version's features.
    /// </summary>
    [JsonPropertyName("minCompatibleVersion")]
    public string? MinCompatibleVersion { get; }

    /// <summary>
    /// Creates a new <see cref="MessageMetadata"/> instance.
    /// </summary>
    public MessageMetadata(
        string? name = null,
        string? description = null,
        string? category = null,
        string? version = null,
        string? author = null,
        IEnumerable<string>? tags = null,
        string? language = null,
        string? provider = null,
        string? model = null,
        DateTimeOffset? created = null,
        DateTimeOffset? modified = null,
        string? minCompatibleVersion = null)
    {
        Name = name;
        Description = description;
        Category = category;
        Version = version;
        Author = author;
        Tags = tags?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        Language = language;
        Provider = provider;
        Model = model;
        Created = created ?? DateTimeOffset.UtcNow;
        Modified = modified ?? DateTimeOffset.UtcNow;
        MinCompatibleVersion = minCompatibleVersion;
    }

    /// <summary>Creates a new metadata with only a name.</summary>
    public static MessageMetadata WithName(string name) => new(name: name);

    /// <summary>Creates empty metadata.</summary>
    public static MessageMetadata Empty => new();

    /// <summary>Returns a copy with the specified name.</summary>
    public MessageMetadata SetName(string name) =>
        new(name, Description, Category, Version, Author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified description.</summary>
    public MessageMetadata SetDescription(string description) =>
        new(Name, description, Category, Version, Author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified semantic version.</summary>
    public MessageMetadata SetVersion(string version) =>
        new(Name, Description, Category, version, Author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified semantic version object.</summary>
    public MessageMetadata SetVersion(PromptVersion version) =>
        SetVersion(version?.ToString() ?? throw new ArgumentNullException(nameof(version)));

    /// <summary>Returns a copy with the specified tags.</summary>
    public MessageMetadata SetTags(IEnumerable<string> tags) =>
        new(Name, Description, Category, Version, Author, tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with an additional tag.</summary>
    public MessageMetadata AddTag(string tag) =>
        SetTags(Tags.Append(tag));

    /// <summary>Returns a copy with the specified author.</summary>
    public MessageMetadata SetAuthor(string author) =>
        new(Name, Description, Category, Version, author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified category.</summary>
    public MessageMetadata SetCategory(string category) =>
        new(Name, Description, category, Version, Author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified language.</summary>
    public MessageMetadata SetLanguage(string language) =>
        new(Name, Description, Category, Version, Author, Tags, language, Provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified provider.</summary>
    public MessageMetadata SetProvider(string provider) =>
        new(Name, Description, Category, Version, Author, Tags, Language, provider, Model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the specified model.</summary>
    public MessageMetadata SetModel(string model) =>
        new(Name, Description, Category, Version, Author, Tags, Language, Provider, model, Created, DateTimeOffset.UtcNow, MinCompatibleVersion);

    /// <summary>Returns a copy with the minimum compatible version set.</summary>
    public MessageMetadata SetMinCompatibleVersion(string minVersion) =>
        new(Name, Description, Category, Version, Author, Tags, Language, Provider, Model, Created, DateTimeOffset.UtcNow, minVersion);

    /// <summary>Returns a copy with the minimum compatible version set from a PromptVersion.</summary>
    public MessageMetadata SetMinCompatibleVersion(PromptVersion minVersion) =>
        SetMinCompatibleVersion(minVersion?.ToString() ?? throw new ArgumentNullException(nameof(minVersion)));
}
