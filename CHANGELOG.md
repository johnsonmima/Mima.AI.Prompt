# Changelog

All notable changes to Mima.AI.Prompt will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Unified Parts-first message model**
  - `Parts` is the canonical body; `Content` is the text projection of `TextPart`s
  - Multimodal `IContentPart` types, annotations, cache control, tool calls, reasoning, refusal
  - `IPrompt.ResponseFormat` + builder parts overloads (`AddSystem`/`AddUser`/`AddAssistant`/`AddDeveloper`)
  - Shared `ProviderContentMapper` for OpenAI, Anthropic, and Ollama
- **Agent sugar (prompt-only)**: `AgentSpec`, `AgentCrew`, host hooks (`IAgentChatClient`, `IAgentToolInvoker`, `IAgentLoop`, `IAgentRetriever`, `IAgentMemory`, `IAgentToolCatalog`); see [AGENT.md](AGENT.md)
- `OutputFormat.Yaml()` / `YamlWithSchema()` for YAML structured output (prompt instructions; OpenAI `response_format` stays `text`)
- `AgentSpec.RequireChatClient` / `RequireToolInvoker` / `RequireLoop` — throw if the host hook was not attached (no null-forgiving `!`)
- **Custom roles**: `MessageRole.Custom`, `ParseOrCreate`, `CustomMessage`, `CustomTemplate`
- Unit tests (`RichMessageTests`, `EndToEndUsageTests`, `AgentTests`) and open-source kit / CI
- PolySharp for `netstandard2.0`

### Changed

- README, NuGet `Description`, and AGENT.md now state the product contract up front: prompt composition only (no model HTTP, no tool runtime, no provider `tools` array)
- `PromptSerializer` always deserializes from `parts` when present (preserves id / name / cache / annotations)
- Provider adapters emit structured content via shared mapper (not string-only)
- `FunctionMessage` documented as legacy relative to tools + tool_calls
- Validator allows empty text when parts / tool calls / refusal are present
- CI/Release `setup-dotnet` uses `cache: true` with committed `packages.lock.json` files and `--locked-mode` restore
- Release publishes with [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC short-lived key) instead of a stored API key

### Fixed

- netstandard2.0 build via PolySharp; `Parse`/`TryParse` remain public on `MessageRole`

## [1.0.0] - 2026-07-30

### Added

- **Core Domain Model**
  - `MessageRole` abstract class with sealed subclasses (`SystemRole`, `UserRole`, `AssistantRole`, `DeveloperRole`, `ToolRole`, `FunctionRole`)
  - Immutable message types (`SystemMessage`, `UserMessage`, `AssistantMessage`, `DeveloperMessage`, `ToolMessage`, `FunctionMessage`)
  - `MessageMetadata` for tagging, versioning, and categorization
  - `PromptVariable` for template variable definitions

- **Template System**
  - `MessageTemplate` abstract base with automatic variable discovery via `{{variableName}}` syntax
  - `SystemTemplate`, `UserTemplate`, `AssistantTemplate`, `DeveloperTemplate`
  - `TemplateValidationResult` for variable validation

- **Prompt Model**
  - `Prompt` — immutable ordered message collection
  - `Conversation` — mutable conversation tracker with windowed context
  - `PromptChain` — sequential prompt execution model
  - `PromptConstraints` — structured constraint/restriction builder
  - `OutputFormat` — structured output format definitions (JSON, Markdown, PlainText, etc.)

- **Fluent Builder API**
  - `PromptBuilder` with `System()`, `Use()`, `AddUser()`, `AddAssistant()`, `AddExample()`, `AddHistory()`, `With()`, `Build()`
  - Static convenience methods `Quick()` and `UserOnly()`

- **Built-in Template Catalog**
  - 30+ system templates (HelpfulAssistant, SoftwareEngineer, CodeReviewer, LegalAssistant, MedicalInformation, …, Configurable)
  - 25+ user templates (Summarize, Translate, Rag, ChainOfThought, Compare, …)

- **Validation**
  - `PromptValidator` / `PromptValidationReport` / `PromptValidationException`

- **Serialization & Rendering**
  - `PromptSerializer` (JSON)
  - `GenericPromptRenderer`
  - In-box provider adapters: `OpenAiAdapter`, `AnthropicAdapter`, `OllamaAdapter`

- **Versioning**
  - `PromptVersion`, `VersionedPromptAsset<T>`, `PromptVersionHistory<T>`

- **Caching**
  - `IPromptCache`, `InMemoryPromptCache` with TTL

- **Localization**
  - `LocalizedTemplate`, `IPromptLocalizer` (interface)

- **Analytics**
  - `IPromptAnalytics`, `InMemoryPromptAnalytics`

- **Multi-targeting**: `netstandard2.0`, `net6.0`, `net8.0`, `net10.0`
