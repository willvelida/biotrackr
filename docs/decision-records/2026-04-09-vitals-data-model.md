---
title: "Vitals Data Model and Partition Key Strategy"
date: 2026-04-09
status: accepted
---

# Decision Record: Vitals Data Model and Partition Key Strategy

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 9 April 2026
- **Related Docs**: [Vitals Integration Research](../../.copilot-tracking/research/2026-04-09/vitals-integration-research.md), PR #255 (Svc), PR #256 (Api)

## Context

Biotrackr integrates a Withings BPM Connect device for blood pressure monitoring alongside the existing Withings body composition scale for weight data. The Weight domain needed to expand to accommodate both weight and blood pressure measurements.

Key considerations:

- Existing data lives in a shared Cosmos DB `records` container with `/documentType` as the partition key
- Four domains (Activity, Sleep, Weight, Food) share this container with partition key values `"Activity"`, `"Sleep"`, `"Weight"`, `"Food"`
- Activity, Sleep, and Food domains store one document per day with nested `List<T>` for multi-entry data
- Weight was the exception, storing one document per Withings measurement using the LogId as the document ID
- Blood pressure readings can occur multiple times per day (2-4 readings typical)
- The Withings API returns both weight and BP data from the same endpoint (`Measure - Getmeas`) with different measure types

## Decision

**Use a unified `VitalsDocument` with one document per day, containing optional weight measurement and optional list of blood pressure readings. Use `"Vitals"` as the partition key value and GUID as the document ID.**

Document structure:

```json
{
  "id": "guid-here",
  "date": "2026-04-09",
  "documentType": "Vitals",
  "provider": "Withings",
  "weight": { "weightKg": 80.2, "bmi": 22.7, ... },
  "bloodPressureReadings": [
    { "systolic": 120, "diastolic": 80, "heartRate": 72, "time": "08:30:00", ... },
    { "systolic": 118, "diastolic": 78, "heartRate": 70, "time": "20:15:00", ... }
  ]
}
```

ID strategy uses GUID (`Guid.NewGuid()`) with a query-first pattern: query by date first, reuse existing GUID if found, else generate new. This is consistent with Activity, Sleep, and Food domains which all use GUIDs.

## Alternatives Considered

### Separate document types (Weight + BloodPressure partitions)

Keep Weight documents unchanged and add new BloodPressure documents in a separate partition.

- **Pro**: No migration needed, independent writes
- **Con**: Cross-partition queries for "get all vitals for a date", breaks the 1-doc-per-day pattern, forces MCP tools to aggregate from multiple sources
- **Rejected because**: Cross-partition queries add cost and complexity

### Hybrid documents in same partition with discriminator

Separate weight and BP documents sharing `documentType: "Vitals"` with a `measurementType` field.

- **Pro**: Independent writes, stays in same partition
- **Con**: Introduces new discriminator complexity, returns multiple documents per date requiring client-side assembly
- **Rejected because**: Unnecessary complexity when unified document matches other domain patterns

### Date string as document ID

Use the date string (e.g., `"2026-04-09"`) as the document ID for simple upsert-by-date semantics.

- **Pro**: Simple upsert without query-first
- **Con**: Breaks consistency with Activity/Sleep/Food which all use GUIDs
- **Rejected because**: GUID is consistent with all other domains. Query-first pattern adds one read per date per sync but avoids breaking established conventions.

## Consequences

- The VitalsWorker fetches weight and BP data in a single Withings API call and groups by date
- One VitalsDocument per date is upserted to Cosmos DB
- The `Weight` property is nullable for days with only BP data
- The `BloodPressureReadings` list is nullable for days with only weight data
- All existing weight data required migration from the `"Weight"` partition to the `"Vitals"` partition
- MCP Server returns combined vitals data in a single tool call (3 tools, 12 total maintained)
