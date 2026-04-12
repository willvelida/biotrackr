---
name: health-data-analysis
description: Biotrackr health data schema, metric extraction patterns, and analysis techniques for Fitbit activity data
---

# Health Data Analysis

## Data Schema

Biotrackr health data arrives as a JSON object with an `items` array. Each item represents one day:

```json
{
  "items": [
    {
      "date": "2026-04-05",
      "activity": {
        "activities": [
          {
            "name": "Strength training",
            "calories": 665,
            "duration": 5271000,
            "steps": 4972,
            "startTime": "06:57",
            "distance": 2.52
          }
        ],
        "summary": {
          "steps": 15732,
          "caloriesOut": 4049,
          "activityCalories": 2341,
          "fairlyActiveMinutes": 47,
          "veryActiveMinutes": 78,
          "lightlyActiveMinutes": 234,
          "sedentaryMinutes": 567,
          "distances": [{"activity": "total", "distance": 12.02}],
          "floors": 238,
          "restingHeartRate": 51
        }
      }
    }
  ],
  "totalCount": 7,
  "note": "Optional note about missing data"
}
```

## Metric Extraction

* **Steps**: `item["activity"]["summary"]["steps"]`
* **Calories (total)**: `item["activity"]["summary"]["caloriesOut"]`
* **Activity calories**: `item["activity"]["summary"]["activityCalories"]`
* **Active minutes**: `fairlyActiveMinutes + veryActiveMinutes` (combine both fields)
* **Distance**: Extract from `distances` array where `activity == "total"`: `next(d["distance"] for d in distances if d["activity"] == "total")`
* **Floors**: `item["activity"]["summary"]["floors"]`
* **Resting heart rate**: `item["activity"]["summary"]["restingHeartRate"]`

## Duration Conversion

Activity durations in the data are in **milliseconds**. Convert to minutes by dividing by 60000:

```python
duration_minutes = round(activity["duration"] / 60000)
```

## Goal Definitions

Standard daily goals for achievement tracking:

| Goal | Target | Field |
|------|--------|-------|
| Steps | ≥ 10,000 | `steps` |
| Distance | ≥ 8.0 km | total distance |
| Active Minutes | ≥ 30 min | `fairlyActiveMinutes + veryActiveMinutes` |
| Calories | ≥ 2,500 kcal | `caloriesOut` |

Goal achievement per day = count of how many of the 4 goals were met.

## Standout Day Identification

Identify these standout categories across the reporting period:

* **Highest steps day**: `max(days, key=lambda d: d["steps"])`
* **Highest calories day**: `max(days, key=lambda d: d["caloriesOut"])`
* **Most active minutes day**: `max(days, key=lambda d: d["activeMinutes"])`
* **Best goal achievement day**: `max(days, key=lambda d: d["goals_met"])`
* **Longest single session**: Find the activity with the maximum `duration` across all days

## Weekly Aggregates

Calculate these summary statistics:

* **Averages**: steps, calories, active minutes, distance, floors, resting heart rate (divide totals by number of days with data)
* **Totals**: steps, distance, floors, calories, active minutes
* **Heart rate range**: min and max resting heart rate with corresponding days
* **Trend**: Compare first day to last day for resting heart rate direction

## Handling Missing Days

* Not all 7 days in a week may have data (the `note` field indicates gaps).
* Calculate averages using actual day count (`len(items)`), not 7.
* Clearly label missing days in output tables and narratives.
