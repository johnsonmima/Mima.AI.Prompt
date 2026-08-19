using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mima.AI.Prompt.Serialization;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for adapters, renderers, and persistence.
/// </summary>
internal static class PromptJsonOptions
{
    /// <summary>Indented camelCase JSON (provider payloads and generic rendering).</summary>
    public static JsonSerializerOptions IndentedCamelCase { get; } = Create();

    /// <summary>Same as <see cref="IndentedCamelCase"/>, omitting null properties (prompt persistence).</summary>
    public static JsonSerializerOptions IndentedCamelCaseIgnoreNull { get; } =
        Create(JsonIgnoreCondition.WhenWritingNull);

    private static JsonSerializerOptions Create(
        JsonIgnoreCondition defaultIgnoreCondition = JsonIgnoreCondition.Never) => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = defaultIgnoreCondition
    };
}
