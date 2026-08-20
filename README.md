# Mima.AI.Prompt

[![CI](https://github.com/johnsonmima/Mima.AI.Prompt/actions/workflows/ci.yml/badge.svg)](https://github.com/johnsonmima/Mima.AI.Prompt/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Mima.AI.Prompt.svg)](https://www.nuget.org/packages/Mima.AI.Prompt)

A strongly typed, fluent **prompt engineering** library for .NET. Build, validate, and serialize prompts as **canonical JSON**.

**This package composes prompts.** It does not call models, run tools, or emit a vendor chat request body. You own HTTP and the mapping to whatever API you use today.

**Repository:** https://github.com/johnsonmima/Mima.AI.Prompt

## Why Mima.AI.Prompt?

Most prompt libraries treat prompts as strings. That works for a few files and then falls apart:

- Prompts are duplicated across projects
- Template variables are inconsistent
- Missing placeholders are not caught until a model call fails
- A prompt has no version, no check before you send it, and no document you can save and load again
- Sharing a prompt catalog across apps means copy-paste

**Mima.AI.Prompt** treats prompts as structured, reusable objects — the same way ASP.NET treats routes as objects and EF treats tables as models.

Vendor chat schemas change often. The JSON this library serializes is **this domain**, so tests and storage stay stable while your host maps to OpenAI, Anthropic, Ollama, or anything else.

The samples below show how the library solves each of those problems. They use:


```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Catalog;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Localization;
using Mima.AI.Prompt.Roles;
using Mima.AI.Prompt.Serialization;
using Mima.AI.Prompt.Templates;
using Mima.AI.Prompt.Validation;
using Mima.AI.Prompt.Versioning;
```

### Define the prompt once and reuse it

Define the instruction **once** as a template (or reuse a built-in). For example both an API and a worker reference the same object instead of two slightly different or similar string literals.

```csharp
public static class AppPrompts
{
    public static readonly SystemTemplate SupportAgent = SystemTemplate.Create(
        "SupportAgent",
        "You are a support agent for {{product}}. Be concise. Never invent policy.");
}

// Web API
var apiPrompt = PromptBuilder
    .Use(AppPrompts.SupportAgent)
    .With("product", "Billing")
    .AddUser("Why was I charged twice?")
    .Build();

// Background worker — same template, different product
var jobPrompt = PromptBuilder
    .Use(AppPrompts.SupportAgent)
    .With("product", "Shipping")
    .AddUser(ticketBody)
    .Build();
```

With that user text, `apiPrompt` is two messages. `{{product}}` is already filled:

```text
system: You are a support agent for Billing. Be concise. Never invent policy.
user:   Why was I charged twice?
```

`PromptSerializer.Serialize(apiPrompt)` stores the body as **`parts`**. In C#, `message.Content` is that same text joined into one string for logging and tests; it is not a second JSON field.

```json
{
  "messages": [
    {
      "role": "system",
      "parts": [{ "type": "text", "text": "You are a support agent for Billing. Be concise. Never invent policy." }],
      "metadata": { "name": "SupportAgent" }
    },
    {
      "role": "user",
      "parts": [{ "type": "text", "text": "Why was I charged twice?" }]
    }
  ]
}
```

The shipping worker is the same JSON shape with `Billing` replaced by `Shipping`.

#### `Quick` vs a named template

Use **`PromptBuilder.Quick`** when the prompt is local and throwaway: a test, a spike, or a single screen that will not be reused. Use a **named `SystemTemplate`** (your own type or `SystemTemplates.*`) when more than one project, job, or endpoint must say the same thing — that object is the contract you share.

```csharp
Prompt oneOff = PromptBuilder.Quick("Be brief.", "Summarize this email.");
```

### Named variables, discovered from the template

Placeholders are discovered from `{{name}}`. `Variables` is the list of names the template actually expects — not a comment in a wiki.

```csharp
var template = SystemTemplate.Create(
    "You are a {{profession}}. Use a {{tone}} tone. Limit responses to {{maxWords}} words.");

// profession, tone, maxWords — always the same names
foreach (var name in template.Variables)
    Console.WriteLine(name);
// profession
// tone
// maxWords

var prompt = PromptBuilder
    .Use(SystemTemplates.Configurable) // built-in: profession, tone, maxWords
    .With("profession", "Teacher")
    .With("tone", "Friendly")
    .With("maxWords", "200")
    .AddUser("Explain generics in C#")
    .Build();
```

Rendered system text (the system turn the model would see):

```text
You are a Teacher.
Use a Friendly tone.
Limit responses to 200 words.
```

User turn: `Explain generics in C#`.

A user-turn template uses the same `{{language}}` / `{{code}}` names everywhere you review code:

```csharp
var review = UserTemplates.ReviewCode.Render(new Dictionary<string, object>
{
    ["language"] = "C#",
    ["code"] = "int x = 1;"
});
```

`review.Content`:

````text
Review the following C# code for:
- Performance issues
- Security vulnerabilities
- Maintainability concerns
- Best practice violations

```C#
int x = 1;
```
````

### Same template, more than one language

`LocalizedTemplate` is one role (system, user, assistant, or developer) with a string per locale. `{{variables}}` still work. `Render` picks the locale, fills variables, and returns a normal `IMessage`. Unknown locales fall back to `WithDefault` (then to the first locale you added). This package does not detect `Accept-Language` — you pass the locale string.

`PromptBuilder.Use` takes a single `IMessageTemplate`, not a `LocalizedTemplate`. Render first, then `AddMessage`.

```csharp
var support = LocalizedTemplate.Create(MessageRole.System)
    .AddLocale("en", "You are a support agent for {{product}}. Be concise.")
    .AddLocale("fr", "Vous êtes un agent de support pour {{product}}. Soyez concis.")
    .WithDefault("en");

string locale = "fr"; // from the user profile, Accept-Language, etc.
var system = support.Render(locale, new Dictionary<string, object>
{
    ["product"] = "Billing"
});

var prompt = PromptBuilder.Create()
    .AddMessage(system)
    .AddUser("Pourquoi ai-je été facturé deux fois ?")
    .Build();
```

System text for `fr`: `Vous êtes un agent de support pour Billing. Soyez concis.`

`support.Render("de", …)` uses English because `"de"` is missing and the default is `"en"`. `HasLocale("fr")` is true; `GetContent("fr")` is the unfilled template string.

### Catch missing placeholders before you call a model

Validate the template **before** `Build()`, or let `Build()` / `Render()` throw `PromptValidationException`.

```csharp
var template = SystemTemplate.Create("You are a {{profession}}. Product: {{product}}.");

var check = template.Validate(new Dictionary<string, object>
{
    ["profession"] = "Teacher"
    // product omitted
});

if (!check.IsValid)
{
    foreach (var missing in check.MissingVariables)
        Console.WriteLine(missing);
}
// check.IsValid == false
// MissingVariables: product
// Errors: Missing required variable: 'product'

// Throws PromptValidationException — never reaches your HTTP client
var prompt = PromptBuilder
    .Use(template)
    .With("profession", "Teacher")
    .AddUser("Hello")
    .Build();
```

That `Build()` throws `PromptValidationException` because `product` was never passed to `With()`.

`PromptValidator` then checks the assembled prompt (empty messages, missing user turn, multiple system messages, and so on):

```csharp
var report = new PromptValidator().Validate(prompt);
if (!report.IsValid)
    throw new InvalidOperationException(string.Join("; ", report.Errors));
```

### Validate, version, and persist the prompt as JSON

A `Prompt` is data you can validate, name, serialize to JSON, store, reload, and pin to a version (or roll back).

```csharp
var promptV1 = PromptBuilder
    .System("You are a helpful assistant.")
    .AddUser("Explain DI.")
    .WithName("ExplainDI")
    .Build();

new PromptValidator().Validate(promptV1); // errors/warnings before you persist

var serializer = new PromptSerializer();
string json = serializer.Serialize(promptV1);   // file, blob, test snapshot
var restored = serializer.DeserializePrompt(json);
```

`json` looks like this (`id` / timestamps omitted). `metadata.name` is `ExplainDI` from `WithName`:

```json
{
  "metadata": { "name": "ExplainDI" },
  "messages": [
    {
      "role": "system",
      "parts": [{ "type": "text", "text": "You are a helpful assistant." }]
    },
    {
      "role": "user",
      "parts": [{ "type": "text", "text": "Explain DI." }]
    }
  ]
}
```

To send this to OpenAI, Grok, Anthropic, etc., map the **`IPrompt` object** (or `DeserializePrompt(json)` if you loaded a file). See [Send a prompt to OpenAI, Grok, Anthropic, etc.](#send-a-prompt-to-openai-grok-anthropic-etc).

`new PromptValidator().Validate(promptV1)` reports `Valid` (no errors, no warnings).

This package turns a `Prompt` into a JSON string (`PromptSerializer`) and keeps a list of versions in memory (`PromptVersionHistory<T>`). It does not write files, open blobs, or talk to a database — you choose where the JSON lives (disk, git, blob storage, a table).

Typical loop:

1. `Serialize` the prompt and store that string yourself.
2. `Add` the same prompt to `PromptVersionHistory<T>` so you can pin, compare, or roll back **in this process**.
3. On the next process start, read your stored JSON, `DeserializePrompt`, and `Add` each version again. The history object is empty until you do that.

One file per version is enough:

```text
prompts/ExplainDI/1.0.0.json
prompts/ExplainDI/1.1.0.json
```

Write a version (validate → serialize → disk → register in history):

```csharp
var serializer = new PromptSerializer();
var history = PromptVersionHistory<IPrompt>.Create("ExplainDI");

void Save(PromptVersion version, IPrompt prompt, string changeLog, string author)
{
    var report = new PromptValidator().Validate(prompt);
    if (!report.IsValid)
        throw new InvalidOperationException(string.Join("; ", report.Errors));

    var dir = Path.Combine("prompts", history.AssetName);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{version}.json"), serializer.Serialize(prompt));
    history.Add(version, prompt, changeLog, author);
}

var promptV1 = PromptBuilder
    .System("You are a helpful assistant.")
    .AddUser("Explain DI.")
    .WithName("ExplainDI")
    .Build();

var promptV2 = PromptBuilder
    .System("You are a concise assistant.")
    .AddUser("Explain DI.")
    .WithName("ExplainDI")
    .Build();

Save(PromptVersion.Create(1, 0, 0), promptV1, "Initial", "platform");
Save(PromptVersion.Create(1, 1, 0), promptV2, "Shorter system text", "platform");
```

Load on process start (rebuild the history from the folder). Version numbers come from the file name. Changelog/author are **not** inside the prompt JSON — keep them in `Add(...)`, a sidecar, or a table if you need an audit trail after restart.


```csharp
var serializer = new PromptSerializer();
var history = PromptVersionHistory<IPrompt>.Create("ExplainDI");
var dir = Path.Combine("prompts", "ExplainDI");

foreach (var path in Directory.GetFiles(dir, "*.json"))
{
    var fileName = Path.GetFileNameWithoutExtension(path);
    if (string.IsNullOrEmpty(fileName))
        continue;
    var version = PromptVersion.Parse(fileName);
    var prompt = serializer.DeserializePrompt(File.ReadAllText(path));
    history.Add(version, prompt, changeLog: null);
}
```

**Use** a version when you actually send to a model. Pin production to a known good version; use latest in a staging app; roll back by loading `1.0.0` again.

```csharp
IPrompt production = history.GetContent(PromptVersion.Create(1, 0, 0))
    ?? throw new InvalidOperationException("ExplainDI 1.0.0 is missing.");

IPrompt staging = history.LatestStable?.Content
    ?? history.Latest?.Content
    ?? throw new InvalidOperationException("ExplainDI has no versions.");

// Compare two versions (A/B) — both are normal Prompt objects
IPrompt a = history.GetContent(PromptVersion.Create(1, 0, 0))
    ?? throw new InvalidOperationException("ExplainDI 1.0.0 is missing.");
IPrompt b = history.GetContent(PromptVersion.Create(1, 1, 0))
    ?? throw new InvalidOperationException("ExplainDI 1.1.0 is missing.");

foreach (var line in history.GetChangeLog(PromptVersion.Create(1, 0, 0), PromptVersion.Create(1, 1, 0)))
    Console.WriteLine(line);
// v1.0.0: Initial
// v1.1.0: Shorter system text
```

`VersionedPromptAsset<T>` is optional metadata around **one** snapshot (name, author, changelog, deprecate, `BumpMinor`). It is not a store. Keep history as the catalog of versions; bump by creating a new prompt and `Save` as above.

```csharp
var asset = VersionedPromptAsset<IPrompt>
    .Create("ExplainDI", PromptVersion.Create(1, 0, 0), promptV1)
    .WithAuthor("platform")
    .WithChangeLog("Initial release");

var next = asset.BumpMinor(promptV2, "Shorter system text");
// next.Version is 1.1.0 — still persist next.Content with PromptSerializer yourself
```

### Share a catalog as a NuGet, not copy-paste

This NuGet **is** the shared catalog: `SystemTemplates` and `UserTemplates` are the same types in every app that `PackageReference`s `Mima.AI.Prompt`. Put your own templates in a small shared class library the same way.

```csharp
// Any service that references the package
var prompt = PromptBuilder
    .Use(SystemTemplates.CodeReviewer)
    .AddMessage(UserTemplates.ReviewCode.Render(new Dictionary<string, object>
    {
        ["language"] = "C#",
        ["code"] = "int x = 1;"
    }))
    .Build();

var rag = PromptBuilder
    .Use(SystemTemplates.HelpfulAssistant)
    .AddMessage(UserTemplates.Rag.Render(new Dictionary<string, object>
    {
        ["documents"] = "Refunds take 5–7 business days.",
        ["question"] = "How long do refunds take?"
    }))
    .Build();
```

Code-review prompt — system (`SystemTemplates.CodeReviewer`) then user (`UserTemplates.ReviewCode`):

````text
system:
You are an expert code reviewer.
Review code for: performance, security, maintainability, readability, and correctness.
Provide specific suggestions with code examples.
Be constructive and explain the reasoning behind each suggestion.
Prioritize issues by severity.

user:
Review the following C# code for:
- Performance issues
- Security vulnerabilities
- Maintainability concerns
- Best practice violations

```C#
int x = 1;
```
````

RAG user turn:

```text
Context:
Refunds take 5–7 business days.

Question: How long do refunds take?

Answer the question using only the provided context. If the context does not contain enough information, say so.
```

System for RAG (`HelpfulAssistant`):

```text
You are a helpful assistant.
Be concise and accurate.
If you are unsure, say so rather than guessing.
```

Your host still sends this `Prompt` (or serializer JSON) to the model.

## What this is (and is not)

| This library **does** | This library **does not** |
|---|---|
| Typed messages, `TextPart` / `ImagePart`, templates, `{{variables}}`, validation, versioning | HTTP / SDK calls |
| `PromptBuilder` / `Conversation` → a `Prompt` you can test and serialize | An agent loop that talks to a model |
| `PromptSerializer` persist / round-trip | Vendor request JSON (`tools[]`, chat completions, Messages API, …) |
| Store `ToolCall` / `ToolMessage` on the prompt | Execute C# tools or register delegates |

If you want Semantic Kernel / LangChain-style “install and run an agent,” this is the wrong package. If you want prompts as domain objects and you already own the wire, this is the right one.

## What it supports

- **Messages:** `system`, `developer`, `user`, `assistant`, `tool`, `function`
- **Content:** `TextPart`, `ImagePart` (URL or base64 string + MIME type)
- **Composition:** `PromptBuilder`, catalog templates (`SystemTemplates` / `UserTemplates`), `Conversation`
- **Transcript:** `ToolCall`, `ToolMessage`, assistant refusal
- **Quality:** `PromptValidator`, `PromptConstraints.Render()`, `OutputFormat` on `Prompt`
- **Persistence:** `PromptSerializer` round-trip JSON for the types above
- **Versioning:** `PromptVersion`, `VersionedPromptAsset`, `PromptVersionHistory`
- **Localization:** `LocalizedTemplate` (per-locale strings, `Render` / fallback; you supply the locale)

```txt
PromptBuilder / templates / Conversation
        → Prompt
        → PromptValidator (optional)
        → PromptSerializer.Serialize / DeserializePrompt
```

Messages are **immutable**. **`Parts` is the stored body** (`text`, `image`, …). **`Content` is not a second copy of the message** — it is a C# convenience: all `text` parts concatenated, so `prompt.LastUserMessage.Content` works for logging and tests. JSON from `PromptSerializer` writes `parts` only.

## Installation

```bash
dotnet add package Mima.AI.Prompt
```

## Quick start

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Catalog;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Serialization;

var prompt = PromptBuilder
    .System("You are a helpful assistant.")
    .AddUser("Explain dependency injection.")
    .Build();

string json = new PromptSerializer().Serialize(prompt);
var restored = new PromptSerializer().DeserializePrompt(json);

var templated = PromptBuilder
    .Use(SystemTemplates.Configurable)
    .With("profession", "Teacher")
    .With("tone", "Friendly")
    .With("maxWords", "200")
    .AddUser("Explain generics in C#")
    .Build();

var vision = PromptBuilder.Create()
    .AddUserWithImage("What is in this photo?", "https://example.com/photo.png")
    .Build();

var inline = PromptBuilder.Create()
    .AddUser(new IContentPart[]
    {
        TextPart.Create("Describe this."),
        ImagePart.FromBase64(pngBase64, "image/png")
    })
    .Build();

var conversation = Conversation.Create("session")
    .WithSystem("Be brief.")
    .AddUser("Hi")
    .AddAssistant("Hello");
var fromHistory = conversation.ToPrompt();
```

Encode image bytes in the host (`Convert.ToBase64String`) and pass the string to `ImagePart.FromBase64`. Use `PromptValidator` before serialize. `PromptConstraints.Render()` produces instruction text for a system message.

Tool follow-up is part of the prompt: assistant `ToolCall`s plus `AddTool`.

## Send a prompt to OpenAI, Grok, Anthropic, etc.

**Recommended:** keep the `Prompt` object in the request path. Map `prompt.Messages` to the vendor body in **your** app, then POST with `HttpClient` or the vendor SDK. Use `PromptSerializer` JSON for files, tests, and version folders — deserialize to `IPrompt` first, then use the same mapper. This package does not call HTTP or emit vendor JSON.

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Models;

Prompt prompt = PromptBuilder
    .System("You are a helpful assistant.")
    .AddUser("Explain DI.")
    .WithName("ExplainDI")
    .Build();

// From storage:
// IPrompt prompt = new PromptSerializer().DeserializePrompt(json);
```

OpenAI and Grok share the **chat completions** `messages` array (Grok: `https://api.x.ai/v1` + your xAI key). Anthropic uses a separate `system` field and does not put system turns in `messages`. The same `IPrompt` mapper pattern works for any other chat API.

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Models;
using Mima.AI.Prompt.Roles;

static List<object> ToOpenAiCompatibleMessages(IPrompt prompt)
{
    List<object> messages = new();
    foreach (IMessage message in prompt.Messages)
    {
        messages.Add(new
        {
            role = message.Role == MessageRole.Developer ? "system" : message.Role.Name,
            content = ToOpenAiContent(message)
        });
    }

    return messages;
}

static object ToOpenAiContent(IMessage message)
{
    List<ImagePart> images = message.Parts.OfType<ImagePart>().ToList();
    if (images.Count == 0)
        return message.Content;

    List<object> blocks = new();
    foreach (IContentPart part in message.Parts)
    {
        if (part is TextPart text)
            blocks.Add(new { type = "text", text = text.Text });
        else if (part is ImagePart image)
        {
            if (image.Url is not null)
                blocks.Add(new { type = "image_url", image_url = new { url = image.Url } });
            else if (image.Base64Data is not null)
                blocks.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:{image.MediaType};base64,{image.Base64Data}" }
                });
        }
    }

    return blocks;
}

static (string? System, List<object> Messages) ToAnthropic(IPrompt prompt)
{
    string system = string.Join("\n\n", prompt.Messages
        .Where(m => m.Role == MessageRole.System || m.Role == MessageRole.Developer)
        .Select(m => m.Content));
    List<object> messages = prompt.Messages
        .Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant)
        .Select(m => (object)new { role = m.Role.Name, content = m.Content })
        .ToList();
    return (string.IsNullOrWhiteSpace(system) ? null : system, messages);
}

using HttpClient http = new();

http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
await http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", new
{
    model = "gpt-4o-mini",
    messages = ToOpenAiCompatibleMessages(prompt)
});

http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("XAI_API_KEY"));
await http.PostAsJsonAsync("https://api.x.ai/v1/chat/completions", new
{
    model = "grok-4",
    messages = ToOpenAiCompatibleMessages(prompt)
});

using HttpClient anthropic = new();
anthropic.DefaultRequestHeaders.Add("x-api-key", Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
anthropic.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
(string? system, List<object> anthropicMessages) = ToAnthropic(prompt);
await anthropic.PostAsJsonAsync("https://api.anthropic.com/v1/messages", new
{
    model = "claude-sonnet-4-0",
    max_tokens = 1024,
    system,
    messages = anthropicMessages
});
```

For the Explain DI prompt above, those POSTs send **vendor JSON** like this (not Mima `parts` JSON).

> **Disclaimer:** Provider APIs, field names, endpoints, and model IDs change. The payloads below are **illustrative** of the usual chat shape (roles + content, Anthropic’s separate `system` field). Treat them as similar examples, not a contract. Confirm against the vendor’s current docs before you ship. Mima.AI.Prompt only guarantees `IPrompt` / `parts`.

OpenAI (`POST https://api.openai.com/v1/chat/completions`):

```json
{
  "model": "gpt-4o-mini",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "Explain DI." }
  ]
}
```

Grok (`POST https://api.x.ai/v1/chat/completions`) — same `messages` array, different `model`:

```json
{
  "model": "grok-4",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "Explain DI." }
  ]
}
```

Anthropic (`POST https://api.anthropic.com/v1/messages`) — system is a top-level field:

```json
{
  "model": "claude-sonnet-4-0",
  "max_tokens": 1024,
  "system": "You are a helpful assistant.",
  "messages": [
    { "role": "user", "content": "Explain DI." }
  ]
}
```

If the user turn also has an image URL, OpenAI-compatible `content` becomes a list of blocks instead of a string:

```json
{
  "role": "user",
  "content": [
    { "type": "text", "text": "What is in this photo?" },
    { "type": "image_url", "image_url": { "url": "https://example.com/photo.png" } }
  ]
}
```

## Built-in templates

`SystemTemplates` are personas (system role). `UserTemplates` are request shapes (user role). Discover placeholders with `template.Variables`. Fill them with `.With("name", value)` before `Build()`. Templates with no variables render as-is.

```csharp
var userTurn = UserTemplates.ExplainConcept.Render(new Dictionary<string, object>
{
    ["topic"] = "generics",
    ["audience"] = "C# developers"
});

var prompt = PromptBuilder
    .Use(SystemTemplates.Configurable)
    .With("profession", "Teacher")
    .With("tone", "Friendly")
    .With("maxWords", "200")
    .AddMessage(userTurn)
    .Build();
```

The builder holds **one** pending template (`Use` / `AddTemplate`). Combine a catalog system template with a catalog user template by rendering the user template, then `AddMessage`.

### `SystemTemplates`

Only **Configurable** has variables. Every other system template is a fixed persona.

| Property | Variables |
|---|---|
| `HelpfulAssistant` | — |
| `SoftwareEngineer` | — |
| `CodeReviewer` | — |
| `TechnicalWriter` | — |
| `Teacher` | — |
| `SqlExpert` | — |
| `SecurityAuditor` | — |
| `DevOpsEngineer` | — |
| `DataScientist` | — |
| `ProductManager` | — |
| `CustomerSupport` | — |
| `JsonGenerator` | — |
| `Configurable` | `profession`, `tone`, `maxWords` |
| `ResearchAssistant` | — |
| `Architect` | — |
| `Translator` | — |
| `LegalAssistant` | — |
| `MedicalInformation` | — |
| `FinancialAnalyst` | — |
| `MarketingCopywriter` | — |
| `UxDesigner` | — |
| `ProjectPlanner` | — |
| `InterviewCoach` | — |
| `Debugger` | — |
| `ApiDesigner` | — |
| `BusinessAnalyst` | — |
| `TechnicalInterviewer` | — |
| `ContentStrategist` | — |
| `DatabaseAdmin` | — |
| `TestEngineer` | — |
| `TechnicalSupport` | — |
| `PerformanceEngineer` | — |
| `CreativeWriter` | — |
| `DataEngineer` | — |
| `CloudArchitect` | — |
| `AccessibilityExpert` | — |

### `UserTemplates`

| Property | Variables |
|---|---|
| `Summarize` | `style`, `content` |
| `Translate` | `targetLanguage`, `text` |
| `ExtractEntities` | `entityType`, `text` |
| `ReviewResume` | `role`, `resume` |
| `GenerateSql` | `schema`, `question` |
| `GenerateDocumentation` | `documentationType`, `language`, `code` |
| `GenerateTests` | `language`, `framework`, `code` |
| `ExplainConcept` | `topic`, `audience` |
| `ReviewCode` | `language`, `code` |
| `ConvertToJson` | `data` |
| `GenerateEmail` | `audience`, `tone`, `purpose`, `keyPoints` |
| `Rag` | `documents`, `question` |
| `ChainOfThought` | `question` |
| `Compare` | `optionA`, `optionB`, `useCase` |
| `GenerateApiDocs` | `endpoint` |
| `CreateUserStory` | `feature`, `persona` |
| `AnalyzeSentiment` | `text` |
| `GenerateCommitMessage` | `diff` |
| `RefactorCode` | `language`, `goal`, `code` |
| `CreateProjectPlan` | `requirements`, `timeline` |
| `WriteBlogPost` | `topic`, `audience`, `tone`, `wordCount` |
| `GenerateTestCases` | `requirement` |
| `ExplainError` | `error`, `language`, `code` |
| `DesignSchema` | `schemaType`, `requirements` |
| `GenerateRegex` | `description` |
| `CreateAgenda` | `meetingName`, `duration`, `attendees`, `topics` |
| `CodeReviewChecklist` | `language`, `projectType`, `focusAreas` |
| `SelfEvaluate` | `response` |
| `ExtractStructuredData` | `text`, `format`, `fields` |

## Testing

```bash
dotnet test Mima.AI.Prompt.sln -c Release
```

## Docs

| Doc | Purpose |
|-----|---------|
| [CHANGELOG.md](CHANGELOG.md) | Version history |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Build / PR |

## License

MIT. See [LICENSE](LICENSE).
