# Mima.AI.Prompt

[![CI](https://github.com/johnsonmima/Mima.AI.Prompt/actions/workflows/ci.yml/badge.svg)](https://github.com/johnsonmima/Mima.AI.Prompt/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Mima.AI.Prompt.svg)](https://www.nuget.org/packages/Mima.AI.Prompt)

A strongly typed, fluent **prompt engineering** library for .NET. Build, validate, serialize, and map prompts to provider JSON.

**This package does not call models or run tools.** `AgentSpec` / `AgentCrew` are prompt builders. Pair them with your own HTTP client, OpenAI/Anthropic SDK, or `Microsoft.Extensions.AI`.

**Repository:** https://github.com/johnsonmima/Mima.AI.Prompt  
**Agent-shaped prompts:** [AGENT.md](AGENT.md)

## What this is (and is not)

| This library **does** | This library **does not** |
|---|---|
| Typed messages, templates, `{{variables}}`, validation, versioning | HTTP / SDK calls to OpenAI, Anthropic, Ollama, Azure |
| `PromptBuilder` and `AgentSpec` → a `Prompt` you can test and serialize | An agent loop (`RunAsync` that talks to a model) |
| Format adapters (`ToJson`) for chat `messages` (+ `response_format` when set) | Emit a provider `tools` / `functions` array |
| Store `ToolCall` / `ToolMessage` in the transcript | Execute C# tools or register delegates |
| Fold tool **names** into system text (`WithTools`) | Bind those names to methods |

If you want Semantic Kernel / LangChain-style “install and run an agent,” this is the wrong package. If you want prompts as domain objects and you already own the wire, this is the right one.

## Why Mima.AI.Prompt?

Most prompt libraries treat prompts as strings. This works for small projects, but quickly becomes unmanageable:

- Prompts are duplicated across files
- Variables are inconsistent
- No validation catches missing placeholders
- Prompts cannot be versioned or tested
- Sharing prompts across projects is painful

**Mima.AI.Prompt** treats prompts as structured, reusable objects. Just as ASP.NET treats routes as objects and EF treats tables as models, this library treats prompts as domain objects with validation, templates, and composition.

## Installation

```bash
dotnet add package Mima.AI.Prompt
```

## Quick Start

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Catalog;
using Mima.AI.Prompt.Agents;

// Simple prompt
var prompt = PromptBuilder
    .System("You are a helpful assistant.")
    .AddUser("Explain dependency injection.")
    .Build();

// Using built-in templates
var prompt = PromptBuilder
    .Use(SystemTemplates.CodeReviewer)
    .AddUser("Review this code for security issues:\n```csharp\nvar query = $\"SELECT * FROM users WHERE id = {id}\";\n```")
    .Build();

// Using templates with variables
var prompt = PromptBuilder
    .Use(SystemTemplates.Configurable)
    .With("profession", "Teacher")
    .With("tone", "Friendly")
    .With("maxWords", "200")
    .AddUser("Explain generics in C#")
    .Build();

// Agent-shaped prompt (still returns a normal Prompt — details in AGENT.md)
var agentPrompt = AgentSpec.Create("helper")
    .WithInstructions("You are a concise assistant.")
    .BuildPrompt("Explain dependency injection in one paragraph.");
```

## Core Concepts

### Messages

Everything starts with messages. A message represents a single unit of communication sent to an LLM.

```csharp
using Mima.AI.Prompt.Messages;

var system = SystemMessage.Create("You are a helpful assistant.");
var user = UserMessage.Create("How do I read a file in C#?");
var assistant = AssistantMessage.Create("You can use File.ReadAllText()...");
var developer = DeveloperMessage.Create("Always respond in Markdown.");
```

Messages are **immutable**. Once created, they never change. This eliminates concurrency bugs and makes prompts safe to share across threads.

### Rich message content

Every message is parts-first. `Parts` is the body; `Content` is the concatenated text of all `TextPart`s (handy for logging and templates). String factories (`UserMessage.Create("…")`, `AddUser("…")`) are shorthand for a single text part.

| Property | Purpose |
|----------|---------|
| `Parts` | Canonical body: text, image, file, audio, video, thinking, computer-use |
| `Content` | Text projection of `TextPart`s (empty when image-/tool-/refusal-only) |
| `Name` | Optional speaker / agent id (multi-agent transcripts) |
| `Annotations` | Citations / grounding |
| `CacheControl` | Prompt-cache hints (e.g. Anthropic ephemeral) |

#### 1) Multimodal user input (text + image / file)

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;

// Shortcut: text + image URL
var vision = PromptBuilder.Create()
    .AddSystem("You are a vision assistant. Describe images accurately.")
    .AddUserWithImage(
        text: "What objects are in this photo?",
        imageUrl: "https://example.com/photo.png")
    .Build();

// Full control: mix any parts
var withPdf = PromptBuilder.Create()
    .AddUser(new IContentPart[]
    {
        TextPart.Create("Summarize this document."),
        FilePart.FromId("file_abc123", filename: "spec.pdf"),
        // or: ImagePart.FromBase64(bytes, "image/png", detail: "high")
    })
    .Build();

// Content is the text projection of TextParts — images/files live only in Parts
Console.WriteLine(withPdf.Messages[0].Content);           // "Summarize this document."
Console.WriteLine(withPdf.Messages[0].Parts.Count);       // 2
```

#### 2) Assistant tool calls (outbound) + tool results

The assistant **asks** for tools via `ToolCall`; the tool **answers** via `ToolMessage` using the **same id**.

You own the tool’s parameter schema, C# implementation, and return JSON. This library only stores the transcript (`ToolCall` + `ToolMessage`). Details: [AGENT.md — tool loop](AGENT.md#tool-using-agent-step-by-step).

```csharp
using Mima.AI.Prompt.Models;

var weather = PromptBuilder.Create()
    .AddSystem("You can call tools when needed.")
    .AddUser("What's the weather in NYC?")
    // Typically parsed from the model API's tool_calls array
    .AddAssistantToolCalls(
        toolCalls: new[]
        {
            ToolCall.Create(
                id: "call_1",
                name: "get_weather",
                argumentsJson: "{\"city\":\"NYC\"}")  // string JSON — you deserialize in C#
        },
        content: "I'll check that.")
    // Your tool returned this string; id must match call_1
    .AddTool("call_1", "{\"temp_f\":72,\"conditions\":\"clear\"}")
    .Build();

var assistant = (AssistantMessage)weather.Messages[2];
Console.WriteLine(assistant.ToolCalls[0].Name); // get_weather
```

> Prefer `ToolCall` + `ToolMessage` for new code. `FunctionMessage` remains for legacy OpenAI function results.

#### 3) Reasoning / thinking and refusals

```csharp
// Store extended thinking separately from the user-visible answer
var answered = AssistantMessage.CreateDetailed(
    content: "The capital is Paris.",
    reasoning: "France's capital is Paris; confirm against atlas…");

Console.WriteLine(answered.Content);    // "The capital is Paris."
Console.WriteLine(answered.Reasoning);  // thinking text
// Parts also include a ThinkingPart when reasoning is set

// Refusal channel (no normal answer body required)
var refused = AssistantMessage.CreateRefusal("I can't help with that request.");
Console.WriteLine(refused.Refusal);
```

OpenAI adapter **omits** reasoning/thinking from the wire payload by default and emits a validation warning so you do not accidentally leak chain-of-thought.

#### 4) Citations, speaker name, and cache control

```csharp
using Mima.AI.Prompt.Models;

var grounded = AssistantMessage.CreateDetailed(
    content: "Paris is the capital of France.",
    name: "research-agent",   // multi-agent speaker identity
    annotations: new[]
    {
        MessageAnnotation.UrlCitation(
            url: "https://example.com/france",
            title: "Atlas",
            quote: "Paris…",
            startIndex: 0,
            endIndex: 5),
        MessageAnnotation.FileCitation("file_notes", title: "Briefing")
    },
    cacheControl: CacheControl.Ephemeral(TimeSpan.FromMinutes(5)));

var prompt = PromptBuilder.Create()
    .AddSystem("Answer with sources when possible.")
    .AddUser("Capital of France?")
    .AddMessage(grounded)
    .Build();
```

Cached system blocks (provider-specific; Anthropic-style):

```csharp
var cachedSystem = SystemMessage.Create(new IContentPart[]
{
    TextPart.Create(
        "Long, stable safety policy…",
        CacheControl.Ephemeral())
});
```

#### 5) Structured response format

Ask the provider for JSON (or a schema) at the **prompt** level:

```csharp
var jsonPrompt = PromptBuilder.Create()
    .AddSystem("Extract entities as JSON.")
    .AddUser("Alice met Bob in Paris.")
    .WithResponseFormat(OutputFormat.JsonWithSchema("""
        {
          "type": "object",
          "properties": {
            "people": { "type": "array", "items": { "type": "string" } },
            "places": { "type": "array", "items": { "type": "string" } }
          }
        }
        """))
    .Build();

// OpenAI adapter maps this to response_format in ToJson / ToProviderFormat
var openAi = new Mima.AI.Prompt.Providers.OpenAiAdapter();
string payload = openAi.ToJson(jsonPrompt);
```

#### 6) Computer-use / UI agent parts

Use these when the model should **see a screen and propose UI actions** (click, type, scroll).  
This library only models the **message parts**. Your app still owns the browser / desktop automation that takes screenshots and performs actions.

**The three parts work together:**

| Part | Meaning | Who produces it |
|------|---------|-----------------|
| `TextPart` | Goal or instruction in words (“Submit the form”) | You / the user |
| `ScreenshotPart` | What the UI looks like right now | Your automation (capture → URL or base64) |
| `ComputerActionPart` | A proposed (or last executed) action + JSON args | Model proposes → you execute → you may echo it back |

Typical loop:

```text
1. Capture screenshot
2. Build user message: text + ScreenshotPart (+ optional last action)
3. Send prompt → model returns next ComputerActionPart (or text “done”)
4. Your runner executes click/type/scroll
5. Capture a new screenshot → go to 2
```

**Basic observation turn** (what the model sees):

```csharp
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;

// Goal + current frame. Action may be omitted on the first turn
// (the model will invent the next ComputerActionPart in its reply).
var observe = UserMessage.Create(new IContentPart[]
{
    TextPart.Create("Click the Submit button near the bottom of the form."),
    ScreenshotPart.FromUrl("https://example.com/screen.png")
});
```

**After you executed an action**, include it so the model knows what already happened:

```csharp
var afterClick = UserMessage.Create(new IContentPart[]
{
    TextPart.Create("Continue until the form is submitted. Report when done."),
    ScreenshotPart.FromUrl("https://example.com/screen-after-click.png"),
    // Echo the action you just ran (protocol / transcript), not “run this again”
    ComputerActionPart.Create("click", "{\"x\":120,\"y\":80}")
});
```

**Common actions** (action name is a string; args are your JSON convention):

```csharp
ComputerActionPart.Create("click",  "{\"x\":120,\"y\":80}");
ComputerActionPart.Create("type",   "{\"text\":\"user@example.com\"}");
ComputerActionPart.Create("scroll", "{\"dy\":400}");
ComputerActionPart.Create("key",    "{\"key\":\"Enter\"}");
ComputerActionPart.Create("wait",   "{\"ms\":500}");
```

**Screenshots from bytes** (no public URL):

```csharp
ScreenshotPart.FromBase64(pngBase64, mediaType: "image/png");
ScreenshotPart.FromBase64(jpegBase64, mediaType: "image/jpeg");
```

**Full prompt with system policy:**

```csharp
var prompt = PromptBuilder.Create()
    .AddSystem("""
        You control a browser via computer_action parts.
        Propose exactly one action per turn.
        When the goal is complete, reply with plain text only (no action).
        """)
    .AddUser(new IContentPart[]
    {
        TextPart.Create("Log in with user@example.com / secret, then submit."),
        ScreenshotPart.FromBase64(currentFramePng, "image/png")
    })
    .Build();
```

`Content` on that user message is only the text projection (“Log in with…”). The screenshot and actions live in `Parts`.

More detail and multi-turn sketches: [AGENT.md — Computer-use agents](AGENT.md#computer-use-agents-see-the-screen-act-on-the-ui).

#### 7) Persist and reload

`PromptSerializer` round-trips parts, tool calls, annotations, cache controls, names, and response format:

```csharp
using Mima.AI.Prompt.Serialization;

var serializer = new PromptSerializer();
string json = serializer.Serialize(vision);
var again = serializer.DeserializePrompt(json);
// again.Messages[n].Parts / ToolCalls / Annotations preserved
```

#### 8) Inline image bytes (base64)

```csharp
var fromBytes = PromptBuilder.Create()
    .AddUser(new IContentPart[]
    {
        TextPart.Create("What brand is this logo?"),
        ImagePart.FromBase64(base64Png, "image/png", detail: "high")
    })
    .Build();
```

#### 9) Multi-agent debate (custom roles + speaker names)

```csharp
var critic = MessageRole.Custom("critic");
var defender = MessageRole.Custom("defender");

var debate = PromptBuilder.Create()
    .AddSystem("You host a short technical debate.")
    .Add(critic, "Challenge: DI always beats service locator.")
    .Add(defender, "Defense: service locator is fine for plugins.")
    .AddMessage(AssistantMessage.CreateDetailed(
        content: "Prefer DI for testability; locator can fit plugin hosts.",
        name: "moderator-bot"))
    .AddUser("Summarize the consensus.")
    .Build();
```

Provider adapters warn on custom roles (many APIs only accept built-ins) but still pass the role name through.

### End-to-end samples

These mirror the scenarios covered by `EndToEndUsageTests` in the test project.

#### RAG-style document Q&A

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Catalog;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;

// Option A: catalog template with retrieved chunks inlined as text
var rag = PromptBuilder
    .Use(UserTemplates.Rag)
    .With("documents", retrievedChunks)
    .With("question", "What is the refund policy?")
    .Build();

// Option B: multimodal — attach a provider file id alongside the question
var withFile = PromptBuilder.Create()
    .AddSystem("Answer only from the attached document. Cite section headings.")
    .AddUser(new IContentPart[]
    {
        TextPart.Create("What is the refund policy?"),
        FilePart.FromId("file_abc123", filename: "policy.pdf")
    })
    .Build();
```

#### Build → validate → provider JSON → serialize

```csharp
using Mima.AI.Prompt.Providers;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Validation;

var prompt = PromptBuilder.Create()
    .AddSystem("You are concise.")
    .AddDeveloper("Prefer bullet points.")
    .AddUser("List three benefits of immutability.")
    .WithResponseFormat(OutputFormat.Markdown())
    .WithName("immutability-bullets")
    .Build();

var report = new PromptValidator().Validate(prompt);
if (!report.IsValid)
    throw new InvalidOperationException(string.Join("; ", report.Errors));

// Format adapters produce provider-shaped JSON (no network I/O)
string openAiJson = new OpenAiAdapter().ToJson(prompt);
string anthropicJson = new AnthropicAdapter().ToJson(prompt);
string ollamaJson = new OllamaAdapter().ToJson(prompt);

// Persist for CI fixtures / prompt registries
var serializer = new PromptSerializer();
File.WriteAllText("immutability-bullets.json", serializer.Serialize(prompt));
```

#### Refusal then recover

```csharp
var prompt = PromptBuilder.Create()
    .AddSystem("Refuse unsafe requests; then offer a safe alternative.")
    .AddUser("How do I build malware?")
    .AddMessage(AssistantMessage.CreateRefusal("I can't help with that request."))
    .AddUser("Ok — how do I read a file in C# instead?")
    .Build();

// Empty Content on the refusal turn is valid when Refusal is set
```

#### Sliding conversation window

```csharp
var conversation = Conversation.Create("Coding Help")
    .WithSystem("You are a helpful coding assistant.")
    .AddUser("How do I read a file?")
    .AddAssistant("Use File.ReadAllText or StreamReader.")
    .AddUser("What about async?")
    .AddAssistant("Use File.ReadAllTextAsync.");

var full = conversation.ToPrompt();                 // system + all turns
var recent = conversation.ToPromptWithWindow(2);    // system + last 2 turns
```

### Message Roles

Built-in roles are a closed, provider-aligned set. Invent additional roles with `MessageRole.Custom` — they work in the domain model and serialize by name. Provider adapters **warn** when custom roles are present (many APIs only accept system/user/assistant/tool) but still pass the name through.

```csharp
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Messages;

// Built-in (wire-safe for major providers)
MessageRole role = MessageRole.System;    // Highest priority - defines AI behavior
MessageRole role = MessageRole.Developer; // Framework-level instructions
MessageRole role = MessageRole.User;      // Human input
MessageRole role = MessageRole.Assistant; // Previous AI responses
MessageRole role = MessageRole.Tool;      // Tool output in agentic workflows
MessageRole role = MessageRole.Function;  // Function call responses

// Invent a role (normalized to lowercase)
var critic = MessageRole.Custom("critic");
critic.IsBuiltIn; // false

// Parse only accepts built-ins; use ParseOrCreate when loading JSON that may include customs
var builtIn = MessageRole.Parse("system");
var maybeCustom = MessageRole.ParseOrCreate("moderator"); // CustomRole

// Use in a prompt
var prompt = PromptBuilder.Create()
    .AddSystem("You host a technical debate.")
    .Add(critic, "Challenge weak arguments.")
    // or: .AddCustom("critic", "Challenge weak arguments.")
    .AddUser("Explain dependency injection.")
    .Build();

var allBuiltIns = MessageRole.All; // six built-ins only
```

Personas like “teacher” or “reviewer” usually belong in **system prompt text / templates**. Use a custom **role** only when you need a distinct message role in the conversation structure.

### Templates

Templates make messages reusable. Use `{{variableName}}` placeholders:

```csharp
using Mima.AI.Prompt.Templates;

var template = SystemTemplate.Create("""
    You are a {{profession}}.
    Use a {{tone}} tone.
    Limit responses to {{maxWords}} words.
    """);

// Discover variables automatically
var vars = template.Variables; // ["profession", "tone", "maxWords"]

// Validate before rendering
var validation = template.Validate(new Dictionary<string, object>
{
    ["profession"] = "Teacher"
}); // IsValid = false, MissingVariables = ["tone", "maxWords"]

// Render to a concrete message
var message = template.Render(new
{
    profession = "Teacher",
    tone = "Friendly",
    maxWords = 200
});
```

### Prompt Builder

The fluent builder composes messages into complete prompts:

```csharp
using Mima.AI.Prompt.Builder;

var prompt = PromptBuilder.Create()
    .AddSystem("You are a senior code reviewer.")
    .AddDeveloper("Always respond using Markdown.")
    .AddUser("Review this code for issues.")
    .Build();

// Few-shot examples
var prompt = PromptBuilder
    .System("You are a sentiment analyzer. Respond with: positive, negative, or neutral.")
    .AddExample("I love this product!", "positive")
    .AddExample("This is terrible.", "negative")
    .AddExample("It works fine.", "neutral")
    .AddUser("The customer service was outstanding!")
    .Build();

// Conversation history
var prompt = PromptBuilder
    .System("You are a helpful assistant.")
    .AddHistory(new[]
    {
        ("What is C#?", "C# is a modern, object-oriented programming language..."),
        ("How do I create a class?", "You can create a class using the class keyword...")
    })
    .AddUser("Now explain interfaces.")
    .Build();
```

### Conversations

For ongoing chat interactions:

```csharp
using Mima.AI.Prompt.Models;

var conversation = Conversation.Create("Coding Help")
    .WithSystem("You are a helpful coding assistant.")
    .AddUser("How do I read a file in C#?")
    .AddAssistant("You can use File.ReadAllText() or StreamReader...");

// Later, add more exchanges
conversation.AddUser("What about async?");

// Convert to a prompt
var prompt = conversation.ToPrompt();

// Or use a sliding window to keep within token limits
var prompt = conversation.ToPromptWithWindow(10); // Last 10 messages
```

### Constraints

Define what the AI must and must not do:

```csharp
using Mima.AI.Prompt.Models;

var constraints = PromptConstraints.Create()
    .MaxWords(200)
    .NoEmojis()
    .NoTables()
    .MustInclude("a conclusion")
    .MustAvoid("jargon")
    .WithFormat(OutputFormat.Markdown());

string constraintText = constraints.Render();
// Append to your system message
```

### Output Formats

Enforce structured output:

```csharp
var json = OutputFormat.Json();
var yaml = OutputFormat.Yaml();
var markdown = OutputFormat.Markdown();
var bullets = OutputFormat.BulletPoints();
var steps = OutputFormat.Steps();

// JSON with a specific schema
var schema = OutputFormat.JsonWithSchema("""
    {
      "summary": "string",
      "sentiment": "positive | negative | neutral",
          "confidence": "number (0-1)"
    }
    """);

// YAML with an example shape
var yamlSchema = OutputFormat.YamlWithSchema("""
    summary: string
    sentiment: positive | negative | neutral
    confidence: number
    """);
```

### Validation

Validate prompts before sending:

```csharp
using Mima.AI.Prompt.Validation;

var validator = new PromptValidator();
var report = validator.Validate(prompt);

if (!report.IsValid)
    Console.WriteLine($"Errors: {string.Join(", ", report.Errors)}");

if (report.HasWarnings)
    Console.WriteLine($"Warnings: {string.Join(", ", report.Warnings)}");
```

### Serialization

Save and load prompts:

```csharp
using Mima.AI.Prompt.Serialization;

var serializer = new PromptSerializer();

// Save
string json = serializer.Serialize(prompt);
File.WriteAllText("my-prompt.json", json);

// Load
string json = File.ReadAllText("my-prompt.json");
var prompt = serializer.DeserializePrompt(json);
```

### Rendering

Convert prompts to provider-specific formats:

```csharp
using Mima.AI.Prompt.Rendering;

var renderer = new GenericPromptRenderer();
string output = renderer.Render(prompt);
// Produces: { "messages": [{ "role": "system", "content": "..." }, ...] }
```

### Prompt Chains

`PromptChain` is a **named, ordered list of prompts** for a multi-step workflow (planner → writer → reviewer).

`.Add(...)` returns **`this`** (the same chain instance) — the fluent / builder pattern — so you can write `.Add(a).Add(b).Add(c)`.

The chain does **not** call the model or pipe outputs for you. Your host runs each step and feeds results into the next (e.g. fill `{{outline}}` / `{{draft}}`).

```csharp
using Mima.AI.Prompt.Models;

var chain = PromptChain.Create("Document Generation")
    .Add(PromptBuilder.Quick("You are a planner.", "Create an outline for: {{topic}}"))
    .Add(PromptBuilder.Quick("You are a writer.", "Write content based on this outline: {{outline}}"))
    .Add(PromptBuilder.Quick("You are a reviewer.", "Review and improve: {{draft}}"));

// chain.StepCount == 3
// chain[0] → first prompt, chain[1] → second, …
```

## Built-in Template Catalog

### System Templates

Out-of-the-box personas ready for production use:

| Template | Description |
|----------|-------------|
| `SystemTemplates.HelpfulAssistant` | General-purpose helpful assistant |
| `SystemTemplates.SoftwareEngineer` | Senior software engineer |
| `SystemTemplates.CodeReviewer` | Code quality reviewer |
| `SystemTemplates.TechnicalWriter` | Documentation writer |
| `SystemTemplates.Teacher` | Patient educator |
| `SystemTemplates.SqlExpert` | Database query specialist |
| `SystemTemplates.SecurityAuditor` | Security vulnerability auditor |
| `SystemTemplates.DevOpsEngineer` | CI/CD and infrastructure |
| `SystemTemplates.DataScientist` | ML and data analysis |
| `SystemTemplates.ProductManager` | Requirements and priorities |
| `SystemTemplates.CustomerSupport` | Empathetic support agent |
| `SystemTemplates.JsonGenerator` | Structured JSON output |
| `SystemTemplates.ResearchAssistant` | Academic research |
| `SystemTemplates.Architect` | System design |
| `SystemTemplates.Translator` | Language translation |
| `SystemTemplates.Configurable` | Parameterized (profession, tone, maxWords) |

### User Templates

Common request patterns:

| Template | Variables | Description |
|----------|-----------|-------------|
| `UserTemplates.Summarize` | content, style | Document summarization |
| `UserTemplates.Translate` | text, targetLanguage | Translation |
| `UserTemplates.ExtractEntities` | text, entityType | Entity extraction |
| `UserTemplates.ReviewResume` | resume, role | Resume review |
| `UserTemplates.GenerateSql` | schema, question | SQL generation |
| `UserTemplates.GenerateDocumentation` | code, language, documentationType | Code docs |
| `UserTemplates.GenerateTests` | code, language, framework | Unit test generation |
| `UserTemplates.ExplainConcept` | topic, audience | Concept explanation |
| `UserTemplates.ReviewCode` | code, language | Code review |
| `UserTemplates.ConvertToJson` | data | JSON conversion |
| `UserTemplates.GenerateEmail` | audience, tone, purpose, keyPoints | Email writing |
| `UserTemplates.Rag` | documents, question | Retrieval Augmented Generation |
| `UserTemplates.ChainOfThought` | question | Step-by-step reasoning |
| `UserTemplates.Compare` | optionA, optionB, useCase | Comparison |

## Building agents (prompt sugar, not a runtime)

**An agent here is a prompt** — instructions, memory, and tool turns packed into a `Prompt`.  
This package builds that prompt. **Your app** calls the model, runs tools, and decides when to stop. See [What this is (and is not)](#what-this-is-and-is-not).

Full walkthrough (step-by-step tool loop, crews, host hooks): **[AGENT.md](AGENT.md)**.

### 1. Define a simple agent

```csharp
using Mima.AI.Prompt.Agents;

var agent = AgentSpec.Create("helper")
    .WithInstructions("You are a concise assistant. Prefer short answers.");

var prompt = agent.BuildPrompt("Explain dependency injection in one paragraph.");
// Send `prompt` with your model client (or inspect JSON via OpenAiAdapter.ToJson)
```

### 2. Add tools and memory (and understand `tool_calls`)

Tools are **your** code. This library stores the chat shape (`ToolCall` → `ToolMessage`); it does not define a required C# tool interface or return type.

| Piece | Who owns it | Shape |
|-------|-------------|-------|
| Parameter schema | You (`IAgentToolCatalog` and/or provider `tools` array) | Usually JSON Schema |
| C# implementation | You (`IAgentToolInvoker` or plain methods) | Any — deserialize `ArgumentsJson` yourself |
| Model request | `ToolCall` | `Id`, `Name`, `ArgumentsJson` (string) |
| Model-visible result | `ToolMessage` | Same `Id` + `Content` string (usually JSON) |

`WithTools("get_weather", "get_forecast")` **advertises those names in the system prompt** so the model knows they exist. It does **not** bind C# methods, register delegates, or emit a provider `tools` array. This package builds prompts; your app runs tools.

| Call | What it does |
|------|----------------|
| `WithTools(...)` | Writes names into system text (`Available tools: …`) |
| `WithToolCatalog` | Same, plus descriptions / schemas in the prompt |
| Your SDK `tools: [...]` | Formal provider function definitions (outside this library) |
| `IAgentToolInvoker` / your `switch` | Actually run C# when a `ToolCall.Name` arrives |

Without advertising names (via `WithTools`, a catalog, or the SDK `tools` list), the model is guessing. Listing a name without an invoker branch for it means the model may still *request* that tool and your host has nothing to run.

**Who decides vs who runs:** the model never executes C#. Call 1 sends schemas so the model can **choose** `name` + `arguments` (`tool_calls` JSON). Your app **runs** the matching method, then Call 2 sends those results as context so the model can write the sentence. If your app already fetched the facts (RAG), skip `tools` / `tool_calls` and put the results in the prompt. Full write-up: [AGENT.md — How the model decides what to run](AGENT.md#how-the-model-decides-what-to-run--and-how-it-actually-runs).

```csharp
using Mima.AI.Prompt.Agents;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Providers;

var conversation = Conversation.Create("weather-session");

var agent = AgentSpec.Create("weather")
    .WithInstructions("You are a weather agent. Call get_weather when needed.")
    .WithTools("get_weather", "get_forecast")   // names in system text only — not C# methods
    .WithMemory(conversation, window: 20);

// --- Call 1: send messages (+ your SDK's `tools: [...]` definitions) ---
var prompt = agent.BuildPrompt("What's the weather in NYC?");
string openAiJson = new OpenAiAdapter().ToJson(prompt); // messages JSON — not the tools array

// Model responds with tool_calls, e.g.:
//   { "id":"call_1", "function": { "name":"get_weather", "arguments":"{\"city\":\"NYC\"}" } }
// You map that into ToolCall, then dispatch in YOUR invoker by ToolCall.Name
// (get_weather vs get_forecast). WithTools does not run either method.

var assistantTurn = AssistantMessage.CreateWithToolCalls(
    new[] { ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}") });

string resultJson = "{\"temp_f\":72,\"conditions\":\"clear\"}"; // shape YOU chose to return

// --- Call 2: assistant tool_calls + tool result, same call id ---
prompt = agent.BuildPromptWithToolResults(
    "What's the weather in NYC?",
    assistantTurn,
    new[] { ("call_1", resultJson) });
// Send `prompt` again → model usually answers in plain language
```

If the model asks for **both** tools in one turn, loop `assistantTurn.ToolCalls`, call `agent.RequireToolInvoker().InvokeAsync(call.Name, …)` for each, and pass every `(call.Id, result)` into `BuildPromptWithToolResults`. The invoker is a `toolName` switch — that is how `get_forecast` is implemented, not `WithTools`. Details: [AGENT.md — How multiple tools are addressed](AGENT.md#how-multiple-tools-are-addressed).

### 3. Multi-agent crew (several roles, one prompt)

```csharp
var crewPrompt = AgentCrew.Create("docs")
    .WithOrchestratorInstructions("Keep turns short.")
    .AddMember("writer", "Draft a paragraph on DI.", displayName: "writer-bot")
    .AddMember("critic", "Challenge weak claims.")
    .BuildTurn("Summarize dependency injection.");
```

### 4. Plug in your own RAG / HTTP / tools (optional)

Hooks let you supply what we intentionally do not ship:

| You implement | Library uses it to… |
|---------------|---------------------|
| `IAgentRetriever` | Inject retrieved context into `BuildPrompt` |
| `IAgentMemory` | Append durable history into `BuildPrompt` |
| `IAgentToolCatalog` | Add tool descriptions / schemas to system text |
| `IAgentChatClient` | *(stored)* — you call it to hit the model |
| `IAgentToolInvoker` | *(stored)* — you call it to run tools |
| `IAgentLoop` | *(stored)* — you call it for the tool while-loop |

```csharp
var agent = AgentSpec.Create("support")
    .WithInstructions("Answer only from retrieved context.")
    .WithRetriever(myRetriever)     // folded into the prompt
    .WithChatClient(myChatClient);  // you invoke: await agent.RequireChatClient().CompleteAsync(prompt)

var prompt = agent.BuildPrompt(userQuestion);
```

Prefer low-level control? The same shapes work with `PromptBuilder` alone — see [AGENT.md](AGENT.md#same-patterns-without-sugar-promptbuilder).

UI / browser agents (screenshot + click/type parts): [README § computer-use](#6-computer-use--ui-agent-parts) and the longer [AGENT.md computer-use guide](AGENT.md#computer-use-agents-see-the-screen-act-on-the-ui).

## Architecture

```
AgentSpec / AgentCrew (thin sugar)
        |
Provider adapters (OpenAI / Anthropic / Ollama — format only)
        |
  Fluent Builder
        |
   Prompt Model
        |
  Template System
        |
  Message Model (foundation)
```

Each layer depends only on the one below it. The builder composes but does not create. Validation, rendering, and serialization are separate responsibilities. Caching, versioning, localization, analytics, and agent sugar sit beside the core model as optional utilities.

## Design Decisions

- **Parts-first messages**: `Parts` is the body; `Content` is the text projection. String factories are shorthand for a single `TextPart`.
- **An agent is a prompt**: `AgentSpec` / `AgentCrew` compose prompts only; host hooks supply HTTP, tools, loops, RAG, and durable memory.
- **Built-in roles are closed; customs via factory**: Six sealed builtins (System → Function). Invent roles with `MessageRole.Custom` / `CustomMessage`.
- **Strict `Parse`, permissive `ParseOrCreate`**: `Parse` only accepts builtins; deserialization uses `ParseOrCreate` so custom roles round-trip.
- **Provider warn, pass-through**: Adapters share content mapping; warn on custom roles / unsupported features rather than silent remap.
- **Immutable messages**: Thread-safe by design once constructed.
- **Templates with auto-discovery**: Variables are found automatically from `{{name}}` patterns.
- **Provider-agnostic core**: Zero HTTP client dependencies; in-box adapters map the domain model to provider JSON shapes (no network I/O).
- **Multi-target + PolySharp**: Targets `netstandard2.0`, `net6.0`, `net8.0`, and `net10.0`. [PolySharp](https://github.com/Sergio0694/PolySharp) polyfills modern C# features (`init`, `record`, etc.) so older TFMs keep working.

## Testing

```bash
dotnet test Mima.AI.Prompt.sln -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

CI enforces ≥ 95% line coverage on the library assembly.

## Open source

| Doc | Purpose |
|-----|---------|
| [AGENT.md](AGENT.md) | Agents as prompts — patterns, `AgentSpec` / `AgentCrew`, host hooks |
| [OPEN_SOURCE.md](OPEN_SOURCE.md) | Why each open-source file exists |
| [GITHUB.md](GITHUB.md) | Branch protection, reviews, forking/PR settings |
| [PR.md](PR.md) | Step-by-step: update `main`, branch names, CI, version, open and merge a PR |
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to build, test, and open PRs |
| [SECURITY.md](SECURITY.md) | Private vulnerability reporting |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community standards |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## Roadmap

- Optional separate packages for HTTP client SDKs (keep in-box format adapters)
- Grow agent sugar into `Mima.AI.Agents` if the in-box surface expands
- Wire `PromptConstraints` / `OutputFormat` into `PromptBuilder`
- Full SemVer pre-release comparison for `PromptVersion`
- Concrete `IPromptLocalizer` implementations
- BenchmarkDotNet performance suite
- Visual prompt designer integration

## Contributing

Contributions are welcome. Please see [CONTRIBUTING.md](CONTRIBUTING.md) and the pull request procedure in [PR.md](PR.md). Repository settings for maintainers are documented in [GITHUB.md](GITHUB.md).

## License

MIT License. See [LICENSE](LICENSE) for details.
