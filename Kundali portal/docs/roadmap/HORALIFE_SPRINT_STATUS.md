# HoraLife Sprint Status and Roadmap Tracker

> **Canonical roadmap source of truth**  
> Repository: `kishsharma/AiKundali`  
> Branch reviewed: `GoogleSingleSignIn`
> Last verified: **2026-11-01**
> Verified source commit: `8c207064f45113072c8331e9ced1a63b42dec1e6`
> Working-tree note: 98 tracked paths and 45 untracked paths are changed, plus local build/output artifacts. Findings distinguish checked-in evidence from current worktree implementation; worktree changes are not release-ready until reviewed, committed, and verified.
> Product owner: **Kishore Sharma**  
> Status owner: HoraLife development team

For release and product-version progression, see `HORALIFE_VERSIONING.md`.

---

## 1. Purpose

This document is the permanent working tracker for HoraLife milestones, sprint status, roadmap decisions, completed work, parked work, and verified implementation gaps.

It must be used whenever a discussion includes any of the following:

- current HoraLife progress or release readiness;
- roadmap, milestone, sprint, priority, or next-step planning;
- whether a module is complete, partial, pending, blocked, or parked;
- a code change that completes or reopens a roadmap item;
- a new module, major capability, or architectural milestone;
- differences between planned status and actual repository status.

### Mandatory update rule

Before giving a future HoraLife roadmap or sprint-status answer:

1. Read this file from the current repository branch.
2. Verify relevant code when the answer depends on implementation status.
3. Update this file when the verified status has changed.
4. Update `Last verified`, `Verified source commit`, the relevant module row, the active sprint, and the change log.
5. Do not mark work complete based only on plans, documents, class names, controllers, models, or commit messages.

This file takes precedence over remembered status when repository evidence differs.

---

## 2. Status definitions

| Status | Meaning |
|---|---|
| **DONE** | Implemented end to end for the agreed scope and usable through its intended flow. |
| **DONE — known gaps** | Main flow works, but clearly listed non-blocking gaps remain. |
| **PARTIAL** | Meaningful implementation exists, but the agreed module or product flow is incomplete. |
| **SKELETON** | Project, models, routes, controller, or placeholders exist without the real business flow. |
| **PLANNED** | Approved or discussed but implementation has not started. |
| **PARKED** | Intentionally paused. It is not counted as active sprint debt. |
| **BLOCKED** | Work cannot proceed because of an identified dependency or unresolved decision. |

### Completion rule

A feature is marked **DONE** only when the required layers for its agreed scope are present:

- business calculation or service logic;
- API contract where required;
- UI flow where the feature is user-facing;
- persistence where the feature promises save/history;
- error handling and authorization where applicable;
- validation through tests or verified end-to-end execution.

Backend-only completion must always be labelled explicitly.

---

## 3. Current product snapshot

**Current formal product version:** `v0.8.0-alpha.1` — Core Platform Baseline
**Release state:** No formal product release tag is present. The repository does not yet prove a go-live-ready V1.0 release.

### Overall assessment

HoraLife is a substantial working Vedic astrology platform with core chart generation, saved Kundalis, compatibility, Dasha, reports, and a deterministic prediction engine in the current source tree. The earlier sprint description that calls Prediction Engine V1 a skeleton is stale: the engine now has seven life-area implementations and is called by premium Kundali generation. Its commercial report productization and validation are still incomplete.

**Go-live assessment: NOT READY.** The approved alpha → beta → RC → stable release path has not been completed. Major outstanding gates include correct birth-time-zone handling in core report flows, reproducible CI/build/test evidence, broad calculation and PDF validation, production security/configuration review, multilingual/content completion, and the finished premium report experience. KP astrology is explicitly deferred to a later product version and is not V1 scope.

### Readiness summary

| Product layer | Current assessment |
|---|---|
| Vedic calculation and Kundali flow | **Substantial implementation; timezone and regression gates remain** |
| Compatibility / Dasha | **Core flows exist; domain, boundary, and report-sharing validation remain** |
| Deterministic Prediction Engine | **Seven areas implemented in source; hardening and premium product integration remain** |
| Multilingual experience | **English, Hindi, and Kannada JSON key parity passes; route/report copy and locale UX remain to be verified** |
| Learning / Muhurtha / Panchang | **Partial product scope; unfinished features must be explicitly cut or completed for V1** |
| Production engineering | **Not release-ready: backend/API and Angular builds pass; production database, security, and deployed evidence remain outstanding** |
| Worktree / release evidence | **Not clean; 143 changed paths plus local build/output artifacts need review and integration** |

---

## 4. Active sprint

# Active milestone: V1 Go-live readiness

**Milestone status:** ACTIVE — product hardening and release definition
**Reviewed:** 2026-11-01
**Priority:** P0 — finish and stabilize the agreed V1 product; do not open KP or another major vertical

### Sprint goal

Complete the agreed commercial V1 using the existing Vedic system and the prediction/report scope already in the repository. Close correctness, quality, security, UX, release, and operational gates before introducing new astrology systems. KP remains next-version work.

### Prediction architecture status

The architecture below is implemented in the current source tree as an early milestone. The remaining work is validation, hardening, and product integration, not restarting the architecture.

- one common `PredictionContext` mapped once from Kundali Engine data;
- one generic `PredictionRequest` supporting report mode and custom-question mode;
- `PredictionAreaProfile` defining life-area relevance;
- one generic `PredictionEngine`;
- one generic `PredictionResult`;
- Dasha timeline support for current, past, and future ranges;
- deterministic claim generation;
- evidence and contradiction validation;
- AI narration only as a later wording/presentation layer.

### Go-live board

| Workstream | Current evidence/status | V1 exit condition |
|---|---|---|
| Vedic Kundali core | **Implemented; current behavior is India/IST.** Kundali, Dasha, and Compatibility convert entered birth times as IST; location search defaults to India. The Kundali request now binds a public `TimeZoneOffset` with a 5.5-hour default, and the UI sends `5.5` (not `5.30`); JSON binding has a regression test. Worldwide birth-time support is not implemented. | Confirm India/IST as the V1 support boundary; validate chart, Panchang, and Dasha fixtures for historical and day-boundary cases. If worldwide birth locations are required, add location time-zone IDs end to end before release. |
| Duplicate saved Kundalis | **Implemented in current source.** Save endpoint supports ask/create/reuse/update behavior through `KundaliBirthMatcher`. | Review concurrency/ownership behavior and verify API/UI duplicate scenarios; code presence alone does not prove release completion. |
| Prediction Engine V1 | **Implemented in source for seven areas and included for premium Kundali generation.** The custom-question endpoint rejects DateRange requests. | Freeze supported V1 request modes and validate advertised predictions for deterministic, explainable, conservative results across saved-chart fixtures. |
| Prediction hardening | **PARTIAL per 2026-08-08 checkpoint.** Dasha weighting, Gochar modifiers in generic synthesis, broader contradiction/confidence validation, and premium integrated report remain follow-up. | Decide which items are required for the V1 promise; finish required work and remove/defer unsupported claims from product surfaces. |
| Premium report | **PARTIAL.** Kundali PDF/email paths exist, but prediction synthesis/narration and the final premium report contract are not proven complete. | Finalize report content/entitlements, preserve evidence traceability, verify all PDF modes and email delivery end to end. |
| Multilingual portal | **PARTIAL — infrastructure and dictionary parity pass; screen coverage does not.** English/Hindi/Kannada selection and persisted preference exist; all 2,339 dictionary paths match. Source audit found user-facing English literals in public landing, Kundali/Compatibility report views, account security, audit logs, Dhruva chat history, consultation, company/legal, and CRM screens. Backend validation/error messages and HTML/PDF report headings are also largely English; generated report services have no locale-aware label path. | V1 should define which surfaces are localized. Translate all V1 route/report text, localize API errors, add locale-aware PDF/email output, and add static key-reference coverage checks. |
| Product UX/mobile | **PARTIAL / unverified.** Alpha.3 polish is still in the version plan; current uncommitted changes need review. | Complete critical first-use, report-open, save/reopen, loading/error, and mobile flows with product-owner acceptance. |
| Compatibility and Dasha | **Core flows implemented; safety/report completion remains.** Dedicated Compatibility report sharing and Dasha PDF/email are not fully verified. | Validate scoring language and timing boundaries; finish or explicitly exclude dedicated report-sharing promises from V1. |
| Muhurtha, Panchang, Learning | **Partial product scope.** Muhurtha lists unsupported rules; standalone Panchang and learning progress/content remain incomplete. | Decide which modules are in the V1 promise. Complete included flows or explicitly defer them from V1 navigation and marketing. |
| CI and regression | **PARTIAL — workflow added in worktree; local checks pass.** `.github/workflows/ci.yml` defines backend build/xUnit tests, Current Life Focus and Learning console checks, Angular production build, locale-key parity, and a PostgreSQL CRM schema/controller-authorization job. Backend solution build succeeds (0 errors); Angular production build succeeds; locale parity passes (2,339 keys in HI and KN); xUnit passes 146/146; Current Life Focus passes 13/13; Learning passes 10/10. The CRM harness compiles but database checks and the workflow have not run remotely. | Run PostgreSQL CI checks, confirm workflow, and add deeper Kundali/timezone/PDF acceptance coverage. |
| Security and operations | **NOT VERIFIED.** Production secret/config review, deployed PDF/email, migration/recovery, monitoring, and rollback evidence are absent from this repository review. | Complete the production checklist against the target environment: secrets, authorization, migrations/backups, logs/alerts, and rollback. |
| Source/release hygiene | **NOT READY.** 98 tracked and 45 untracked paths are changed; local build/output artifacts are also untracked, and no release tag is recorded. | Review and commit intended changes, exclude local outputs, resolve migrations/config, cut a clean RC, then tag only after acceptance gates pass. |

### V1 scope-control decision

- Finish the existing Vedic product and agreed prediction/report experience; do not start KP, horary, or another major vertical in V1.
- Practitioner CRM (clients, appointments, reminders, and in-app notifications) is explicitly included in V1 per product-owner decision. Treat its current worktree implementation as in progress until schema, authorization/ownership, reminder behavior, and end-to-end acceptance are verified.
- Store customer UI/payments, Android, Vastu, and other parked modules stay out of scope unless explicitly added to the V1 launch promise.
- For each partial module, decide whether it is part of the public V1 promise. If excluded, defer it cleanly and document the boundary.
- Keep the approved sequence: multilingual and UX completion → core beta safety → controlled RC/feature freeze → production approval → stable release. Version changes require evidence and product-owner approval.

### Current worktree feature inventory and V1 disposition

| Worktree change | Current status | Recommended V1 treatment |
|---|---|---|
| Kundali report/UI, chart, Yoga/Dosha, remedies and PDF updates | **PARTIAL — implemented in current worktree.** API and Angular production builds pass; PDF/visual and calculation acceptance are not proven. | **Include core report behavior** if this is the V1 promise. Review every changed report section, verify PDF/email modes, and avoid shipping new deterministic narrative until its rules and translations are approved. |
| Premium report additions (personality, life-area readings, two-year outlook) | **PARTIAL — builder and PDF paths added.** The deterministic personality heuristics and copy do not yet have expert/content validation evidence. | **V1 candidate**, because premium reporting is a stated product goal; keep each section gated or omit it until astrologer/content review, timing validation, and PDF pagination/locale checks pass. |
| Compatibility relationship-house analysis and UI | **PARTIAL — new chart-derived analysis and presentation are present.** Updated result model and controller mapping need saved/reopen and regression verification. | **Include the existing Compatibility product** in V1; treat the added house-analysis layer as optional until domain validation and marriage-safety review are complete. |
| Standalone Panchang calendar | **IMPLEMENTED in current source.** API supports calendar, daily/range, and eclipse endpoints; UI has daily/weekly/monthly calendar and location selection. | **Include core Panchang** if advertised, but validate sunrise/day boundaries, location time zones, month transitions, and eclipse visibility before launch. This replaces the old “empty controller / coming soon” status. |
| Muhurtha | **PARTIAL — established API/UI exist with known unsupported rules.** | **Include only within validated event/rule coverage**; show unsupported conditions clearly and avoid claiming full classical coverage. |
| Numerology localization and report UI changes | **PARTIAL — current UI uses translation keys for more result copy and Lo Shu interpretation.** Locale completeness and the revised content are unverified. | Include only if Numerology is in the V1 sales/menu promise; otherwise defer the vertical cleanly. If included, audit response copy and translation parity. |
| Consultation sketch/whiteboard moved to global overlay | **PARTIAL — code was moved from the Consultation page into a shared overlay.** No interaction/export regression evidence yet. | **Keep if the existing practitioner consultation flow is in V1**; verify drawing, chart switching, undo/redo, zoom, and export on desktop/mobile. |
| Gyan Kosha learning map/reference browser | **PARTIAL — new multilingual map JSON, API reference endpoints, route, and page exist.** Lesson-ID integrity is checked by the content service, but user completion/progress remains absent. | **Defer as an enhancement by default.** Keep Level 1 learning only if it is an explicit V1 commitment; do not let the new reference browser block core astrology go-live. |
| Practitioner CRM: clients, linked charts, consultations, and dashboard | **PARTIAL — included in V1 by product-owner decision.** New Premium Plus API/UI/models are present; appointment cancellation has active-state handling, idempotent repeat cancellation, a visible action, and 5 validation cases in the Muhurtha test project. The PostgreSQL CRM harness now invokes real controller methods to check cross-account client read/update, appointment cancellation ownership, and Premium Plus denial; solution build succeeds. Local database execution is unavailable, so these and schema checks await CI. Overlap behavior acceptance and full user-flow acceptance remain unverified. | **V1 scope.** Run the PostgreSQL CI harness; verify cancellation/edit/complete and appointment timezone behavior; perform role/plan end-to-end acceptance. |
| CRM reminders and in-app notifications | **PARTIAL — included in V1 with CRM.** Hosted reminder worker, notification API, and UI are present. Multi-instance delivery, retention, retry/idempotency, and production scheduling remain unverified. | Validate reminder timing and duplicate suppression, notification read/dismiss behavior, retention, and worker operations in a production-like environment. |
| Anonymous usage activity events | **PARTIAL — rate-limited event endpoint and frontend tracking service exist.** Privacy/retention and consent/product analytics policy are not recorded. | **Do not make a launch feature.** Include only after privacy, retention, and data-access rules are approved; otherwise disable/remove from the V1 release. |
| Dhruva prompt catalog and chart facts | **PARTIAL — prompt loading, tone rules, and expanded chart-fact selection are in the current tree.** Tests use the production prompt file and `DhruvaPromptCatalog.ResolveTone`; included in the passing 106-test prediction/report suite. | **Keep existing assistant only if promised in V1.** Review provider failure behavior and fact boundaries; defer new AI scope if it is not essential to the launch promise. |
| Worktree artifacts | **Not product work.** `.build-check*`, `output/`, and `restore-diagnostic.log` are local artifacts. | Exclude from V1 source/release and review ignore/cleanup policy separately. |

### Recommended V1 boundary for planning

- **Core launch:** Vedic Kundali generation and saved reports; validated Compatibility and Dasha; Panchang and supported Muhurtha flows; Practitioner CRM client/appointment/reminder/notification flows; authentication, billing/entitlements, sharing, and the premium report promise actually sold to users.
- **Conditional V1 commitments:** Numerology, Learning Level 1, Dhruva, and practitioner consultation tools only if these are part of the launch marketing or paid-plan promise. Each needs its own acceptance evidence.
- **Default post-V1:** KP astrology, CRM expansion beyond the agreed client/appointment/reminder scope, Gyan Kosha, analytics, and prediction/report sections that have not passed content, privacy, and runtime validation.
- **Release blockers regardless of scope:** hardcoded IST in core birth chart/Compatibility/Dasha entry points unless supported regions are explicitly limited; incompatible backend tests; clean migrations/deployment path; full user-flow/security review; and a clean committed RC.

### V1 release exit criteria

V1 is ready only when included scope is frozen, critical flows pass in a production-like environment, calculations and reports are validated against reference cases, supported locale/time-zone limits are explicit, security/configuration and recovery are reviewed, CI is green, and a clean RC completes controlled user acceptance with no release-blocking defects.

### Explicitly out of V1 scope

- KP astrology and horary/prashna: next-version enhancement.
- New astrology verticals or expansion beyond the agreed Vedic product scope.
- Store customer UI/payments, Android, Vastu, and Learning Level 2 unless separately approved as V1 launch commitments.

---

## 5. Verified roadmap status

### 5.1 Platform, authentication, and administration

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| JWT authentication | **DONE** | Password registration/login, JWT validation, protected routes, and change password exist. | Continue security regression tests. |
| Email OTP login and registration | **DONE** | Start, verify, and complete-registration flows exist with expiry, attempt limits, resend control, hashing, and rate limiting. | Production SMTP monitoring only. |
| Phone/SMS OTP | **DONE — known gaps** | Phone channel and 2Factor sender integration exist in code. | Verify actual SMS delivery, DLT/template/provider configuration, and prevent voice-route misconfiguration. |
| Audit logs | **DONE** | Authentication, data change, export, and communication actions are logged. | Define retention and admin filtering policy. |
| User login control | **DONE** | Admin route and backend support exist. | Production permission review. |
| White-label settings | **PARTIAL** | UI route exists. | Verify persistence, branding scope, tenant isolation, and production use case. |
| Dashboard | **DONE — known gaps** | Authenticated dashboard and summary service exist. | Continue UX refinement as modules mature. |
| Themes | **DONE** | Light and dark modes with persisted selection exist. | Remove stale references to the retired `hora` theme from documentation. |
| Multilingual portal | **PARTIAL** | EN/HI/KN Transloco infrastructure, language preference persistence, API term mappings, and selected localized prediction/Dhruva content exist. Dictionary key parity passes, but visible UI/API/report strings remain hardcoded English across multiple routes. | Complete V1 screen and report localization; localize API validation/errors; verify locale switching/persistence and add missing-reference detection. |

### 5.2 Kundali engine and report

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| D1 chart and planet calculations | **DONE** | Swiss Ephemeris-based chart, planets, signs, degrees, houses, nakshatra, pada, dignity, combustion, retrograde and related fields exist. | Maintain calculation regression samples. |
| Ascendant and houses | **DONE** | Ascendant, house signs, owners, occupants, aspects, themes, and house scores exist. | Domain validation and regression testing. |
| Planet and house scoring | **DONE — known gaps** | Planet strength, house scores, overall/stability/growth/challenge scores and breakdowns exist. | Review user-facing score safety and avoid fear-inducing unsupported labels. |
| Panchang inside birth report | **DONE** | Tithi, Paksha, Nakshatra, Pada, Akshara, Yoga, Karana, Vaar, Maas and Samvat data are included. | None for birth-report scope. |
| D9 Navamsa | **DONE** | D9 chart is generated and returned in the Kundali response. | Add deeper D9 interpretation only through approved prediction rules. |
| Sudarshan Chakra | **DONE** | Calculation and report output exist. | UI/report polish only. |
| Yogas | **DONE — known gaps** | Multiple Yoga rules, metadata, evidence, involved planets/houses and activation periods exist. | Final rule validation and coverage review. |
| Doshas | **DONE — known gaps** | Multiple Dosha rules, severity, reasons, metadata, remedies and activation periods exist. | Final rule validation and safe presentation. |
| Advanced strength tables | **DONE — known gaps** | Advanced strength-table service is integrated. | Confirm Shadbala/Ashtakavarga terminology only where full calculations exist. |
| Remedies | **DONE for current MVP scope** | Recommendations use Lagna, Rashi, chart and current Dasha factors. | Keep frozen simple scope; do not over-expand before Prediction Engine. |
| Gochar in Kundali report | **DONE** | Current transit chart, natal-house activation and Dasha links are included. | Migrate interpretation into the generic Prediction Engine later. |
| Kundali generation UI | **DONE** | Authenticated generation and report flow exist. | Continue responsive/print alignment fixes. |
| Save Kundali | **DONE — known gaps** | Per-user save, limits, history list and audit logging exist. | Add duplicate birth-profile prevention. |
| Reopen saved Kundali | **DONE** | Saved detail route and API exist. | None for current scope. |
| Delete saved Kundali | **DONE** | User-scoped delete and dependent report cleanup exist. | None for current scope. |
| Edit/update saved Kundali | **PLANNED** | Frontend endpoint configuration mentions update, but no actual update flow exists. | Define edit semantics and regenerate/replace behaviour. |
| Kundali PDF | **DONE** | Corporate, desktop colour, mobile-friendly, black-and-white, and fallback renderer paths exist. | Continue visual regression and pagination tests. |
| Kundali email sharing | **DONE** | Authorized, rate-limited SMTP sharing with PDF attachment and communication logging exists. | Production delivery monitoring. |

### 5.3 Compatibility

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Compatibility generation | **DONE — known gaps** | Both charts are generated and six explainable relationship dimensions are calculated. | Final domain-validation pass for marriage-decision safety. |
| Ashtakoota / Gun Milan | **DONE** | Eight Kuta calculations and total score are returned. | Confirm classical tables and edge cases. |
| Dosha comparison | **DONE — known gaps** | Both charts are evaluated and comparison analysis is produced. | Final approved include/exclude list and cancellation rules. |
| Save/history/reopen/delete | **DONE** | Full saved compatibility history flow exists. | Duplicate comparison prevention may be added later. |
| Saved Kundali selector integration | **DONE** | Compatibility can reuse saved person details instead of repeated entry. | UX refinement only. |
| Compatibility PDF | **PARTIAL** | Common report infrastructure exists, but the current PDF service is strongly Kundali-response-specific. | Add a dedicated Compatibility report contract and verified renderer. |
| Compatibility email sharing | **PARTIAL** | Email service exists, but dedicated Compatibility attachment generation is not fully verified. | Complete end-to-end compatibility report sharing. |
| Final marriage verdict wording | **PARTIAL** | Technical scores and dimensions exist. | Add cautious, evidence-based wording and avoid deterministic marry/do-not-marry claims. |

### 5.4 Dasha

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Vimshottari Mahadasha | **DONE** | Full 120-year timeline and current period analysis exist. | Regression samples. |
| Antardasha | **DONE** | Timeline, current period, strength and combined effect exist. | Regression samples. |
| Pratyantardasha | **DONE** | Nested periods, current PD, remaining duration, timeline and strength exist. | Add to any remaining Kundali summary contracts where required. |
| Saved Kundali selector | **DONE** | Dasha UI can directly choose a saved chart. | None for current scope. |
| Dasha report UI | **DONE — known gaps** | Dedicated page shows current MD/AD/PD, strength, timeline, life areas, challenges and actions. | Continue visual and wording refinement. |
| Dasha prediction text | **PARTIAL / legacy** | Older `PredictionService` and controller builders generate text. | Migrate factual conclusions into the frozen generic Prediction Engine. |
| Dasha PDF/email | **PLANNED** | No fully verified dedicated Dasha report-sharing contract exists. | Implement after generic report abstraction or dedicated contract. |

### 5.5 Prediction and synthesis

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Prediction Engine project | **PARTIAL — implementation exists** | Generic context, request/result pipeline, and seven life-area rule services exist. | Harden and complete V1 report integration; do not restart architecture. |
| Prediction API | **PARTIAL** | Authorized custom-question endpoint exists; it rejects DateRange requests. Kundali generation invokes report mode for seven premium areas. | Complete/clarify advertised request modes and validate API behavior. |
| Seven V1 life areas | **IMPLEMENTED — validation pending** | Career, Finance, Relationships, Education, Family, Property, and Health are generated for premium Kundali reports per August checkpoint and current controller. | Validate results and wording across chart fixtures; keep Health conservative. |
| Common `PredictionContext` | **IMPLEMENTED** | Mapper is used by Kundali report generation and question API. | Freeze compatibility contract and test missing/legacy fields. |
| Deterministic claims and evidence | **IMPLEMENTED — hardening pending** | Area services produce claim structures and evidence; checkpoint records traceability. | Confirm evidence completeness and repeatability across fixtures. |
| Contradiction/confidence validation | **PARTIAL** | Basic mixed-tone consolidation exists; checkpoint identifies generalized validation and advanced confidence/weighting as unfinished. | Complete agreed validation and confidence limits before commercial claims. |
| Past/current/future range handling | **PARTIAL** | Dasha selector supports date ranges internally, but custom-question API explicitly rejects DateRange. | Decide V1 date-range promise and complete the public flow if included. |
| AI narration layer | **PARKED / incomplete** | Deterministic synthesis feasibility exists; product narration integration is not verified. | Add only after factual outputs and content boundaries are frozen. |
| Premium integrated report | **PARTIAL** | Premium Kundali response includes predictions; report service is Vedic-response-specific and final synthesis integration is not verified. | Finalize contract, UI/PDF/email presentation, and entitlement behavior. |

### 5.6 Current Life Focus and Gochar

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Current Life Focus API | **DONE** | Authenticated top-area analysis and focused drill-down endpoints exist. | Persistence/expiry policy review. |
| Current Life Focus UI | **DONE** | Integrated component supports analysis, area selection and specific concerns. | UX and multilingual completion. |
| Full Gochar dashboard | **PLANNED** | Transit data exists inside Kundali and Current Life Focus, but no standalone complete Gochar product exists. | Define scope after Prediction Engine. |
| Daily/weekly/monthly prediction | **PLANNED** | No final unified date-range prediction product exists. | Build on generic Prediction Engine plus Gochar. |

### 5.7 Panchang and Muhurtha

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Standalone Panchang API | **IMPLEMENTED — validation pending** | Calendar, daily, range, and eclipse endpoints exist in `PanchangController`. | Validate astronomical/day-boundary results and supported location time zones. |
| Standalone Panchang UI | **IMPLEMENTED — polish/validation pending** | Page supports location search and daily/weekly/monthly calendar modes. | Complete end-to-end locale and responsive checks. |
| Monthly Panchang | **IMPLEMENTED — regression pending** | Calendar range requests and month navigation are wired in the UI. | Verify month/year transitions and server range limits. |
| Rahukal/Choghadiya free tools | **PARTIAL** | Related calculation concepts exist in Muhurtha. | Expose dedicated user tools with validated timings. |
| Muhurtha API | **DONE — known gaps** | Candidate generation, rule evaluation, scoring and recommendation endpoint exist. | Complete missing classical calculations. |
| Muhurtha UI | **DONE — known gaps** | Search, location, personalization, filters, card/table/Panchang views and rule evidence exist. | UX validation and remaining rule coverage. |
| Muhurtha event coverage | **PARTIAL** | Fifteen event types are enumerated. | Validate event-specific metadata/rules for each type. |
| Personal horoscope checks | **PARTIAL** | Optional support exists. | Complete event-specific natal contraindications. |
| Missing Muhurtha calculations | **PARTIAL** | Service explicitly declares unsupported Varjyam, eclipse/Sutak, Sankranti, detailed Navamsha/Vargottama, expert override and some natal rules. | Complete and test before claiming classical completeness. |

### 5.8 Learning by HoraLife

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Learning API/platform | **DONE** | Level/session APIs, language resolution, fallback, glossary and schema validation exist. | Add persistence and progress APIs. |
| Learning UI | **DONE — known gaps** | Level and session routes/components exist. | Progress, locking and completion UX. |
| Gyan Kosha reference map | **PARTIAL — new worktree feature** | Multilingual knowledge-map data, API endpoints, reference detail loading, and a dedicated route/page exist. | Validate all referenced lesson IDs/content and responsive/language behavior; decide if it is in V1. |
| Level 1 structure | **DONE** | Fifteen-session manifest exists. | Reconcile manifest status with actual files. |
| Level 1 content | **PARTIAL** | Substantial EN/HI/KN content exists; manifest still marks the level draft and sessions planned/in-progress. | Complete content review, metadata reconciliation and final validation. |
| Bonus Bridge | **PARTIAL** | Session 16 content exists; manifest remains in progress. | Final review and mark complete. |
| Level 2 | **PLANNED** | Manifest contains zero sessions. | Start only after Level 1/Bridge freeze. |
| Student progress | **PLANNED** | Manifest expresses progress/locking intentions, but durable tracking is not complete. | Define schema and APIs. |
| Quiz evaluation | **PLANNED** | Content self-checks exist; scored completion workflow is not complete. | Implement only with final course rules. |
| Certification | **PLANNED** | Certificate is explicitly disabled in manifests. | Implement after progress and assessment. |

### 5.9 Dhruva Assistant

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Product contract and roadmap | **DONE** | Detailed Dhruva documentation exists. | Keep aligned with repository changes. |
| Static Kundali coverage | **PARTIAL** | Chart-fact builder and UI/API assistant surfaces exist; current work expands the fact set. | Validate facts, omissions, safe wording, and report/user context boundaries. |
| Prompt catalog and tone profiles | **PARTIAL — current worktree** | Prompt file/catalog refactor exists; tests resolve tone through `DhruvaPromptCatalog` and load the actual prompt file. Covered by the passing 107-test prediction/report suite. | Verify prompt fallback/provider errors, and review supported language/tone behavior. |
| Compact/expanded/floating UI | **PARTIAL / unverified** | Assistant component exists and has current layout changes. | Verify opening, mobile positioning, language switching, and failure/loading behavior. |
| Tours | **PLANNED** | Previously validated conceptually, but no final application implementation verified in current repository. | Re-implement/verify when Dhruva resumes. |
| Janma Story | **PARKED** | Behaviour is designed; implementation not verified. | Resume in a later Dhruva phase. |
| AI prediction/remedies | **PARKED / out of scope** | Explicitly not part of Dhruva MVP. | Do not implement without a new approved contract. |

### 5.10 HoraLife Store

| Roadmap item | Status | Verified actual state | Remaining work / completion condition |
|---|---|---|---|
| Store domain/application/infrastructure | **DONE — backend** | Separate Store projects are included in the solution. | Maintain module boundaries. |
| Catalogue APIs | **DONE — backend** | Category, product and variant endpoints exist. | Customer UI later. |
| Cart APIs | **DONE — backend** | Cart flow and conversion infrastructure exist. | Customer UI later. |
| Address CRUD | **DONE — backend** | Verified canonical Step 1. | None for backend checkpoint. |
| Checkout preview | **DONE — backend** | Verified canonical Step 2. | None for backend checkpoint. |
| Price/stock revalidation | **DONE — backend** | Verified canonical Step 3. | None for backend checkpoint. |
| Order header/items | **DONE — backend** | Verified canonical Steps 4–5. | None for backend checkpoint. |
| Inventory reservation | **DONE — backend** | Verified canonical Step 6. | Payment/cancellation release logic later. |
| Convert cart | **DONE — backend** | Verified canonical Step 7. | None for backend checkpoint. |
| Backend E2E Postman suite | **DONE** | Recorded result: 113 passed, 0 failed, 0 skipped, 0 errors. | Keep collection versioned and rerun after changes. |
| Store customer UI | **PARKED** | No Angular Store route/product flow exists. | Resume only when Store workstream reopens. |
| Payments | **PARKED** | No gateway integration exists. | Resume with Store UI. |
| Store admin | **PARKED** | No complete admin UI exists. | Resume later. |

### 5.11 Future and parked modules

| Roadmap item | Status | Verified actual state | Resume condition |
|---|---|---|---|
| Vastu consultation engine | **PARKED** | Architecture is frozen outside the current implementation; only Vastu Muhurtha eligibility code exists here. | Resume with sample floor-plan implementation and feasibility validation. |
| Android app | **PARKED** | No Capacitor/Android project is present in current repository. | Reuse Angular frontend through Capacitor after web product stabilizes. |
| Political/Mundane astrology | **PARKED** | No active implementation. | Separate future decision. |
| HoraChakra | **PARKED** | No active implementation. | Separate future decision. |

---

## 6. Production engineering and technical debt

| Item | Status | Required action |
|---|---|---|
| GitHub Actions CI | **PARTIAL — worktree implementation** | Root `.github/workflows/ci.yml` builds the backend, runs xUnit plus the two console validation harnesses, checks locale-key parity, and builds the frontend on pull requests and pushes to `main` / `GoogleSingleSignIn`. Local checks pass; workflow has not run remotely yet. | Confirm workflow execution and adjust SDK/runtime from CI evidence. |
| Automated Kundali regression tests | **PARTIAL** | Add fixed birth-chart reference samples for planets, ascendant, houses, Panchang and Dasha. |
| Compatibility regression tests | **PLANNED** | Add Ashtakoota tables, Dosha comparison and dimension-score edge cases. |
| Dasha regression tests | **PLANNED** | Add MD/AD/PD boundary and timezone tests. |
| Muhurtha regression tests | **PLANNED** | Add event/date/location rule-result fixtures and transition boundaries. |
| PDF visual regression | **PLANNED** | Verify page count, overflow, charts, fonts and spacing for all modes. |
| Learning tests | **PARTIAL** | Session loading and fallback tests exist; expand to all manifests/content files. |
| Current Life Focus tests | **PARTIAL** | Dedicated tests/specs exist; keep aligned with metadata/rules. |
| Global timezone support | **PARTIAL — important** | Remove hardcoded India Standard Time assumptions from Kundali, Compatibility and Dasha request handling. |
| Duplicate Kundali save prevention | **IMPLEMENTED — verification pending** | Current save endpoint uses `KundaliBirthMatcher` and supports ask/create/reuse/update outcomes. Verify concurrent requests, ownership, and UI recovery behavior before beta. |
| Duplicate DI registration | **PLANNED cleanup** | `AddInfrastructure` is registered twice in `Program.cs`; remove duplication after validation. |
| Default sample controller | **PLANNED cleanup** | Remove unused `WeatherForecastController`. |
| API authorization consistency | **PLANNED review** | Review existing public/authenticated routes and the new anonymous usage-event endpoint; document data minimization, privacy, and rate-limit policy. |
| CRM database deployment | **PARTIAL — shared idempotent SQL and CI harness added in worktree.** EF migrations and startup bootstrap now call a common schema SQL definition. The PostgreSQL harness covers fresh creation, upgrade from pre-expansion tables (including legacy Scheduled status), repeated bootstrap, controller ownership, and entitlement gating. Local run skipped because `CRM_SCHEMA_TEST_CONNECTION` and PostgreSQL are unavailable. | Run the PostgreSQL job successfully in CI; then verify migration history/startup behavior against the target deployment database and backup/rollback procedure before CRM launch. |
| PostgreSQL driver security | **PATCHED IN WORKTREE; audit not fully verified.** Restore surfaced Npgsql 8.0.0 as affected by GHSA-x9vc-6hfv-hg8c. Direct Npgsql references are now 8.0.9 and EF provider/Core packages are aligned at 8.0.10. NuGet vulnerability-feed connectivity remains unavailable in this environment. | Run CI restore/security audit with feed access, confirm the resolved dependency graph contains no affected Npgsql version, and review all other dependency advisories. |
| Generic report abstraction | **PLANNED** | Support Kundali, Compatibility, Dasha and future premium report contracts cleanly. |
| Documentation drift | **PARTIAL** | Update stale theme, module and status references as code changes. |

---

## 7. Release milestones

### Milestone A — Core report beta

**Status:** NOT READY — release gates remain unverified

Required before declaring ready:

- [ ] Verify duplicate Kundali handling, including concurrent save and client ownership cases.
- [ ] Fix global timezone handling or explicitly limit beta to IST/India in product, API, and deployment documentation.
- [ ] Run frontend and backend production builds.
- [ ] Execute calculation regression samples.
- [ ] Verify PDF/email in deployed environment.
- [ ] Add minimum CI build checks.
- [ ] Complete security/configuration review for production secrets.

### Milestone B — Premium deterministic report V1

**Status:** PARTIAL — seven-area engine exists; commercial report remains unfinished

Required:

- [x] Implement deterministic prediction paths for seven approved life areas in current source.
- [x] Include prediction results in premium Kundali response.
- [ ] Complete confidence/contradiction hardening and reference-chart validation.
- [ ] Decide and implement V1 date-range and Gochar behavior.
- [ ] Finalize premium report contract and UI/PDF/email integration.
- [ ] Add controlled narration/presentation only after factual freeze, if required for the V1 promise.

### Milestone C — Learning Level 1 release

**Status:** PARTIAL

Required:

- [ ] Reconcile all session manifests with actual files.
- [ ] Complete EN/HI/KN content validation.
- [ ] Freeze Level 1 and Bonus Bridge.
- [ ] Implement progress and completion tracking.
- [ ] Decide quiz/certificate release scope.

### Milestone D — Store customer release

**Status:** PARKED

Required after reopening:

- [ ] Customer Store UI.
- [ ] Product/detail/cart/checkout screens.
- [ ] Payment gateway.
- [ ] Payment success/failure/idempotency flow.
- [ ] Order history and customer communication.
- [ ] Admin product/order/inventory tools.

---

## 8. Priority order

Unless a new explicit decision is recorded, use this order:

1. **Freeze the V1 product promise and included modules; keep KP deferred.**
2. Core calculation correctness, timezone boundaries, duplicate-save, and security fixes.
3. CI, production builds, reference-chart regression, and PDF/email verification.
4. Complete required premium prediction/report hardening and integration.
5. Finish included multilingual and UX/mobile work; explicitly defer excluded modules.
6. Controlled beta, RC feature freeze, production operations/recovery review, and stable release approval.
7. Resume KP and other parked modules only after V1 release.

### Scope-control rule

Do not open another large module while the active P0 milestone remains incomplete, unless a critical production defect requires immediate work.

---

## 9. Sprint update template

Use the following structure for every sprint update.

```md
## Sprint Update — YYYY-MM-DD

### Sprint
- Name:
- Status: Planned / Active / Blocked / Completed
- Source branch:
- Source commit:

### Completed
- Item:
  - Evidence:
  - Tests:
  - User-visible impact:

### In progress
- Item:
  - Current state:
  - Remaining:

### Blockers / risks
- Risk:
  - Impact:
  - Decision/action:

### Roadmap status changes
- Module: Old status -> New status
- Reason:

### Next committed work
1.
2.
3.
```

---

## 10. Change log

### 2026-11-01 — V1 go-live readiness board reconciled

- Updated the verified source commit and recorded the extensive uncommitted working tree.
- Replaced the stale Career-foundation sprint status with a V1 go-live readiness milestone.
- Reconciled the stale Prediction Engine skeleton claim with the seven-area implementation documented in the August 8 checkpoint and currently wired into premium Kundali generation.
- Recorded current release blockers and deferred KP astrology to the next product version.
- Initial source-level review found the tree had not been validated. The current follow-up records actual build/test results below.

### 2026-11-01 — Worktree inventory and verification update

- Audited tracked and untracked worktree changes, including CRM/notifications/activity analytics, Gyan Kosha, Compatibility, report, Dhruva, Panchang, Numerology, and consultation UI work.
- Updated Panchang from stale skeleton status to implemented API/UI with calculation and location validation still required.
- Classified new worktree features by current implementation state and proposed V1 inclusion/deferment boundaries.
- Angular production build succeeded with template and Sass warnings; API build succeeded with warnings.
- Full backend solution test run is not green: modified Dhruva tests refer to removed `DhruvaToneProfile.Resolve` and VSTest aborts after a 90-second testhost connection timeout.

### 2026-11-01 — V1 hardening started

- Fixed Dhruva tests to use the new prompt catalog and the production prompt file; adjusted PDF hierarchy assertions and fixtures to match current sections and premium additions.
- Added `.github/workflows/ci.yml` for backend build/xUnit tests, Current Life Focus and Learning console checks, and frontend production builds on pull requests and the `main` / `GoogleSingleSignIn` branches.
- Made the India/IST offset bindable and consistent: Kundali UI sends `5.5`, request model defaults to `5.5`, and a JSON binding regression test covers explicit/default values.
- Backend validation passes locally outside the sandbox: 107 Prediction/Report tests, 34 Muhurtha tests, 13 Current Life Focus checks, and 10 Learning checks. Angular production build passes with Angular diagnostic and Sass deprecation warnings.
- Added the missing English prediction-period label and a CI locale-key check; English, Hindi and Kannada now have 2,339 matching keys each.
- The testhost stall was sandbox-specific: a focused test passes outside the sandbox. CI has not yet run remotely.
- Confirmed core birth-time interpretation is India/IST: location search defaults to India, while Kundali, Compatibility, and Dasha controllers convert birth times with IST. The Kundali form now sends a bindable `5.5` offset for saved-chart metadata; global location-specific timezone handling remains unimplemented.
- Working tree currently contains 95 tracked and 42 untracked paths, including pre-existing in-progress product changes and local artifacts; these remain to be reviewed and intentionally integrated.

### 2026-11-01 — CRM included in V1 and schema hardening

- Recorded the product-owner decision to include Practitioner CRM in V1; KP astrology remains deferred.
- Added appointment cancellation state handling (including idempotent repeat cancellation), exposed cancellation in the appointment UI, and corrected appointment display to preserve the stored wall time/timezone label.
- Consolidated CRM schema creation/expansion SQL for EF migrations and startup bootstrap; added a PostgreSQL fresh/upgrade/idempotency harness and a dedicated PostgreSQL CI job. Local schema harness reports SKIP because no PostgreSQL connection is configured; CI database execution remains pending.
- Latest local checks: backend solution build succeeds (0 errors); Angular production build succeeds; locale parity passes (2,339 Hindi and Kannada keys); full xUnit suite passes (146/146: Prediction/Report 107, Muhurtha/CRM validation 39); Current Life Focus passes 13/13 and Learning 10/10. CRM database checks skip locally because no PostgreSQL connection is configured. NuGet audit-feed connectivity and existing code/compiler warnings remain.
- CRM source review found controllers require authentication, resolve active users from the identity claim, enforce Premium Plus on CRM data routes, and scope data access by the current user; notification endpoints also filter by owner.
- Extended the PostgreSQL CRM harness to invoke real controller actions for cross-account client reads/updates, appointment cancellation ownership, and non-Premium Plus denial. The harness compiles; the database-backed checks still require the PostgreSQL CI job.
- Fixed the Npgsql high-severity advisory surfaced during harness build by upgrading the driver reference to 8.0.9 and aligning Npgsql EF provider/EF Core package versions at 8.0.10. Build succeeds with no package downgrade or assembly-conflict warnings; NuGet audit-feed connectivity still prevents a complete current dependency audit.
- Current worktree inventory is 98 tracked and 45 untracked paths, including local build/output artifacts; source review, CI, CRM database execution, and release integration remain outstanding.

### 2026-08-04 — Initial canonical tracker

- Created the repository-backed HoraLife sprint and roadmap source of truth.
- Reconciled roadmap memory with actual `main` branch implementation.
- Confirmed Kundali, Compatibility, Dasha through Pratyantardasha, report sharing, Current Life Focus, authentication, and Store backend milestones.
- Corrected standalone Panchang to skeleton status.
- Corrected generic Prediction Engine V1 to skeleton/planned status.
- Corrected Dhruva to documentation/planned status.
- Recorded Learning as platform-complete but content/completion-partial.
- Set **Prediction Engine V1 — Career Foundation** as the active P0 sprint.

---

## 11. Historical decisions that remain frozen

- Remedies remain simple for MVP: Lagna/Rashi plus active Dasha factors; do not over-expand before synthesis.
- Prediction facts must be deterministic; AI may only narrate approved structured results later.
- Pratyantardasha is required as the narrow timing layer.
- Compatibility must use cautious, evidence-based language because it can influence marriage decisions.
- Store backend canonical eight-step order is complete and parked.
- Android should reuse Angular through Capacitor when resumed.
- Vastu consultation remains a separate architecture/workstream.
- Dhruva must not invent predictions, remedies, medical claims, or missing chart facts.
- Do not count a parked module as active unfinished sprint work.

---

_End of canonical tracker._
