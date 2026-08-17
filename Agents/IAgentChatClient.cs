using SaaFarr.AI.Prompt.Interfaces;

namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Host-provided model client. This package never calls it from prompt builders;
/// attach via <see cref="AgentSpec.WithChatClient"/> and invoke from your own loop.
/// </summary>
public interface IAgentChatClient
{
    /// <summary>Sends a prompt to a model and returns the assistant (or other) reply message.</summary>
    /// <param name="prompt">The prompt asset to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model reply as an <see cref="IMessage"/> (typically an assistant message).</returns>
    Task<IMessage> CompleteAsync(IPrompt prompt, CancellationToken cancellationToken = default);
}
