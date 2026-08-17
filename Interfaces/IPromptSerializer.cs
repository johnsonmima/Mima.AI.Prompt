namespace SaaFarr.AI.Prompt.Interfaces;

/// <summary>
/// Serializes and deserializes prompts, messages, and templates for persistence.
/// Enables saving prompt assets to files, databases, or other storage.
/// </summary>
public interface IPromptSerializer
{
    /// <summary>Serializes a prompt to a JSON string.</summary>
    /// <param name="prompt">The prompt to serialize.</param>
    /// <returns>A JSON representation of the prompt.</returns>
    string Serialize(IPrompt prompt);

    /// <summary>Deserializes a prompt from a JSON string.</summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized prompt.</returns>
    IPrompt DeserializePrompt(string json);

    /// <summary>Serializes a message to a JSON string.</summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A JSON representation of the message.</returns>
    string SerializeMessage(IMessage message);

    /// <summary>Deserializes a message from a JSON string.</summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized message.</returns>
    IMessage DeserializeMessage(string json);

    /// <summary>Serializes a template to a JSON string.</summary>
    /// <param name="template">The template to serialize.</param>
    /// <returns>A JSON representation of the template.</returns>
    string SerializeTemplate(IMessageTemplate template);
}
