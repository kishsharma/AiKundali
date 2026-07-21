# Current Life Focus / Gochar Life Area Analysis

## Current Architecture Findings
- Reuse `ChartService.GenerateChart`, `DashaService.CalculateCurrentDasha`, `AspectService.CalculateGrahaDrishti`, `HouseScoreService.CalculateAllHouseScores`, `PlanetStrengthService`, `AstrologyKnowledgeService`, and `HelperData.GetSignOwner`.
- Saved Kundalis are loaded from `KundaliDbContext.Kundalis` by `kundaliId` and current user.
- Current Gochar should be calculated for the requested analysis date at the saved birth location, then read from natal Lagna and natal Moon.
- No existing analytical-event persistence mechanism was found; Phase 2 logs event names through structured logging.

## Reusable Components
- Natal chart: `ChartService.GenerateChart`.
- Current Mahadasha/Antardasha: `DashaService.GetBirthMahadasha` and `CalculateCurrentDasha`.
- Natal sensitivity: `HouseScoreService.CalculateAllHouseScores`.
- Gochar aspects: `AspectService.CalculateGrahaDrishti`.
- House lord lookup: `HelperData.GetSignOwner`.

## Missing Dependencies
- No backend test project currently exists.
- No persisted analysis store currently exists; Phase 2 uses an in-memory deterministic snapshot cache keyed by `analysisId`.

## Implementation Phases
- Phase 2 backend: metadata, scoring services, mapper, orchestration, focus endpoint, structured logs.
- Later frontend phase: render top-three cards below Gochar on the Kundali page.

## File-By-File Change List
- `KundaliEngine.KnowledgeBase/Data/CurrentLifeFocus/life-areas.json`: configurable life-area metadata and scoring weights.
- `KundaliEngine.API/DTOs/Requests/CurrentLifeFocus*.cs`: request contracts.
- `KundaliEngine.API/DTOs/Responses/CurrentLifeFocus*.cs`: response contracts.
- `KundaliEngine.API/Services/CurrentLifeFocus/*`: scoring, mapping, analysis cache, services, and models.
- `KundaliEngine.API/Controllers/GocharController.cs`: `/api/gochar/current-life-focus` endpoints.
- `KundaliEngine.API/Program.cs`: dependency injection registrations.

## API Contracts
- `POST /api/gochar/current-life-focus`
- `POST /api/gochar/current-life-focus/{analysisId}/focus`

## Scoring Approach
- Keep dasha, gochar, and natal-sensitivity components separately.
- Final activation uses configurable weights from metadata.
- Antardasha has stronger immediate activation than Mahadasha.
- Saturn, Jupiter, Rahu, and Ketu have stronger long-term transit weight; Mars has medium weight; Moon is excluded from ranking.
- Experience tone is separate from activation level and is based on positive and pressure factors.

## Risks
- In-memory analysis snapshots expire on app restart.
- Saved birth time/time zone handling follows existing saved Kundali conventions.
- High activation can be supportive, mixed, or pressured depending on factor balance.

## Checklist Items
- [x] Add configurable life-area metadata.
- [x] Add backend DTOs.
- [x] Add scoring and mapping services.
- [x] Add top-three endpoint.
- [x] Add focused-analysis endpoint.
- [x] Add structured analytical-event logging.
- [x] Keep Angular UI untouched in Phase 2.

## Phase 3.2 Metadata Expansion
- Expanded Current Life Focus metadata from 9 to 14 life areas.
- Added relatable user-facing areas: family/parents, self-confidence/decision-making, daily work/routine, friends/network/public image, and spiritual growth/inner change.
- Broadened follow-up options across existing areas for job search, family money, loans/EMI, parents, boss issues, legal matters, home peace, exams, child concerns, travel/visa, and similar everyday concerns.
- Kept deterministic scoring unchanged; this phase only improves metadata coverage and user selection vocabulary.
