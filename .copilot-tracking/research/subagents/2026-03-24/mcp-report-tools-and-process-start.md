# Research: MCP Report Tools & Process.Start Python In-Container

## Research Topics

1. **OPTION E**: Agent Framework + Process.Start Python In-Container
2. **OPTION F**: MCP-Based Report Tools (No Second Agent)

---

## OPTION E: Agent Framework + Process.Start Python In-Container

### E1. How Process.Start("python3", "script.py") Works in a .NET Container

`System.Diagnostics.Process.Start` on Linux (inside Docker) calls `fork()`/`exec()` to spawn the Python interpreter as a child process. The .NET `ProcessStartInfo` class provides controls:

```csharp
var psi = new ProcessStartInfo
{
    FileName = "python3",
    Arguments = "/app/scripts/generate_report.py --format pdf",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true,
    WorkingDirectory = "/app/scripts"
};
using var process = Process.Start(psi);
```

**Security model**: The child process runs with the same Linux user (`$APP_UID`) as the .NET process by default. There is no sandbox, no cgroup isolation, and no separate namespace — it is a peer process in the same container. The .NET docs explicitly warn: "Calling this method with untrusted data is a security risk. Call this method only with trusted data."

**Key concern**: Because the Python scripts are bundled into the container image at build time (not user-supplied), the command injection risk is limited to the arguments passed to the script. Arguments derived from user input (date ranges, report types) must be strictly validated.

### E2. Dockerfile for .NET 10 + Python 3 + pip Packages

The base `mcr.microsoft.com/dotnet/aspnet:10.0` image is based on Debian (bookworm). Python 3 can be installed from apt:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root

# Install Python 3 + pip + system dependencies for matplotlib
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        python3 \
        python3-pip \
        python3-venv \
        libfreetype6 \
        libpng16-16 \
    && python3 -m venv /opt/pyenv \
    && /opt/pyenv/bin/pip install --no-cache-dir \
        pandas \
        matplotlib \
        reportlab \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

ENV PATH="/opt/pyenv/bin:$PATH"
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Multi-stage build for .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Biotrackr.Report.Svc/Biotrackr.Report.Svc.csproj", "Biotrackr.Report.Svc/"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY scripts/ /app/scripts/
ENTRYPOINT ["dotnet", "Biotrackr.Report.Svc.dll"]
```

**Notes**:

- Must temporarily switch to `USER root` for apt-get, then switch back to `$APP_UID`.
- Using a Python venv avoids `externally-managed-environment` errors on Debian bookworm.
- `matplotlib` requires `libfreetype6` and `libpng16-16` as system dependencies.

### E3. Image Size Impact

| Component | Approximate Size |
|---|---|
| `mcr.microsoft.com/dotnet/aspnet:10.0` (Debian bookworm) | ~220 MB |
| Python 3 + pip + venv | ~50–70 MB |
| pandas | ~70 MB |
| matplotlib (+ dependencies like numpy) | ~80–100 MB |
| reportlab | ~10–15 MB |
| System libs (freetype, png) | ~5 MB |
| **Total estimated image size** | **~435–480 MB** |

Compared to the existing MCP Server Dockerfile (pure .NET, ~220 MB), adding the Python stack roughly **doubles the image size** (~+200–260 MB). This impacts:

- Container App pull times and cold start latency.
- ACR storage costs (marginal).
- Build pipeline times.

### E4. stdout/stderr Streaming from the Python Process

```csharp
var psi = new ProcessStartInfo
{
    FileName = "python3",
    Arguments = "/app/scripts/generate_chart.py",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = new Process { StartInfo = psi };
process.OutputDataReceived += (sender, e) =>
{
    if (e.Data != null) logger.LogInformation("[Python stdout] {Line}", e.Data);
};
process.ErrorDataReceived += (sender, e) =>
{
    if (e.Data != null) logger.LogWarning("[Python stderr] {Line}", e.Data);
};

process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();
await process.WaitForExitAsync(cancellationToken);

if (process.ExitCode != 0)
    throw new InvalidOperationException($"Python script failed with exit code {process.ExitCode}");
```

**Important caveats**:

- **Deadlock risk**: If both stdout and stderr buffers fill simultaneously, the child process can block. Using `BeginOutputReadLine()` + `BeginErrorReadLine()` with async events avoids this.
- **Binary output**: For binary artifacts (PDF/PNG), the Python script should write to a file and return the file path via stdout, rather than streaming binary through stdout.
- **Encoding**: `StandardOutputEncoding` / `StandardErrorEncoding` should be set to UTF-8 if the script outputs non-ASCII text.

### E5. Sandboxing: Running Python as a Restricted User

**Within-container sandboxing options**:

1. **Dedicated Linux user**: Create a non-root user with minimal permissions. The Python process runs under this user while the .NET host runs as `$APP_UID`. However, both users are in the same container filesystem namespace.
2. **Read-only filesystem**: Mount the scripts directory read-only and provide a specific writable `/tmp/reports` directory for output.
3. **Resource limits via ProcessStartInfo**: .NET does not provide CPU/memory limits for child processes. Container-level resource limits (Container Apps CPU/memory settings) apply to **all** processes in the container.
4. **Timeout enforcement**: Use `CancellationToken` with `process.WaitForExitAsync()` and `process.Kill()` as a fallback.

**What you cannot do**:

- No cgroup-level isolation for the child process within the same container.
- No seccomp/AppArmor profiles per-process (only per-container).
- No network namespace isolation — Python can make outbound HTTP requests.

### E6. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| **Process hangs / infinite loop** | High | Enforce timeout via `CancellationToken` + `Kill(entireProcessTree: true)`. |
| **Resource exhaustion** (Python consuming all container memory) | High | Container Apps memory limit is the only protection. Python has no built-in memory cap. |
| **Exit code mishandling** | Medium | Always check `process.ExitCode`. |
| **Zombie processes** | Medium | Ensure `process.Dispose()` is called. Use `Kill(entireProcessTree: true)` if the script spawns sub-processes. |
| **Orphaned temp files** | Low | Clean up output directory after use. |
| **Command injection** | Medium | Never interpolate user input into arguments without validation. Use validated parameters only. |
| **Python dependency vulnerabilities** | Medium | Pin versions in `requirements.txt`. Scan with Dependabot or pip-audit. |
| **Different failure modes (2 runtimes)** | Medium | .NET and Python failures manifest differently. Unified error handling in the .NET host is needed. |

### E7. Production Usage

**The Process.Start-Python-in-container pattern is used in production**, but it is more common in:

- **Data pipeline containers** where a Python script runs once per invocation (batch jobs, Azure Functions with custom handlers).
- **ML inference containers** where .NET is the API layer and Python handles model inference.
- **Legacy migration scenarios** where Python scripts exist and rewriting is not cost-effective.

**Known issues**:

- **Two-language debugging**: Debugging across .NET and Python in the same container is significantly harder. No unified stack trace.
- **CI/CD complexity**: Two dependency ecosystems (NuGet + pip) in one container.
- **Upgrade coupling**: Python runtime and .NET runtime upgrades must be coordinated.
- **Container scanning**: Security scanners must cover both ecosystems.

**General industry guidance**: This pattern is acceptable for trusted, bundled scripts but is considered a **code smell** for new greenfield development. Pure .NET libraries are preferred when available.

---

## OPTION F: MCP-Based Report Tools (No Second Agent)

### F1. MCP Protocol Support for Binary Data (Images, PDFs)

The MCP specification (2025-06-18 revision) **natively supports binary content in tool responses**:

**Image content** — First-class support via `ImageContentBlock`:

```json
{
  "type": "image",
  "data": "base64-encoded-data",
  "mimeType": "image/png"
}
```

**Embedded resources** — Arbitrary binary data via `EmbeddedResourceBlock` with `BlobResourceContents`:

```json
{
  "type": "resource",
  "resource": {
    "uri": "report://weekly/2026-03-24.pdf",
    "mimeType": "application/pdf",
    "blob": "<base64-encoded-pdf-bytes>"
  }
}
```

**Audio content** — Also supported (not relevant here but shows extensibility).

**C# MCP SDK support** (modelcontextprotocol/csharp-sdk):

The SDK fully supports returning binary content from tools. Example patterns:

```csharp
// Return an image directly
[McpServerTool, Description("Generates a chart image")]
public static ImageContentBlock GenerateChart()
{
    byte[] pngBytes = CreateChart();
    return ImageContentBlock.FromBytes(pngBytes, "image/png");
}

// Return mixed content (text description + image)
[McpServerTool, Description("Generates a report with chart")]
public static IEnumerable<ContentBlock> GenerateWeeklyReport()
{
    yield return new TextContentBlock { Text = "Weekly activity summary." };
    yield return ImageContentBlock.FromBytes(chartBytes, "image/png");
}

// Return a PDF as an embedded resource
[McpServerTool, Description("Generates a PDF report")]
public static EmbeddedResourceBlock GeneratePdfReport()
{
    byte[] pdfBytes = CreatePdf();
    return new EmbeddedResourceBlock
    {
        Resource = new BlobResourceContents
        {
            Uri = "report://weekly.pdf",
            MimeType = "application/pdf",
            Blob = Encoding.UTF8.GetBytes(Convert.ToBase64String(pdfBytes))
        }
    };
}
```

**Key finding**: Tools can also return `DataContent` from `Microsoft.Extensions.AI`, which the SDK automatically maps to the correct MCP content block based on MIME type. Image MIME types become `ImageContentBlock`, and other types become `EmbeddedResourceBlock`.

**Practical limitation for PDFs**: While images are natively renderable and LLMs can "see" them, **PDFs returned as base64 embedded resources are opaque blobs** to the LLM. The LLM cannot read or analyze the PDF contents — it can only pass them through to the user. This means:

- Chart images (PNG) returned from tools **can be analyzed by Claude** in the response.
- PDFs returned from tools are **pass-through only** — the LLM cannot describe their contents.
- A practical approach: Return chart images + text summary + a PDF download link.

### F2. Multi-Step Report Orchestration by Chat.Api + Claude

**Can Claude orchestrate multi-step report generation using MCP tools?** Yes.

Claude can already orchestrate multi-step workflows using tools. The existing Biotrackr setup demonstrates this:

1. User asks: "Give me a weekly activity summary for last week."
2. Claude calls `GetActivityByDateRange` tool.
3. Claude interprets the results and presents a summary.

For report generation, the workflow extends naturally:

1. User asks: "Generate a weekly health report for last week."
2. Claude calls `GetActivityByDateRange`, `GetSleepByDateRange`, `GetWeightByDateRange`, `GetFoodByDateRange` (data gathering).
3. Claude calls `GenerateWeeklyChart` with the gathered data (chart generation).
4. Claude calls `GenerateWeeklyPdf` with data + chart (PDF composition).
5. Claude returns the chart image inline + a link to the PDF.

**Considerations**:

- **Context window**: Large datasets serialized as JSON in tool results consume tokens. Pagination is already supported in existing tools.
- **Multiple tool calls**: Claude supports parallel and sequential tool calling. A report requiring 4 data fetches + 2 generation steps is well within its capabilities.
- **System prompt update**: The existing system prompt at `scripts/chat-system-prompt/system-prompt.txt` would need additions to instruct Claude how and when to use report generation tools.

### F3. Complexity Trade-offs vs. Separate Biotrackr.Report.Svc

| Aspect | Option F: MCP Tools in Existing Server | Separate Report.Svc |
|---|---|---|
| **New services** | 0 | 1 new Container App |
| **New deployments** | 0 | 1 new deployment pipeline |
| **New infrastructure** | 0 | Bicep modules, Container App, identity, app config |
| **Code location** | `src/Biotrackr.Mcp.Server/Tools/` | New `src/Biotrackr.Report.Svc/` |
| **Dependencies** | Add QuestPDF + ScottPlot NuGet packages to MCP Server | Same packages in new project |
| **Testing** | Extend existing unit/integration tests | New test project |
| **Scaling** | Report generation shares resources with MCP Server | Independent scaling |
| **Failure blast radius** | A report generation OOM/crash affects MCP Server | Isolated failures |
| **Image size impact** | MCP Server image grows (QuestPDF is ~36 MB NuGet) | New image only for Report.Svc |
| **Multi-agent orchestration** | Not needed — Claude calls tools directly | Needed if using Agent Framework |
| **Deployment risk** | Changes to MCP Server affect existing chat functionality | No impact on existing services |

**Key trade-off**: Option F is **significantly simpler** to implement and deploy (zero new infrastructure), but it **couples** report generation with the MCP Server. Heavy report generation could impact the responsiveness of existing data tools.

**Mitigation for coupling**: Report generation tools could be rate-limited or run on a background thread with a timeout. QuestPDF is documented as "thousands of pages per second" with minimal CPU/memory, so the impact for single-report generation should be negligible.

### F4. Storing and Serving Generated Report Artifacts

**Options for serving generated PDFs/images from MCP tools**:

1. **Inline base64 in MCP response**: The simplest approach. The tool returns the PDF/image as a base64-encoded `EmbeddedResourceBlock` or `ImageContentBlock`.
   - **Pros**: No external storage needed. Self-contained.
   - **Cons**: Large PDFs inflate the MCP response size and consume LLM context window tokens. A 2 MB PDF becomes ~2.7 MB base64.
   - **Practical limit**: Charts (PNG, ~50-200 KB) work well inline. Multi-page PDFs should use a different approach.

2. **Azure Blob Storage + SAS URL**: The tool generates the PDF, uploads it to Blob Storage, and returns a time-limited SAS URL.
   - **Pros**: Minimal MCP response size. Standard Azure pattern. URL can be shared.
   - **Cons**: Requires Blob Storage account, managed identity permissions, and SAS token generation. Adds infrastructure.
   - **Approach**: `GenerateWeeklyPdf` tool → generates PDF → uploads to `reports/{userId}/{date}.pdf` → returns SAS URL (1-hour expiry).

3. **Temporary in-memory cache with download endpoint**: The MCP Server generates the PDF, stores it in an in-memory cache with a GUID key, and returns a URL like `https://mcp-server/reports/{guid}`. A new HTTP endpoint serves the file.
   - **Pros**: No external storage. Simple.
   - **Cons**: Lost on restart. Memory pressure. Not suitable for large reports.

4. **Hybrid approach** (recommended): Return chart images inline (base64 `ImageContentBlock`) and PDFs via Blob Storage SAS URL.

### F5. Existing MCP Server Tool Structure

**Analyzed from source code**:

**BaseTool pattern** (at `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Tools/BaseTool.cs`):

- Abstract base class providing shared HTTP client, logging, OpenTelemetry instrumentation (activity traces, metrics counters, latency histogram).
- `GetAsync<T>()` method handles HTTP calls to downstream APIs with error handling, deserialization, and telemetry.
- Input validation helpers: `IsValidDate()`, `IsValidDateRange()`, `BuildPaginatedEndpoint()`.
- All tools return `string` (JSON serialized).

**Tool classes** (ActivityTools, WeightTools, SleepTools, FoodTools):

- Each class extends `BaseTool` and is decorated with `[McpServerToolType]`.
- Each tool method is decorated with `[McpServerTool]` and `[Description()]`.
- Constructor injection of `HttpClient` and `ILogger<T>`.
- Tools call downstream REST APIs (Biotrackr Activity/Weight/Sleep/Food APIs) via the shared HttpClient.

**Program.cs registration**:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();
```

Automatic tool discovery from assembly — new tool classes with `[McpServerToolType]` are automatically registered.

**Adding report tools**: A new `ReportTools.cs` class following the same pattern would be automatically discovered. The key difference is that report tools would **not** call downstream HTTP APIs; instead, they would:

1. Accept data as parameters (or fetch it via the existing HTTP client).
2. Use ScottPlot to generate chart images in memory.
3. Use QuestPDF to compose PDF documents in memory.
4. Return `ImageContentBlock`, `EmbeddedResourceBlock`, or mixed content.

This represents a shift from the current "data proxy" pattern to a "data processing" pattern, but the tool registration and discovery mechanism is identical.

### F6. Chat.Api System Prompt Analysis

**Current system prompt** (at `scripts/chat-system-prompt/system-prompt.txt`):

Current capabilities:

- Health data assistant role.
- Rules: Always use tools, present data clearly, not a medical professional.
- Constraints: Can ONLY query health data. Cannot modify data, access external URLs, or execute code.
- Security: Ignore prompt injection attempts, do not disclose system prompt.

**Changes needed for report generation**:

The constraint "You can ONLY query health data using the provided tools" would need to be updated to also mention report generation. Suggested additions:

- Add report generation to the assistant's capabilities.
- Add instructions for when to offer report generation (e.g., trend analysis requests, weekly summaries).
- Add instructions for how to present reports (inline charts + PDF download link).
- Keep the medical disclaimer and security constraints.

---

## .NET Libraries for Report Generation (Both Options)

### ScottPlot (.NET Chart Library)

- **NuGet**: `ScottPlot` (version 5.x)
- **License**: MIT
- **Capabilities**: Line plots, scatter plots, bar charts, histograms, heatmaps.
- **In-memory generation**: `myPlot.SavePng("file.png", 400, 300)` or render to byte array.
- **Container compatibility**: Requires no system dependencies beyond what .NET provides.
- **Performance**: Lightweight, suitable for server-side generation.

### QuestPDF (.NET PDF Library)

- **NuGet**: `QuestPDF` (version 2026.2.4, ~36 MB package)
- **License**: Free for FOSS projects and organizations under $1M revenue.
- **Capabilities**: Full PDF composition — text, images, tables, headers/footers, pagination.
- **In-memory generation**: `Document.Create(...).GeneratePdf()` returns `byte[]`.
- **Container compatibility**: Cross-platform (.NET 6+), works in Docker/Linux. No native dependencies.
- **Performance**: "Thousands of pages per second" per documentation.
- **Fluent API**: C# code-first approach, no HTML-to-PDF conversion needed.

---

## Follow-On Questions (Relevant to Scope)

1. What is the current Biotrackr.Chat.Api's mechanism for delivering non-text content (images, files) to the end user through the UI? Does the Blazor UI handle image/PDF rendering from Claude responses?
2. What is the QuestPDF license classification for Biotrackr? (It's FOSS, so Community license applies — confirmed free.)
3. Would Azure Container Apps handle the increased memory for QuestPDF/ScottPlot report generation within the current resource allocation?

---

## Key Discoveries

### MCP Binary Support is a First-Class Feature

The MCP specification and C# SDK both fully support returning images (`ImageContentBlock`) and arbitrary binary data (`EmbeddedResourceBlock`) from tools. The C# SDK provides `ImageContentBlock.FromBytes()` and automatic `DataContent` mapping. This is proven in the SDK's test suite and sample code.

### Adding Report Tools to the Existing MCP Server Is Architecturally Simple

The existing tool registration pattern (`[McpServerToolType]` + `WithToolsFromAssembly()`) means adding new tool classes requires zero infrastructure changes. A `ReportTools.cs` class would be automatically discovered.

### Process.Start Python Is Functional but Not Ideal for Greenfield

The pattern works and is used in production for legacy/ML scenarios, but it introduces significant complexity (two runtimes, doubled image size, two dependency ecosystems, debugging difficulty) compared to pure .NET alternatives like QuestPDF + ScottPlot.

### QuestPDF + ScottPlot Cover All Report Requirements in Pure .NET

Both libraries run cross-platform in Docker, generate output in-memory (byte arrays), and are actively maintained. This eliminates the need for Python entirely.

---

## Clarifying Questions

1. **UI delivery mechanism**: How does the Biotrackr.UI (Blazor) currently handle multi-modal content from Claude responses? Can it render inline images and provide file download links? This affects whether MCP tools should return inline base64 or Blob Storage URLs.
2. **Report frequency expectation**: Are reports generated on-demand per user request, or is there a scheduled batch generation use case? On-demand favors Option F; batch favors a separate service.
3. **Report size expectations**: Are weekly reports expected to be simple (1-2 charts + summary text) or complex (multi-page PDFs with many charts)? Simple reports favor inline base64; complex reports favor Blob Storage.

---

## References

- MCP Specification (2025-06-18): https://modelcontextprotocol.io/specification/2025-06-18/server/tools/
- MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- .NET Process.Start docs: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start
- .NET ProcessStartInfo docs: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo
- QuestPDF: https://www.questpdf.com/ (NuGet: https://www.nuget.org/packages/QuestPDF)
- ScottPlot: https://scottplot.net/ (NuGet: https://www.nuget.org/packages/ScottPlot)
- Docker Hub - ASP.NET: https://hub.docker.com/r/microsoft/dotnet-aspnet/
- Docker Hub - Python: https://hub.docker.com/_/python/
- Biotrackr MCP Server source: `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Tools/`
- Biotrackr Chat system prompt: `scripts/chat-system-prompt/system-prompt.txt`
