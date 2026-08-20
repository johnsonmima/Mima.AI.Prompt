# Changelog

All notable changes to Mima.AI.Prompt will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-08-20

Initial release. This package **composes** prompts: typed messages, templates, validation, in-memory versioning, and canonical JSON. It does **not** call models, map vendor chat APIs, run tools, or ship an agent runtime.

### Added

- **Messages and roles:** `system`, `developer`, `user`, `assistant`, `tool`, `function` (`MessageRole` and matching `*Message` types). Bodies are **`Parts`** (`TextPart`, `ImagePart`). `IMessage.Content` is concatenated text in memory only; `PromptSerializer` writes `parts`, not `content`.
- **Transcript extras:** `ToolCall`, `ToolMessage`, `FunctionMessage`, assistant refusal, `MessageAnnotation`, `MessageMetadata`, `PromptVariable`.
- **Templates:** `MessageTemplate` with `{{variable}}` discovery; `SystemTemplate`, `UserTemplate`, `AssistantTemplate`, `DeveloperTemplate`; `TemplateValidationResult`. Missing variables fail `Validate` / `Render` / `PromptBuilder.Build()` with `PromptValidationException`.
- **Catalog:** `SystemTemplates` (36) and `UserTemplates` (29).
- **Composition:** `Prompt` / `IPrompt` (optional `OutputFormat`); `PromptBuilder` (`System`, `Use`, `AddUser` / `AddUserWithImage`, `AddAssistant`, `AddTool`, `AddHistory`, `With`, `WithResponseFormat`, `Quick`, `UserOnly`, …); `Conversation` with a windowed `ToPrompt`.
- **Constraints and format:** `PromptConstraints.Render()`, `OutputFormat` (JSON, YAML, Markdown, plain text, bullets, steps, schema, custom).
- **Validation:** `PromptValidator`, `PromptValidationReport`. Empty text is allowed when parts, tool calls, or refusal are present.
- **Serialization:** `PromptSerializer` / `IPromptSerializer`, `PromptJsonOptions`. Host maps this JSON (or `IPrompt`) to OpenAI, Anthropic, Grok, etc.
- **Versioning:** `PromptVersion`, `VersionedPromptAsset<T>`, `PromptVersionHistory<T>` (in-memory; you persist JSON yourself).
- **Localization:** `LocalizedTemplate` (per-locale strings, `Render`, fallback). You pass the locale.
- **Targets:** `netstandard2.0`, `net6.0`, `net8.0`, `net10.0` (PolySharp on older TFMs).
