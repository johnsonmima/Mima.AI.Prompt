using FluentAssertions;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Caching;

namespace Mima.AI.Prompt.Tests;

public class CachingTests
{
    [Fact]
    public void Set_AndGet_ReturnsSamePrompt()
    {
        var cache = new InMemoryPromptCache();
        var prompt = PromptBuilder.UserOnly("hi");

        cache.Set("key1", prompt);

        cache.Get("key1").Should().Be(prompt);
    }

    [Fact]
    public void Get_MissingKey_ReturnsNull()
    {
        var cache = new InMemoryPromptCache();
        cache.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Get_WhitespaceKey_ReturnsNull()
    {
        var cache = new InMemoryPromptCache();
        cache.Get("   ").Should().BeNull();
    }

    [Fact]
    public void Get_EmptyKey_ReturnsNull()
    {
        var cache = new InMemoryPromptCache();
        cache.Get(string.Empty).Should().BeNull();
    }

    [Fact]
    public void Set_EmptyKey_Throws()
    {
        var cache = new InMemoryPromptCache();
        var prompt = PromptBuilder.UserOnly("hi");

        var act = () => cache.Set(string.Empty, prompt);
        act.Should().Throw<ArgumentException>().WithMessage("*Cache key cannot be empty*");
    }

    [Fact]
    public void Set_WhitespaceKey_Throws()
    {
        var cache = new InMemoryPromptCache();
        var prompt = PromptBuilder.UserOnly("hi");

        var act = () => cache.Set("   ", prompt, TimeSpan.FromMinutes(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Set_NullPrompt_Throws()
    {
        var cache = new InMemoryPromptCache();
        var act = () => cache.Set("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_WithCustomTtl_Works()
    {
        var cache = new InMemoryPromptCache();
        var prompt = PromptBuilder.UserOnly("hi");

        cache.Set("key", prompt, TimeSpan.FromMinutes(5));

        cache.Contains("key").Should().BeTrue();
    }

    [Fact]
    public void Contains_ExistingKey_ReturnsTrue()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"));

        cache.Contains("key").Should().BeTrue();
    }

    [Fact]
    public void Contains_MissingKey_ReturnsFalse()
    {
        var cache = new InMemoryPromptCache();
        cache.Contains("missing").Should().BeFalse();
    }

    [Fact]
    public async Task Contains_ExpiredEntry_ReturnsFalseAndRemoves()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"), TimeSpan.FromMilliseconds(10));

        await Task.Delay(60);

        cache.Contains("key").Should().BeFalse();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task Get_ExpiredEntry_ReturnsNullAndRemoves()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"), TimeSpan.FromMilliseconds(10));

        await Task.Delay(60);

        cache.Get("key").Should().BeNull();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndRemoves()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"));

        cache.Remove("key").Should().BeTrue();
        cache.Contains("key").Should().BeFalse();
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        var cache = new InMemoryPromptCache();
        cache.Remove("missing").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("k1", PromptBuilder.UserOnly("a"));
        cache.Set("k2", PromptBuilder.UserOnly("b"));

        cache.Clear();

        cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task Evict_RemovesOnlyExpiredEntries()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("expiring", PromptBuilder.UserOnly("a"), TimeSpan.FromMilliseconds(10));
        cache.Set("persistent", PromptBuilder.UserOnly("b"), TimeSpan.FromMinutes(5));

        await Task.Delay(60);
        cache.Evict();

        cache.Count.Should().Be(1);
        cache.Contains("persistent").Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_TracksHitsAndEntries()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"));

        cache.Get("key");
        cache.Get("key");

        var stats = cache.GetStatistics();

        stats.TotalEntries.Should().Be(1);
        stats.TotalHits.Should().Be(2);
        stats.ExpiredEntries.Should().Be(0);
        stats.ActiveEntries.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_CountsExpiredEntries()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"), TimeSpan.FromMilliseconds(10));

        await Task.Delay(60);

        var stats = cache.GetStatistics();
        stats.TotalEntries.Should().Be(1);
        stats.ExpiredEntries.Should().Be(1);
        stats.ActiveEntries.Should().Be(0);
    }

    [Fact]
    public void DefaultTtl_UsedWhenNotSpecified()
    {
        var cache = new InMemoryPromptCache(TimeSpan.FromMinutes(1));
        cache.Set("key", PromptBuilder.UserOnly("hi"));

        cache.Contains("key").Should().BeTrue();
    }

    [Fact]
    public void Ctor_NoTtlProvided_UsesDefault()
    {
        var cache = new InMemoryPromptCache();
        cache.Set("key", PromptBuilder.UserOnly("hi"));
        cache.Contains("key").Should().BeTrue();
    }

    [Fact]
    public void CacheStatistics_ActiveEntries_ComputesCorrectly()
    {
        var stats = new CacheStatistics(10, 3, 5);
        stats.ActiveEntries.Should().Be(7);
    }
}
