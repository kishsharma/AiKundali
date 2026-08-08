# HoraLife Sprint Status

> Canonical source of truth for current implementation status, active sprint, priorities and blockers.

For release/version progression see: `HORALIFE_VERSIONING.md`.

## Current Product Version

`v0.8.0-alpha.1` - Core Platform Baseline

## Active Sprint

**Prediction Engine V1 - Career Foundation**

**Status:** ACTIVE  
**Priority:** P0

### Goal

Implement the smallest valid deterministic Career prediction slice using the frozen generic Prediction Engine architecture.

### Current Scope

- common `PredictionContext`
- generic `PredictionRequest`
- `PredictionAreaProfile`
- evidence selection
- deterministic Career claims
- natal promise first
- Dasha range selection
- MD / AD / PD evidence
- contradiction / unsupported-claim validation
- real API flow
- focused automated tests
- repeatable deterministic output

### Definition of Done

Career V1 is complete only when:

- results contain traceable evidence;
- the engine is generic rather than Career-only architecture;
- unsupported conclusions are rejected or downgraded;
- timing roles are distinguishable;
- same input produces the same factual output;
- important tests pass;
- the result is exposed through an actual API flow;
- AI is not required for factual correctness.

## Immediate Release Sequence

1. Continue active Prediction Engine Career work.
2. Complete multilingual work for `v0.8.0-alpha.2`.
3. Complete Premium UI Experience for `v0.8.0-alpha.3`.
4. Production-safety work for `v0.8.0-beta.1`.
5. RC validation and stable `v0.8.0`.
6. Complete broader Prediction Engine / premium synthesis for `v0.9.0`.

## Key Current Product Status

- Core Kundali calculation/report foundation - substantially complete
- Compatibility - implemented; final safety/domain validation still important
- Dasha through Pratyantardasha - implemented
- Authentication / OTP / saved Kundalis - implemented
- PDF / email sharing - implemented with production verification still required
- Multilingual portal - partial
- Learning platform - implemented; content/progress/completion still partial
- Store backend - complete and parked
- Prediction Engine - active/incomplete
- Premium synthesis - planned
- Android / Vastu / other parked modules - parked

## Production Readiness Gaps

Before Beta:

- GitHub Actions CI
- production frontend/backend builds
- calculation regression samples
- timezone handling
- duplicate Kundali prevention
- deployed PDF/email verification
- secrets/security/configuration review
- authorization consistency review
- critical mobile/report UX verification

## Scope Control

Do not open another large module while the active P0 milestone remains incomplete unless a critical production defect requires it.

## Update Rule

Whenever verified repository status materially changes:

- update the active sprint;
- update completed/in-progress/blockers;
- update relevant module status;
- update source commit/date;
- update versioning only when an explicit version decision is made.
