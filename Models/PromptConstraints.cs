namespace SaaFarr.AI.Prompt.Models;

/// <summary>
/// Defines constraints and negative instructions for a prompt.
/// Constraints tell the AI what NOT to do and set boundaries.
/// </summary>
/// <remarks>
/// Constraints cover: length limits, tone restrictions, format requirements,
/// scope limitations, and explicit prohibitions.
/// </remarks>
/// <example>
/// <code>
/// var constraints = PromptConstraints.Create()
///     .MaxWords(200)
///     .NoEmojis()
///     .NoTables()
///     .MustInclude("Conclusion")
///     .MustAvoid("jargon");
/// </code>
/// </example>
public sealed class PromptConstraints
{
    private readonly List<string> _positiveConstraints = new();
    private readonly List<string> _negativeConstraints = new();

    /// <summary>Gets the maximum number of words allowed (null = no limit).</summary>
    public int? MaxWordCount { get; private set; }

    /// <summary>Gets the minimum number of words required (null = no minimum).</summary>
    public int? MinWordCount { get; private set; }

    /// <summary>Gets the required output format, if set.</summary>
    public OutputFormat? Format { get; private set; }

    /// <summary>Gets positive constraints (things the AI MUST do).</summary>
    public IReadOnlyList<string> MustDo => _positiveConstraints.AsReadOnly();

    /// <summary>Gets negative constraints (things the AI must NOT do).</summary>
    public IReadOnlyList<string> MustNot => _negativeConstraints.AsReadOnly();

    private PromptConstraints() { }

    /// <summary>Creates a new empty constraints builder.</summary>
    public static PromptConstraints Create() => new();

    /// <summary>Sets a maximum word count.</summary>
    public PromptConstraints MaxWords(int count) { MaxWordCount = count; return this; }

    /// <summary>Sets a minimum word count.</summary>
    public PromptConstraints MinWords(int count) { MinWordCount = count; return this; }

    /// <summary>Prohibits emojis in output.</summary>
    public PromptConstraints NoEmojis() { _negativeConstraints.Add("Do not use emojis."); return this; }

    /// <summary>Prohibits tables in output.</summary>
    public PromptConstraints NoTables() { _negativeConstraints.Add("Do not use tables."); return this; }

    /// <summary>Prohibits code blocks in output.</summary>
    public PromptConstraints NoCode() { _negativeConstraints.Add("Do not include code blocks."); return this; }

    /// <summary>Prohibits links in output.</summary>
    public PromptConstraints NoLinks() { _negativeConstraints.Add("Do not include URLs or links."); return this; }

    /// <summary>Adds a specific thing the AI must include.</summary>
    public PromptConstraints MustInclude(string requirement) { _positiveConstraints.Add(requirement); return this; }

    /// <summary>Adds a specific thing the AI must avoid.</summary>
    public PromptConstraints MustAvoid(string prohibition) { _negativeConstraints.Add($"Avoid: {prohibition}"); return this; }

    /// <summary>Sets the required output format.</summary>
    public PromptConstraints WithFormat(OutputFormat format) { Format = format; return this; }

    /// <summary>Adds a custom positive constraint.</summary>
    public PromptConstraints Must(string constraint) { _positiveConstraints.Add(constraint); return this; }

    /// <summary>Adds a custom negative constraint.</summary>
    public PromptConstraints Not(string constraint) { _negativeConstraints.Add(constraint); return this; }

    /// <summary>
    /// Renders constraints into a string suitable for appending to a system message.
    /// </summary>
    public string Render()
    {
        List<string> parts = new List<string>();

        if (MaxWordCount.HasValue)
            parts.Add($"Limit your response to {MaxWordCount} words maximum.");
        if (MinWordCount.HasValue)
            parts.Add($"Your response must be at least {MinWordCount} words.");
        if (Format is not null)
            parts.Add(Format.Instructions);

        if (_positiveConstraints.Count > 0)
        {
            parts.Add("Requirements:");
            parts.AddRange(_positiveConstraints.Select(c => $"- {c}"));
        }

        if (_negativeConstraints.Count > 0)
        {
            parts.Add("Restrictions:");
            parts.AddRange(_negativeConstraints.Select(c => $"- {c}"));
        }

        return string.Join("\n", parts);
    }
}
