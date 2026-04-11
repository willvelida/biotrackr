---
description: "Analyze and optimize front-end performance for Blazor pages and components"
agent: "Front-End Designer"
argument-hint: "Page or component to optimize (e.g., Components/Pages/Activity.razor)"
---

Analyze the specified page or component for front-end performance issues and provide actionable optimizations.

## Analysis Areas

### Render Performance
- Unnecessary re-renders or missing `ShouldRender()` overrides
- Deep component trees that could be flattened
- Missing `@key` directives on repeated elements
- Long lists not using `<Virtualize>`

### Asset Delivery
- Unoptimized images (missing lazy loading, missing dimensions, large file sizes)
- Render-blocking CSS or scripts
- Opportunities for preloading critical resources

### Layout Stability (CLS)
- Elements without explicit dimensions causing layout shift
- Dynamic content injection above the fold
- Font loading causing text reflow

### Interaction Responsiveness (INP)
- Heavy event handlers blocking the main thread
- Missing debounce on frequent interactions
- Expensive computations that could be deferred

## Output Format

For each finding:
- **Impact**: High / Medium / Low
- **Metric affected**: LCP, CLS, INP, or general
- **Location**: File and line reference
- **Current behavior**: What happens now
- **Recommendation**: Specific fix with code
- **Expected improvement**: Qualitative or quantitative estimate
