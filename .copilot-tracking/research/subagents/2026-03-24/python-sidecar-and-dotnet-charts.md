# Research: Python Sidecar vs .NET Report Generation

## Research Topics

- **OPTION A**: Python Sidecar Container in Azure Container Apps
- **OPTION B**: Pure .NET Report Generation (Charts, PDFs, Visualizations)

## Status: Complete

---

## OPTION A: Python Sidecar Container

### ACA Sidecar Architecture

Source: [Azure Container Apps — Containers](https://learn.microsoft.com/en-us/azure/container-apps/containers#sidecar-containers)

**How sidecars work:**

- Multiple containers in a single Container App share hard disk and network resources and experience the same application lifecycle.
- Sidecar containers are defined in the `containers` array alongside the main container in `properties.template`.
- All containers within a replica share the same network namespace — **a .NET container can call a Python sidecar via `localhost` HTTP** (e.g., `http://localhost:8000/generate-report`).
- Use cases documented by Microsoft include: log forwarding agents, cache refresh processes, and data processing sidecars.

**Key constraints:**

- All containers in a replica must fit within the vCPU/memory allocation. Consumption plan environments are limited to 2 vCPU / 4 GiB total; Consumption workload profiles support up to 4 vCPU / 8 GiB.
- Linux-based (`linux/amd64`) container images only.
- Container images can total up to 8 GB per replica on Consumption workload profiles.

### Python FastAPI Sidecar Pattern

A Python sidecar for report generation would look like:

```python
# main.py — FastAPI sidecar
from fastapi import FastAPI
from pydantic import BaseModel
import matplotlib
matplotlib.use('Agg')  # headless rendering
import matplotlib.pyplot as plt
import io, base64

app = FastAPI()

class ReportRequest(BaseModel):
    report_type: str  # "weight_trend", "sleep_summary", etc.
    data: dict
    format: str = "png"  # "png" or "pdf"

@app.post("/generate-report")
async def generate_report(req: ReportRequest):
    # Generate chart with matplotlib/seaborn
    fig, ax = plt.subplots()
    # ... plot data based on report_type ...
    buf = io.BytesIO()
    fig.savefig(buf, format=req.format, dpi=150, bbox_inches='tight')
    plt.close(fig)
    buf.seek(0)
    return {"image": base64.b64encode(buf.read()).decode(), "format": req.format}

@app.get("/health")
async def health():
    return {"status": "healthy"}
```

**Deployment approach:**

- Build a separate Docker image (e.g., `biotrackr-report-sidecar:latest`) with Python 3.12, FastAPI, uvicorn, matplotlib, pandas, seaborn.
- Push to the same Azure Container Registry.
- Deploy as a sidecar in the Chat API container app definition.
- The .NET Chat API calls `http://localhost:8000/generate-report` via HttpClient.

### Bicep Configuration for Sidecar

The sidecar is defined by adding a second entry to the `containers` array in the ACA template. Based on the existing `container-app-http.bicep` module pattern:

```bicep
template: {
  containers: [
    {
      // Main .NET container
      name: 'chat-api'
      image: 'myacr.azurecr.io/biotrackr-chat-api:latest'
      resources: { cpu: json('0.5'), memory: '1Gi' }
      env: envVariables
      probes: healthProbes
    }
    {
      // Python sidecar
      name: 'report-sidecar'
      image: 'myacr.azurecr.io/biotrackr-report-sidecar:latest'
      resources: { cpu: json('0.5'), memory: '1Gi' }
      env: [
        { name: 'PORT', value: '8000' }
      ]
      probes: [
        {
          type: 'Liveness'
          httpGet: { path: '/health', port: 8000 }
          initialDelaySeconds: 5
          periodSeconds: 10
        }
      ]
      volumeMounts: [
        { mountPath: '/shared', volumeName: 'shared-volume' }
      ]
    }
  ]
  volumes: [
    { name: 'shared-volume', storageType: 'EmptyDir' }
  ]
}
```

**Impact on existing Bicep modules:** The current `container-app-http.bicep` module hardcodes a single container in the `containers` array. To support sidecars, the module would need a new optional `sidecarContainers` parameter (type `array`, default `[]`) that gets merged into the containers list.

### Shared Volumes

Source: [Azure Container Apps — Storage Mounts](https://learn.microsoft.com/en-us/azure/container-apps/storage-mounts)

**Yes, sidecars and the main container can share volumes.**

- **Replica-scoped storage (EmptyDir):** Best fit. Files persist for the lifetime of the replica. Any container in the replica can mount the same volume. Data disappears when the replica shuts down. Ideal for transient artifacts like generated charts/PDFs.
- **Azure Files:** For persistent storage across replicas/revisions. Both SMB and NFS supported.
- **Ephemeral storage limits** by vCPU allocation:
  - 0.25 vCPU -> 1 GiB
  - 0.5 vCPU -> 2 GiB
  - 1 vCPU -> 4 GiB
  - Over 1 vCPU -> 8 GiB

For report generation, `EmptyDir` is sufficient — the sidecar writes the file, the main container reads it, and it doesn't need to persist beyond the request.

### Health Checks

Source: [Azure Container Apps — Health Probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)

**Yes, sidecars can have independent health probes.** Each container in the `containers` array can define its own `probes` array with Startup, Liveness, and Readiness probes (HTTP or TCP). However:

- Default probes are **only** auto-added to the **main app container** (the one serving ingress). The portal does not automatically add default probes to sidecar containers.
- Sidecar probes must be explicitly configured.
- Each probe must use the port of its own container (not the ingress port).
- Restrictions: only one probe of each type per container; `exec` probes not supported; gRPC not supported.

### Scaling

**Yes, sidecars scale with the main container.** All containers in a replica are co-located; when the ACA runtime scales the app (adding or removing replicas), each replica includes all containers.

**Resource overhead:**

- The sidecar's CPU/memory counts against the total allocation. With a Python sidecar needing ~0.5 vCPU / 1 GiB, combined with the .NET container at 0.5 vCPU / 1 GiB, the total per replica would be 1 vCPU / 2 GiB.
- Python container images with matplotlib/pandas typically run 500 MB–1 GB in image size.
- Cold start for the Python container (loading matplotlib) adds ~2-5 seconds to the first request.

---

## OPTION B: Pure .NET Report Generation

### Chart Generation Libraries

| Library | Version | License | Total Downloads | Target | Headless PNG Export | Notes |
|---------|---------|---------|-----------------|--------|---------------------|-------|
| **ScottPlot** | 5.1.57 | MIT | 4.1M | .NET 8+ / .NET Standard 2.0 | **Yes** — `myPlot.SavePng("chart.png", w, h)` | Best fit. Console/server-side ready. 30+ plot types (scatter, bar, pie, heatmap, signal, box, radar, line, candlestick). 6,475 GitHub stars. Active development. |
| **OxyPlot.Core** | 2.2.0 | MIT | 9.0M | .NET 6+ / .NET Standard 2.0 | **Yes** — via PNG export to stream | Mature cross-platform library. Good for line/scatter/bar/area charts. Last updated Sep 2024. |
| **LiveChartsCore.SkiaSharpView** | 2.0.0-rc6.1 | MIT | 1.9M | .NET 8+ / .NET Standard 2.0 | Possible via SkiaSharp backend | Primarily designed for interactive UI; server-side headless rendering is less documented. Still in prerelease (rc6). |
| **SkiaSharp** | 3.119.2 | MIT | 245.6M | .NET 6+ / .NET Standard 2.0 | **Yes** — full 2D graphics API | Low-level 2D rendering engine (Google Skia). Maximum flexibility but requires manual chart drawing code. Microsoft-owned. |

**Recommendation for this project:** ScottPlot is the clear leader for server-side headless chart generation in .NET:

- Explicit console/server-side quickstart: `myPlot.SavePng("file.png", 400, 300)` — no GUI dependency.
- Supports all chart types relevant to health data: line plots (weight trends), bar charts (activity comparison), scatter (correlation), pie (sleep stages), heatmaps (weekly patterns), box plots (distribution), radar charts (multi-metric summaries), histograms (data distribution), and regression lines.
- DateTime axis support for time-series health data.
- Built-in histogram, KDE, and regression support.
- Multiplot support for dashboard-style multi-chart layouts.

### PDF Generation Libraries

| Library | Version | License | Total Downloads | Notes |
|---------|---------|---------|-----------------|-------|
| **QuestPDF** | 2026.2.4 | Community (free for <$1M revenue, individuals, non-profits, FOSS) | 18.6M | **Top recommendation.** Fluent C# API, production-ready, companion preview app, PDF/A & PDF/UA compliance. Supports images (embed ScottPlot PNGs), tables, layouts, encryption, merge. Works on Windows/Linux/macOS + Docker. |
| **iText 7** (formerly iTextSharp) | 9.5.0 | AGPL (or commercial license) | 43.7M | Feature-rich but AGPL license requires open-sourcing your app or purchasing commercial license. Package `itext7` is deprecated in favor of `itext`. |
| **PdfSharpCore** | 1.3.67 | MIT | 30.5M | Port of PdfSharp for .NET Standard/Core. Lower-level API than QuestPDF. Adequate for simple PDFs but more manual work for complex layouts. |

**Recommendation:** QuestPDF. Its fluent API makes composing health reports with embedded charts, tables, and text straightforward:

```csharp
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Header().Text("Weekly Health Report").Bold().FontSize(24);
        page.Content().Column(col =>
        {
            col.Item().Image(weightChartPngBytes); // ScottPlot output
            col.Item().Table(table => { /* activity summary */ });
            col.Item().Image(sleepChartPngBytes);
        });
    });
}).GeneratePdf("report.pdf");
```

**License note:** QuestPDF is free for open-source projects and organizations under $1M annual revenue. Biotrackr as an open-source project qualifies for the Community license.

### Data Analysis in .NET

| Library | Version | License | Total Downloads | Capabilities |
|---------|---------|---------|-----------------|--------------|
| **MathNet.Numerics** | 5.0.0 | MIT | 59.9M | Statistics (mean, median, std dev, percentiles, correlation, covariance), probability distributions, regression (linear, polynomial), interpolation, curve fitting, numerical integration. |
| **LINQ** | Built-in | MIT | N/A | Aggregation (Sum, Average, Min, Max, GroupBy), filtering, projection. Covers most basic health data analysis needs. |

**MathNet.Numerics capabilities relevant to health data:**

- `Statistics.Mean()`, `Statistics.StandardDeviation()`, `Statistics.Percentile()`
- `Correlation.Pearson(weights, sleepScores)` — correlate weight vs sleep quality
- `Fit.Line(x, y)` — linear trend fitting for weight/activity over time
- `GoodnessOfFit.RSquared()` — evaluate trend quality
- Moving averages via sliding window with LINQ
- Descriptive statistics on any numeric health metric

This covers the vast majority of what pandas provides for this use case. The gap vs. Python is mainly in exploratory/ad-hoc analysis (pandas DataFrames are more ergonomic for exploration), but for a production API generating predefined reports, MathNet.Numerics + LINQ is sufficient.

### Quality Comparison vs Python (pandas + matplotlib)

| Capability | Python (pandas + matplotlib) | .NET (ScottPlot + QuestPDF + MathNet) |
|------------|------------------------------|---------------------------------------|
| **Chart types** | Extensive (matplotlib has 100+ plot types) | ScottPlot has 30+ types — covers all health dashboard needs (line, bar, scatter, pie, heatmap, box, radar, histogram, candlestick) |
| **Chart quality** | Publication-quality with seaborn themes | Production-quality; customizable styles, dark mode, color palettes |
| **PDF generation** | ReportLab, WeasyPrint, or matplotlib to PDF | QuestPDF — arguably superior fluent API for structured reports |
| **Statistical analysis** | pandas + scipy + numpy — gold standard | MathNet.Numerics — covers statistics, regression, correlation. Sufficient for health data. |
| **DataFrame ergonomics** | pandas DataFrames — excellent for ad-hoc exploration | No equivalent; use typed C# collections + LINQ. Less ergonomic but fine for predefined report logic. |
| **Server-side rendering** | Native (matplotlib `Agg` backend) | ScottPlot `SavePng()` — native headless support |
| **Docker image size** | ~800 MB–1.2 GB (Python + numpy + matplotlib + pandas) | ~200-300 MB (.NET runtime + NuGet packages via native AOT or self-contained) |
| **Cold start** | 2-5 sec (importing matplotlib/pandas) | <1 sec |
| **Type safety** | Dynamic typing | Strong typing — compile-time safety |
| **Deployment complexity** | Requires separate container/sidecar, inter-process communication | Same process as the rest of the .NET app |
| **Maintenance** | Two language ecosystems (C# + Python), two build pipelines, two sets of dependencies | Single language, single build pipeline |

**Verdict:** For predefined health report generation (not ad-hoc analysis), the .NET stack is production-ready and significantly simpler to deploy and maintain. Python's advantages (pandas ergonomics, matplotlib plot variety) are not critical for this use case since the reports are template-driven with known data shapes.

### Agent Framework Tool Integration

Based on the existing biotrackr architecture (source: `src/Biotrackr.Chat.Api/`):

The current pattern uses **MCP tools** exposed by the `Biotrackr.Mcp.Server` (using `[McpServerTool]` attributes). The Chat API connects to the MCP Server via `McpToolService` and makes those tools available to the `AIAgent`.

For .NET chart generation as an agent tool, there are two integration approaches:

**Approach 1: Add MCP tool to Biotrackr.Mcp.Server (Recommended)**

Add a new tool class in `Biotrackr.Mcp.Server/Tools/ReportTools.cs`:

```csharp
[McpServerToolType]
public class ReportTools : BaseTool
{
    [McpServerTool, Description("Generates a health report chart as a PNG image. Returns base64-encoded image data.")]
    public async Task<string> GenerateChart(
        string chartType,  // "weight_trend", "sleep_stages", "activity_weekly"
        string startDate,
        string endDate)
    {
        // Fetch data from APIs, generate ScottPlot chart, return base64 PNG
        var plot = new ScottPlot.Plot();
        // ... configure chart ...
        byte[] pngBytes = plot.GetImageBytes(600, 400, ScottPlot.ImageFormat.Png);
        return Convert.ToBase64String(pngBytes);
    }

    [McpServerTool, Description("Generates a comprehensive PDF health report.")]
    public async Task<string> GenerateReport(
        string reportType,
        string startDate,
        string endDate)
    {
        // Fetch data, generate charts, compose QuestPDF document, return base64 PDF
    }
}
```

This approach follows existing patterns: tools are discovered by the Chat API via MCP protocol, and the agent decides when to call them.

**Approach 2: Direct AITool in Chat API**

Use `AIFunctionFactory.Create()` to create function tools directly in the Chat API, bypassing MCP:

```csharp
var reportTool = AIFunctionFactory.Create(
    async (string chartType, string startDate, string endDate) =>
    {
        // Generate chart and return base64
    },
    name: "GenerateChart",
    description: "Generates a health data chart");
```

This approach is simpler but doesn't follow the existing MCP tool architecture.

**Recommendation:** Approach 1 (MCP tool) aligns with the existing architecture. The MCP Server already has access to the health data APIs and the tool discovery/caching infrastructure is in place.

---

## Follow-on Questions

- What specific chart types and report layouts does the product owner envision? (e.g., weekly summary PDF with weight trend + sleep chart + activity bars)
- Should generated reports be persisted (Azure Blob/Files) or returned inline as base64?
- What is the target resolution/quality for charts — screen display (72-96 DPI) or print-quality (150-300 DPI)?
- Will the AG-UI/chat interface render inline images, or will reports be downloadable links?

## Clarifying Questions

- **License compliance:** QuestPDF's Community license is free for open-source projects. Is biotrackr expected to remain FOSS? If commercialized, QuestPDF requires a paid license for organizations over $1M revenue.
- **Existing Bicep module changes:** The current `container-app-http.bicep` only supports a single container. If Option A is chosen, the module needs modification. Is the team open to refactoring this shared module, or should a separate module be created for sidecar-enabled apps?

---

## References

- [Azure Container Apps — Containers (Sidecar)](https://learn.microsoft.com/en-us/azure/container-apps/containers#sidecar-containers)
- [Azure Container Apps — Storage Mounts](https://learn.microsoft.com/en-us/azure/container-apps/storage-mounts)
- [Azure Container Apps — Health Probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)
- [Azure Container Apps — ARM/YAML Template Spec](https://learn.microsoft.com/en-us/azure/container-apps/azure-resource-manager-api-spec)
- [ScottPlot NuGet](https://www.nuget.org/packages/ScottPlot) — v5.1.57, MIT, 4.1M downloads
- [ScottPlot Cookbook](https://scottplot.net/cookbook/5.0/)
- [ScottPlot Console Quickstart](https://scottplot.net/quickstart/console/)
- [OxyPlot.Core NuGet](https://www.nuget.org/packages/OxyPlot.Core) — v2.2.0, MIT, 9M downloads
- [LiveChartsCore.SkiaSharpView NuGet](https://www.nuget.org/packages/LiveChartsCore.SkiaSharpView) — v2.0.0-rc6.1, MIT, 1.9M downloads
- [SkiaSharp NuGet](https://www.nuget.org/packages/SkiaSharp) — v3.119.2, MIT, 245.6M downloads
- [QuestPDF NuGet](https://www.nuget.org/packages/QuestPDF) — v2026.2.4, 18.6M downloads
- [iText 7 NuGet](https://www.nuget.org/packages/itext7) — v9.5.0, AGPL, 43.7M downloads
- [PdfSharpCore NuGet](https://www.nuget.org/packages/PdfSharpCore) — v1.3.67, MIT, 30.5M downloads
- [MathNet.Numerics NuGet](https://www.nuget.org/packages/MathNet.Numerics) — v5.0.0, MIT, 59.9M downloads
- Existing biotrackr source: `src/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs`
- Existing biotrackr infra: `infra/modules/host/container-app-http.bicep`
- Existing biotrackr MCP tools: `src/Biotrackr.Mcp.Server/Biotrackr.Mcp.Server/Tools/`
