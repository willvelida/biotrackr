---
description: "Review a Blazor component or page for UX quality, accessibility, responsiveness, and performance"
agent: "Front-End Designer"
argument-hint: "Component or page path to review (e.g., Components/Pages/Home.razor)"
---

Perform a structured design review of the specified component or page.

## Review Checklist

Evaluate each area and provide findings with severity (critical, warning, suggestion):

### 1. Visual Hierarchy and Layout
- Is content logically ordered and scannable?
- Are spacing, alignment, and grouping consistent?
- Does typography guide the user's eye appropriately?

### 2. Accessibility (WCAG 2.2 AA)
- Are all interactive elements keyboard accessible?
- Do color combinations meet contrast requirements (4.5:1 text, 3:1 UI)?
- Are ARIA attributes correct and complete?
- Does the component work with screen readers?

### 3. Responsiveness
- Does the layout work at 320px, 768px, and 1200px?
- Are touch targets at least 44x44px on mobile?
- Does content reflow without horizontal scrolling?

### 4. Performance
- Are images optimized and lazy-loaded where appropriate?
- Is the component tree depth reasonable?
- Are there unnecessary re-renders or heavy computations?

### 5. Blazor Patterns
- Does the component follow project conventions (Radzen usage, parameter patterns)?
- Is state management handled correctly?
- Is the render mode appropriate?

## Output Format

For each finding:
- **[Severity]** Brief description
- **Location**: File and line reference
- **Recommendation**: Specific fix with code if applicable
