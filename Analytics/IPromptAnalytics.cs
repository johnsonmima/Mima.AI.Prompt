using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Analytics;

/// <summary>
/// Interface for prompt analytics and telemetry.
/// Tracks prompt usage, performance, and quality metrics.
/// </summary>
/// <remarks>
/// Analytics help teams understand:
/// - Which prompts are used most frequently
/// - Which prompts produce the best results
/// - Average rendering time and token counts
/// - Template variable usage patterns
/// </remarks>
public interface IPromptAnalytics
{
    /// <summary>Records that a prompt was rendered.</summary>
    void TrackRender(string promptName, TimeSpan renderDuration, int messageCount);

    /// <summary>Records that a prompt was sent to a provider.</summary>
    void TrackExecution(string promptName, string provider, int estimatedTokens);

    /// <summary>Records that a prompt produced a successful response.</summary>
    void TrackSuccess(string promptName, string provider, TimeSpan latency);

    /// <summary>Records that a prompt execution failed.</summary>
    void TrackFailure(string promptName, string provider, string errorMessage);

    /// <summary>Records template variable usage for analysis.</summary>
    void TrackVariableUsage(string templateName, IReadOnlyDictionary<string, object> variables);

    /// <summary>Gets aggregated metrics for a prompt.</summary>
    PromptMetrics? GetMetrics(string promptName);

    /// <summary>Gets metrics for all tracked prompts.</summary>
    IReadOnlyList<PromptMetrics> GetAllMetrics();
}

/// <summary>Aggregated metrics for a single prompt.</summary>
public sealed class PromptMetrics
{
    /// <summary>Gets the prompt name.</summary>
    public string PromptName { get; init; } = string.Empty;

    /// <summary>Gets the total number of renders.</summary>
    public long RenderCount { get; init; }

    /// <summary>Gets the total number of executions.</summary>
    public long ExecutionCount { get; init; }

    /// <summary>Gets the success count.</summary>
    public long SuccessCount { get; init; }

    /// <summary>Gets the failure count.</summary>
    public long FailureCount { get; init; }

    /// <summary>Gets the average render duration.</summary>
    public TimeSpan AverageRenderDuration { get; init; }

    /// <summary>Gets the average execution latency.</summary>
    public TimeSpan AverageLatency { get; init; }

    /// <summary>Gets the average estimated tokens.</summary>
    public double AverageTokenCount { get; init; }

    /// <summary>Gets the success rate (0.0 to 1.0).</summary>
    public double SuccessRate => ExecutionCount > 0 ? (double)SuccessCount / ExecutionCount : 0;

    /// <summary>Gets the last time this prompt was used.</summary>
    public DateTimeOffset? LastUsed { get; init; }
}
