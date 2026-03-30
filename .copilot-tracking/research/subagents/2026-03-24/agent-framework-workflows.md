# Microsoft Agent Framework — Workflow Research

## Status: Complete

## Research Topics

1. Sequential/Handoff workflows in .NET: How to wire agents into sequential pipelines
2. Graph-based workflows: API for data flows, streaming, checkpointing, human-in-the-loop
3. Mixing agent providers in a workflow: Anthropic + GitHub Copilot SDK or other providers
4. The GitHubCopilotAgent class: Source code analysis, interface, RunAsync return type
5. AgentResponse type: How to pass one agent's output as input to another
6. AGUI hosting with workflows: How workflow results stream back to UI

---

## 1. Sequential/Handoff Workflows in .NET

### Core Concepts

The workflow system is built on **Executors** and **Edges**:

- **Executor**: A processing unit (typed with input/output) that receives messages and produces results. Base class: `Executor<TInput, TOutput>`.
- **Edge**: A connection between executors — output from one flows as input to the next.
- **WorkflowBuilder**: Fluent API for constructing workflow graphs.

### Basic Sequential Workflow (Non-Agent)

From `dotnet/samples/03-workflows/_StartHere/01_Streaming/Program.cs`:

```csharp
// Create executors
UppercaseExecutor uppercase = new();
ReverseTextExecutor reverse = new();

// Build sequential graph
WorkflowBuilder builder = new(uppercase);          // Start with first executor
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);  // Chain and mark output

var workflow = builder.Build();

// Execute with streaming
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input: "Hello, World!");
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is ExecutorCompletedEvent executorCompleted)
        Console.WriteLine($"{executorCompleted.ExecutorId}: {executorCompleted.Data}");
}
```

Custom executors implement `Executor<TInput, TOutput>`:

```csharp
internal sealed class UppercaseExecutor() : Executor<string, string>("UppercaseExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message, IWorkflowContext context, CancellationToken ct = default)
        => ValueTask.FromResult(message.ToUpperInvariant());
}
```

### Agent Sequential Workflow

From `dotnet/samples/03-workflows/_StartHere/02_AgentsInWorkflows/Program.cs`:

```csharp
AIAgent frenchAgent = GetTranslationAgent("French", chatClient);
AIAgent spanishAgent = GetTranslationAgent("Spanish", chatClient);
AIAgent englishAgent = GetTranslationAgent("English", chatClient);

var workflow = new WorkflowBuilder(frenchAgent)
    .AddEdge(frenchAgent, spanishAgent)
    .AddEdge(spanishAgent, englishAgent)
    .Build();

// Execute — agents require a TurnToken to trigger processing
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
    workflow, new ChatMessage(ChatRole.User, "Hello World!"));
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
```

**Key Detail**: When agents are used as executors in workflows, they cache incoming `ChatMessage`s and only begin processing when they receive a `TurnToken`. This is the "Chat Protocol" for agents in workflows.

### AgentWorkflowBuilder — High-Level Patterns

From `dotnet/src/Microsoft.Agents.AI.Workflows/AgentWorkflowBuilder.cs`, the framework provides convenience builders:

#### Sequential Pipeline

```csharp
Workflow workflow = AgentWorkflowBuilder.BuildSequential(agent1, agent2, agent3);
```

Internally, this:
1. Binds each agent as an executor with `agent.BindAsExecutor(options)`
2. Chains them with `AddEdge(previous, next)`
3. Adds an `OutputMessagesExecutor` at the end
4. Uses `ReassignOtherAgentsAsUsers = true` and `ForwardIncomingMessages = true`

#### Concurrent (Fan-Out/Fan-In)

```csharp
Workflow workflow = AgentWorkflowBuilder.BuildConcurrent(agent1, agent2, agent3);
```

- All agents receive the same input in parallel
- Results are aggregated (default: last message from each agent)
- Custom aggregator function supported

#### Handoff Workflow

```csharp
var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
    .WithHandoffs(triageAgent, [mathTutor, historyTutor])
    .WithHandoffs([mathTutor, historyTutor], triageAgent)
    .Build();
```

Agents use tool-calling to hand off to other agents. The triage agent gets tools that represent the other agents and calls them to transfer.

#### Group Chat

```csharp
var workflow = AgentWorkflowBuilder.CreateGroupChatBuilderWith(
        agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 5 })
    .AddParticipants(agent1, agent2, agent3)
    .WithName("My Group Chat")
    .WithDescription("A round-robin group chat workflow.")
    .Build();
```

---

## 2. Graph-Based Workflows

### WorkflowBuilder API

The `WorkflowBuilder` is the core graph construction API:

```csharp
WorkflowBuilder builder = new(startExecutor);
builder.AddEdge(executorA, executorB);           // Direct edge
builder.AddFanOutEdge(start, [a, b, c]);         // Fan-out to multiple
builder.AddFanInBarrierEdge([a, b, c], end);     // Fan-in barrier
builder.AddSwitch(critic, sw => sw               // Conditional routing
    .AddCase<CriticDecision>(cd => cd.Approved == true, summary)
    .AddCase<CriticDecision>(cd => cd.Approved == false, writer));
builder.WithOutputFrom(outputExecutor);
builder.WithName("My Workflow");
var workflow = builder.Build();
```

### Key Workflow Features

#### Streaming

All workflow execution supports streaming via `InProcessExecution.RunStreamingAsync()`:

```csharp
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    // ExecutorCompletedEvent, AgentResponseUpdateEvent, WorkflowOutputEvent, etc.
}
```

Event types include:
- `ExecutorCompletedEvent` — An executor finished processing
- `AgentResponseUpdateEvent` — Streaming update from an agent executor
- `WorkflowOutputEvent` — Final workflow output
- `WorkflowErrorEvent` — Error during execution

#### Checkpointing

From `dotnet/samples/03-workflows/Checkpoint/`:
- Checkpoint and Resume — Save/restore workflow state for time-travel
- Checkpoint and Rehydrate — Hydrate new workflow instance from saved checkpoint
- Checkpoint with Human-in-the-Loop — Combine pausing with user interaction

#### Human-in-the-Loop

From `dotnet/samples/03-workflows/HumanInTheLoop/`:
- Uses input ports and external requests to pause execution
- Workflow halts at a port, external code provides a response, then execution resumes

#### Shared State

Via `IWorkflowContext`:

```csharp
// Write state
await context.QueueStateUpdateAsync("key", value, scopeName: "scope");
// Read state
var state = await context.ReadStateAsync<T>("key", scopeName: "scope");
```

#### Sub-Workflows

Workflows can be embedded as executors within parent workflows:

```csharp
var subWorkflow = new WorkflowBuilder(uppercase)
    .AddEdge(uppercase, reverse)
    .WithOutputFrom(reverse)
    .Build();

ExecutorBinding subWorkflowExecutor = subWorkflow.BindAsExecutor("TextProcessing");

var mainWorkflow = new WorkflowBuilder(prefix)
    .AddEdge(prefix, subWorkflowExecutor)
    .AddEdge(subWorkflowExecutor, postProcess)
    .WithOutputFrom(postProcess)
    .Build();
```

#### Conditional Routing (Switch-Case)

The Writer-Critic pattern from `07_WriterCriticWorkflow`:

```csharp
WorkflowBuilder workflowBuilder = new WorkflowBuilder(writer)
    .AddEdge(writer, critic)
    .AddSwitch(critic, sw => sw
        .AddCase<CriticDecision>(cd => cd?.Approved == true, summary)
        .AddCase<CriticDecision>(cd => cd?.Approved == false, writer))  // Loop back
    .WithOutputFrom(summary);
```

#### Observability

OpenTelemetry integration available via `OpenTelemetryWorkflowBuilderExtensions`.

#### Visualization

```csharp
string mermaid = workflow.ToMermaidString();
```

---

## 3. Mixing Agent Providers in a Workflow

### Confirmed: Multi-Provider Workflows Work

From `dotnet/samples/03-workflows/_StartHere/04_MultiModelService/Program.cs` — this sample explicitly demonstrates using **three different AI providers** in a single sequential workflow:

```csharp
// Amazon Bedrock (AWS)
IChatClient aws = new AmazonBedrockRuntimeClient(...)
    .AsIChatClient("amazon.nova-pro-v1:0");

// Anthropic Claude
IChatClient anthropic = new Anthropic.AnthropicClient(
    new() { ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") })
    .AsIChatClient("claude-sonnet-4-20250514");

// OpenAI
IChatClient openai = new OpenAI.OpenAIClient(...)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

// Create agents with different providers
AIAgent researcher = new ChatClientAgent(aws, instructions: "...");
AIAgent factChecker = new ChatClientAgent(openai, instructions: "...", [new HostedWebSearchTool()]);
AIAgent reporter = new ChatClientAgent(anthropic, instructions: "...");

// Build sequential workflow: Researcher (AWS) -> Fact-Checker (OpenAI) -> Reporter (Anthropic)
AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(researcher, factChecker, reporter)
    .AsAIAgent();

// Run the workflow, streaming output
await foreach (var update in workflowAgent.RunStreamingAsync(Topic))
{
    Console.Write(update.Text);
}
```

### Implications for GitHubCopilotAgent in Workflows

Since `GitHubCopilotAgent` extends `AIAgent`, it can participate in any workflow that accepts `AIAgent` instances. A mixed-provider workflow could include:

```csharp
AIAgent researcher = new ChatClientAgent(anthropicClient, instructions: "...");
AIAgent copilotAgent = copilotClient.AsAIAgent(name: "Copilot Coder");

// Mix providers
Workflow workflow = AgentWorkflowBuilder.BuildSequential(researcher, copilotAgent);
```

**Caveat**: `GitHubCopilotAgent` uses `CopilotClient` which has its own session management (via `CopilotSession`). When used in workflows it should work since `BindAsExecutor` wraps agents in `AIAgentHostExecutor` which handles the Chat Protocol (ChatMessage + TurnToken).

---

## 4. The GitHubCopilotAgent Class

### Source: `dotnet/src/Microsoft.Agents.AI.GitHub.Copilot/GitHubCopilotAgent.cs`

#### Class Hierarchy

```
AIAgent (abstract)                     ← Microsoft.Agents.AI namespace
  └── GitHubCopilotAgent (sealed)      ← Microsoft.Agents.AI.GitHub.Copilot namespace
```

**Implements**: `AIAgent, IAsyncDisposable`

#### Key Members

```csharp
public sealed class GitHubCopilotAgent : AIAgent, IAsyncDisposable
{
    // Constructors
    public GitHubCopilotAgent(
        CopilotClient copilotClient,
        SessionConfig? sessionConfig = null,
        bool ownsClient = false,
        string? id = null, string? name = null, string? description = null);

    public GitHubCopilotAgent(
        CopilotClient copilotClient,
        bool ownsClient = false,
        string? id = null, string? name = null, string? description = null,
        IList<AITool>? tools = null,
        string? instructions = null);

    // Core overrides
    protected override Task<AgentResponse> RunCoreAsync(...);
    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(...);
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(...);

    // Properties
    protected override string? IdCore => this._id;
    public override string Name => this._name;
    public override string Description => this._description;
}
```

#### How RunAsync Works

`RunCoreAsync` delegates to `RunCoreStreamingAsync` and aggregates:

```csharp
protected override Task<AgentResponse> RunCoreAsync(...)
    => this.RunCoreStreamingAsync(messages, session, options, cancellationToken)
        .ToAgentResponseAsync(cancellationToken);
```

`RunCoreStreamingAsync`:
1. Creates or resumes a `CopilotSession` via the `CopilotClient`
2. Subscribes to session events via `copilotSession.On(evt => ...)`
3. Converts Copilot SDK events to `AgentResponseUpdate` objects
4. Sends the user prompt via `copilotSession.SendAsync(messageOptions)`
5. Yields `AgentResponseUpdate` items from a channel as they arrive
6. Handles `DataContent` attachments by writing to temp files

Event mapping:
- `AssistantMessageDeltaEvent` → `AgentResponseUpdate` with `TextContent` (streaming tokens)
- `AssistantMessageEvent` → `AgentResponseUpdate` with full message
- `AssistantUsageEvent` → `AgentResponseUpdate` with `UsageContent`
- `SessionIdleEvent` → Signals completion, closes channel
- `SessionErrorEvent` → Error update, closes channel with exception

### CopilotClientExtensions

From `dotnet/src/Microsoft.Agents.AI.GitHub.Copilot/CopilotClientExtensions.cs`:

```csharp
public static class CopilotClientExtensions
{
    public static AIAgent AsAIAgent(
        this CopilotClient client,
        SessionConfig? sessionConfig = null,
        bool ownsClient = false,
        string? id = null, string? name = null, string? description = null)
    {
        return new GitHubCopilotAgent(client, sessionConfig, ownsClient, id, name, description);
    }

    public static AIAgent AsAIAgent(
        this CopilotClient client,
        bool ownsClient = false,
        string? id = null, string? name = null, string? description = null,
        IList<AITool>? tools = null, string? instructions = null)
    {
        return new GitHubCopilotAgent(client, ownsClient, id, name, description, tools, instructions);
    }
}
```

### Can It Participate in Workflows?

**Yes.** `GitHubCopilotAgent` extends `AIAgent`, so it can be:
- Passed to `AgentWorkflowBuilder.BuildSequential()`
- Bound as an executor via `agent.BindAsExecutor()`
- Used in any workflow pattern (sequential, concurrent, handoffs, group chat)

---

## 5. AgentResponse Type

### Source: `dotnet/src/Microsoft.Agents.AI.Abstractions/AgentResponse.cs`

```csharp
public class AgentResponse
{
    public IList<ChatMessage> Messages { get; set; }   // Response messages
    public string Text { get; }                         // Concatenated text content
    public string? AgentId { get; set; }
    public string? ResponseId { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public ChatFinishReason? FinishReason { get; set; }
    public UsageDetails? Usage { get; set; }
    public object? RawRepresentation { get; set; }
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    public ResponseContinuationToken? ContinuationToken { get; set; }  // [Experimental]
    
    // Constructors
    public AgentResponse();
    public AgentResponse(ChatMessage message);
    public AgentResponse(ChatResponse response);
    public AgentResponse(IList<ChatMessage>? messages);
    
    // Convert to streaming updates
    public AgentResponseUpdate[] ToAgentResponseUpdates();
    
    public override string ToString() => this.Text;
}
```

### How Output Flows Between Agents in Workflows

In sequential workflows, the framework handles output-to-input flow automatically:

1. **Agent A** produces `AgentResponse` containing `IList<ChatMessage>` (Messages property)
2. The workflow engine passes these messages (with role reassignment) as input to **Agent B**
3. When `ReassignOtherAgentsAsUsers = true` is set (default for `BuildSequential`), previous agent responses get role-adjusted so the next agent sees them as user messages
4. `ForwardIncomingMessages = true` ensures that original workflow input messages are also forwarded

For custom executors, outputs flow via the return value of `HandleAsync`:

```csharp
// Executor<TInput, TOutput> — return value is sent along edges
public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, ...)
    => ValueTask.FromResult(message.ToUpperInvariant());
```

### Manual Agent Chaining (Outside Workflows)

```csharp
AgentResponse response1 = await agent1.RunAsync("Initial prompt");
// Pass output as input to next agent
AgentResponse response2 = await agent2.RunAsync(response1.Messages);
```

### AIAgent.RunAsync Overloads

```csharp
public Task<AgentResponse> RunAsync(AgentSession? session = null, ...);
public Task<AgentResponse> RunAsync(string message, AgentSession? session = null, ...);
public Task<AgentResponse> RunAsync(ChatMessage message, AgentSession? session = null, ...);
public Task<AgentResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, ...);

// Streaming variants
public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(string message, ...);
public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages, ...);
```

---

## 6. AGUI Hosting with Workflows

### AGUI Server Setup

From `dotnet/samples/02-agents/AGUI/Step01_GettingStarted/Server/Program.cs`:

```csharp
builder.Services.AddAGUI();

AIAgent agent = chatClient.AsAIAgent(name: "AGUIAssistant", instructions: "...");
app.MapAGUI("/", agent);
```

### How AGUI Works

- **Server-Side**: Client sends HTTP POST → ASP.NET endpoint via `MapAGUI` → Agent processes → Responses stream back as Server-Sent Events (SSE)
- **Client-Side**: `AGUIAgent` sends POST → Server responds with SSE stream → Client parses `AgentResponseUpdate` objects

### Workflow + AGUI Integration

Since workflows can be wrapped as `AIAgent` instances via `.AsAIAgent()`:

```csharp
// Build a workflow
Workflow workflow = AgentWorkflowBuilder.BuildSequential(agent1, agent2, agent3);

// Wrap as AIAgent
AIAgent workflowAgent = workflow.AsAIAgent(
    id: "my-workflow",
    name: "Multi-Agent Pipeline",
    description: "Sequential pipeline of three agents");

// Host via AGUI/SSE
app.MapAGUI("/", workflowAgent);
```

This means:
1. Workflow execution streams `AgentResponseUpdate` items from `RunStreamingAsync`
2. AGUI maps these to SSE events
3. The client receives real-time streaming from all agents in the pipeline

### WorkflowHostAgent (Internal)

The `Workflow.AsAIAgent()` extension creates a `WorkflowHostAgent` that:
- Wraps the `Workflow` as an `AIAgent`
- Implements `RunCoreAsync` and `RunCoreStreamingAsync` by invoking the workflow
- Uses a `WorkflowSession` for state management
- Validates the workflow accepts the Chat Protocol
- Supports checkpointing
- Merges streaming updates from all workflow executors

### WorkflowAsAnAgent Sample

From `dotnet/samples/03-workflows/Agents/WorkflowAsAnAgent/Program.cs`:

```csharp
var workflow = WorkflowFactory.BuildWorkflow(chatClient);
var agent = workflow.AsAIAgent("workflow-agent", "Workflow Agent");
var session = await agent.CreateSessionAsync();

// Interact with the workflow as a normal agent
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(input, session))
{
    Console.Write(update.Text);
}
```

---

## Key Discoveries Summary

| Question | Answer |
|----------|--------|
| How do you build sequential pipelines? | `WorkflowBuilder` with `AddEdge()` or `AgentWorkflowBuilder.BuildSequential(agents)` |
| What is the graph API? | `WorkflowBuilder` — supports edges, fan-out, fan-in, switch/case, sub-workflows |
| Can you mix providers? | **Yes** — confirmed by `04_MultiModelService` sample using AWS Bedrock + Anthropic + OpenAI |
| Can GitHubCopilotAgent participate? | **Yes** — it extends `AIAgent`, can be used in any workflow pattern |
| What does RunAsync return? | `Task<AgentResponse>` containing `IList<ChatMessage>`, with `.Text` for concatenated text |
| How does output flow between agents? | Workflow engine passes `ChatMessage`s from agent A as input to agent B with role reassignment |
| Can workflows stream to AGUI? | **Yes** — `workflow.AsAIAgent()` wraps workflow as `AIAgent`, then `MapAGUI("/", workflowAgent)` |

## NuGet Packages

- `Microsoft.Agents.AI.Abstractions` — `AIAgent`, `AgentResponse`, `AgentResponseUpdate`
- `Microsoft.Agents.AI.Workflows` — `WorkflowBuilder`, `AgentWorkflowBuilder`, `InProcessExecution`, `Executor<T>`
- `Microsoft.Agents.AI.GitHub.Copilot` — `GitHubCopilotAgent`, `CopilotClientExtensions`
- `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` — `MapAGUI()` for SSE hosting

## References

- Agent Framework repo: https://github.com/microsoft/agent-framework
- Workflow samples: `dotnet/samples/03-workflows/`
- _StartHere foundational samples: `dotnet/samples/03-workflows/_StartHere/`
- Multi-model sample: `dotnet/samples/03-workflows/_StartHere/04_MultiModelService/Program.cs`
- AGUI samples: `dotnet/samples/02-agents/AGUI/`
- AIAgent source: `dotnet/src/Microsoft.Agents.AI.Abstractions/AIAgent.cs`
- AgentResponse source: `dotnet/src/Microsoft.Agents.AI.Abstractions/AgentResponse.cs`
- GitHubCopilotAgent source: `dotnet/src/Microsoft.Agents.AI.GitHub.Copilot/GitHubCopilotAgent.cs`
- CopilotClientExtensions: `dotnet/src/Microsoft.Agents.AI.GitHub.Copilot/CopilotClientExtensions.cs`
- AgentWorkflowBuilder: `dotnet/src/Microsoft.Agents.AI.Workflows/AgentWorkflowBuilder.cs`
- WorkflowHostAgent: `dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowHostAgent.cs`
- WorkflowHostingExtensions: `dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowHostingExtensions.cs`
