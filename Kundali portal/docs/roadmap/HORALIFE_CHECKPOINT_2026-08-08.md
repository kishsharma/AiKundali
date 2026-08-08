# HoraLife Checkpoint — 2026-08-08

## Purpose

This checkpoint records the current product and Prediction Engine state before pausing/compacting the working conversation.

## Current Product Version

**Current formal product version remains:** `v0.8.0-alpha.1` — Core Platform Baseline.

Reason: the approved version sequence still requires multilingual completion (`v0.8.0-alpha.2`), premium UI polish (`v0.8.0-alpha.3`), beta/RC/core release before the planned `v0.9.0` Prediction & Synthesis release. Prediction Engine work has landed ahead of that release sequence and should be tracked as an early-completed milestone, not used to skip the approved version path.

## Prediction Engine V1 — Pause / Freeze Checkpoint

**Status:** PAUSED AFTER V1 LIFE-AREA BREADTH COMPLETION

The generic deterministic Prediction Engine now runs end-to-end through the Kundali API for all seven approved V1 life areas:

- Career — V1 frozen
- Finance — V1 frozen
- Relationships — V1 operational
- Education — V1 operational
- Family — V1 operational
- Property — V1 operational
- Health — V1 operational with conservative non-diagnostic scope

### Proven V1 pipeline

`Kundali calculations -> PredictionContext -> metadata-driven evidence -> deterministic claims -> MD/AD/PD timing -> generic consolidation -> generic synthesis -> API response`

### Implemented / proven

- common `PredictionContext` mapped from Kundali data
- generic `PredictionRequest`
- metadata-driven `PredictionAreaProfile`
- generic house/planet evidence selection
- generic house-lord placement facts and semantic interpretation
- natal house aspect claims
- primary-house-lord aspect claims
- primary-house-lord dignity claims where qualifying evidence exists
- active Mahadasha / Antardasha / Pratyantardasha claim generation
- generic current-period consolidation
- effective synthesis timing based on overlapping active Dasha claims
- generic house-connection synthesis
- API exposes all seven prediction areas
- deterministic evidence/rule traceability
- natural-language synthesis feasibility proven from structured output

### Scope intentionally not added yet

- Dasha-level weighting (MD background / AD theme / PD trigger)
- point-in-time future date selector refinement
- date-range prediction-window orchestration
- Gochar modifiers inside the generic Prediction Engine
- Yoga/Dosha modifiers inside generic synthesis
- advanced confidence/weighting model
- generalized contradiction validator beyond current mixed-tone consolidation
- AI narration integration
- premium report production integration
- additional life areas beyond the seven approved V1 areas

### Known follow-up

- Review dignity parsing/normalization so labels such as `Exalted (Weak)` are not treated as unqualified strong dignity merely through loose string matching.
- Add broader regression tests using multiple saved charts before commercial reliance.
- Keep Health outputs conservative: no disease diagnosis, lifespan claims, medical certainty or treatment recommendations.

## Current Roadmap Position

Prediction Engine breadth is no longer the active blocker it was on 2026-08-04. The next product priority returns to the approved core sequence:

1. Complete multilingual support across the portal, including dynamic backend/rule-based text.
2. Ask preferred language once during first login/registration, persist it, restore automatically, and keep safe English fallback.
3. Continue premium synthesis/narration integration after multilingual completion.
4. Production/core beta safety work: CI, regression, timezone handling, duplicate Kundali prevention, PDF/email verification, security/config review.
5. Premium UI/UX polish and mobile refinement according to the version plan.

## Version Roadmap Position

- `v0.8.0-alpha.1` — CURRENT: Core Platform Baseline; Prediction Engine V1 breadth completed ahead of planned release sequence.
- `v0.8.0-alpha.2` — NEXT: Multilingual completion + persisted preferred language.
- `v0.8.0-alpha.3` — Premium UI/UX polish.
- `v0.8.0-beta.1` — Core beta / CI / regression / production readiness.
- `v0.8.0-rc.1` — Feature freeze / controlled final testing.
- `v0.8.0` — Stable Core Release.
- `v0.9.0` — Productize deterministic Prediction & Synthesis: hardening, advanced timing/modifiers, narration/report integration.
- `v1.0.0` — Stable commercial production release.

## Resume Instruction

When Prediction work resumes, do **not** restart life-area breadth or redesign the architecture. Treat the seven-area V1 implementation as the frozen baseline. Resume at generic engine hardening or narration/report integration depending on the active product milestone.
