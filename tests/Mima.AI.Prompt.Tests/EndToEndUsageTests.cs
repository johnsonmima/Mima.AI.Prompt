using FluentAssertions;
using Mima.AI.Prompt.Agents;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Catalog;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Providers;
using Mima.AI.Prompt.Rendering;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Validation;

namespace Mima.AI.Prompt.Tests;

/// <summary>
/// End-to-end scenarios that mirror README usage samples.
/// These tests double as executable documentation for common workflows.
/// </summary>
public class EndToEndUsageTests
{
    private readonly PromptSerializer _serializer = new();
    private readonly PromptValidator _validator = new();

    /// <summary>
    /// README: Quick Start — system + user, then validate and render.
    /// </summary>
    [Fact]
    public void Sample_QuickStart_BuildValidateAndRender()
    {
        // Arrange: classic chat prompt
        var prompt = PromptBuilder
            .System("You are a helpful assistant.")
            .AddUser("Explain dependency injection.")
            .Build();

        // Act: validate before send; render generically
        var report = _validator.Validate(prompt);
        var json = new GenericPromptRenderer().Render(prompt);

        // Assert
        report.IsValid.Should().BeTrue();
        prompt.MessageCount.Should().Be(2);
        prompt.SystemMessage!.Content.Should().Contain("helpful");
        prompt.LastUserMessage!.Content.Should().Contain("dependency injection");
        json.Should().Contain("system").And.Contain("user");
    }

    /// <summary>
    /// README: Built-in catalog template with variables.
    /// </summary>
    [Fact]
    public void Sample_CatalogTemplate_WithVariables()
    {
        var prompt = PromptBuilder
            .Use(SystemTemplates.Configurable)
            .With("profession", "Teacher")
            .With("tone", "Friendly")
            .With("maxWords", "200")
            .AddUser("Explain generics in C#")
            .Build();

        prompt.Messages[0].Role.Should().Be(MessageRole.System);
        prompt.Messages[0].Content.Should().Contain("Teacher");
        prompt.Messages[0].Content.Should().Contain("Friendly");
        prompt.LastUserMessage!.Content.Should().Contain("generics");
    }

    /// <summary>
    /// README: Vision — text + image URL via AddUserWithImage.
    /// </summary>
    [Fact]
    public void Sample_Vision_TextPlusImage()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem("You are a vision assistant. Describe images accurately.")
            .AddUserWithImage(
                text: "What objects are in this photo?",
                imageUrl: "https://example.com/photo.png")
            .Build();

        var user = prompt.LastUserMessage!;
        user.Parts.Should().HaveCount(2);
        user.Parts[0].Should().BeOfType<TextPart>();
        user.Parts[1].Should().BeOfType<ImagePart>()
            .Which.Url.Should().Be("https://example.com/photo.png");

        // OpenAI payload should use content array with image_url
        var openAiJson = new OpenAiAdapter().ToJson(prompt);
        openAiJson.Should().Contain("image_url");
        openAiJson.Should().Contain("What objects are in this photo?");
    }

    /// <summary>
    /// README: Document Q&A — text + file part.
    /// </summary>
    [Fact]
    public void Sample_DocumentQa_TextPlusFile()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem("Answer only from the attached document.")
            .AddUser(new IContentPart[]
            {
                TextPart.Create("Summarize section 3."),
                FilePart.FromId("file_abc123", filename: "spec.pdf")
            })
            .Build();

        prompt.LastUserMessage!.Content.Should().Be("Summarize section 3.");
        prompt.LastUserMessage.Parts.OfType<FilePart>().Single().Filename.Should().Be("spec.pdf");
    }

    /// <summary>
    /// README: Agentic tool loop — assistant tool_calls then tool result.
    /// </summary>
    [Fact]
    public void Sample_AgenticToolLoop_Weather()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem("You can call tools when needed.")
            .AddUser("What's the weather in NYC?")
            .AddAssistantToolCalls(
                toolCalls: new[]
                {
                    ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}")
                },
                content: "I'll check that.")
            .AddTool("call_1", "{\"temp_f\":72,\"conditions\":\"clear\"}")
            .AddAssistant("It is 72°F and clear in NYC.")
            .Build();

        prompt.MessageCount.Should().Be(5);

        var assistantCall = (AssistantMessage)prompt.Messages[2];
        assistantCall.ToolCalls.Should().ContainSingle(t => t.Id == "call_1" && t.Name == "get_weather");

        var tool = (ToolMessage)prompt.Messages[3];
        tool.ToolCallId.Should().Be("call_1");
        tool.Content.Should().Contain("72");

        // Wire format for OpenAI includes tool_calls + tool_call_id
        var json = new OpenAiAdapter().ToJson(prompt);
        json.Should().Contain("tool_calls");
        json.Should().Contain("get_weather");
        json.Should().Contain("call_1");
    }

    /// <summary>
    /// README: Multi-agent debate using custom roles + speaker names.
    /// </summary>
    [Fact]
    public void Sample_MultiAgentDebate_CustomRolesAndNames()
    {
        var critic = MessageRole.Custom("critic");
        var defender = MessageRole.Custom("defender");

        var prompt = PromptBuilder.Create()
            .AddSystem("You host a technical debate. Keep turns short.")
            .Add(critic, "Challenge: DI always beats service locator.")
            .Add(defender, "Defense: service locator is fine for plugins.")
            .AddMessage(AssistantMessage.CreateDetailed(
                content: "Both have trade-offs; prefer DI for testability.",
                name: "moderator-bot"))
            .AddUser("Who won?")
            .Build();

        prompt.Messages[1].Role.Name.Should().Be("critic");
        prompt.Messages[1].Role.IsBuiltIn.Should().BeFalse();
        prompt.Messages[2].Role.Name.Should().Be("defender");
        ((AssistantMessage)prompt.Messages[3]).Name.Should().Be("moderator-bot");

        // Providers should warn about custom roles but still be valid
        var openAi = new OpenAiAdapter().Validate(prompt);
        openAi.IsValid.Should().BeTrue();
        openAi.Warnings.Should().Contain(w => w.Contains("critic") || w.Contains("Custom role"));
    }

    /// <summary>
    /// README: Grounded answer with citations + structured JSON response format.
    /// </summary>
    [Fact]
    public void Sample_GroundedAnswer_CitationsAndJsonSchema()
    {
        var grounded = AssistantMessage.CreateDetailed(
            content: "Paris is the capital of France.",
            name: "research-agent",
            reasoning: "Confirm capital from atlas entry.",
            annotations: new[]
            {
                MessageAnnotation.UrlCitation(
                    url: "https://example.com/france",
                    title: "Atlas",
                    quote: "Paris…",
                    startIndex: 0,
                    endIndex: 5),
                MessageAnnotation.FileCitation("file_notes", title: "Briefing")
            });

        var prompt = PromptBuilder.Create()
            .AddSystem("Answer with sources when possible.")
            .AddUser("Capital of France?")
            .AddMessage(grounded)
            .WithResponseFormat(OutputFormat.JsonWithSchema("""
                {
                  "type": "object",
                  "properties": {
                    "answer": { "type": "string" },
                    "sources": { "type": "array", "items": { "type": "string" } }
                  }
                }
                """))
            .Build();

        grounded.Annotations.Should().HaveCount(2);
        prompt.ResponseFormat!.Schema.Should().Contain("sources");

        var openAiJson = new OpenAiAdapter().ToJson(prompt);
        openAiJson.Should().Contain("response_format");
        // Reasoning must not leak into OpenAI content by default
        openAiJson.Should().NotContain("Confirm capital from atlas entry.");
    }

    /// <summary>
    /// README: Cached system policy block (Anthropic-style ephemeral hint).
    /// </summary>
    [Fact]
    public void Sample_CachedSystemPolicy_EphemeralCacheControl()
    {
        var cachedSystem = SystemMessage.Create(new IContentPart[]
        {
            TextPart.Create(
                "Long, stable safety policy that rarely changes…",
                CacheControl.Ephemeral(TimeSpan.FromMinutes(30)))
        });

        var prompt = PromptBuilder.Create()
            .AddMessage(cachedSystem)
            .AddUser("Is this allowed?")
            .Build();

        prompt.Messages[0].Parts[0].CacheControl!.Type.Should().Be("ephemeral");

        var warnings = new OpenAiAdapter().Validate(prompt).Warnings;
        warnings.Should().Contain(w => w.Contains("CacheControl"));
    }

    /// <summary>
    /// README: Computer-use turn — screenshot + action parts.
    /// </summary>
    [Fact]
    public void Sample_ComputerUse_ScreenshotAndClick()
    {
        var turn = UserMessage.Create(new IContentPart[]
        {
            TextPart.Create("Click the Submit button."),
            ScreenshotPart.FromUrl("https://example.com/screen.png"),
            ComputerActionPart.Create("click", "{\"x\":120,\"y\":80}")
        });

        var prompt = PromptBuilder.Create()
            .AddSystem("You control a browser via computer-use actions.")
            .AddMessage(turn)
            .Build();

        prompt.LastUserMessage!.Parts.Should().HaveCount(3);
        prompt.LastUserMessage.Parts.OfType<ComputerActionPart>().Single().Action.Should().Be("click");

        // Round-trip preserves agent-protocol parts
        var restored = (Models.Prompt)_serializer.DeserializePrompt(_serializer.Serialize(prompt));
        restored.LastUserMessage!.Parts.OfType<ScreenshotPart>().Should().ContainSingle();
        restored.LastUserMessage.Parts.OfType<ComputerActionPart>().Single()
            .ArgumentsJson.Should().Contain("120");
    }

    /// <summary>
    /// README: Persist → reload preserves rich fields.
    /// </summary>
    [Fact]
    public void Sample_SerializeReload_PreservesRichFields()
    {
        var original = PromptBuilder.Create()
            .AddSystem("Vision + tools assistant")
            .AddUserWithImage("Describe", "https://example.com/a.png")
            .AddAssistantToolCalls(new[] { ToolCall.Create("c1", "lookup", "{}") })
            .AddTool("c1", "{\"ok\":true}")
            .WithResponseFormat(OutputFormat.Json())
            .WithName("demo-prompt")
            .Build();

        var json = _serializer.Serialize(original);
        var again = _serializer.DeserializePrompt(json);

        again.Metadata.Name.Should().Be("demo-prompt");
        again.ResponseFormat.Should().NotBeNull();
        again.Messages[1].Parts.OfType<ImagePart>().Should().ContainSingle();
        ((AssistantMessage)again.Messages[2]).ToolCalls[0].Name.Should().Be("lookup");
        ((ToolMessage)again.Messages[3]).ToolCallId.Should().Be("c1");
    }

    /// <summary>
    /// Full pipeline: build → validate → adapt (OpenAI / Anthropic / Ollama) → serialize.
    /// </summary>
    [Fact]
    public void Sample_FullPipeline_ValidateAdaptSerialize()
    {
        var prompt = PromptBuilder.Create()
            .AddSystem("You are concise.")
            .AddDeveloper("Prefer bullet points.")
            .AddUser("List three benefits of immutability.")
            .WithResponseFormat(OutputFormat.Markdown())
            .Build();

        _validator.Validate(prompt).IsValid.Should().BeTrue();

        new OpenAiAdapter().ToJson(prompt).Should().Contain("messages");
        new AnthropicAdapter().ToJson(prompt).Should().Contain("messages");
        new OllamaAdapter().ToJson(prompt).Should().Contain("messages");

        var roundTrip = _serializer.DeserializePrompt(_serializer.Serialize(prompt));
        roundTrip.MessageCount.Should().Be(3);
        roundTrip.Messages[1].Role.Should().Be(MessageRole.Developer);
    }

    /// <summary>
    /// Few-shot + history builder patterns from README Prompt Builder section.
    /// </summary>
    [Fact]
    public void Sample_FewShotAndHistory()
    {
        var fewShot = PromptBuilder
            .System("You are a sentiment analyzer. Respond with: positive, negative, or neutral.")
            .AddExample("I love this product!", "positive")
            .AddExample("This is terrible.", "negative")
            .AddUser("The customer service was outstanding!")
            .Build();

        // system + 2 examples (user+assistant each) + final user = 1 + 4 + 1 = 6
        fewShot.MessageCount.Should().Be(6);
        fewShot.LastUserMessage!.Content.Should().Contain("outstanding");

        var withHistory = PromptBuilder
            .System("You are a helpful assistant.")
            .AddHistory(new[]
            {
                ("What is C#?", "C# is a modern language."),
                ("How do I create a class?", "Use the class keyword.")
            })
            .AddUser("Now explain interfaces.")
            .Build();

        withHistory.MessageCount.Should().Be(6); // system + 2 pairs + user
        withHistory.LastUserMessage!.Content.Should().Contain("interfaces");
    }

    /// <summary>
    /// Conversation helper → prompt / windowed prompt.
    /// </summary>
    [Fact]
    public void Sample_Conversation_ToPromptAndWindow()
    {
        var conversation = Conversation.Create("Coding Help")
            .WithSystem("You are a helpful coding assistant.")
            .AddUser("How do I read a file in C#?")
            .AddAssistant("Use File.ReadAllText or StreamReader.")
            .AddUser("What about async?")
            .AddAssistant("Use File.ReadAllTextAsync.");

        var full = conversation.ToPrompt();
        full.MessageCount.Should().Be(5); // system + 4 turns

        // Sliding window: system + last 2 of [u, a, u, a] => [sys, u, a]
        var windowed = conversation.ToPromptWithWindow(2);
        windowed.MessageCount.Should().Be(3);
        windowed.LastUserMessage!.Content.Should().Contain("async");
        windowed.Messages[^1].Role.Should().Be(MessageRole.Assistant);
    }

    /// <summary>
    /// README: RAG catalog template + file-attached Q&A.
    /// </summary>
    [Fact]
    public void Sample_RagTemplate_And_FileAttachment()
    {
        var rag = PromptBuilder
            .Use(UserTemplates.Rag)
            .With("documents", "Refunds within 30 days with receipt.")
            .With("question", "What is the refund policy?")
            .Build();

        rag.MessageCount.Should().Be(1);
        rag.Messages[0].Role.Should().Be(MessageRole.User);
        rag.Messages[0].Content.Should().Contain("30 days");
        rag.Messages[0].Content.Should().Contain("refund policy");

        var withFile = PromptBuilder.Create()
            .AddSystem("Answer only from the attached document.")
            .AddUser(new IContentPart[]
            {
                TextPart.Create("What is the refund policy?"),
                FilePart.FromId("file_abc123", filename: "policy.pdf")
            })
            .Build();

        withFile.LastUserMessage!.Parts.OfType<FilePart>().Single().Filename.Should().Be("policy.pdf");
    }

    /// <summary>
    /// README / AGENT.md: AgentSpec build → OpenAI JSON → tool follow-up.
    /// </summary>
    [Fact]
    public void Sample_AgentSpec_WeatherToolLoop_PromptOnly()
    {
        var conversation = Conversation.Create("weather-session");

        var agent = AgentSpec.Create("weather")
            .WithInstructions("You are a weather agent. Call get_weather when needed.")
            .WithTools("get_weather", "get_forecast")
            .WithMemory(conversation, window: 20)
            .WithResponseFormat(OutputFormat.Json());

        var first = agent.BuildPrompt("What's the weather in NYC?");
        first.SystemMessage!.Content.Should().Contain("get_weather");
        first.LastUserMessage!.Content.Should().Contain("NYC");
        first.ResponseFormat.Should().NotBeNull();

        var openAiJson = new OpenAiAdapter().ToJson(first);
        openAiJson.Should().Contain("messages");
        openAiJson.Should().Contain("response_format");

        var assistantTurn = AssistantMessage.CreateWithToolCalls(
            new[] { ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}") },
            content: "Checking…");

        var second = agent.BuildPromptWithToolResults(
            "What's the weather in NYC?",
            assistantTurn,
            new[] { ("call_1", "{\"temp_f\":72,\"conditions\":\"clear\"}") });

        second.Messages.OfType<AssistantMessage>().Should().ContainSingle(a => a.ToolCalls.Count == 1);
        second.Messages.OfType<ToolMessage>().Should().ContainSingle(t => t.Content.Contains("72"));
        new PromptValidator().Validate(second).IsValid.Should().BeTrue();
    }

    /// <summary>
    /// AGENT.md: WithTools advertises names; the invoker switches on ToolCall.Name for each call.
    /// </summary>
    [Fact]
    public async Task Sample_AgentSpec_MultipleTools_InvokerDispatchesByName()
    {
        var invoker = new WeatherToolInvoker();
        var agent = AgentSpec.Create("weather")
            .WithInstructions("Call get_weather and get_forecast when needed.")
            .WithTools("get_weather", "get_forecast")
            .WithToolInvoker(invoker);

        agent.BuildPrompt("NYC weather and forecast").SystemMessage!.Content
            .Should().Contain("get_weather").And.Contain("get_forecast");

        var assistantTurn = AssistantMessage.CreateWithToolCalls(
            new[]
            {
                ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}"),
                ToolCall.Create("call_2", "get_forecast", "{\"city\":\"NYC\",\"days\":2}")
            });

        var results = new List<(string ToolCallId, string Result)>();
        foreach (var call in assistantTurn.ToolCalls)
        {
            var json = await invoker.InvokeAsync(call.Name, call.ArgumentsJson);
            results.Add((call.Id, json));
        }

        results.Should().HaveCount(2);
        results[0].Result.Should().Contain("temp_f");
        results[1].Result.Should().Contain("days");
        (await invoker.InvokeAsync("nope", "{}")).Should().Contain("unknown tool");

        var followUp = agent.BuildPromptWithToolResults(
            "Weather and a 2-day forecast for NYC?",
            assistantTurn,
            results);

        followUp.Messages.OfType<AssistantMessage>().Should().ContainSingle(a => a.ToolCalls.Count == 2);
        followUp.Messages.OfType<ToolMessage>().Should().HaveCount(2);
        followUp.Messages.OfType<ToolMessage>().Select(t => t.ToolCallId)
            .Should().Equal("call_1", "call_2");
    }

    /// <summary>
    /// README / AGENT.md: AgentCrew multi-agent turn.
    /// </summary>
    [Fact]
    public void Sample_AgentCrew_DocsDebate()
    {
        var crewPrompt = AgentCrew.Create("docs")
            .WithOrchestratorInstructions("Keep turns short.")
            .AddMember("writer", "Draft a paragraph on DI.", displayName: "writer-bot")
            .AddMember("critic", "Challenge weak claims.")
            .BuildTurn("Summarize dependency injection.");

        crewPrompt.SystemMessage!.Content.Should().Contain("writer");
        crewPrompt.Messages.Should().Contain(m => m.Role.Name == "writer" && m.Name == "writer-bot");
        crewPrompt.Messages.Should().Contain(m => m.Role.Name == "critic");
        crewPrompt.LastUserMessage!.Content.Should().Contain("dependency injection");

        var withContributions = AgentCrew.Create("docs")
            .AddMember("writer", "You write.", "w1")
            .AddMember("critic", "You critique.")
            .BuildTurn("Who won?", new[]
            {
                ("writer", "DI is clearer for tests."),
                ("critic", "Service locator still fits plugins.")
            });

        withContributions.Messages.Should().Contain(m => m.Content.Contains("clearer"));
        withContributions.Messages.Should().Contain(m => m.Content.Contains("plugins"));
    }

    /// <summary>
    /// AGENT.md: host retriever folded into BuildPrompt; chat client stored unused.
    /// </summary>
    [Fact]
    public void Sample_AgentSpec_HostRetriever_And_Serialize()
    {
        var agent = AgentSpec.Create("support")
            .WithInstructions("Answer only from retrieved context.")
            .WithRetriever(new InlineRetriever("Refunds within 30 days."))
            .WithChatClient(new NoopChatClient());

        var prompt = agent.BuildPrompt("What is the refund policy?");
        prompt.Messages.Should().Contain(m =>
            m.Role == MessageRole.Developer && m.Content.Contains("30 days"));

        agent.ChatClient.Should().NotBeNull();

        var serializer = new PromptSerializer();
        var restored = serializer.DeserializePrompt(serializer.Serialize(prompt));
        restored.Messages.Should().Contain(m => m.Content.Contains("refund policy"));
    }

    private sealed class InlineRetriever : IAgentRetriever
    {
        private readonly string _text;
        public InlineRetriever(string text) => _text = text;
        public AgentRetrievalResult Retrieve(string query) => AgentRetrievalResult.FromText(_text);
    }

    private sealed class NoopChatClient : IAgentChatClient
    {
        public Task<IMessage> CompleteAsync(IPrompt prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult<IMessage>(AssistantMessage.Create("noop"));
    }

    private sealed class WeatherToolInvoker : IAgentToolInvoker
    {
        public Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken ct = default) =>
            Task.FromResult(toolName switch
            {
                "get_weather" => "{\"temp_f\":72,\"conditions\":\"clear\"}",
                "get_forecast" => "{\"days\":[{\"day\":\"Mon\",\"high_f\":70},{\"day\":\"Tue\",\"high_f\":68}]}",
                _ => "{\"error\":\"unknown tool\"}"
            });
    }
}
