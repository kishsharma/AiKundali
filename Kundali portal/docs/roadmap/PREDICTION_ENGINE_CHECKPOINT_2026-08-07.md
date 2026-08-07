# Prediction Engine Checkpoint — 2026-08-07

Repository: `kishsharma/AiKundali`

## Status

Prediction Engine V1 Career slice has reached a verified end-to-end integration checkpoint through the existing Kundali generation API.

This checkpoint is **not** the final completion of Career V1. Plumbing/integration is working; Career interpretation depth, validation, broader time scopes, and automated tests remain.

## Frozen architecture in use

- One common `PredictionContext` mapped from the completed `GenerateKundaliResponse`.
- One generic `PredictionRequest` supporting report/custom-question modes.
- `PredictionAreaProfile` for life-area relevance.
- Generic `PredictionEngineService` and `PredictionResult`.
- Deterministic source claims with `RuleIds` and `EvidenceKeys`.
- Generic claim consolidation.
- AI narration remains deferred and may later change wording only, not facts/evidence/timing.

## API integration completed

The API project now references `HoraLife.PredictionEngine`.

API-side integration is under the logical `Prediction` area.

### Mapping boundary

```text
GenerateKundaliResponse
        ↓
PredictionContextMapper
        ↓
PredictionContext
        ↓
PredictionEngineService
```

`PredictionContextMapper` maps:

- Houses
- Planets
- Vimshottari Dasha timeline
  - Mahadasha
  - Antardasha
  - Pratyantardasha
- Yogas — top-level structured evidence only for now
- Present Doshas only — top-level structured evidence only for now

No duplicate Kundali prediction-source DTO was introduced.

## Dasha integration completed

The Kundali response now populates Pratyantardasha periods inside each Antardasha using the existing `DashaService.GeneratePratyantardashaForTimelinePeriod(...)` logic.

Prediction Engine receives the full timing hierarchy:

```text
Mahadasha
  → Antardasha
      → Pratyantardasha
```

This allows `PredictionTimeScope.Current` to select the active MD/AD/PD periods at the supplied `ReferenceDate`.

## Existing Kundali API is the report integration point

No separate report Prediction API was created.

Current report path:

```text
POST /api/Kundali/generate
        ↓
Kundali calculations
        ↓
GenerateKundaliResponse
        ↓
PredictionContextMapper
        ↓
PredictionEngineService.Generate(...)
        ↓
Prediction response DTO
        ↓
GenerateKundaliResponse.Predictions
```

`GenerateKundaliResponse` now includes a `Predictions` collection so later life areas can be added without changing the response contract shape.

## Career rules currently active

### Natal foundation

Current approved Career natal rule uses the 10th-house aspect state only:

- Benefic aspect only → Supportive
- Malefic aspect only → Challenging
- Both → Mixed
- Neither / missing 10th house → no claim

Current verified chart produced:

- `career_foundation_pressure`
- Tone: `Challenging`
- Stage: `Natal`
- Rule: `career.10th_house.malefic_aspect`

### Dasha lord tone

Current Career Dasha tone resolver uses only natal aspect flags of the selected Dasha lord:

- Benefic aspect only → Supportive
- Malefic aspect only → Challenging
- Both → Mixed
- Neither → no directional claim

No strength, functional nature, combustion, retrograde, Yoga/Dosha or Gochar modifiers are combined yet.

### Current verified timing result

For the verified 2026-08-07 response:

- Mahadasha: Mercury → Supportive
- Antardasha: Venus → Supportive
- Pratyantardasha: Mercury → Supportive
- Natal Career foundation → Challenging

The consolidated Career result is therefore:

- Key: `career_current_outlook_mixed`
- Tone: `Mixed`
- Stage: `Consolidated`
- Effective current window: `2026-07-21` to `2026-12-15`

The consolidated window correctly narrows to the active Pratyantardasha because it is the smallest active timing interval.

## Verified end-to-end behavior

The existing `/api/Kundali/generate` response successfully returned:

1. Natal Career claim.
2. Active Mahadasha Career claim.
3. Active Antardasha Career claim.
4. Active Pratyantardasha Career claim.
5. One consolidated current Career outlook.

Each claim includes traceable `RuleIds` and `EvidenceKeys`.

The full integration path is therefore verified:

```text
Kundali data
   ↓
PredictionContext
   ↓
Career natal evidence
   +
Current MD / AD / PD evidence
   ↓
Deterministic source claims
   ↓
Generic consolidation
   ↓
Existing Kundali API response
```

## Intentionally not changed yet

Do not introduce these until explicitly resumed:

- strong 10th-house score rule
- house-lord Career rules
- additional supporting-house rules
- planet-strength weighting
- functional-nature weighting
- combustion/retrograde weighting
- Yoga/Dosha Career modifiers
- Gochar Career modifiers
- confidence weighting
- dynamic JSON rule framework
- AI narration
- separate report prediction endpoint
- custom-question endpoint
- tests during the current implementation pass

## Known minor observation

MD, AD and PD can currently emit the same semantic claim key such as `career_dasha_support`. Their exact origin remains distinguishable through `dasha-level:*` and Dasha period evidence keys. This is not treated as a blocker at this checkpoint.

## Next committed work

Stop plumbing/integration changes here.

Next work should improve **Career interpretation depth one deterministic factor at a time**, while preserving the current generic architecture and evidence traceability.

The next Career factor should be selected deliberately before implementation; do not introduce multiple new rules at once.

## Checkpoint label

**Career Prediction V1 — end-to-end integration complete; interpretation-depth phase next.**
