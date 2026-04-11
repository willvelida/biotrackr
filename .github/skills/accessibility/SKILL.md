---
name: accessibility
description: "WCAG 2.2 compliance, ARIA patterns, keyboard navigation, screen reader support, and inclusive design. Use when: reviewing accessibility, adding ARIA attributes, fixing contrast issues, implementing keyboard navigation, creating accessible forms, building accessible components, WCAG audit."
---

# Accessibility

## When to Use

- Reviewing or auditing UI components for accessibility compliance
- Adding ARIA roles, states, and properties
- Fixing color contrast issues
- Implementing keyboard navigation patterns
- Making forms accessible (labels, errors, grouping)
- Building custom interactive components (modals, menus, tabs)
- Responding to accessibility audit findings

## WCAG 2.2 Quick Reference

### Level A (Minimum)

| Criterion | Requirement |
|-----------|-------------|
| 1.1.1 Non-text Content | All images have meaningful `alt` text or are marked decorative |
| 1.3.1 Info and Relationships | Structure conveyed visually is also in markup (headings, lists, tables) |
| 2.1.1 Keyboard | All functionality available via keyboard |
| 2.4.1 Bypass Blocks | Skip navigation link or landmark regions |
| 4.1.2 Name, Role, Value | Custom controls expose name, role, state to assistive tech |

### Level AA (Target)

| Criterion | Requirement |
|-----------|-------------|
| 1.4.3 Contrast (Minimum) | 4.5:1 for normal text, 3:1 for large text |
| 1.4.11 Non-text Contrast | 3:1 for UI components and graphical objects |
| 2.4.7 Focus Visible | Keyboard focus indicator is clearly visible |
| 2.5.8 Target Size (Minimum) | 24x24 CSS pixels minimum for pointer targets |

## ARIA Patterns

### Rules of ARIA

1. **Don't use ARIA if native HTML works** — `<button>` over `<div role="button">`
2. **Don't change native semantics** — don't add `role="heading"` to a `<button>`
3. **All interactive ARIA controls must be keyboard operable**
4. **Don't use `role="presentation"` or `aria-hidden="true"` on focusable elements**
5. **All interactive elements must have an accessible name**

### Common Patterns

| Component | Key ARIA | Keyboard |
|-----------|----------|----------|
| Modal dialog | `role="dialog"`, `aria-modal="true"`, `aria-labelledby` | Trap focus, Escape to close |
| Tab panel | `role="tablist/tab/tabpanel"`, `aria-selected` | Arrow keys between tabs, Tab into panel |
| Menu | `role="menu/menuitem"`, `aria-expanded` | Arrow keys to navigate, Enter to select, Escape to close |
| Accordion | `<button>` with `aria-expanded`, `aria-controls` | Enter/Space to toggle |
| Alert | `role="alert"` or `aria-live="assertive"` | Announced automatically |
| Status | `role="status"` or `aria-live="polite"` | Announced at next opportunity |

## Keyboard Navigation

- **Tab**: Move between interactive elements
- **Shift+Tab**: Move backward
- **Enter/Space**: Activate buttons and links
- **Arrow keys**: Navigate within composite widgets (tabs, menus, trees)
- **Escape**: Close modals, menus, popups
- **Home/End**: Jump to first/last item in a list

### Focus Management

- Never remove the default focus outline without providing a visible alternative
- Use `tabindex="0"` to make non-interactive elements focusable when needed
- Use `tabindex="-1"` for programmatic focus (e.g., error summary, modal container)
- Never use `tabindex` greater than 0

## Forms

- Every input must have a visible `<label>` with matching `for`/`id`
- Group related fields with `<fieldset>` and `<legend>`
- Mark required fields with `aria-required="true"` and visible indicator
- Associate error messages with `aria-describedby`
- Use `aria-invalid="true"` on fields with validation errors
- Provide error summary at the top of the form on submission failure

## Color and Contrast

- **Text contrast**: 4.5:1 minimum (3:1 for large text — 18pt or 14pt bold)
- **UI component contrast**: 3:1 for borders, icons, and interactive states
- **Don't rely on color alone** to convey information — add icons, text, or patterns
- Test with simulated color blindness (protanopia, deuteranopia, tritanopia)

## Testing Checklist

1. Tab through the entire page — can you reach and operate everything?
2. Use a screen reader (NVDA, VoiceOver, Narrator) to verify announced content
3. Check color contrast with a tool (axe, Lighthouse, Colour Contrast Analyser)
4. Zoom to 200% — does layout remain usable?
5. Test with high contrast mode enabled
6. Verify focus indicators are visible on every interactive element

## Procedure

1. Review the component's semantic HTML structure
2. Verify all interactive elements are keyboard accessible
3. Check ARIA roles, states, and properties against the pattern library
4. Test color contrast for text and UI components
5. Verify focus management (order, visibility, trapping where needed)
6. Test with a screen reader to confirm announced content matches visual content
