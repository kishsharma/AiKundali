# HoraLife Architecture

> High-level architecture and boundary document. Detailed implementation status belongs in the sprint tracker.

## System Shape

```text
Angular UI
   ↓
ASP.NET Core API
   ↓
Application / Domain Services
   ↓
Astrology Engine + Knowledge Base
   ↓
Prediction Engine
   ↓
Persistence / PostgreSQL
```

Additional bounded areas include Learning, Reports/PDF, Current Life Focus and the separate Store backend.

## Core Principles

### Deterministic Astrology First
Swiss Ephemeris, chart calculations, Dasha, Yoga/Dosha, strengths, remedies and prediction facts remain deterministic.

### Prediction Engine
The generic Prediction Engine uses:

- one normalized `PredictionContext`;
- generic `PredictionRequest`;
- life-area profiles;
- deterministic evidence selection;
- deterministic claims;
- contradiction/unsupported-claim validation;
- traceable rule/evidence output.

Career is the first implementation slice.

### AI Boundary
AI must not invent or alter astrology facts.

AI may later:
- organize approved structured facts;
- narrate them clearly;
- improve wording/presentation;
- perform controlled consistency/quality checks.

### API Boundary
Product versions and API contract versions are independent.

Use `/api/v1` while contracts remain backward compatible.

### Store Boundary
HoraLife Store remains a detachable generic e-commerce backend. Astrology/remedy linkage is optional and must not make core commerce objects astrology-specific.

### Learning Boundary
Learning content, progress and certification are independent of Kundali calculation logic.

## Frontend Principles

- Angular frontend remains shared for web and future Capacitor mobile reuse.
- Multilingual behavior must be centralized and persisted.
- UI should remain calm, premium, trustworthy and readable.
- Avoid excessive nested cards, heavy orange/gold treatment and unnecessary animation.

## Deployment Direction

Current deployment is IIS-hosted web/API.

Before stable commercial release, production discipline should include:

- CI
- repeatable builds
- database migration process
- secrets/configuration review
- logging/monitoring
- backups/recovery
- regression validation

## Architecture Governance

Major architectural changes should be documented here only after an explicit decision.

Implementation progress belongs in `HORALIFE_SPRINT_STATUS.md`.

Version/release decisions belong in `HORALIFE_VERSIONING.md`.
