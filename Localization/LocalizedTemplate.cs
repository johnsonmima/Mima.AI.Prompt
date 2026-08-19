using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Templates;

namespace Mima.AI.Prompt.Localization;

/// <summary>
/// A template that holds localized versions of its content for multiple locales.
/// Renders in the requested locale, falling back to the default if unavailable.
/// </summary>
/// <example>
/// <code>
/// var template = LocalizedTemplate.Create(MessageRole.System)
///     .AddLocale("en", "You are a helpful assistant.")
///     .AddLocale("fr", "Vous etes un assistant utile.")
///     .AddLocale("es", "Eres un asistente util.")
///     .WithDefault("en");
///
/// var msg = template.Render("fr", new Dictionary&lt;string, object&gt;()); // French version
/// </code>
/// </example>
public sealed class LocalizedTemplate
{
    private readonly Dictionary<string, string> _localizedContent = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultLocale = "en";

    /// <summary>Gets the role this template produces.</summary>
    public MessageRole Role { get; }

    /// <summary>Gets the metadata.</summary>
    public MessageMetadata Metadata { get; private set; }

    /// <summary>Gets all available locales.</summary>
    public IReadOnlyList<string> AvailableLocales => _localizedContent.Keys.ToList().AsReadOnly();

    /// <summary>Gets the default locale.</summary>
    public string DefaultLocale => _defaultLocale;

    private LocalizedTemplate(MessageRole role, MessageMetadata? metadata = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Metadata = metadata ?? MessageMetadata.Empty;
    }

    /// <summary>Creates a new localized template for the given role.</summary>
    public static LocalizedTemplate Create(MessageRole role, MessageMetadata? metadata = null) =>
        new(role, metadata);

    /// <summary>Adds or updates content for a locale.</summary>
    public LocalizedTemplate AddLocale(string locale, string content)
    {
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale cannot be empty.", nameof(locale));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content cannot be empty.", nameof(content));
        _localizedContent[locale.Trim()] = content;
        return this;
    }

    /// <summary>Sets the default fallback locale.</summary>
    public LocalizedTemplate WithDefault(string locale)
    {
        _defaultLocale = locale;
        return this;
    }

    /// <summary>Sets the metadata.</summary>
    public LocalizedTemplate WithMetadata(MessageMetadata metadata)
    {
        Metadata = metadata;
        return this;
    }

    /// <summary>Gets the content for the specified locale, falling back to default.</summary>
    public string GetContent(string locale)
    {
        if (_localizedContent.TryGetValue(locale, out var content))
            return content;
        if (_localizedContent.TryGetValue(_defaultLocale, out var fallback))
            return fallback;
        return _localizedContent.Values.FirstOrDefault()
            ?? throw new InvalidOperationException("No localized content has been added.");
    }

    /// <summary>Renders the template for the specified locale with variables.</summary>
    public IMessage Render(string locale, IDictionary<string, object> variables)
    {
        string content = GetContent(locale);
        MessageTemplate template = Role == MessageRole.System
            ? SystemTemplate.Create(content, Metadata)
            : Role == MessageRole.User
                ? UserTemplate.Create(content, Metadata)
                : Role == MessageRole.Assistant
                    ? AssistantTemplate.Create(content, Metadata)
                    : Role == MessageRole.Developer
                        ? DeveloperTemplate.Create(content, Metadata)
                        : CustomTemplate.Create(Role, content, Metadata);
        return template.Render(variables);
    }

    /// <summary>Checks if a locale has content.</summary>
    public bool HasLocale(string locale) =>
        _localizedContent.ContainsKey(locale);
}
