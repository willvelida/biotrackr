---
description: "Razor component conventions for Biotrackr.UI Blazor project. Use when: creating or editing Razor components (.razor), component CSS isolation files, Blazor pages, or shared UI components."
applyTo: "**/*.razor"
---

# Razor Component Conventions

## Project Structure

- Pages go in `Components/Pages/` with `@page` directive
- Reusable components go in `Components/Shared/`
- Layout components go in `Components/Layout/`
- CSS isolation files sit alongside their component: `Component.razor.css`

## Component Patterns

- Use Radzen components (`RadzenStack`, `RadzenCard`, `RadzenText`, `RadzenButton`) over raw HTML where a matching component exists
- Use `RadzenStack` with `Orientation`, `AlignItems`, `JustifyContent`, `Gap` for layout — avoid manual flexbox CSS
- Pass data down via `[Parameter]`, communicate up via `EventCallback`
- Use `RenderFragment` for content slots (see `SummaryCard.razor` pattern)

## Naming

- Component files: PascalCase (`SummaryCard.razor`)
- Parameters: PascalCase public properties with `[Parameter]`
- Private fields: `_camelCase` prefix

## Accessibility Requirements

- All interactive elements must be keyboard accessible
- Images need meaningful `alt` text or `role="presentation"`
- Form inputs must have associated labels
- Use `aria-label` or `aria-labelledby` on custom interactive components
- Color must not be the sole indicator of state — pair with icons or text

## Responsiveness

- Design mobile-first — smallest viewport works without breakpoint overrides
- Use Radzen responsive utilities (`rz-display-none`, `rz-display-md-block`) for show/hide
- Test all components at 320px, 768px, and 1200px widths
