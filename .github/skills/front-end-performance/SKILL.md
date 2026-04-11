---
name: front-end-performance
description: "Front-end performance optimization including Core Web Vitals, render performance, asset optimization, and perceived speed. Use when: diagnosing slow page loads, optimizing Largest Contentful Paint (LCP), reducing Cumulative Layout Shift (CLS), improving Interaction to Next Paint (INP), bundle size analysis, lazy loading, caching strategies."
---

# Front-End Performance

## When to Use

- Diagnosing or improving page load times
- Optimizing Core Web Vitals (LCP, CLS, INP)
- Reducing bundle size or eliminating render-blocking resources
- Implementing lazy loading for images, components, or routes
- Improving perceived performance with loading states and skeleton screens
- Configuring caching, compression, or CDN delivery
- Profiling runtime render performance

## Core Web Vitals

| Metric | Good | Needs Improvement | Poor |
|--------|------|-------------------|------|
| **LCP** (Largest Contentful Paint) | ≤ 2.5s | ≤ 4.0s | > 4.0s |
| **INP** (Interaction to Next Paint) | ≤ 200ms | ≤ 500ms | > 500ms |
| **CLS** (Cumulative Layout Shift) | ≤ 0.1 | ≤ 0.25 | > 0.25 |

## LCP Optimization

LCP measures when the largest visible element renders. Common culprits:

- **Slow server response**: Optimize backend, use CDN, cache HTML
- **Render-blocking resources**: Defer non-critical CSS/JS, inline critical CSS
- **Slow resource load**: Preload hero images, use modern formats (WebP, AVIF)
- **Client-side rendering delay**: Server-render critical content, reduce JS execution

### Quick Wins

- Add `fetchpriority="high"` to the LCP image
- Preload LCP resources: `<link rel="preload" as="image" href="...">`
- Avoid lazy-loading above-the-fold images
- Inline critical CSS for the initial viewport

## CLS Optimization

CLS measures unexpected layout shifts. Common causes:

- **Images without dimensions**: Always set `width` and `height` or use `aspect-ratio`
- **Dynamic content injection**: Reserve space for ads, embeds, and async content
- **Web fonts**: Use `font-display: swap` with size-adjusted fallbacks
- **Animations**: Use `transform` animations, not layout-triggering properties

### Quick Wins

- Set explicit dimensions on all `<img>` and `<video>` elements
- Use CSS `aspect-ratio` for responsive containers
- Avoid inserting content above existing content after load
- Preload critical fonts

## INP Optimization

INP measures responsiveness to user interactions:

- **Break up long tasks**: Yield to the main thread with `scheduler.yield()` or `requestIdleCallback`
- **Reduce JavaScript execution**: Minimize and tree-shake bundles
- **Debounce expensive handlers**: Avoid running heavy logic on every keystroke or scroll
- **Use CSS for visual feedback**: Pseudo-classes (`:active`, `:hover`) respond instantly

## Asset Optimization

### Images

- Use modern formats: WebP (lossy/lossless), AVIF (best compression)
- Serve responsive sizes with `srcset` and `sizes`
- Lazy-load below-the-fold images: `loading="lazy"`
- Use CSS for decorative graphics when possible

### CSS

- Remove unused CSS (PurgeCSS or tree-shaking)
- Minimize CSS file size with minification
- Avoid `@import` in CSS files (causes sequential loading)
- Use CSS containment (`contain: layout style paint`) for isolated components

### JavaScript

- Tree-shake unused code
- Code-split by route or feature
- Defer non-critical scripts: `<script defer>` or dynamic `import()`
- Measure and budget bundle size

## Caching Strategy

| Resource Type | Cache Duration | Strategy |
|---------------|---------------|----------|
| Static assets (CSS, JS, images) | 1 year | Immutable with content hash in filename |
| HTML pages | Short or no-cache | Revalidate every request |
| API responses | Varies | `Cache-Control` with `stale-while-revalidate` |
| Fonts | 1 year | Immutable, self-host for reliability |

## Perceived Performance

- Show skeleton screens instead of spinners for content-heavy pages
- Use optimistic UI updates for user actions
- Prefetch likely next pages on hover or viewport proximity
- Provide instant visual feedback on interaction (button press states)

## Blazor-Specific Performance

- Use `@key` directive on repeated elements to optimize diffing
- Avoid unnecessary re-renders — use `ShouldRender()` override when appropriate
- Use `StateHasChanged()` only when necessary
- Virtualize long lists with `<Virtualize>` component
- Minimize component tree depth
- Use streaming rendering for server-side Blazor when data loading is slow

## Procedure

1. Measure current performance (Lighthouse, WebPageTest, browser DevTools)
2. Identify the bottleneck: is it LCP, CLS, INP, or total load time?
3. Apply targeted optimizations from the relevant section above
4. Re-measure to confirm improvement
5. Set performance budgets and monitor ongoing
