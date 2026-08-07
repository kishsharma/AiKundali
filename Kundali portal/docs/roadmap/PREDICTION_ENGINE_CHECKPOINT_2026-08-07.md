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

## FROZEN SYNTHESIS APPROACH — FACTS + METADATA

This approach is now frozen as the direction for Prediction Engine V1 expansion across Career and the other life areas.

### Core principle

Do **not** build prediction depth as large trees of life-area-specific `if/else` combinations such as every house lord in every possible destination house.

Instead:

```text
Kundali calculations
        ↓
PredictionContext
        ↓
Generic deterministic facts
        ↓
Reusable metadata
        ↓
Life-area relevance / interpretation mapping
        ↓
Prediction claims
        ↓
Generic consolidation
        ↓
Basic deterministic synthesis
        ↓
Optional controlled OpenAI narration later
```

### Generic facts

Facts describe what is present in the chart without deciding the final life-area narrative by themselves.

Examples:

- House lord placement: source house, lord planet, destination house.
- Planet placement.
- Benefic/malefic aspect on a house.
- Benefic/malefic aspect on a planet.
- Planet dignity/condition.
- House strength.
- Active Mahadasha / Antardasha / Pratyantardasha.
- Yoga presence.
- Dosha presence.
- Later: Gochar activation.

Example:

```text
HOUSE_LORD_PLACEMENT
SourceHouse = 10
LordPlanet = Mercury
TargetHouse = 11
```

The fact itself does **not** mean `Supportive` or `Challenging`.

### House-lord placement interpretation

Do not create a 12×12 table of manually written base interpretations unless a genuinely special classical combination requires an exception.

Use the existing significations/themes of the source and target houses and a generic connector rule:

```text
Matters of SourceHouse
        ↓ connect through its lord
Matters of TargetHouse
```

For example, if the 10th lord is placed in the 11th, the base fact means that profession/status/public-role matters are connected with gains/income/networks/ambitions. It does **not** automatically mean a successful career.

Tone and strength come from independent modifiers such as dignity, benefic/malefic aspects, house condition, supporting houses, Dasha activation, Yoga/Dosha influences and later Gochar.

### Reuse existing metadata before creating new metadata

Existing repository metadata should be reused wherever it already expresses the needed knowledge:

- `CurrentLifeFocus/life-areas.json` already defines life-area houses, house weights and karakas. Career currently contains houses 2/6/10/11 and karakas Sun/Saturn/Mercury.
- `HouseMetadataService` already defines generic house names and life-area/signification themes.
- Yoga metadata already contains nature, descriptions, references and impact areas.
- Dosha metadata already contains nature and impact areas.

Do not duplicate these definitions inside Prediction Engine when a reusable source already exists.

The Prediction Engine should eventually consume a neutral/shared metadata contract rather than directly depending on API-specific `CurrentLifeFocus` classes.

### Life areas

The same fact model must be reusable across at least the planned life areas such as:

- Career
- Finance
- Relationships
- Education
- Health
- Family
- Property

Life areas select and interpret relevant facts; they should not require separate duplicated calculation engines.

### Generic composition before special exceptions

Base behavior should be generic composition from facts + house/planet/life-area metadata.

Only add a special combination rule when:

1. the generic composition is insufficient,
2. the combination has a distinct astrological meaning,
3. the meaning is source-verified,
4. the special rule adds information rather than duplicating the generic statement.

This keeps the engine scalable without losing the ability to support classical exceptions later.

### AI boundary remains strict

OpenAI/API narration may be introduced later **after deterministic facts, relevance, tone, timing and claims are established**.

AI may:

- combine approved facts into natural, meaningful sentences,
- improve readability,
- avoid repetitive template wording,
- adapt presentation/language.

AI must not:

- invent chart facts,
- alter deterministic tone or timing,
- introduce unsupported astrological rules,
- override evidence,
- create remedies or predictions outside the approved deterministic input.

Conceptually:

```text
Approved deterministic facts + claims
              ↓
       Controlled synthesis payload
              ↓
          OpenAI narration
              ↓
       Human-readable report text
```

## Intentionally not changed yet

Do not introduce these until explicitly resumed under the frozen facts + metadata approach:

- strong 10th-house score rule
- hardcoded 12×12 house-lord placement rules
- duplicated Career/Finance/etc. metadata in Prediction Engine
- additional supporting-house rules as one-off conditional trees
- planet-strength weighting
- functional-nature weighting
- combustion/retrograde weighting
- Yoga/Dosha Career modifiers
- Gochar Career modifiers
- confidence weighting
- a large general-purpose dynamic JSON rule engine
- OpenAI narration before deterministic synthesis is stable
- separate report prediction endpoint
- custom-question endpoint
- tests during the current implementation pass

## Known minor observation

MD, AD and PD can currently emit the same semantic claim key such as `career_dasha_support`. Their exact origin remains distinguishable through `dasha-level:*` and Dasha period evidence keys. This is not treated as a blocker at this checkpoint.

## Next committed work

Stop adding more Career-specific `if/else` rules.

Next implementation work should first align the Prediction Engine with the frozen **generic facts + reusable metadata** model.

Priority:

1. Reuse existing life-area metadata instead of hardcoded Career profile duplication.
2. Introduce the minimal generic fact representation needed by the next real use case.
3. Map facts to life-area claims through reusable metadata/interpretation logic.
4. Preserve existing claim evidence traceability and consolidation.
5. Add only source-verified special-case rules when generic composition is insufficient.

## Checkpoint label

**Career Prediction V1 — end-to-end integration complete; facts + metadata synthesis architecture frozen for expansion.**
