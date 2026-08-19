using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Agents;

/// <summary>
/// Host-provided retrieval (RAG). This package does not retrieve documents;
/// when attached, <see cref="AgentSpec.BuildPrompt(string)"/> incorporates the result into the prompt.
/// </summary>
public interface IAgentRetriever
{
    /// <summary>Retrieves context for the current user query.</summary>
    /// <param name="query">User query / turn text used for retrieval.</param>
    /// <returns>Text and/or content parts to inject into the prompt.</returns>
    AgentRetrievalResult Retrieve(string query);
}

/// <summary>Result of a host retrieval call.</summary>
public sealed class AgentRetrievalResult
{
    /// <summary>Optional plain-text context (injected as a developer/context block).</summary>
    public string? Text { get; }

    /// <summary>Optional multimodal / file parts (e.g. <c>FilePart</c>) to include with the user turn.</summary>
    public IReadOnlyList<IContentPart>? Parts { get; }

    /// <summary>Creates an empty result.</summary>
    public static AgentRetrievalResult Empty { get; } = new(null, null);

    /// <summary>Creates a text-only retrieval result.</summary>
    public static AgentRetrievalResult FromText(string text) =>
        new(text ?? throw new ArgumentNullException(nameof(text)), null);

    /// <summary>Creates a parts-only retrieval result.</summary>
    public static AgentRetrievalResult FromParts(IEnumerable<IContentPart> parts) =>
        new(null, (parts ?? throw new ArgumentNullException(nameof(parts))).ToList());

    /// <summary>Creates a combined text + parts result.</summary>
    public static AgentRetrievalResult Create(string? text, IEnumerable<IContentPart>? parts) =>
        new(text, parts?.ToList());

    private AgentRetrievalResult(string? text, IReadOnlyList<IContentPart>? parts)
    {
        Text = string.IsNullOrWhiteSpace(text) ? null : text;
        Parts = parts is { Count: > 0 } ? parts : null;
    }

    /// <summary>True when neither text nor parts are present.</summary>
    public bool IsEmpty => Text is null && Parts is null;
}
