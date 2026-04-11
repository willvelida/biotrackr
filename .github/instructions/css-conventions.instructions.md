---
description: "CSS conventions for Biotrackr.UI Blazor project. Use when: writing or editing CSS isolation files, Radzen theme overrides, or component styles."
applyTo: "**/*.razor.css"
---

# CSS Conventions

## CSS Isolation

- Every component with custom styles uses a co-located `.razor.css` file
- Scoped styles apply automatically — avoid global selectors
- Use `::deep` only when styling child component markup that CSS isolation cannot reach

## Radzen Theming

- Use Radzen CSS variables for colors, spacing, and typography where available
- Use `rz-` utility classes for common patterns (`rz-p-x-2`, `rz-m-0`, `rz-color-secondary`)
- Override Radzen defaults via CSS custom properties, not `!important`

## Naming

- Use kebab-case for custom class names (`summary-card`, `card-icon`)
- Prefix project-specific utility classes with `bt-` to avoid Radzen collisions

## Layout

- Prefer `RadzenStack` (Radzen's flex wrapper) over manual `display: flex` in CSS
- Use CSS Grid only for two-dimensional layouts not covered by Radzen layout components
- Avoid fixed widths — use `max-width`, percentages, or `fr` units

## Performance

- Avoid `@import` in CSS files
- Keep selectors shallow (max 2-3 levels of nesting)
- Use `transform` and `opacity` for animations — avoid layout-triggering properties (`width`, `height`, `top`, `left`)

## Accessibility

- Never set `outline: none` without providing a visible focus alternative
- Ensure contrast ratios meet WCAG AA: 4.5:1 for text, 3:1 for UI components
- Use `prefers-reduced-motion` media query for animations
