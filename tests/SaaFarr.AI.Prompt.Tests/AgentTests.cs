using FluentAssertions;
using SaaFarr.AI.Prompt.Agents;
using SaaFarr.AI.Prompt.Content;
using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Messages;
using SaaFarr.AI.Prompt.Models;
using SaaFarr.AI.Prompt.Roles;

namespace SaaFarr.AI.Prompt.Tests;

public class AgentTests
{
    [Fact]
    public void AgentSpec_BuildPrompt_IncludesInstructionsToolsAndUser()
    {
        var prompt = AgentSpec.Create("weather")
            .WithInstructions("You are a weather agent.")
            .WithTools("get_weather", "get_forecast")
            .WithResponseFormat(OutputFormat.Json())
            .BuildPrompt("Weather in NYC?");

        prompt.Metadata.Name.Should().Be("weather");
        prompt.SystemMessage!.Content.Should().Contain("weather agent");
        prompt.SystemMessage.Content.Should().Contain("get_weather");
        prompt.SystemMessage.Content.Should().Contain("get_forecast");
        prompt.LastUserMessage!.Content.Should().Be("Weather in NYC?");
        prompt.ResponseFormat.Should().NotBeNull();
    }

    [Fact]
    public void AgentSpec_WithMemoryWindow_IncludesRecentHistory()
    {
        var conversation = Conversation.Create("chat")
            .AddUser("old")
            .AddAssistant("old-answer")
            .AddUser("recent")
            .AddAssistant("recent-answer");

        var prompt = AgentSpec.Create("assistant")
            .WithInstructions("Helpful.")
            .WithMemory(conversation, window: 2)
            .BuildPrompt("new question");

        // system + last 2 history + new user
        prompt.Messages.Should().Contain(m => m.Content == "recent");
        prompt.Messages.Should().Contain(m => m.Content == "recent-answer");
        prompt.Messages.Should().NotContain(m => m.Content == "old");
        prompt.LastUserMessage!.Content.Should().Be("new question");
    }

    [Fact]
    public void AgentSpec_Retriever_IncorporatesTextAndParts()
    {
        var retriever = new FakeRetriever(AgentRetrievalResult.Create(
            "Refunds within 30 days.",
            new IContentPart[] { FilePart.FromId("file_policy", "policy.pdf") }));

        var prompt = AgentSpec.Create("support")
            .WithInstructions("Answer from context.")
            .WithRetriever(retriever)
            .BuildPrompt("What is the refund policy?");

        prompt.Messages.Should().Contain(m =>
            m.Role == MessageRole.Developer && m.Content.Contains("30 days"));
        prompt.LastUserMessage!.Parts.OfType<FilePart>().Should().ContainSingle(f => f.FileId == "file_policy");
        prompt.LastUserMessage.Content.Should().Contain("refund policy");
    }

    [Fact]
    public void AgentSpec_ExternalMemory_And_ToolCatalog_Incorporate()
    {
        var memory = new FakeMemory(new IMessage[]
        {
            UserMessage.Create("prior question"),
            AssistantMessage.Create("prior answer")
        });
        var catalog = new FakeCatalog(new[]
        {
            AgentToolDescriptor.Create("search", "Search the knowledge base", "{\"type\":\"object\"}")
        });

        var prompt = AgentSpec.Create("kb")
            .WithInstructions("KB agent.")
            .WithExternalMemory(memory)
            .WithToolCatalog(catalog)
            .BuildPrompt("next");

        prompt.SystemMessage!.Content.Should().Contain("search");
        prompt.SystemMessage.Content.Should().Contain("knowledge base");
        prompt.SystemMessage.Content.Should().Contain("Parameters schema");
        prompt.Messages.Should().Contain(m => m.Content == "prior question");
    }

    [Fact]
    public void AgentSpec_BuildPromptWithToolResults_AppendsAssistantAndTools()
    {
        var assistant = AssistantMessage.CreateWithToolCalls(
            new[] { ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}") },
            content: "Checking…");

        var prompt = AgentSpec.Create("weather")
            .WithInstructions("Weather agent.")
            .WithTools("get_weather")
            .BuildPromptWithToolResults(
                "What's the weather in NYC?",
                assistant,
                new[] { ("call_1", "{\"temp_f\":72}") });

        prompt.Messages.OfType<AssistantMessage>().Should().ContainSingle();
        prompt.Messages.OfType<ToolMessage>().Should().ContainSingle(t => t.ToolCallId == "call_1");
        prompt.LastUserMessage.Should().NotBeNull();
    }

    [Fact]
    public void AgentSpec_HostHooks_AreStoredButNotRequired()
    {
        var client = new FakeChatClient();
        var invoker = new FakeToolInvoker();
        var loop = new FakeLoop();

        var agent = AgentSpec.Create("x")
            .WithInstructions("Hi")
            .WithChatClient(client)
            .WithToolInvoker(invoker)
            .WithLoop(loop);

        agent.ChatClient.Should().BeSameAs(client);
        agent.ToolInvoker.Should().BeSameAs(invoker);
        agent.Loop.Should().BeSameAs(loop);
        agent.RequireChatClient().Should().BeSameAs(client);
        agent.RequireToolInvoker().Should().BeSameAs(invoker);
        agent.RequireLoop().Should().BeSameAs(loop);

        // BuildPrompt still works without calling hooks
        agent.BuildPrompt("hello").LastUserMessage!.Content.Should().Be("hello");
        client.CallCount.Should().Be(0);
        invoker.CallCount.Should().Be(0);
        loop.CallCount.Should().Be(0);
    }

    [Fact]
    public void AgentCrew_BuildTurn_EmitsCustomRolesAndTopic()
    {
        var prompt = AgentCrew.Create("docs")
            .WithOrchestratorInstructions("Host a short debate.")
            .AddMember("writer", "Draft a paragraph on DI.", displayName: "writer-bot")
            .AddMember("critic", "Challenge weak claims.")
            .BuildTurn("Summarize dependency injection.");

        prompt.SystemMessage!.Content.Should().Contain("writer");
        prompt.SystemMessage.Content.Should().Contain("critic");
        prompt.Messages.Should().Contain(m => m.Role.Name == "writer" && m.Name == "writer-bot");
        prompt.Messages.Should().Contain(m => m.Role.Name == "critic");
        prompt.LastUserMessage!.Content.Should().Contain("dependency injection");
    }

    [Fact]
    public void AgentCrew_BuildTurn_WithContributions()
    {
        var crew = AgentCrew.Create("docs")
            .AddMember("writer", "You write.", "w1")
            .AddMember("critic", "You critique.");

        var prompt = crew.BuildTurn(
            "Who won?",
            new[]
            {
                ("writer", "DI is clearer."),
                ("critic", "Not always.")
            });

        prompt.Messages.Should().Contain(m => m.Role.Name == "writer" && m.Content.Contains("clearer") && m.Name == "w1");
        prompt.Messages.Should().Contain(m => m.Role.Name == "critic" && m.Content.Contains("Not always"));
    }

    [Fact]
    public void AgentSpec_BuildPrompt_Parts_Works()
    {
        var prompt = AgentSpec.Create("vision")
            .WithInstructions("Describe images.")
            .BuildPrompt(new IContentPart[]
            {
                TextPart.Create("What is this?"),
                ImagePart.FromUrl("https://example.com/a.png")
            });

        prompt.LastUserMessage!.Parts.Should().HaveCount(2);
    }

    [Fact]
    public void AgentSpec_DeveloperInstructions_And_Metadata()
    {
        var prompt = AgentSpec.Create("dev")
            .WithInstructions("System policy.")
            .WithDeveloperInstructions("Prefer bullets.")
            .WithMetadata(MessageMetadata.WithName("custom-meta"))
            .BuildPrompt("hi");

        prompt.Messages.Should().Contain(m => m.Role == MessageRole.Developer && m.Content.Contains("bullets"));
        prompt.Metadata.Name.Should().Be("custom-meta");
    }

    [Fact]
    public void AgentSpec_Validation_ThrowsOnEmptyInputs()
    {
        FluentActions.Invoking(() => AgentSpec.Create(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AgentSpec.Create("x").BuildPrompt(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AgentSpec.Create("x").BuildPrompt(Array.Empty<IContentPart>()))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AgentSpec.Create("x").WithTools(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AgentSpec_BuildPromptWithToolResults_SkipsDuplicateUser_WhenInMemory()
    {
        var conversation = Conversation.Create("c").AddUser("What's the weather in NYC?");
        var assistant = AssistantMessage.CreateWithToolCalls(
            new[] { ToolCall.Create("call_1", "get_weather", "{}") });

        var prompt = AgentSpec.Create("weather")
            .WithInstructions("Weather.")
            .WithMemory(conversation)
            .BuildPromptWithToolResults(
                "What's the weather in NYC?",
                assistant,
                new[] { ("call_1", "{}") });

        prompt.Messages.Count(m => m.Role == MessageRole.User && m.Content.Contains("NYC"))
            .Should().Be(1);
    }

    [Fact]
    public void AgentCrew_Validation_And_DefaultOrchestrator()
    {
        FluentActions.Invoking(() => AgentCrew.Create(" ").AddMember("a", "b")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AgentCrew.Create("c").BuildTurn("topic"))
            .Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => new AgentMember(" ", "x")).Should().Throw<ArgumentException>();

        var prompt = AgentCrew.Create("c")
            .AddMember(new AgentMember("writer", "Write."))
            .BuildTurn("Topic");

        prompt.SystemMessage!.Content.Should().Contain("multi-agent");
        prompt.SystemMessage.Content.Should().Contain("writer");
    }

    [Fact]
    public void AgentRetrievalResult_And_ToolDescriptor_Factories()
    {
        AgentRetrievalResult.Empty.IsEmpty.Should().BeTrue();
        AgentRetrievalResult.FromText("ctx").Text.Should().Be("ctx");
        AgentRetrievalResult.FromParts(new[] { TextPart.Create("p") }).Parts.Should().ContainSingle();
        AgentToolDescriptor.Create("t", "desc", "{}").ParametersSchemaJson.Should().Be("{}");
        FluentActions.Invoking(() => AgentToolDescriptor.Create(" ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AgentSpec_Retriever_PartsOnly_MergesIntoUserParts()
    {
        var retriever = new FakeRetriever(AgentRetrievalResult.FromParts(
            new IContentPart[] { FilePart.FromId("f1") }));

        var prompt = AgentSpec.Create("rag")
            .WithInstructions("Use files.")
            .WithRetriever(retriever)
            .BuildPrompt(new IContentPart[] { TextPart.Create("Q?") });

        prompt.LastUserMessage!.Parts.OfType<FilePart>().Should().ContainSingle();
        prompt.LastUserMessage.Parts.OfType<TextPart>().Should().ContainSingle();
    }

    [Fact]
    public void AgentSpec_ToolNames_Deduplicate_And_WhitespaceSkipped()
    {
        var agent = AgentSpec.Create("t")
            .WithInstructions("X")
            .WithTools("search", "search", "  ", "lookup");

        agent.ToolNames.Should().Equal("search", "lookup");
    }

    [Fact]
    public void AgentSpec_NullHookArguments_Throw()
    {
        var agent = AgentSpec.Create("x").WithInstructions("i");
        FluentActions.Invoking(() => agent.WithRetriever(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithExternalMemory(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithToolCatalog(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithChatClient(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithToolInvoker(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithLoop(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithMemory(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => agent.WithResponseFormat(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AgentSpec_RequireHooks_Throw_WhenNotAttached()
    {
        var agent = AgentSpec.Create("x").WithInstructions("i");
        FluentActions.Invoking(() => agent.RequireChatClient())
            .Should().Throw<InvalidOperationException>().WithMessage("*WithChatClient*");
        FluentActions.Invoking(() => agent.RequireToolInvoker())
            .Should().Throw<InvalidOperationException>().WithMessage("*WithToolInvoker*");
        FluentActions.Invoking(() => agent.RequireLoop())
            .Should().Throw<InvalidOperationException>().WithMessage("*WithLoop*");
    }

    [Fact]
    public async Task AgentSpec_HostLoop_CanBeInvokedByHost()
    {
        var loop = new FakeLoop();
        var agent = AgentSpec.Create("x")
            .WithInstructions("Hi")
            .WithLoop(loop);

        var result = (Models.Prompt)await agent.RequireLoop().RunAsync(agent, "hello");
        loop.CallCount.Should().Be(1);
        result.LastUserMessage!.Content.Should().Be("hello");
    }

    [Fact]
    public void AgentCrew_WithResponseFormat_And_EmptyTopic_Throws()
    {
        var crew = AgentCrew.Create("c")
            .AddMember("writer", "Write.")
            .WithResponseFormat(OutputFormat.Markdown());

        crew.BuildTurn("Topic").ResponseFormat.Should().NotBeNull();
        FluentActions.Invoking(() => crew.BuildTurn(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => crew.BuildTurn("t", null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AgentSpec_Properties_ExposeAttachedState()
    {
        var conversation = Conversation.Create("c");
        var agent = AgentSpec.Create("weather")
            .WithInstructions("I")
            .WithTools("a")
            .WithMemory(conversation, 5)
            .WithResponseFormat(OutputFormat.Json());

        agent.Name.Should().Be("weather");
        agent.Instructions.Should().Be("I");
        agent.ToolNames.Should().ContainSingle("a");
        agent.Memory.Should().BeSameAs(conversation);
        agent.ResponseFormat.Should().NotBeNull();
    }

    private sealed class FakeRetriever : IAgentRetriever
    {
        private readonly AgentRetrievalResult _result;
        public FakeRetriever(AgentRetrievalResult result) => _result = result;
        public AgentRetrievalResult Retrieve(string query) => _result;
    }

    private sealed class FakeMemory : IAgentMemory
    {
        private readonly IReadOnlyList<IMessage> _history;
        public FakeMemory(IReadOnlyList<IMessage> history) => _history = history;
        public IReadOnlyList<IMessage> GetHistory() => _history;
    }

    private sealed class FakeCatalog : IAgentToolCatalog
    {
        private readonly IReadOnlyList<AgentToolDescriptor> _tools;
        public FakeCatalog(IReadOnlyList<AgentToolDescriptor> tools) => _tools = tools;
        public IReadOnlyList<AgentToolDescriptor> GetTools() => _tools;
    }

    private sealed class FakeChatClient : IAgentChatClient
    {
        public int CallCount { get; private set; }
        public Task<IMessage> CompleteAsync(IPrompt prompt, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IMessage>(AssistantMessage.Create("ok"));
        }
    }

    private sealed class FakeToolInvoker : IAgentToolInvoker
    {
        public int CallCount { get; private set; }
        public Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult("{}");
        }
    }

    private sealed class FakeLoop : IAgentLoop
    {
        public int CallCount { get; private set; }
        public Task<IPrompt> RunAsync(AgentSpec agent, string userTurn, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IPrompt>(agent.BuildPrompt(userTurn));
        }
    }
}
