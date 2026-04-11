---
name: web-design
description: "Web design fundamentals including layout systems, CSS architecture, design tokens, typography, and component-based UI patterns. Use when: building page layouts, creating design systems, structuring CSS, choosing layout approaches (grid vs flexbox), implementing design tokens, theming."
---

# Web Design

## When to Use

- Building or restructuring page layouts
- Creating or maintaining a design system or component library
- Choosing between CSS Grid, Flexbox, or other layout methods
- Implementing design tokens for consistent spacing, color, and typography
- Theming or skin-switching (light/dark mode)
- Reviewing visual hierarchy, alignment, or whitespace

## Layout Systems

### CSS Grid

Use for two-dimensional layouts (rows AND columns):

- Page-level layout (header, sidebar, main, footer)
- Card grids with equal-height items
- Complex dashboard layouts

### Flexbox

Use for one-dimensional layouts (row OR column):

- Navigation bars
- Centering content
- Distributing space between items
- Reordering items at different breakpoints

### When to Choose

| Scenario | Use |
|----------|-----|
| Page skeleton | Grid |
| Row of buttons | Flexbox |
| Card grid with equal heights | Grid |
| Centering a single element | Flexbox |
| Dashboard with mixed areas | Grid |
| Toolbar with variable items | Flexbox |

## Design Tokens

Structure tokens in three tiers:

1. **Global tokens**: Raw values (`--color-blue-500: #3b82f6`)
2. **Semantic tokens**: Purpose-mapped (`--color-primary: var(--color-blue-500)`)
3. **Component tokens**: Scoped (`--button-bg: var(--color-primary)`)

## Color

- Define a palette with consistent lightness steps (50-950)
- Ensure text/background combinations meet WCAG AA contrast (4.5:1 normal text, 3:1 large text)
- Provide semantic color roles: primary, secondary, success, warning, error, neutral
- Support dark mode by swapping semantic tokens, not individual component colors

## Typography Scale

Use a modular scale (e.g., 1.25 ratio):

| Step | Size | Use |
|------|------|-----|
| xs | 0.75rem | Captions, labels |
| sm | 0.875rem | Secondary text |
| base | 1rem | Body text |
| lg | 1.25rem | Subheadings |
| xl | 1.563rem | Section headings |
| 2xl | 1.953rem | Page titles |
| 3xl | 2.441rem | Hero text |

## Spacing Scale

Use a consistent spacing scale based on a base unit (typically 4px or 8px):

| Token | Value |
|-------|-------|
| space-1 | 0.25rem (4px) |
| space-2 | 0.5rem (8px) |
| space-3 | 0.75rem (12px) |
| space-4 | 1rem (16px) |
| space-6 | 1.5rem (24px) |
| space-8 | 2rem (32px) |
| space-12 | 3rem (48px) |

## Component Patterns

### Composition Over Specialization

- Prefer small, composable components (`Card`, `CardHeader`, `CardBody`) over monolithic ones
- Use slots or render fragments for flexible content areas
- Keep component APIs minimal — add props only when needed

### Visual Hierarchy

1. Size and weight guide the eye first
2. Color draws attention to actions and status
3. Whitespace groups related content and separates sections
4. Alignment creates order and scanability

## CSS Architecture

- Scope styles to components — avoid global selectors
- Use CSS custom properties for theming
- Prefer utility patterns for one-off spacing/alignment adjustments
- Keep specificity low — avoid `!important` and deep nesting

## Procedure

1. Define or review the design tokens (color, typography, spacing)
2. Choose the appropriate layout system for the structure
3. Build with semantic HTML first
4. Apply styles using the token system
5. Verify visual hierarchy, alignment, and whitespace
6. Test across breakpoints (mobile-first)
