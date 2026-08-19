namespace Mima.AI.Prompt.Models;

/// <summary>
/// Defines structured output format constraints for prompts.
/// Use with system messages to enforce specific response formats.
/// </summary>
/// <remarks>
/// Structured output ensures the AI responds in a predictable format
/// that can be parsed programmatically (JSON, YAML, Markdown, etc.).
/// </remarks>
public sealed class OutputFormat
{
    /// <summary>Gets the format type (e.g., "json", "yaml", "markdown").</summary>
    public string Type { get; }

    /// <summary>Gets the schema or example shape for structured output (if applicable).</summary>
    public string? Schema { get; }

    /// <summary>Gets the format instructions to include in the prompt.</summary>
    public string Instructions { get; }

    private OutputFormat(string type, string instructions, string? schema = null)
    {
        Type = type;
        Instructions = instructions;
        Schema = schema;
    }

    /// <summary>Requires JSON output.</summary>
    public static OutputFormat Json(string? schema = null) => new(
        "json",
        "Respond with valid JSON only. Do not include markdown code fences or any other text.",
        schema);

    /// <summary>Requires YAML output.</summary>
    public static OutputFormat Yaml(string? schema = null) => new(
        "yaml",
        "Respond with valid YAML only. Do not include markdown code fences or any other text.",
        schema);

    /// <summary>Requires Markdown output.</summary>
    public static OutputFormat Markdown() => new(
        "markdown",
        "Respond using well-formatted Markdown with proper headers, code blocks, and lists.");

    /// <summary>Requires plain text output.</summary>
    public static OutputFormat PlainText() => new(
        "plaintext",
        "Respond with plain text only. No formatting, no markdown, no code blocks.");

    /// <summary>Requires structured bullet-point output.</summary>
    public static OutputFormat BulletPoints() => new(
        "bullets",
        "Respond using bullet points. Each point should be concise and actionable.");

    /// <summary>Requires numbered step-by-step output.</summary>
    public static OutputFormat Steps() => new(
        "steps",
        "Respond with numbered steps. Each step should be clear and self-contained.");

    /// <summary>Requires a specific JSON shape.</summary>
    /// <param name="schema">JSON schema or example shape.</param>
    public static OutputFormat JsonWithSchema(string schema) => new(
        "json",
        $"Respond with valid JSON only matching this schema:\n{schema}",
        schema);

    /// <summary>Requires a specific YAML shape.</summary>
    /// <param name="schema">YAML schema or example shape.</param>
    public static OutputFormat YamlWithSchema(string schema) => new(
        "yaml",
        $"Respond with valid YAML only matching this schema:\n{schema}",
        schema);

    /// <summary>Custom format with provided instructions.</summary>
    /// <param name="type">Format type name.</param>
    /// <param name="instructions">Format instructions for the AI.</param>
    /// <param name="schema">Optional schema or example shape.</param>
    public static OutputFormat Custom(string type, string instructions, string? schema = null) =>
        new(type, instructions, schema);
}
