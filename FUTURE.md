# Future work (not in the current release)

Features **removed or never shipped** so this version’s public API is fully usable: prompts + **correct** OpenAI / Anthropic / Ollama JSON. Host still owns HTTP.

Do not re-add anything here until it has a real wire mapping (or a dedicated package) and golden tests. Implementation plan for the current cut: [TEMP.md](TEMP.md).

---

## Package: `Mima.AI.Agents` (or similar)

Prompt-only `AgentSpec.BuildPrompt` stays in `Mima.AI.Prompt`. This package would own runtime.

| Item | Notes when restoring |
|---|---|
| `IAgentChatClient` / `CompleteAsync` | Must call a real SDK or HttpClient |
| `IAgentToolInvoker` | Dispatch `ToolCall.Name` → C# |
| `IAgentLoop` / `RunAsync` | Tool while-loop, budgets, cancellation |
| Provider **tools / input_schema catalog** on the request | OpenAI `tools[]`, Anthropic `tools[]`, Ollama if documented — not system-text names only |
| `AgentCrew` | Multi-agent **scheduling**, not custom `role` strings. Speakers as `name` on user/assistant or separate prompts |

---

## Domain / parts (restore only with provider-accurate mappers)

| Item | Why it left | Restore when |
|---|---|---|
| `VideoPart` | Invented JSON | A provider documents video content blocks; golden fixture |
| `FilePart` (`FromId` / `FromUrl` / `FromBase64`) | Base64 dropped; ids not mapped per API | OpenAI `file` / Anthropic `document` (and Ollama: error if unsupported) |
| `ComputerActionPart` + screenshot-as-protocol | Not CUA / Anthropic computer-use | Map to that provider’s action schema; `ScreenshotPart` may return as `ImagePart` |
| `ScreenshotPart` | Duplicate of image | Optional alias over `ImagePart` for computer-use package |
| `ThinkingPart` / assistant `Reasoning` on the wire | Stripped from payloads | Anthropic thinking / OpenAI reasoning fields as documented |
| `CacheControl` on messages/parts | Ignored on OpenAI | Anthropic `cache_control` on content blocks |
| `MessageRole.Custom`, `CustomMessage`, `CustomTemplate` | Invalid chat `role` | Never as OpenAI/Anthropic/Ollama `role`. Use `name` or extra system text |
| `FunctionMessage` (if removed later) | Legacy functions API | Keep only if still supporting old OpenAI `function` role |

---

## Platform stubs (do not ship half-done in Prompt)

| Item | Restore as |
|---|---|
| `IPromptCache` / Redis | Cache **completions** or provider cache keys — not a `Prompt` dictionary pretending to be prompt caching |
| `IPromptAnalytics` | Metrics backend (OpenTelemetry); `TrackVariableUsage` must record something |
| `IPromptLocalizer` | Real locale pipeline; `LocalizedTemplate` can stay as a dictionary helper in Prompt if already kept |
| `PromptChain` execution | Pipe step N output into step N+1 (`{{previousOutput}}` or explicit bind) |
| `IProviderAdapter.EstimateTokens` | tiktoken / Anthropic count API — or omit forever |
| `GenericPromptRenderer` as HTTP body | Logging/debug only, never a fourth “provider” |
| `FromBytes` / `FromFile` | Optional DX on `ImagePart`; file read stays host or a tiny helper with clear I/O |

---

## Adapter extras (still format-only)

- OpenAI `json_schema` + `strict` with a **parsed JSON object** (not a string)
- Anthropic URL image `source.type = url` if we did not ship it in v1
- Ollama tools, if/when their chat API is stable
- Emit `tools` arrays (belongs with Agents package more than Prompt)

---

## Docs

When restoring, add a README section only after tests exist. Until then this file is the backlog — not the product.
