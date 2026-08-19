namespace Mima.AI.Prompt.Agents;

/// <summary>
/// Host-provided tool catalog. Optional richer descriptions are folded into system instructions
/// by <see cref="AgentSpec"/>; this package does not register or invoke C# delegates.
/// </summary>
public interface IAgentToolCatalog
{
    /// <summary>Gets tool descriptors available to the agent.</summary>
    IReadOnlyList<AgentToolDescriptor> GetTools();
}

/// <summary>Describes a tool for prompt text (name / description / optional JSON schema).</summary>
public sealed class AgentToolDescriptor
{
    /// <summary>Tool name as the model should call it.</summary>
    public string Name { get; }

    /// <summary>Human / model-facing description.</summary>
    public string? Description { get; }

    /// <summary>Optional JSON Schema for parameters (prompt guidance only).</summary>
    public string? ParametersSchemaJson { get; }

    /// <summary>Creates a tool descriptor.</summary>
    public AgentToolDescriptor(string name, string? description = null, string? parametersSchemaJson = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim();
        ParametersSchemaJson = string.IsNullOrWhiteSpace(parametersSchemaJson) ? null : parametersSchemaJson;
    }

    /// <summary>Creates a tool descriptor.</summary>
    public static AgentToolDescriptor Create(string name, string? description = null, string? parametersSchemaJson = null) =>
        new(name, description, parametersSchemaJson);
}
