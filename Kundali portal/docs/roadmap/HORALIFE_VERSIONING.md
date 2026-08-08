# HoraLife Product Versioning

> Canonical source of truth for HoraLife product versions and release progression.

## Current Version

**Version:** `v0.8.0-alpha.1`  
**Name:** Core Platform Baseline  
**Status:** CURRENT / ACTIVE DEVELOPMENT  
**Formal versioning start:** 2026-08-07

HoraLife uses Semantic Versioning:

`MAJOR.MINOR.PATCH[-PRERELEASE]`

- `alpha` = active development
- `beta` = agreed functionality substantially ready for broader testing
- `rc` = release candidate; feature freeze, fixes only
- stable version = production-approved release

## Approved Release Plan

| Version | Goal |
|---|---|
| `v0.8.0-alpha.1` | Current Core Platform Baseline |
| `v0.8.0-alpha.2` | Complete multilingual support + persist preferred language |
| `v0.8.0-alpha.3` | Premium UI/UX polish - landing, login, dashboard, report opening, mobile, transitions |
| `v0.8.0-beta.1` | Core beta + CI, regression, timezone, duplicate-save, security and production readiness |
| `v0.8.0-rc.1` | Feature freeze and controlled final testing |
| `v0.8.0` | Stable Core Release |
| `v0.9.0` | Prediction Engine + deterministic premium synthesis |
| `v1.0.0` | Stable commercial production release |

## Release Notes

### v0.8.0-alpha.2 - Multilingual Completion

Focus on remaining portal translations, dynamic backend text strategy, first-login preferred language, persistence, automatic restore and safe English fallback.

### v0.8.0-alpha.3 - Premium UI Experience

Focus on landing page, login/OTP, onboarding, dashboard, Kundali report opening, mobile responsiveness, transitions, loaders, typography and consistent premium visual language.

### v0.8.0-beta.1 - Core Beta

Requires minimum CI, production builds, important regression tests, timezone handling, duplicate Kundali prevention, PDF/email verification, security/config review and no known release-blocking core defect.

### v0.8.0-rc.1 - Release Candidate

Feature freeze. Only critical bugs, calculation defects, deployment problems, security issues and essential release polish.

### v0.9.0 - Prediction & Synthesis

Complete deterministic prediction architecture, Career first and later approved life areas, evidence, claims, contradiction validation, MD/AD/PD timing, Gochar modifiers and premium report synthesis.

### v1.0.0 - Commercial Release

Reserved for a stable production-ready commercial product with premium reporting, deployment/recovery discipline, monitoring, security review and acceptable regression coverage.

## Version Rules

- Do not advance versions automatically because of dates, commits or PR count.
- Version changes require verified implementation status and explicit product decision.
- Do not invent historical versions before `v0.8.0-alpha.1`.
- Product version is separate from API version and DB migrations.
- API may remain `/api/v1` across many product versions until a breaking API contract requires `/api/v2`.
- Database migrations keep descriptive names and are not renamed to product version numbers.
- Planned versions must not be listed as released.

## Relationship to Sprint Tracker

`HORALIFE_VERSIONING.md` owns release/version decisions.

`HORALIFE_SPRINT_STATUS.md` owns actual implementation status, active sprint, backlog, blockers and priority.

Version progression must be based on verified sprint/repository status.

## Version Decision Log

### 2026-08-07 - Formal versioning starts at `v0.8.0-alpha.1`

Previous state: no formal product version.

Current version: `v0.8.0-alpha.1`

Decision: formal semantic versioning starts from the current substantial implementation rather than creating artificial historical versions.

Approved path:

`v0.8.0-alpha.1` -> `v0.8.0-alpha.2` -> `v0.8.0-alpha.3` -> `v0.8.0-beta.1` -> `v0.8.0-rc.1` -> `v0.8.0` -> `v0.9.0` -> `v1.0.0`

## Release History

No formal HoraLife release has been tagged yet.
