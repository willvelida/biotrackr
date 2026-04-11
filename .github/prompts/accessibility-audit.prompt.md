---
description: "Run a WCAG 2.2 AA accessibility audit on Blazor components or pages"
agent: "Front-End Designer"
argument-hint: "Scope of audit (e.g., Components/Pages/Home.razor or entire Components/ folder)"
---

Perform a WCAG 2.2 Level AA accessibility audit on the specified scope.

## Audit Areas

### Perceivable
- Text alternatives for non-text content (1.1.1)
- Color contrast meets minimums (1.4.3, 1.4.11)
- Content is readable and understandable without CSS (1.3.1)
- Text can be resized to 200% without loss (1.4.4)

### Operable
- All functionality available via keyboard (2.1.1)
- No keyboard traps (2.1.2)
- Focus order is logical (2.4.3)
- Focus indicator is visible (2.4.7)
- Target size meets minimum 24x24px (2.5.8)

### Understandable
- Form inputs have visible labels (3.3.2)
- Error messages are descriptive and associated (3.3.1)
- Navigation is consistent (3.2.3)

### Robust
- Valid HTML structure (4.1.1)
- Custom components expose name, role, value (4.1.2)
- ARIA attributes are correctly used (4.1.2)

## Output Format

| # | Criterion | Severity | Component | Issue | Fix |
|---|-----------|----------|-----------|-------|-----|
| 1 | 1.4.3 | Critical | ... | ... | ... |

Provide a summary count: critical / warning / pass, with code fixes for all critical and warning findings.
