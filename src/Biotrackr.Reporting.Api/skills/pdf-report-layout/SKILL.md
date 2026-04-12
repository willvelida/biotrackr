---
name: pdf-report-layout
description: Professional PDF report generation patterns using reportlab PLATYPUS framework for health reports
---

# PDF Report Layout

## Framework Setup

Use reportlab's PLATYPUS (Page Layout and Typography Using Scripts) framework:

```python
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    Image as RLImage, PageBreak, HRFlowable
)
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
```

## Document Template

* Page size: A4
* Margins: 2cm on all sides
* Output path: `/tmp/reports/report.pdf`

```python
doc = SimpleDocTemplate(
    "/tmp/reports/report.pdf",
    pagesize=A4,
    leftMargin=2*cm, rightMargin=2*cm,
    topMargin=2*cm, bottomMargin=2*cm
)
```

## Paragraph Styles

Define a consistent set of styles:

* **Title**: 24pt, centered, with spacing
* **Heading1**: 14pt bold, section headers
* **Heading2**: 11pt bold, subsection headers
* **Body**: 9pt normal, content text
* **Disclaimer**: 7.5pt grey, centered, for the mandatory disclaimer
* **Small**: 8pt for compact content

## Table Styling

Professional table design with a consistent header style:

* Header row: dark background (`#1a3a5c`), white bold text, 8pt
* Body rows: alternating colors (`#f0f4f8` and white), 8pt
* Grid: thin lines in `#ccd6e0`
* Cell padding: 4pt top/bottom, 6pt left for first column
* Alignment: first column left, data columns center
* Highlight rows (e.g., averages) with distinct background (`#d0e8ff`)

## Chart Embedding

Embed PNG chart images using `RLImage`:

```python
story.append(RLImage("chart_path.png", width=16*cm, height=8*cm))
```

* Width: 16cm for full-width charts
* Height: 7.5-9cm depending on chart complexity
* Add `Spacer(1, 0.3*cm)` between charts and other content

## Page Structure

Organize the report into distinct pages with `PageBreak()` between sections:

1. **Cover page**: Title, date range, summary metrics table, weekly overview chart
2. **Daily metrics**: Full metrics table with averages row, steps chart
3. **Calories and activity**: Calories chart, active minutes chart
4. **Distance, floors, heart rate**: Distance chart, floors chart, heart rate chart
5. **Goal achievement**: Goal tracking table with conditional cell coloring (green for met, red for missed), goal chart
6. **Logged activities**: Detailed activity table with duration, calories, steps/distance
7. **Weekly summary**: Aggregate stats table, standout days table, narrative highlights

## Required Disclaimer

Include this disclaimer on **every page** of the report:

> This report is generated from personal health data and is not medical advice. Consult a healthcare provider for medical guidance.

**Preferred approach:** Use `onPage` callbacks on the document template to render the disclaimer as a fixed footer on every page, ensuring it appears even when flowable content overflows:

```python
def add_disclaimer_footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 7)
    canvas.setFillColor(colors.grey)
    canvas.drawCentredString(A4[0] / 2, 1.2 * cm, DISCLAIMER)
    canvas.restoreState()

doc.build(story, onFirstPage=add_disclaimer_footer, onLaterPages=add_disclaimer_footer)
```

**Alternative approach:** Add a Disclaimer paragraph (7.5pt, grey, centered) at the bottom of each page's content before `PageBreak()`. This works for manually paginated reports but is unreliable when content overflows to unexpected pages.

## Building the Document

Construct a `story` list of flowable elements, then build:

```python
story = []
# ... add all flowables ...
doc.build(story)
print("/tmp/reports/report.pdf")
```

## Output

* Save the PDF to `/tmp/reports/report.pdf`
* Print the output path to stdout after successful generation
* All referenced chart PNGs should already exist in `/tmp/reports/` before PDF assembly
