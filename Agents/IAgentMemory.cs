using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Host-provided memory beyond in-process <see cref="Models.Conversation"/>.
/// Return prior turns to incorporate into <see cref="AgentSpec.BuildPrompt(string)"/>.
/// </summary>
public interface IAgentMemory
{
    /// <summary>Gets history messages to include before the current user turn (no system message required).</summary>
    IReadOnlyList<IMessage> GetHistory();
}
