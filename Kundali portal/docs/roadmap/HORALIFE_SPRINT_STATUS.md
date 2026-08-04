# HoraLife Sprint Status and Roadmap Tracker

> **Canonical roadmap source of truth**  
> Repository: `kishsharma/AiKundali`  
> Branch reviewed: `main`  
> Last verified: **2026-08-04**  
> Verified source commit: `3a20cd392915de84af0aee241a2c3d7b56e64440`  
> Product owner: **Kishore Sharma**  
> Status owner: HoraLife development team

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

### Overall assessment

HoraLife is a substantial working astrology platform with a strong calculation and report foundation. The core Kundali, Compatibility, Dasha, authentication, report sharing, Current Life Focus, and Store backend milestones are implemented.

The platform is **not yet the final premium prediction product** because the frozen generic Prediction Engine V1 remains at skeleton stage.

### Readiness summary

| Product layer | Current assessment |
|---|---|
| Astrology calculation foundation | **Strong / substantially complete** |
| Core Kundali report flow | **Release-candidate quality with listed gaps** |
| Compatibility flow | **Functionally complete; final domain-safety validation required** |
| Dasha timing flow | **Complete through Pratyantardasha** |
| Premium deterministic prediction | **Not implemented** |
| Learning product | **Platform complete; course content and completion workflow incomplete** |
| Store | **Backend milestone complete; customer product parked** |
| Production engineering | **Needs CI and broader automated regression coverage** |

---

## 4. Active sprint

# Sprint: Prediction Engine V1 — Career Foundation

**Sprint status:** ACTIVE  
**Started:** 2026-08-04  
**Target end:** To be scheduled  
**Priority:** P0 — primary product milestone

### Sprint goal

Implement the smallest valid deterministic Career prediction slice using the frozen Prediction Engine V1 architecture. The output must be based on calculated Kundali facts and explicit evidence, not generic or AI-generated astrology text.

### Frozen architecture to implement

- one common `PredictionContext` mapped once from Kundali Engine data;
- one generic `PredictionRequest` supporting report mode and custom-question mode;
- `PredictionAreaProfile` defining life-area relevance;
- one generic `PredictionEngine`;
- one generic `PredictionResult`;
- Dasha timeline support for current, past, and future ranges;
- deterministic claim generation;
- evidence and contradiction validation;
- AI narration only as a later wording/presentation layer.

### Sprint backlog

- [ ] Define the final common `PredictionContext` contract.
- [ ] Implement one mapper from normalized Kundali data to `PredictionContext`.
- [ ] Define generic `PredictionRequest` and request modes.
- [ ] Define `PredictionAreaProfile` and Career profile configuration.
- [ ] Define generic claim, evidence, confidence, timing, and result contracts.
- [ ] Implement deterministic Career rule evaluation.
- [ ] Include natal promise before timing.
- [ ] Include house, house lord, Karaka, strength, Yoga/Dosha activation, MD/AD/PD, and Gochar modifiers.
- [ ] Add contradiction and unsupported-claim validation.
- [ ] Expose a real Prediction API endpoint.
- [ ] Connect the Career result to a minimal UI or verified API consumer.
- [ ] Add focused automated tests with known Kundali samples.
- [ ] Validate results against multiple saved charts.
- [ ] Document what AI narration may and may not change.

### Definition of done

The Career slice is complete only when:

1. the builder no longer returns an empty result;
2. a generic engine is used rather than a Career-only architecture;
3. each conclusion contains traceable evidence;
4. timing distinguishes Mahadasha, Antardasha, Pratyantardasha, and Gochar roles;
5. unsupported conclusions are rejected or downgraded;
6. repeat execution with the same context produces the same factual result;
7. tests cover positive, mixed, contradictory, and insufficient-evidence cases;
8. the result is available through an actual API flow;
9. AI is not required for factual correctness.

### Explicitly out of this sprint

- Marriage, finance, health, education, children, and other prediction areas;
- open-ended AI questions;
- full premium report narration;
- Dhruva implementation;
- Store UI or payments;
- Android app;
- Vastu consultation;
- Learning Level 2.

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
| Multilingual portal | **PARTIAL** | English, Hindi, and Kannada infrastructure/content exist in major areas. | Complete coverage across all screens; Marathi is not uniformly integrated. |

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
| Prediction Engine project | **SKELETON** | Dedicated project and Career models exist. | Implement the frozen generic architecture. |
| Prediction API | **SKELETON** | `PredictionController` exists but has no endpoints. | Implement real request/response flow. |
| Career prediction | **SKELETON** | Career builder currently returns an empty result. | Active sprint. |
| Common `PredictionContext` | **PLANNED** | Not present in final frozen form. | Active sprint. |
| Generic request modes | **PLANNED** | Report/custom-question request model not implemented. | Active sprint. |
| Deterministic claims and evidence | **PLANNED** | Not implemented in the new engine. | Active sprint. |
| Contradiction/claim validation | **PLANNED** | Not implemented. | Active sprint. |
| Past/current/future range handling | **PLANNED** | Dasha calculations exist, but generic prediction-range orchestration is not implemented. | Active sprint. |
| AI narration layer | **PARKED** | Architectural boundary is approved; implementation intentionally deferred. | Start only after deterministic results are stable. |
| Premium integrated report | **PLANNED** | Detailed data report exists, but final synthesis/prediction layer is missing. | Complete Prediction Engine and report narration contract. |

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
| Standalone daily Panchang API | **SKELETON** | `PanchangController` is empty. | Implement date/location request and response. |
| Standalone daily Panchang UI | **SKELETON** | Page explicitly shows “coming soon.” | Build complete day view. |
| Monthly Panchang | **PLANNED** | No complete monthly calendar flow exists. | Implement after daily API. |
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
| Static Kundali coverage | **PLANNED** | Approved scope exists in documentation. | Implement normalized safe fact coverage. |
| Compact/expanded/floating UI | **PLANNED** | UX is approved but no verified production component exists. | Implement after current priority milestone. |
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
| GitHub Actions CI | **PLANNED — high priority** | Add backend build/tests and frontend build/tests for pull requests and main. |
| Automated Kundali regression tests | **PARTIAL** | Add fixed birth-chart reference samples for planets, ascendant, houses, Panchang and Dasha. |
| Compatibility regression tests | **PLANNED** | Add Ashtakoota tables, Dosha comparison and dimension-score edge cases. |
| Dasha regression tests | **PLANNED** | Add MD/AD/PD boundary and timezone tests. |
| Muhurtha regression tests | **PLANNED** | Add event/date/location rule-result fixtures and transition boundaries. |
| PDF visual regression | **PLANNED** | Verify page count, overflow, charts, fonts and spacing for all modes. |
| Learning tests | **PARTIAL** | Session loading and fallback tests exist; expand to all manifests/content files. |
| Current Life Focus tests | **PARTIAL** | Dedicated tests/specs exist; keep aligned with metadata/rules. |
| Global timezone support | **PARTIAL — important** | Remove hardcoded India Standard Time assumptions from Kundali, Compatibility and Dasha request handling. |
| Duplicate Kundali save prevention | **PLANNED — important** | Add normalized identity/fingerprint rule and return existing record or conflict. |
| Duplicate DI registration | **PLANNED cleanup** | `AddInfrastructure` is registered twice in `Program.cs`; remove duplication after validation. |
| Default sample controller | **PLANNED cleanup** | Remove unused `WeatherForecastController`. |
| API authorization consistency | **PLANNED review** | Decide which calculation endpoints are public vs authenticated and apply consistently. |
| Generic report abstraction | **PLANNED** | Support Kundali, Compatibility, Dasha and future premium report contracts cleanly. |
| Documentation drift | **PARTIAL** | Update stale theme, module and status references as code changes. |

---

## 7. Release milestones

### Milestone A — Core report beta

**Status:** NEARLY READY

Required before declaring ready:

- [ ] Add duplicate Kundali save prevention.
- [ ] Fix global timezone handling or explicitly limit beta to IST/India.
- [ ] Run frontend and backend production builds.
- [ ] Execute calculation regression samples.
- [ ] Verify PDF/email in deployed environment.
- [ ] Add minimum CI build checks.
- [ ] Complete security/configuration review for production secrets.

### Milestone B — Premium deterministic report V1

**Status:** NOT READY

Required:

- [ ] Complete Prediction Engine V1 Career slice.
- [ ] Add additional approved life-area profiles.
- [ ] Generate traceable deterministic claims.
- [ ] Validate contradictions and confidence.
- [ ] Integrate time ranges and Gochar modifiers.
- [ ] Create premium report contract.
- [ ] Add controlled narration/presentation layer only after factual freeze.

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

1. **Prediction Engine V1 — Career slice**
2. Production safety fixes for the core report beta
3. Generic/premium report integration
4. Compatibility final safety validation
5. Muhurtha missing-rule completion
6. Learning Level 1 reconciliation and completion workflow
7. Standalone daily/monthly Panchang
8. Dhruva static MVP
9. Store UI/payments/admin
10. Vastu, Android and other parked modules

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
