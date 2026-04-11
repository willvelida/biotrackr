---
name: blazor-design
description: "Blazor component architecture, Razor component patterns, Radzen UI library, and Blazor-specific UX patterns. Use when: building Blazor components, structuring Razor component hierarchy, using Radzen components, implementing Blazor forms, managing component state, Blazor rendering modes, Blazor layout design."
---

# Blazor Design

## When to Use

- Building or restructuring Blazor Razor components
- Designing component hierarchy and data flow
- Using Radzen Blazor component library
- Implementing forms with validation in Blazor
- Choosing between render modes (Server, WebAssembly, Auto, Static SSR)
- Managing component state and cascading values
- Creating reusable component libraries

## Component Architecture

### Component Hierarchy

```
App.razor
├── Routes.razor
│   └── Layout/MainLayout.razor
│       ├── Layout/NavMenu.razor
│       ├── Pages/*.razor (routable pages)
│       │   └── Shared/*.razor (reusable components)
```

### Component Design Principles

- **Single responsibility**: Each component handles one concern
- **Props down, events up**: Pass data via `[Parameter]`, communicate changes via `EventCallback`
- **Minimal parameters**: Keep component APIs small — prefer composition over configuration
- **Render fragments**: Use `RenderFragment` for flexible content slots

### Component Template

```razor
@* Brief description of what this component does *@

<div class="my-component">
    @ChildContent
</div>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<string> OnAction { get; set; }
}
```

## Radzen Component Library

This project uses **Radzen.Blazor** for UI components. Key patterns:

### Layout

- `RadzenLayout`, `RadzenHeader`, `RadzenSidebar`, `RadzenBody`, `RadzenFooter` for page structure
- `RadzenStack` for flex-based layouts (replaces manual flexbox)
- `RadzenRow` and `RadzenColumn` for grid layouts

### Data Display

- `RadzenDataGrid` for tabular data with sorting, filtering, paging
- `RadzenDataList` for card/list layouts
- `RadzenChart` for data visualization (line, bar, pie, donut)

### Forms

- `RadzenTemplateForm` for form handling with validation
- `RadzenTextBox`, `RadzenNumeric`, `RadzenDatePicker` for inputs
- `RadzenRequiredValidator`, `RadzenRegexValidator` for validation
- `RadzenButton` for actions

### Feedback

- `RadzenNotification` for toast messages
- `RadzenDialog` for modals and confirmation dialogs
- `RadzenProgressBar` and `RadzenProgressBarCircular` for loading states

### Best Practices with Radzen

- Use Radzen's built-in responsive properties (`Visible`, `Style`) over custom media query overrides
- Leverage `RadzenStack` with `Gap`, `AlignItems`, `JustifyContent` for consistent spacing
- Use `RadzenTheme` for consistent theming across components
- Prefer Radzen validators inside `RadzenTemplateForm` over custom validation logic

## State Management

### Component State

- Use private fields for internal state
- Call `StateHasChanged()` only when the framework cannot detect the change automatically
- Override `ShouldRender()` to prevent unnecessary re-renders on stable components

### Cascading Values

- Use `CascadingValue` for cross-cutting concerns (theme, user context, layout state)
- Prefer named cascading values to avoid ambiguity
- Keep cascading value objects immutable or use `IsFixed="true"` when values do not change

### Service-Based State

- Use scoped services for state shared between components on the same page
- Inject via `@inject` and notify components via events or `StateHasChanged()`
- Avoid singleton state services in server-side Blazor (shared across users)

## Forms and Validation

```razor
<RadzenTemplateForm TItem="MyModel" Data="@model" Submit="@OnSubmit">
    <RadzenStack Gap="1rem">
        <RadzenFormField Text="Name">
            <ChildContent>
                <RadzenTextBox @bind-Value="@model.Name" />
            </ChildContent>
            <Helper>
                <RadzenRequiredValidator Component="Name" Text="Name is required" />
            </Helper>
        </RadzenFormField>

        <RadzenButton ButtonType="ButtonType.Submit" Text="Save" />
    </RadzenStack>
</RadzenTemplateForm>
```

## Render Modes

| Mode | When to Use |
|------|-------------|
| Static SSR | Content pages with no interactivity |
| Interactive Server | Rich interactivity, data-heavy pages, no WASM download |
| Interactive WebAssembly | Offline-capable, client-heavy processing |
| Interactive Auto | Server first, then WebAssembly after download |

### Choosing a Mode

- Default to **Interactive Server** for data-driven pages
- Use **Static SSR** for content that does not require user interaction
- Apply render modes at the component level, not globally, for fine-grained control

## Styling

- Use CSS isolation (`Component.razor.css`) for component-scoped styles
- Follow Radzen's CSS variable system for theming overrides
- Avoid inline styles except for dynamic, data-driven values
- Use `::deep` combinator in isolated CSS only when styling child component markup

## Procedure

1. Identify the component's responsibility and where it fits in the hierarchy
2. Choose the appropriate render mode based on interactivity needs
3. Design the component API: parameters, events, render fragments
4. Implement using Radzen components where applicable
5. Add CSS isolation for component-specific styles
6. Verify accessibility (keyboard, ARIA, contrast) on the finished component
7. Test across viewports for responsive behavior
