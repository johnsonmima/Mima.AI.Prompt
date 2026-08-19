# Building agent-shaped prompts with Mima.AI.Prompt

This guide is about **composing** agent-style prompts (`AgentSpec` → `Prompt` → adapter JSON).

It is **not** an agent runtime. There is no model HTTP client, no tool executor, and no `RunAsync` that talks to OpenAI for you. Your app owns those.

**Start here if you want the short version:** [README → What this is (and is not)](README.md#what-this-is-and-is-not) and [Building agents](README.md#building-agents)

---

## What you need to know first

### An agent is a prompt

In this library, an agent is **not** a background service that calls OpenAI or whatever for you.

An agent is a **shaped prompt**:

1. **Who it is** — system instructions (“You are a weather agent…”)
2. **What it remembers** — prior turns (`Conversation` or your own store)
3. **What it can do** — tool names / descriptions the model may call
4. **What the user said** — the current turn (text and/or images/files)

You build that prompt with `AgentSpec` (or `PromptBuilder`). **Your app** then:

1. Sends the prompt to a model  
2. Runs any tools the model requested  
3. Builds the next prompt with the tool results  
4. Repeats until the model answers normally  

```text
┌──────────────────────────────────────────┐
│  Your app (HTTP, tools, retries, UI)     │
└──────────────────┬───────────────────────┘
                   │ builds / updates
                   ▼
┌──────────────────────────────────────────┐
│  Mima.AI.Prompt                       │
│  AgentSpec → Prompt → adapter JSON       │
└──────────────────────────────────────────┘
```

### Two ways to build the same thing

| Approach | When to use |
|----------|-------------|
| **`AgentSpec` / `AgentCrew`** | Everyday agents — less boilerplate |
| **`PromptBuilder` directly** | Full control, or learning how messages are ordered |

Both produce a normal `Prompt`. Prefer sugar first; drop to the builder when you need something custom.

---

## 5-minute start: your first agent

```csharp
using Mima.AI.Prompt.Agents;

var agent = AgentSpec.Create("helper")
    .WithInstructions("You are a concise assistant. Prefer short answers.");

var prompt = agent.BuildPrompt("Explain dependency injection in one paragraph.");
```

What you get:

- A **system** message from `WithInstructions`
- A **user** message from `BuildPrompt(...)`
- Prompt metadata name `"helper"`

Next step in your app: send `prompt` to your model (or use `new OpenAiAdapter().ToJson(prompt)` to see the wire JSON).

---

## Tool-using agent (step by step)

This is the common “agent loop”. **Who does what:**

| Step | Mima.AI.Prompt | Your app |
| ------ | ------------------- | ---------- |
| 1. Define agent + advertise tools | `AgentSpec`, `WithTools` / `IAgentToolCatalog` | Implement the real C# tools |
| 2. Build prompt + call model | `BuildPrompt`, `OpenAiAdapter.ToJson` | HTTP / SDK request; optionally send a provider `tools` array |
| 3. Handle `tool_calls` | `ToolCall`, `ToolMessage`, `BuildPromptWithToolResults` | Parse response, run tools, produce result JSON |
| 4. Call model again | Same as step 2 | Stop when there are no more tool calls |

### Important: tools are *yours* — this library stores the conversation shape

There is **no** required C# interface for a tool body in this package.

| Concept | Where it lives | Shape |
| --------- | ---------------- | ------- |
| Tool **definition** (name, description, parameter JSON Schema) | Your app (and/or `IAgentToolCatalog` text) | You choose; providers often want OpenAI-style `tools: [{ type, function: { name, parameters } }]` |
| Tool **implementation** | Your C# methods / services | Any signature you like |
| Tool **call** from the model | `ToolCall` on `AssistantMessage` | `Id` + `Name` + `ArgumentsJson` (string) |
| Tool **result** back to the model | `ToolMessage` | `ToolCallId` (same as `Id`) + `Content` (usually a JSON **string**) |

`WithTools("get_weather")` only adds names to the **system prompt text**.  
It does **not** register delegates and does **not** emit a provider `tools` array by itself. To make the model call tools reliably you typically:

1. Tell the model about tools in instructions / catalog text, **and/or**  
2. Pass a formal `tools` / `functions` list on the HTTP request with your SDK (outside this library).

### How the model decides what to run — and how it actually runs

The model **never executes** your C# methods. OpenAI (or any chat API) does not receive `GetWeather` as a runnable function. It receives **text plus a schema**. It replies with **JSON** that *names* a tool. **Your process** is what runs the method.

**Decide (the model).** On Call 1 you send instructions, the user question, and (usually) a `tools` array. The model is a next-token predictor. Given “What’s the weather in NYC?” and a schema that lists `get_weather` with a required `city`, it typically emits a `tool_calls` item instead of inventing a temperature:

- **which** tool — `name`: `get_weather` vs `get_forecast` (or both, as parallel calls)
- **with which arguments** — `arguments`: `{"city":"NYC"}`
- **correlation id** — `id`: `call_1` (you must send this back with the result)

Nothing in this library or on the provider host *invokes* that name. The HTTP response is a request for work, not a weather report.

**Run (your app).** Your client parses `tool_calls`, maps each item to `ToolCall`, then dispatches on `ToolCall.Name` (`IAgentToolInvoker` / a `switch`). That is the only place `get_weather` becomes `GetWeather(city)`. You produce a result string (usually JSON), attach it to `ToolMessage` with the **same** `id`, and call `BuildPromptWithToolResults`.

**Answer (the model again).** Call 2 is a second POST: original messages + the assistant `tool_calls` turn + each tool result. Now the model has facts (`{"temp_f":72,"conditions":"clear"}`). It usually returns ordinary assistant text (“It’s 72°F and clear in NYC.”). If it emits more `tool_calls`, you loop.

```text
  Call 1 POST                    Model                          Your app
  messages + tools array  →  tool_calls JSON  →  switch(name) → run C#
                                                                  │
  Call 2 POST                    Model                            │
  messages + results      ←  “It’s 72°F…”     ←  ToolMessage  ←──┘
```

**Why send `tools` if the client already has the functions?** Because the client has *implementations*, not *answers*, and it does not yet know which method or which arguments. The `tools` array is the contract that makes the model return structured `tool_calls` instead of a guessed forecast. Listing names only in system text (`WithTools`) is weaker; the provider `tools` array is what most APIs use to enable native function calling.

**The other pattern (host already ran the tools).** If *your code* already decided to call `GetWeather("NYC")` (search, RAG, a button, a router), you do **not** send `tools` and you do **not** wait for `tool_calls`. Concatenate the results into the prompt and POST messages only. That is retrieve-then-generate. This guide’s weather sample is the **agent** loop: the model chooses the call, then you run it, then the model writes the sentence.

---

### Step 1 — Define the agent (and optionally a catalog schema)

```csharp
using Mima.AI.Prompt.Agents;
using Mima.AI.Prompt.Models;

var conversation = Conversation.Create("weather-session");

// Optional: richer descriptions so BuildPrompt folds schemas into system text
var catalog = new InlineToolCatalog(
    AgentToolDescriptor.Create(
        name: "get_weather",
        description: "Current weather for a city.",
        parametersSchemaJson: """
            {
              "type": "object",
              "properties": {
                "city": { "type": "string", "description": "City name" }
              },
              "required": ["city"]
            }
            """),
    AgentToolDescriptor.Create(
        name: "get_forecast",
        description: "Multi-day forecast for a city.",
        parametersSchemaJson: """
            {
              "type": "object",
              "properties": {
                "city": { "type": "string" },
                "days": { "type": "integer", "description": "How many days" }
              },
              "required": ["city"]
            }
            """));

var agent = AgentSpec.Create("weather")
    .WithInstructions("""
        You are a weather agent.
        Call get_weather for current conditions and get_forecast for upcoming days.
        After tools return, answer in one short sentence.
        """)
    .WithTools("get_weather", "get_forecast") // names in system text (does not bind C#)
    .WithToolCatalog(catalog)                   // optional: descriptions + schemas tool
    .WithMemory(conversation, window: 20)
    .WithToolInvoker(new WeatherToolInvoker()); // your runner — see “multiple tools” below
```

Example catalog + invoker (all host code):

```csharp
public sealed class InlineToolCatalog : IAgentToolCatalog
{
    private readonly IReadOnlyList<AgentToolDescriptor> _tools;
    public InlineToolCatalog(params AgentToolDescriptor[] tools) => _tools = tools;
    public IReadOnlyList<AgentToolDescriptor> GetTools() => _tools;
}

public sealed class WeatherToolInvoker : IAgentToolInvoker
{
    public Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken ct = default)
    {
        // This library never maps WithTools(...) to C# methods.
        // Your invoker is the router: ToolCall.Name → implementation.
        // argumentsJson is whatever the model emitted, e.g. {"city":"NYC"}.
        return Task.FromResult(toolName switch
        {
            "get_weather" => GetWeather(argumentsJson),
            "get_forecast" => GetForecast(argumentsJson),
            _ => "{\"error\":\"unknown tool\"}"
        });
    }

    private static string GetWeather(string argumentsJson) =>
        "{\"temp_f\":72,\"conditions\":\"clear\"}";

    private static string GetForecast(string argumentsJson) =>
        "{\"days\":[{\"day\":\"Mon\",\"high_f\":70},{\"day\":\"Tue\",\"high_f\":68}]}";
}
```

`IAgentToolInvoker` is optional sugar for *your* loop. You can call plain C# methods instead.

#### How multiple tools are addressed

**The client (your loop) loops. `WeatherToolInvoker` does not.**

`InvokeAsync` takes **one** `toolName` and **one** `argumentsJson`. Inside it is a `switch`, not a `for` — it runs whichever single method matches that name and returns one JSON string.

The loop lives in **your** agent loop (or `SimpleToolLoop` below). Use `RequireToolInvoker()` instead of `ToolInvoker!` — it returns the attached invoker or throws if you forgot `WithToolInvoker`.

```csharp
foreach (var call in assistantTurn.ToolCalls)  // client handles this
{
    var json = await agent.RequireToolInvoker().InvokeAsync(call.Name, call.ArgumentsJson);
    results.Add((call.Id, json));
}
```

If the model asks for both tools, that `foreach` runs twice: first `get_weather`, then `get_forecast`. Each time the invoker sees one name and picks one branch.

This library never calls the invoker from `BuildPrompt`. If you skip the `foreach` and only invoke once, the second `tool_call` never runs.

`WithTools("get_weather", "get_forecast")` and the catalog only **advertise** names (and optional schemas) in the prompt. They do **not** register delegates. Execution is always:

1. The model returns one or more `tool_calls`, each with its own `id` and `name`.
2. Your loop calls `InvokeAsync(call.Name, call.ArgumentsJson)` **once per call**.
3. `WeatherToolInvoker` switches on `toolName` — that is how two tools share one invoker.
4. You pass every `(call.Id, resultJson)` into `BuildPromptWithToolResults` so each result stays bound to the request that produced it.

If the invoker only handled `get_weather`, a `get_forecast` call would hit the `_` branch (`unknown tool`). Listing both names in `WithTools` does not implement the second tool.

One model turn can request **both** tools (parallel `tool_calls`):

```csharp
var assistantTurn = AssistantMessage.CreateWithToolCalls(
    new[]
    {
        ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}"),
        ToolCall.Create("call_2", "get_forecast", "{\"city\":\"NYC\",\"days\":2}")
    });

var results = new List<(string ToolCallId, string Result)>();
foreach (var call in assistantTurn.ToolCalls)
{
    // call.Name is "get_weather", then "get_forecast" — same invoker, different branch
    var json = await agent.RequireToolInvoker().InvokeAsync(call.Name, call.ArgumentsJson);
    results.Add((call.Id, json));
}

prompt = agent.BuildPromptWithToolResults(
    "Weather and a 2-day forecast for NYC?",
    assistantTurn,
    results);
// Tool messages: tool_call_id=call_1 and tool_call_id=call_2
```

The `foreach` is required even for a single tool; with two tools it just runs twice. `SimpleToolLoop` below does the same thing.

---

### Step 2 — Build the prompt, send it, understand what comes back

#### 2a. Build messages

```csharp
using Mima.AI.Prompt.Providers;

var prompt = agent.BuildPrompt("What's the weather in NYC?");
string messagesJson = new OpenAiAdapter().ToJson(prompt);
// → { "messages": [ { "role":"system", ... }, { "role":"user", ... } ], ... }
```

`OpenAiAdapter` formats **messages** (and `response_format` if set). It does **not** invent your tool definitions and it does **not** call the network. The JSON it prints is only the `messages` array.

Your OpenAI or Azure SDK call is a **separate** HTTP request: you take those messages, add `model` and a `tools` array, and POST that to the provider. The next section is that payload with every field filled in from this weather example.

#### What is actually sent to the LLM (baked-in weather example)

This is **not** part of this package. It is the chat-completions body **your app** would send after Step 1’s `AgentSpec` and `BuildPrompt("What's the weather in NYC?")`.

Two POSTs happen in the tool loop. Both are written out in full below (no `...` placeholders).

**System text** produced by `WithInstructions` + `WithTools` + `WithToolCatalog` (this string is the `messages[0].content` value):

```text
You are a weather agent.
Call get_weather for current conditions and get_forecast for upcoming days.
After tools return, answer in one short sentence.

Available tools:
- get_weather: Current weather for a city.
  Parameters schema: {
  "type": "object",
  "properties": {
    "city": { "type": "string", "description": "City name" }
  },
  "required": ["city"]
}
- get_forecast: Multi-day forecast for a city.
  Parameters schema: {
  "type": "object",
  "properties": {
    "city": { "type": "string" },
    "days": { "type": "integer", "description": "How many days" }
  },
  "required": ["city"]
}
Use tools via tool calls when needed. The host executes tools and returns results.
```

`OpenAiAdapter.ToJson(prompt)` emits only `messages`. You add `model` and `tools` yourself. The `tools` array is how the provider knows allowed names and argument **shapes** (this library never sends that array).

**Call 1 — first model request** (`BuildPrompt`, before any tool ran):

```json
{
  "model": "gpt-4.1",
  "messages": [
    {
      "role": "system",
      "content": "You are a weather agent.\nCall get_weather for current conditions and get_forecast for upcoming days.\nAfter tools return, answer in one short sentence.\n\nAvailable tools:\n- get_weather: Current weather for a city.\n  Parameters schema: {\n  \"type\": \"object\",\n  \"properties\": {\n    \"city\": { \"type\": \"string\", \"description\": \"City name\" }\n  },\n  \"required\": [\"city\"]\n}\n- get_forecast: Multi-day forecast for a city.\n  Parameters schema: {\n  \"type\": \"object\",\n  \"properties\": {\n    \"city\": { \"type\": \"string\" },\n    \"days\": { \"type\": \"integer\", \"description\": \"How many days\" }\n  },\n  \"required\": [\"city\"]\n}\nUse tools via tool calls when needed. The host executes tools and returns results."
    },
    {
      "role": "user",
      "content": "What's the weather in NYC?"
    }
  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "get_weather",
        "description": "Current weather for a city.",
        "parameters": {
          "type": "object",
          "properties": {
            "city": { "type": "string", "description": "City name" }
          },
          "required": ["city"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "get_forecast",
        "description": "Multi-day forecast for a city.",
        "parameters": {
          "type": "object",
          "properties": {
            "city": { "type": "string" },
            "days": { "type": "integer", "description": "How many days" }
          },
          "required": ["city"]
        }
      }
    }
  ]
}
```

POST that JSON to `https://api.openai.com/v1/chat/completions` (or your Azure OpenAI chat-completions URL) with your API key. This package does not perform that POST.

**Call 2 — second model request** (after `BuildPromptWithToolResults`, both tools already executed). Same `model` and `tools`. `messages` now include the assistant `tool_calls` plus one `tool` result per id:

```json
{
  "model": "gpt-4.1",
  "messages": [
    {
      "role": "system",
      "content": "You are a weather agent.\nCall get_weather for current conditions and get_forecast for upcoming days.\nAfter tools return, answer in one short sentence.\n\nAvailable tools:\n- get_weather: Current weather for a city.\n  Parameters schema: {\n  \"type\": \"object\",\n  \"properties\": {\n    \"city\": { \"type\": \"string\", \"description\": \"City name\" }\n  },\n  \"required\": [\"city\"]\n}\n- get_forecast: Multi-day forecast for a city.\n  Parameters schema: {\n  \"type\": \"object\",\n  \"properties\": {\n    \"city\": { \"type\": \"string\" },\n    \"days\": { \"type\": \"integer\", \"description\": \"How many days\" }\n  },\n  \"required\": [\"city\"]\n}\nUse tools via tool calls when needed. The host executes tools and returns results."
    },
    {
      "role": "user",
      "content": "What's the weather in NYC?"
    },
    {
      "role": "assistant",
      "content": "",
      "tool_calls": [
        {
          "id": "call_1",
          "type": "function",
          "function": {
            "name": "get_weather",
            "arguments": "{\"city\":\"NYC\"}"
          }
        },
        {
          "id": "call_2",
          "type": "function",
          "function": {
            "name": "get_forecast",
            "arguments": "{\"city\":\"NYC\",\"days\":2}"
          }
        }
      ]
    },
    {
      "role": "tool",
      "tool_call_id": "call_1",
      "content": "{\"temp_f\":72,\"conditions\":\"clear\"}"
    },
    {
      "role": "tool",
      "tool_call_id": "call_2",
      "content": "{\"days\":[{\"day\":\"Mon\",\"high_f\":70},{\"day\":\"Tue\",\"high_f\":68}]}"
    }
  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "get_weather",
        "description": "Current weather for a city.",
        "parameters": {
          "type": "object",
          "properties": {
            "city": { "type": "string", "description": "City name" }
          },
          "required": ["city"]
        }
      }
    },
    {
      "type": "function",
      "function": {
        "name": "get_forecast",
        "description": "Multi-day forecast for a city.",
        "parameters": {
          "type": "object",
          "properties": {
            "city": { "type": "string" },
            "days": { "type": "integer", "description": "How many days" }
          },
          "required": ["city"]
        }
      }
    }
  ]
}
```

The `tool` rows’ `content` values are exactly what `WeatherToolInvoker` returns for those names. After this second POST, the model usually answers in plain language (no `tool_calls`).

#### 2b. What the model returns (`tool_calls`)

Providers differ slightly; OpenAI-style responses look like the following. This is the reply to **Call 1** in the section above (here the model asked for **both** tools — the same ids used in **Call 2**):

```json
{
  "choices": [{
    "message": {
      "role": "assistant",
      "content": null,
      "tool_calls": [
        {
          "id": "call_1",
          "type": "function",
          "function": {
            "name": "get_weather",
            "arguments": "{\"city\":\"NYC\"}"
          }
        },
        {
          "id": "call_2",
          "type": "function",
          "function": {
            "name": "get_forecast",
            "arguments": "{\"city\":\"NYC\",\"days\":2}"
          }
        }
      ]
    }
  }]
}
```

Map that into this library’s types:

| Provider field | Mima type / property |
| ---------------- | ------------------------- |
| `tool_calls[].id` | `ToolCall.Id` |
| `tool_calls[].function.name` | `ToolCall.Name` |
| `tool_calls[].function.arguments` | `ToolCall.ArgumentsJson` (string — still JSON text) |
| optional assistant text | `AssistantMessage` content |

```csharp
using Mima.AI.Prompt.Messages;
using Mima.AI.Prompt.Models;

// After you parse the HTTP response yourself:
var assistantTurn = AssistantMessage.CreateWithToolCalls(
    toolCalls: new[]
    {
        ToolCall.Create(
            id: "call_1",
            name: "get_weather",
            argumentsJson: "{\"city\":\"NYC\"}"),
        ToolCall.Create(
            id: "call_2",
            name: "get_forecast",
            argumentsJson: "{\"city\":\"NYC\",\"days\":2}")
    },
    content: null); // many providers send null/empty when only tool_calls are present
```

**What `ToolCall` expects:** only those three strings. There is no generic `TArgs` type parameter — you deserialize `ArgumentsJson` in your tool code (`JsonSerializer.Deserialize<WeatherArgs>(...)`).

The array can contain **several** calls in one reply (e.g. `get_weather` as `call_1` and `get_forecast` as `call_2`). Map every element to a `ToolCall` — the invoker switch plus `foreach` in step 3 handle more than one name.

If `tool_calls` is missing and you only get normal assistant text, skip step 3 and finish.

---

### Step 3 — Run tools and rebuild the prompt

#### 3a. Execute by name

```csharp
foreach (var call in assistantTurn.ToolCalls)
{
    // Your dispatch: name → implementation (see WeatherToolInvoker switch)
    string resultJson = await agent.RequireToolInvoker()
        .InvokeAsync(call.Name, call.ArgumentsJson);

    // resultJson shape is YOUR contract with the model, e.g.:
    //   get_weather  → {"temp_f":72,"conditions":"clear"}
    //   get_forecast → {"days":[...]}
    // The library does not validate it. Keep it JSON text unless you have a reason not to.
}
```

How does the invoker “know” the return shape?

- **You** document it in the tool description / system prompt (“returns `{ temp_f, conditions }`”).  
- **You** implement that shape in C#.  
- The **model** learns it from prior turns and docs — there is no separate return-schema API in this package (providers are also uneven here).

#### 3b. Pair results with call ids

Every result must use the **same** `Id` the model sent. That is how chat APIs bind “this output belongs to that request”.

```csharp
prompt = agent.BuildPromptWithToolResults(
    userTurn: "What's the weather in NYC?",
    assistantToolCallTurn: assistantTurn,
    toolResults: new[]
    {
        ("call_1", "{\"temp_f\":72,\"conditions\":\"clear\"}")
        //     ^ ToolCall.Id          ^ ToolMessage.Content
        // add ("call_2", forecastJson) when the model also called get_forecast
    });
```

Under the hood that appends:

1. The assistant message with `tool_calls`  
2. One `ToolMessage` per result: `role=tool`, `tool_call_id=call_1`, `content=...`

When you send that prompt again, `OpenAiAdapter` fills the `messages` array; **Call 2** in [What is actually sent to the LLM](#what-is-actually-sent-to-the-llm-baked-in-weather-example) is the complete POST body (messages + `tools` + `model`).

#### 3c. End-to-end picture

```text
You define schema + C# tool
        │
        ▼
Request: messages (+ optional provider `tools` array)
        │
        ▼
Model responds with tool_calls[{ id, name, arguments }]
        │
        ▼
You: parse → ToolCall → InvokeAsync(name, argumentsJson) → result string
        │
        ▼
BuildPromptWithToolResults → messages include assistant tool_calls + tool results
        │
        ▼
Model responds with final natural-language answer (usually)
```

---

### Step 4 — Call the model again

Send the rebuilt `prompt`. When the reply has **no** `tool_calls`, treat `content` as the user-facing answer, append it to `conversation`, and stop.

> Tip: keep `Conversation` updated with user / assistant / tool messages so the next `BuildPrompt` includes history.

---

## Multi-agent crew (roles in one prompt)

Sometimes one prompt should contain several “speakers” (writer, critic, moderator). That is still **one** `Prompt` with custom roles — not a scheduler.

```csharp
using Mima.AI.Prompt.Agents;

var crew = AgentCrew.Create("docs")
    .WithOrchestratorInstructions("Host a short debate. Keep each turn focused.")
    .AddMember(
        roleName: "writer",
        instructions: "Draft a short paragraph on dependency injection.",
        displayName: "writer-bot")   // optional speaker id (message.name)
    .AddMember(
        roleName: "critic",
        instructions: "Challenge weak claims in the draft.");

// Members contribute from their instructions, then the user topic:
var turn = crew.BuildTurn("Summarize dependency injection.");

// Or supply the exact lines yourself:
turn = crew.BuildTurn(
    topic: "Who made the stronger case?",
    contributions: new[]
    {
        ("writer", "DI makes testing clearer."),
        ("critic", "Service locator can still fit plugin hosts.")
    });
```

Your host still decides **who speaks next** and when to call the model again.

---

## Bring your own runtime (host hooks)

We do **not** ship HTTP clients, tool runners, or RAG engines. We expose **hooks** so you can plug yours in.

| Hook | What it does | Used when you `BuildPrompt`? |
| ------ | ---------------- | ------------------------------ |
| `IAgentRetriever` | Return docs / text for the user query | **Yes** — injected into the prompt |
| `IAgentMemory` | Return prior messages from your DB | **Yes** — appended as history |
| `IAgentToolCatalog` | Tool name + description + optional JSON schema | **Yes** — folded into system text |
| `IAgentChatClient` | Call your model API | **No** — stored; you call it |
| `IAgentToolInvoker` | Run a C# tool by name | **No** — stored; you call it |
| `IAgentLoop` | Your `while (needsTools)` orchestration | **No** — stored; you call it |

### Example: RAG without owning HTTP

```csharp
using Mima.AI.Prompt.Agents;

public sealed class MyRetriever : IAgentRetriever
{
    public AgentRetrievalResult Retrieve(string query) =>
        AgentRetrievalResult.FromText(SearchMyIndex(query));
}

var agent = AgentSpec.Create("support")
    .WithInstructions("Answer only from the retrieved context. If it is missing, say so.")
    .WithRetriever(new MyRetriever())
    .WithChatClient(myChatClient);   // your implementation of IAgentChatClient

var prompt = agent.BuildPrompt("What is the refund policy?");
// Retrieved text is already in the prompt as a developer/context block.

// var reply = await agent.RequireChatClient().CompleteAsync(prompt);
```

### Example: a simple loop you own

```csharp
public sealed class SimpleToolLoop : IAgentLoop
{
    public async Task<IPrompt> RunAsync(
        AgentSpec agent,
        string userTurn,
        CancellationToken ct = default)
    {
        var prompt = agent.BuildPrompt(userTurn);

        for (var i = 0; i < 8; i++)
        {
            var reply = await agent.RequireChatClient().CompleteAsync(prompt, ct);

            if (reply is not AssistantMessage assistant || assistant.ToolCalls.Count == 0)
                return prompt;

            var results = new List<(string, string)>();
            foreach (var call in assistant.ToolCalls)
            {
                var json = await agent.RequireToolInvoker().InvokeAsync(
                    call.Name, call.ArgumentsJson, ct);
                results.Add((call.Id, json));
            }

            prompt = agent.BuildPromptWithToolResults(userTurn, assistant, results);
        }

        return prompt;
    }
}

// Attach, then run from your app:
agent.WithLoop(new SimpleToolLoop());
// await agent.RequireLoop().RunAsync(agent, userTurn);
```

---

## AgentSpec cheat sheet

| Method | Purpose |
| -------- | --------- |
| `Create(name)` | Start a named agent |
| `WithInstructions` | System persona / policy |
| `WithDeveloperInstructions` | Extra framework rules |
| `WithTools(...)` | List tool names in system text (does not bind C# methods) |
| `WithToolCatalog` | Richer tool docs / schemas in system text |
| `WithMemory(conversation, window?)` | In-process chat history |
| `WithExternalMemory` | History from your `IAgentMemory` |
| `WithRetriever` | RAG via `IAgentRetriever` |
| `WithResponseFormat` | Ask for JSON / YAML / schema / Markdown, etc. |
| `WithChatClient` / `WithToolInvoker` / `WithLoop` | Attach host hooks (not called by build) |
| `RequireChatClient` / `RequireToolInvoker` / `RequireLoop` | Same hooks, or throw if missing (no `!`) |
| `BuildPrompt(...)` | Build the prompt for a user turn |
| `BuildPromptWithToolResults(...)` | Continue after tools ran |

---

## AgentCrew cheat sheet

| Method | Purpose |
| -------- | --------- |
| `Create(name)` | Start a crew |
| `WithOrchestratorInstructions` | System text for the host / moderator |
| `AddMember(role, instructions, displayName?)` | Add a custom-role speaker |
| `BuildTurn(topic)` | Members + user topic |
| `BuildTurn(topic, contributions)` | Explicit role → content lines |

---

## Same patterns without sugar (`PromptBuilder`)

Useful when you want every message explicit.

### Tools

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Models;

var prompt = PromptBuilder.Create()
    .AddSystem("You are a weather agent. Call get_weather when needed.")
    .AddUser("What's the weather in NYC?")
    .AddAssistantToolCalls(
        new[] { ToolCall.Create("call_1", "get_weather", "{\"city\":\"NYC\"}") },
        content: "Checking…")
    .AddTool("call_1", "{\"temp_f\":72}")
    .Build();
```

### Custom roles

```csharp
using Mima.AI.Prompt.Roles;

var writer = MessageRole.Custom("writer");
var critic = MessageRole.Custom("critic");

var prompt = PromptBuilder.Create()
    .AddSystem("You host a short technical debate.")
    .Add(writer, "Draft a paragraph on DI.")
    .Add(critic, "Challenge weak claims.")
    .AddUser("Summarize the consensus.")
    .Build();
```

### Computer-use agents (see the screen, act on the UI)

Computer-use agents are still **prompts**. The user (or observation) turn carries **structured parts** so the model can see a screenshot and propose UI actions.

This package does **not** open Chrome or click the mouse. It only stores the protocol-shaped content your runner and the model exchange.

#### What each part means

```csharp
TextPart.Create("Submit the form.")
ScreenshotPart.FromUrl("https://example.com/screen.png")
ComputerActionPart.Create("click", "{\"x\":120,\"y\":80}")
```

| Part | Role in the turn | Notes |
| ------ | ------------------ | ------- |
| **`TextPart`** | Natural-language goal or status | Always useful: “fill the email field”, “stop when logged in” |
| **`ScreenshotPart`** | Current pixels the model should “see” | From a URL **or** base64 (`FromBase64`) after your runner captures the frame |
| **`ComputerActionPart`** | One discrete UI action + JSON arguments | `Action` is a free-form name (`click`, `type`, …); `ArgumentsJson` is your schema |

Put them on the **same message** (usually `UserMessage`) so one observation is atomic:

```csharp
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;
using Mima.AI.Prompt.Messages;

var observation = UserMessage.Create(new IContentPart[]
{
    TextPart.Create("Click the Submit button near the bottom of the form."),
    ScreenshotPart.FromUrl("https://cdn.example.com/frames/step-3.png"),
    ComputerActionPart.Create("click", "{\"x\":120,\"y\":80}")
});

// Text projection ignores screenshot/action — only TextParts concatenate:
// observation.Content == "Click the Submit button near the bottom of the form."
// observation.Parts.Count == 3
```

#### Recommended loop

```text
┌─────────────┐     ┌──────────────────────┐     ┌─────────────┐
│ Capture UI  │────▶│ Build Prompt         │────▶│ Call model  │
│ (screenshot)│     │ text + ScreenshotPart│     │             │
└─────────────┘     │ (+ last action echo) │     └──────┬──────┘
       ▲            └──────────────────────┘            │
       │                                                ▼
       │                                    ┌───────────────────────┐
       └────────────────────────────────────│ Parse ComputerAction  │
            your runner executes action     │ Execute click/type/…  │
                                            └───────────────────────┘
```

1. **Capture** a screenshot (Playwright, Selenium, OS API, …).  
2. **Build** a prompt with system rules + user parts (`TextPart` + `ScreenshotPart`).  
3. **Call** the model (your HTTP / SDK).  
4. **Read** the next `ComputerActionPart` from the assistant reply (or parse your provider payload).  
5. **Execute** that action in the real UI.  
6. Optionally **echo** the action you ran on the next user turn, plus a **new** screenshot.  
7. Repeat until the model answers with plain text (“done”) or you hit a step limit.

#### First turn — goal + screenshot only

On the first step you often have **no** prior action yet:

```csharp
using Mima.AI.Prompt.Builder;
using Mima.AI.Prompt.Content;
using Mima.AI.Prompt.Interfaces;

byte[] png = CaptureScreen(); // your code
string b64 = Convert.ToBase64String(png);

var prompt = PromptBuilder.Create()
    .AddSystem("""
        You control a browser through computer_action parts.
        Propose exactly one action per turn.
        When the goal is finished, reply with plain text only (no computer_action).
        """)
    .AddUser(new IContentPart[]
    {
        TextPart.Create("Log in as user@example.com with password secret, then submit the form."),
        ScreenshotPart.FromBase64(b64, "image/png")
    })
    .Build();
```

#### Later turns — include the action you just ran

Echoing the last action helps the model avoid repeating it and keeps a clear transcript:

```csharp
var next = PromptBuilder.Create()
    .AddSystem("One computer_action per turn. Plain text when done.")
    .AddUser(new IContentPart[]
    {
        TextPart.Create("Continue. Stop when the success banner is visible."),
        ScreenshotPart.FromUrl("https://cdn.example.com/frames/step-4.png"),
        // Action you already executed — transcript / grounding for the model
        ComputerActionPart.Create("type", "{\"text\":\"user@example.com\",\"selector\":\"#email\"}")
    })
    .Build();
```

#### Action catalog (conventions you choose)

`ComputerActionPart` does not validate argument JSON — your runner and model prompt should agree on a schema. Common patterns:

```csharp
// Pointer
ComputerActionPart.Create("click",       "{\"x\":120,\"y\":80}");
ComputerActionPart.Create("double_click","{\"x\":120,\"y\":80}");
ComputerActionPart.Create("move",        "{\"x\":50,\"y\":10}");

// Keyboard / text
ComputerActionPart.Create("type",        "{\"text\":\"hello@example.com\"}");
ComputerActionPart.Create("key",         "{\"key\":\"Enter\"}");
ComputerActionPart.Create("hotkey",      "{\"keys\":[\"Meta\",\"l\"]}");

// Viewport
ComputerActionPart.Create("scroll",      "{\"dy\":400}");
ComputerActionPart.Create("scroll",      "{\"dx\":0,\"dy\":-200}");

// Timing / misc
ComputerActionPart.Create("wait",        "{\"ms\":800}");
ComputerActionPart.Create("drag",        "{\"from\":{\"x\":10,\"y\":10},\"to\":{\"x\":200,\"y\":80}}");
```

Describe allowed actions in the **system** message so the model stays on-script:

```csharp
.AddSystem("""
    Allowed computer_action names: click, type, scroll, key, wait.
    click/type args use pixel coordinates from the screenshot unless a CSS selector is given.
    """)
```

#### Screenshot sources

```csharp
// Public or signed URL your model host can fetch
ScreenshotPart.FromUrl("https://cdn.example.com/frame.png");

// Inline bytes (local capture, no upload)
ScreenshotPart.FromBase64(pngBase64, "image/png");
ScreenshotPart.FromBase64(jpegBase64, "image/jpeg");
```

Prefer `ScreenshotPart` over `ImagePart` for UI-agent loops so the intent (“this is the live frame”) stays clear in your domain model. Adapters may still map screenshots to provider image fields.

#### Using AgentSpec

```csharp
using Mima.AI.Prompt.Agents;

var agent = AgentSpec.Create("browser")
    .WithInstructions("""
        You are a computer-use agent. One computer_action per turn.
        Finish with plain text when the user's goal is complete.
        """);

var prompt = agent.BuildPrompt(new IContentPart[]
{
    TextPart.Create("Open settings and enable dark mode."),
    ScreenshotPart.FromBase64(frame, "image/png")
});
```

#### Boundaries

| This library | Your runner |
| -------------- | ------------- |
| Stores `ScreenshotPart` / `ComputerActionPart` on messages | Captures screenshots |
| Serializes / validates prompt shape | Executes click/type/scroll |
| Adapts parts toward provider JSON | Talks to Playwright / OS APIs |
| Does **not** launch a browser | Owns safety limits, allowlists, step budgets |

---

## What this package does *not* do

| Not included | How you get it |
| -------------- | ---------------- |
| Calling OpenAI / Anthropic / Ollama over HTTP | Your SDK + optional `IAgentChatClient` |
| Running C# tool methods | Your code + optional `IAgentToolInvoker` |
| Autonomous multi-step loops | Your code + optional `IAgentLoop` |
| Vector DB / RAG engines | Your retriever + `IAgentRetriever` |
| Durable chat stores | Your DB + `IAgentMemory` (or keep using `Conversation` in-process) |
| Browser / desktop automation | Your runner + `ScreenshotPart` / `ComputerActionPart` (see [Computer-use](#computer-use-agents-see-the-screen-act-on-the-ui)) |

We stay focused on **prompt assets**: typed, testable, serializable messages you can validate and adapt to provider JSON.

---

## Related reading

| Doc / API | Why |
| ----------- | ----- |
| [README.md](README.md) | Install, core concepts, short agent overview |
| `Conversation` | In-process session memory |
| `ToolCall` / `ToolMessage` | Tool request and result turns |
| `OpenAiAdapter` / `AnthropicAdapter` / `OllamaAdapter` | Prompt → provider JSON (no network) |
| `PromptSerializer` | Save / replay agent prompts in tests |
| `tests/.../AgentTests.cs` | Executable samples |
| `tests/.../EndToEndUsageTests.cs` | README-aligned agent scenarios |
