---
name: mobile-design
description: "Mobile-first responsive design patterns, touch interaction guidelines, and adaptive layouts. Use when: designing for mobile viewports, implementing responsive breakpoints, optimizing touch targets, creating adaptive navigation, handling mobile-specific UX like swipe gestures or bottom sheets."
---

# Mobile Design

## When to Use

- Designing or reviewing layouts for mobile viewports
- Implementing responsive breakpoints and fluid grids
- Sizing touch targets and interactive elements
- Creating adaptive navigation patterns (hamburger menus, bottom nav, tab bars)
- Handling mobile-specific interactions (swipe, pull-to-refresh, long press)

## Responsive Breakpoints

Use a mobile-first approach with `min-width` media queries:

| Breakpoint | Target | Min-width |
|------------|--------|-----------|
| Default | Mobile portrait | 0 |
| sm | Mobile landscape | 576px |
| md | Tablet | 768px |
| lg | Desktop | 992px |
| xl | Large desktop | 1200px |

## Touch Target Sizing

- **Minimum touch target**: 44x44 CSS pixels (WCAG 2.5.8)
- **Recommended touch target**: 48x48 CSS pixels (Material Design)
- **Minimum spacing between targets**: 8px
- **Interactive text links**: ensure adequate padding to meet target size

## Mobile Layout Patterns

### Single Column

Default for mobile — content stacks vertically in reading order.

### Bottom Navigation

- Use for 3-5 top-level destinations
- Keep labels short (1-2 words)
- Highlight the active item
- Avoid hiding primary navigation behind a hamburger menu on mobile

### Cards and Lists

- Cards at full viewport width on mobile with internal padding
- List items with adequate row height (minimum 48px)
- Use swipe actions sparingly — provide visible alternatives

## Typography

- Base font size: 16px minimum (prevents iOS zoom on input focus)
- Line height: 1.4-1.6 for body text
- Limit line length to ~60-70 characters on larger viewports
- Use relative units (`rem`, `em`) over fixed `px`

## Images and Media

- Use `srcset` and `sizes` for responsive images
- Serve appropriately sized images per viewport
- Use `aspect-ratio` CSS to prevent layout shift
- Lazy-load images below the fold with `loading="lazy"`

## Forms on Mobile

- Use appropriate `inputmode` attributes (`numeric`, `email`, `tel`, `url`)
- Stack form fields vertically
- Place labels above inputs, not beside
- Use large tap targets for submit buttons (full width on mobile)
- Avoid multi-column form layouts on small screens

## Performance Considerations

- Minimize DOM depth for scrolling performance
- Use `will-change` sparingly for animated elements
- Avoid fixed elements that cause repaint during scroll
- Test on real devices with throttled CPU and network

## Procedure

1. Start with mobile viewport (320-375px width)
2. Build the layout with semantic HTML and flexible CSS
3. Add breakpoints only when the layout breaks — not at arbitrary device widths
4. Test touch targets meet minimum 44x44px
5. Verify text readability without pinch-to-zoom
6. Test with device emulation AND real devices when possible
