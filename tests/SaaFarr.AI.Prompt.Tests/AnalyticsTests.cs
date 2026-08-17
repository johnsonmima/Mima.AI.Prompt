using FluentAssertions;
using SaaFarr.AI.Prompt.Analytics;

namespace SaaFarr.AI.Prompt.Tests;

public class AnalyticsTests
{
    [Fact]
    public void TrackRender_AccumulatesRenderCountAndDuration()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackRender("MyPrompt", TimeSpan.FromMilliseconds(100), 2);
        analytics.TrackRender("MyPrompt", TimeSpan.FromMilliseconds(200), 2);

        var metrics = analytics.GetMetrics("MyPrompt");

        metrics.Should().NotBeNull();
        metrics!.RenderCount.Should().Be(2);
        metrics.AverageRenderDuration.Should().Be(TimeSpan.FromMilliseconds(150));
        metrics.LastUsed.Should().NotBeNull();
    }

    [Fact]
    public void TrackExecution_AccumulatesExecutionCountAndTokens()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackExecution("MyPrompt", "openai", 100);
        analytics.TrackExecution("MyPrompt", "openai", 200);

        var metrics = analytics.GetMetrics("MyPrompt");

        metrics!.ExecutionCount.Should().Be(2);
        metrics.AverageTokenCount.Should().Be(150);
    }

    [Fact]
    public void TrackSuccess_AccumulatesSuccessCountAndLatency()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackExecution("MyPrompt", "openai", 100);
        analytics.TrackSuccess("MyPrompt", "openai", TimeSpan.FromMilliseconds(500));

        var metrics = analytics.GetMetrics("MyPrompt");

        metrics!.SuccessCount.Should().Be(1);
        metrics.AverageLatency.Should().Be(TimeSpan.FromMilliseconds(500));
        metrics.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void TrackFailure_AccumulatesFailureCount()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackExecution("MyPrompt", "openai", 100);
        analytics.TrackFailure("MyPrompt", "openai", "timeout");

        var metrics = analytics.GetMetrics("MyPrompt");

        metrics!.FailureCount.Should().Be(1);
        metrics.SuccessRate.Should().Be(0.0);
    }

    [Fact]
    public void TrackVariableUsage_CreatesMetricsEntryForTemplate()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackVariableUsage("MyTemplate", new Dictionary<string, object> { ["name"] = "Bob" });

        var metrics = analytics.GetMetrics("MyTemplate");
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void GetMetrics_UnknownPrompt_ReturnsNull()
    {
        var analytics = new InMemoryPromptAnalytics();
        analytics.GetMetrics("Unknown").Should().BeNull();
    }

    [Fact]
    public void GetAllMetrics_ReturnsAllTrackedPrompts()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackRender("Prompt1", TimeSpan.FromMilliseconds(10), 1);
        analytics.TrackRender("Prompt2", TimeSpan.FromMilliseconds(20), 1);

        var all = analytics.GetAllMetrics();

        all.Should().HaveCount(2);
        all.Should().Contain(m => m.PromptName == "Prompt1");
        all.Should().Contain(m => m.PromptName == "Prompt2");
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        var analytics = new InMemoryPromptAnalytics();
        analytics.TrackRender("Prompt1", TimeSpan.FromMilliseconds(10), 1);

        analytics.Reset();

        analytics.GetAllMetrics().Should().BeEmpty();
        analytics.GetMetrics("Prompt1").Should().BeNull();
    }

    [Fact]
    public void SuccessRate_WithZeroExecutions_ReturnsZero()
    {
        var metrics = new PromptMetrics { PromptName = "Test" };
        metrics.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void PromptMetrics_DefaultValues_AreZeroOrDefault()
    {
        var metrics = new PromptMetrics();

        metrics.PromptName.Should().BeEmpty();
        metrics.RenderCount.Should().Be(0);
        metrics.ExecutionCount.Should().Be(0);
        metrics.SuccessCount.Should().Be(0);
        metrics.FailureCount.Should().Be(0);
        metrics.AverageRenderDuration.Should().Be(TimeSpan.Zero);
        metrics.AverageLatency.Should().Be(TimeSpan.Zero);
        metrics.AverageTokenCount.Should().Be(0);
        metrics.LastUsed.Should().BeNull();
    }

    [Fact]
    public void MultiplePromptsTrackedIndependently()
    {
        var analytics = new InMemoryPromptAnalytics();

        analytics.TrackRender("A", TimeSpan.FromMilliseconds(10), 1);
        analytics.TrackRender("B", TimeSpan.FromMilliseconds(20), 1);
        analytics.TrackRender("B", TimeSpan.FromMilliseconds(40), 1);

        analytics.GetMetrics("A")!.RenderCount.Should().Be(1);
        analytics.GetMetrics("B")!.RenderCount.Should().Be(2);
    }

    [Fact]
    public void NoRenders_AverageRenderDurationIsZero()
    {
        var analytics = new InMemoryPromptAnalytics();
        analytics.TrackExecution("A", "openai", 10);

        var metrics = analytics.GetMetrics("A");
        metrics!.AverageRenderDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void NoSuccesses_AverageLatencyIsZero()
    {
        var analytics = new InMemoryPromptAnalytics();
        analytics.TrackExecution("A", "openai", 10);

        var metrics = analytics.GetMetrics("A");
        metrics!.AverageLatency.Should().Be(TimeSpan.Zero);
    }
}
