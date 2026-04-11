---
name: chart-best-practices
description: Best practices for creating professional health data visualizations with matplotlib and seaborn
---

# Chart Generation Best Practices

## Environment Setup

* Always call `matplotlib.use('Agg')` before importing `matplotlib.pyplot` — required for headless rendering in containerized environments.
* Apply seaborn theme early: `sns.set_theme(style="whitegrid", palette="muted")`.
* Import order: `matplotlib` → `matplotlib.use('Agg')` → `matplotlib.pyplot as plt` → `seaborn as sns`.

## Figure Sizing and Resolution

* Single charts: `figsize=(10, 5)` at `dpi=150`.
* Overview/subplot layouts: `figsize=(16, 9)` at `dpi=150`.
* Always use `bbox_inches="tight"` in `savefig()` to prevent label clipping.
* Call `plt.tight_layout()` before saving multi-subplot figures.
* Close figures after saving with `plt.close(fig)` to free memory.

## Color Palette

* Use a consistent muted palette from seaborn: `PALETTE = sns.color_palette("muted")`.
* Assign fixed colors: blue (index 0), orange (index 1), green (index 2), red (index 3), purple (index 4).
* Use green for values meeting goals, red for goal lines, blue as the default bar color.
* Custom accent colors (e.g., teal `#2196a0`) are acceptable for variety.

## Goal Lines

* Draw goal reference lines with: `ax.axhline(goal, color="red", linestyle="--", linewidth=1.5, label=f"Goal: {goal}")`.
* Always include goal lines in the legend.
* Color bars conditionally: green when value meets/exceeds goal, default color otherwise.

## Bar Charts

* Annotate values on bars using an offset text above each bar.
* Use `edgecolor="white"` and `linewidth=0.6` for clean bar separation.
* For grouped bars, use `width=0.38` with `x - width/2` and `x + width/2` positioning.
* For stacked bars, use the `bottom` parameter and annotate total values.

## Line Charts

* Use `marker="o"` with `linewidth=2.5` and `markersize=8` for data points.
* Annotate each point with value labels using `textcoords="offset points"`.
* Set y-axis limits with padding: `ylim(min - 5, max + 8)`.

## Axis Formatting

* Use short day labels for x-axis (e.g., "Sun Apr 5", "Mon Apr 6").
* Rotate x-tick labels: `rotation=20, ha="right"` for single charts, `rotation=30` for subplots.
* Format large numbers with comma separators: `plt.FuncFormatter(lambda v, _: f"{v:,.0f}")`.
* Always include axis labels (`set_ylabel`) and a bold title (`fontsize=14, fontweight="bold"`).

## Subplot Layouts

* Use `plt.subplots(rows, cols)` with `fig.suptitle()` for overview charts.
* Smaller font sizes in subplots (`fontsize=7-10`).
* Include goal lines in overview subplots for consistency.

## Output

* Save all chart files to `/tmp/reports/` with descriptive names (e.g., `steps_chart.png`, `calories_chart.png`).
* Print each output file path to stdout after saving.
