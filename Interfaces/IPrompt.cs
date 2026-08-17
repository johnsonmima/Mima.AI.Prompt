using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Interfaces;

/// <summary>
/// A complete prompt: ordered messages plus optional response format for structured outputs.
/// </summary>
public interface IPrompt
{
    /// <summary>Gets the ordered collection of messages in this prompt.</summary>
    IReadOnlyList<IMessage> Messages { get; }

    /// <summary>Gets the metadata associated with this prompt.</summary>
    MessageMetadata Metadata { get; }

    /// <summary>Gets the unique identifier for this prompt.</summary>
    string Id { get; }

    /// <summary>Gets the total number of messages in this prompt.</summary>
    int MessageCount { get; }

    /// <summary>
    /// Optional structured response format (JSON schema, etc.) requested from the provider.
    /// </summary>
    OutputFormat? ResponseFormat { get; }
}
