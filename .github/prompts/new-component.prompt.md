---
description: "Scaffold a new Blazor component with Radzen, accessibility, and responsive design built in"
agent: "Front-End Designer"
argument-hint: "Component name and purpose (e.g., MetricCard - displays a single health metric with trend)"
---

Create a new Blazor component following Biotrackr.UI conventions.

## Requirements

1. **Determine placement**: Page (`Components/Pages/`) or shared component (`Components/Shared/`)
2. **Build with Radzen**: Use Radzen components for layout, inputs, and data display
3. **Parameters**: Define a minimal, clean API with `[Parameter]` and `EventCallback`
4. **Content slots**: Use `RenderFragment` where flexible content is needed
5. **CSS isolation**: Create a co-located `.razor.css` file for component-specific styles
6. **Accessibility**: Ensure keyboard navigation, ARIA attributes, and sufficient contrast
7. **Responsiveness**: Mobile-first layout that adapts at standard breakpoints

## Deliverables

- The `.razor` component file
- The `.razor.css` isolation file (if styles are needed)
- Brief explanation of the component's API and intended usage
