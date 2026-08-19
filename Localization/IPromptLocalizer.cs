using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Localization;

/// <summary>
/// Interface for localizing prompts and templates to different languages/locales.
/// Enables multi-language prompt catalogs for international applications.
/// </summary>
public interface IPromptLocalizer
{
    /// <summary>Gets the supported locales.</summary>
    IReadOnlyList<string> SupportedLocales { get; }

    /// <summary>Gets the default locale.</summary>
    string DefaultLocale { get; }

    /// <summary>Localizes a template to the specified locale.</summary>
    IMessageTemplate Localize(IMessageTemplate template, string locale);

    /// <summary>Localizes a prompt (all its messages) to the specified locale.</summary>
    IPrompt Localize(IPrompt prompt, string locale);

    /// <summary>Checks if a specific locale is supported.</summary>
    bool IsLocaleSupported(string locale);
}
