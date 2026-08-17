namespace SaaFarr.AI.Prompt.Agents;

/// <summary>
/// Host-provided tool executor. This package never invokes tools;
/// attach via <see cref="AgentSpec.WithToolInvoker"/> and call from your own agent loop.
/// </summary>
public interface IAgentToolInvoker
{
    /// <summary>Executes a tool by name with JSON arguments and returns a string/JSON result.</summary>
    /// <param name="toolName">
    /// Tool name from <see cref="SaaFarr.AI.Prompt.Models.ToolCall.Name"/> (for example <c>get_weather</c>).
    /// <see cref="AgentSpec.WithTools"/> only advertises names in the prompt — this method is where you
    /// switch on <paramref name="toolName"/> and run the matching C# implementation.
    /// </param>
    /// <param name="argumentsJson">JSON arguments from the model tool call.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool output to place in a <see cref="Messages.ToolMessage"/>.</returns>
    Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
