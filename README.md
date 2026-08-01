# SaaFarr.Prompt

A strongly typed, fluent prompt engineering library for .NET applications. Build, validate, and render prompts as first-class software assets.

## Why SaaFarr.Prompt?

Most prompt libraries treat prompts as strings. This works for small projects, but quickly becomes unmanageable:

- Prompts are duplicated across files
- Variables are inconsistent
- No validation catches missing placeholders
- Prompts cannot be versioned or tested
- Sharing prompts across projects is painful

**SaaFarr.Prompt** treats prompts as structured, reusable objects. Just as ASP.NET treats routes as objects and EF treats tables as models, this library treats prompts as domain objects with validation, templates, and composition.

## Installation

```bash
dotnet add package SaaFarr.Prompt
```

## Quick Start

```csharp
using SaaFarr.Prompt.Builder;
using SaaFarr.Prompt.Catalog;

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
```

## Core Concepts

### Messages

Everything starts with messages. A message represents a single unit of communication sent to an LLM.

```csharp
using SaaFarr.Prompt.Messages;

var system = SystemMessage.Create("You are a helpful assistant.");
var user = UserMessage.Create("How do I read a file in C#?");
var assistant = AssistantMessage.Create("You can use File.ReadAllText()...");
var developer = DeveloperMessage.Create("Always respond in Markdown.");
```

Messages are **immutable**. Once created, they never change. This eliminates concurrency bugs and makes prompts safe to share across threads.

### Message Roles

Roles are modeled as an abstract class with sealed subclasses, preventing arbitrary extension while providing type safety:

```csharp
using SaaFarr.Prompt.Roles;

MessageRole role = MessageRole.System;    // Highest priority - defines AI behavior
MessageRole role = MessageRole.Developer; // Framework-level instructions
MessageRole role = MessageRole.User;      // Human input
MessageRole role = MessageRole.Assistant; // Previous AI responses
MessageRole role = MessageRole.Tool;      // Tool output in agentic workflows
MessageRole role = MessageRole.Function;  // Function call responses

// Parse from string (e.g., from API response)
var role = MessageRole.Parse("system");

// Get all roles
var allRoles = MessageRole.All;
```

### Templates

Templates make messages reusable. Use `{{variableName}}` placeholders:

```csharp
using SaaFarr.Prompt.Templates;

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
using SaaFarr.Prompt.Builder;

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
using SaaFarr.Prompt.Models;

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
using SaaFarr.Prompt.Models;

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
```

### Validation

Validate prompts before sending:

```csharp
using SaaFarr.Prompt.Validation;

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
using SaaFarr.Prompt.Serialization;

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
using SaaFarr.Prompt.Rendering;

var renderer = new GenericPromptRenderer();
string output = renderer.Render(prompt);
// Produces: { "messages": [{ "role": "system", "content": "..." }, ...] }
```

### Prompt Chains

For multi-step AI workflows:

```csharp
using SaaFarr.Prompt.Models;

var chain = PromptChain.Create("Document Generation")
    .Add(PromptBuilder.Quick("You are a planner.", "Create an outline for: {{topic}}"))
    .Add(PromptBuilder.Quick("You are a writer.", "Write content based on this outline: {{outline}}"))
    .Add(PromptBuilder.Quick("You are a reviewer.", "Review and improve: {{draft}}"));
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

## Architecture

```
Provider Integrations (future packages)
        |
  Fluent Builder
        |
   Prompt Model
        |
  Template System
        |
  Message Model (foundation)
```

Each layer depends only on the one below it. The builder composes but does not create. Validation, rendering, and serialization are separate responsibilities.

## Design Decisions

- **Abstract class for MessageRole** (not enum): Sealed subclasses prevent arbitrary extension while allowing behavior on each role.
- **Immutable messages**: Thread-safe by design. No concurrency bugs.
- **Templates with auto-discovery**: Variables are found automatically from `{{name}}` patterns.
- **Provider-agnostic core**: The core library has zero provider dependencies. Adapters are separate packages.
- **Multi-target**: Supports netstandard2.0 through net10.0 for maximum compatibility.

## Roadmap

- [ ] Additional templates (Legal, Medical, Finance, Marketing)
- [ ] Prompt versioning and migration
- [ ] Prompt caching with TTL
- [ ] Localization support (i18n)
- [ ] SaaFarr.Prompt.OpenAI adapter package
- [ ] SaaFarr.Prompt.Anthropic adapter package
- [ ] SaaFarr.Prompt.Ollama adapter package
- [ ] Prompt analytics and telemetry
- [ ] Visual prompt designer integration
- [ ] BenchmarkDotNet performance suite

## Contributing

Contributions are welcome. Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License. See [LICENSE](LICENSE) for details.
