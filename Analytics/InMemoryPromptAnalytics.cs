using System.Collections.Concurrent;
using Mima.AI.Prompt.Interfaces;

namespace Mima.AI.Prompt.Analytics;

/// <summary>
/// In-memory prompt analytics implementation.
/// Suitable for development and single-instance production.
/// For distributed systems, implement <see cref="IPromptAnalytics"/> with a persistent store.
/// </summary>
public sealed class InMemoryPromptAnalytics : IPromptAnalytics
{
    private readonly ConcurrentDictionary<string, MetricsAccumulator> _metrics = new();

    /// <inheritdoc />
    public void TrackRender(string promptName, TimeSpan renderDuration, int messageCount)
    {
        var acc = GetOrCreate(promptName);
        acc.AddRender(renderDuration);
    }

    /// <inheritdoc />
    public void TrackExecution(string promptName, string provider, int estimatedTokens)
    {
        var acc = GetOrCreate(promptName);
        acc.AddExecution(estimatedTokens);
    }

    /// <inheritdoc />
    public void TrackSuccess(string promptName, string provider, TimeSpan latency)
    {
        var acc = GetOrCreate(promptName);
        acc.AddSuccess(latency);
    }

    /// <inheritdoc />
    public void TrackFailure(string promptName, string provider, string errorMessage)
    {
        var acc = GetOrCreate(promptName);
        acc.AddFailure();
    }

    /// <inheritdoc />
    public void TrackVariableUsage(string templateName, IReadOnlyDictionary<string, object> variables)
    {
        // Track for analysis - could be extended with variable frequency tracking
        GetOrCreate(templateName);
    }

    /// <inheritdoc />
    public PromptMetrics? GetMetrics(string promptName)
    {
        return _metrics.TryGetValue(promptName, out var acc) ? acc.ToMetrics(promptName) : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<PromptMetrics> GetAllMetrics()
    {
        return _metrics.Select(kv => kv.Value.ToMetrics(kv.Key)).ToList().AsReadOnly();
    }

    /// <summary>Resets all collected metrics.</summary>
    public void Reset() => _metrics.Clear();

    private MetricsAccumulator GetOrCreate(string name) =>
        _metrics.GetOrAdd(name, _ => new MetricsAccumulator());

    private sealed class MetricsAccumulator
    {
        private long _renderCount;
        private long _executionCount;
        private long _successCount;
        private long _failureCount;
        private double _totalRenderMs;
        private double _totalLatencyMs;
        private double _totalTokens;
        private DateTimeOffset _lastUsed;

        public void AddRender(TimeSpan duration)
        {
            Interlocked.Increment(ref _renderCount);
            Interlocked.Exchange(ref _totalRenderMs, _totalRenderMs + duration.TotalMilliseconds);
            _lastUsed = DateTimeOffset.UtcNow;
        }

        public void AddExecution(int tokens)
        {
            Interlocked.Increment(ref _executionCount);
            Interlocked.Exchange(ref _totalTokens, _totalTokens + tokens);
            _lastUsed = DateTimeOffset.UtcNow;
        }

        public void AddSuccess(TimeSpan latency)
        {
            Interlocked.Increment(ref _successCount);
            Interlocked.Exchange(ref _totalLatencyMs, _totalLatencyMs + latency.TotalMilliseconds);
        }

        public void AddFailure()
        {
            Interlocked.Increment(ref _failureCount);
        }

        public PromptMetrics ToMetrics(string name) => new()
        {
            PromptName = name,
            RenderCount = _renderCount,
            ExecutionCount = _executionCount,
            SuccessCount = _successCount,
            FailureCount = _failureCount,
            AverageRenderDuration = _renderCount > 0
                ? TimeSpan.FromMilliseconds(_totalRenderMs / _renderCount)
                : TimeSpan.Zero,
            AverageLatency = _successCount > 0
                ? TimeSpan.FromMilliseconds(_totalLatencyMs / _successCount)
                : TimeSpan.Zero,
            AverageTokenCount = _executionCount > 0 ? _totalTokens / _executionCount : 0,
            LastUsed = _lastUsed == default ? null : _lastUsed,
        };
    }
}
