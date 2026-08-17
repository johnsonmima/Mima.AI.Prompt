using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Host-provided multi-step agent loop (tool cycles, budgets, approvals).
/// This package does not run loops; attach via <see cref="AgentSpec.WithLoop"/> for composition.
/// </summary>
public interface IAgentLoop
{
    /// <summary>
    /// Runs the host-defined loop for <paramref name="agent"/> and the given user turn,
    /// typically calling <see cref="IAgentChatClient"/> and <see cref="IAgentToolInvoker"/>.
    /// </summary>
    /// <param name="agent">Agent prompt spec (instructions, tools, hooks).</param>
    /// <param name="userTurn">User utterance for this run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Final prompt state after the loop (useful for logging / serialize).</returns>
    Task<IPrompt> RunAsync(AgentSpec agent, string userTurn, CancellationToken cancellationToken = default);
}
