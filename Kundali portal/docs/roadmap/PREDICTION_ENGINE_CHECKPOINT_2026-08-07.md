# Prediction Engine Checkpoint — 2026-08-08

Repository: `kishsharma/AiKundali`

## Exact resume status

Prediction Engine V1 Career is working end-to-end through `/api/Kundali/generate` and is being migrated to a generic **facts + metadata** architecture.

### Verified runtime output

Current Career output still returns:
- `career_foundation_pressure` — Challenging
- `career_10th_lord_support` — Supportive
- MD/AD/PD Career Dasha support claims
- `career_current_outlook_mixed` — Mixed
- Current effective window narrowed to active Pratyantardasha

The generic house-lord evidence is verified in runtime output:
`house-lord:10:Mercury:house:11`

## Frozen architecture

```text
Kundali calculations
→ PredictionContext
→ Generic deterministic facts
→ Prediction metadata
→ Life-area relevance / interpretation
→ PredictionClaim[]
→ Consolidation
→ Basic deterministic synthesis
→ Optional OpenAI narration later
```

Rules:
- Do not build 12×12 hardcoded house-lord combinations.
- House-lord placement is a fact, not automatically Supportive/Challenging.
- Source-house themes + target-house themes create the base connection meaning.
- Tone comes separately from aspects, dignity, strength, Dasha, Yoga/Dosha, later Gochar.
- Metadata contains knowledge; C# contains behavior.
- OpenAI later may improve wording only; it must not invent facts/rules/timing/tone.
- CurrentLifeFocus is temporary and will eventually be removed after Prediction Engine is proven. Prediction Engine must not depend on CurrentLifeFocus.

## Prediction metadata status

Prediction-specific metadata now lives inside `HoraLife.PredictionEngine`.

Current structure includes:
```text
Metadata/
  Data/
    life-areas.json
```

Career profile is now loaded from metadata instead of hardcoded C#.
Hardcoded Career profile fallback was removed after runtime verification.

Career metadata currently represents:
- Primary house: 10
- Supporting houses: 2, 6, 11
- Natural significators: Sun, Saturn, Mercury

`PredictionMetadataProvider` loads the JSON and `PredictionAreaProfileProviderService` maps it to the runtime `PredictionAreaProfile`.

## Factory/composition cleanup

`PredictionEngineFactory.cs` was moved under:
```text
Factory/
```

Factory owns default metadata construction. `PredictionEngineService` remains focused on orchestration.

## Generic facts completed

Facts are grouped under `Facts/` by responsibility.

### 1. HouseLordPlacementFact — completed and integrated

Generic extraction for all houses:
```text
SourceHouse
LordPlanet
TargetHouse
EvidenceKey
```

Evidence-key format:
`house-lord:{SourceHouse}:{LordPlanet}:house:{TargetHouse}`

Career now selects the fact where `SourceHouse == 10` instead of rediscovering the 10th-lord placement itself.

### 2. HouseAspectFact — completed and integrated

Generic extraction of house aspect state:
- HouseNumber
- HasBeneficAspect
- HasMaleficAspect
- AspectedBy
- EvidenceKey

Career 10th-house foundation logic now consumes the generic HouseAspectFact instead of reading raw house aspect flags directly.
The old `tenthHouse` variable became unused and was removed.

### 3. PlanetAspectFact — completed and integrated into evidence

Generic extraction of planet aspect state:
- Planet
- HasBeneficAspect
- HasMaleficAspect
- AspectedBy
- EvidenceKey

Career 10th-lord aspect evaluation has been migrated to the generic `PlanetAspectFact`.

## Existing Career logic intentionally still present

Do not over-clean in the next session.

Still present for now:
- Career-specific claim generator
- Career-specific Dasha claim generator / tone resolver
- 10th-lord dignity logic using current planet data
- existing consolidation

These remain until the next generic facts are introduced and proven.

## Do NOT do next

Do not jump immediately to:
- Finance / Relationships / other life areas
- Yoga/Dosha integration
- Gochar
- complicated confidence weighting
- combustion/retrograde expansion
- large rule engine
- OpenAI narration
- 12×12 house-lord interpretation metadata

Finish one complete Career V1 proof cycle first.

## Remaining work for complete Career V1

Approximately 3 mini-phases remain.

### Phase A — meaning + dignity
1. Add Prediction Engine house-significations metadata for all 12 houses.
2. Load it through Prediction metadata provider.
3. Add generic `PlanetDignityFact` + extractor.
4. Wire dignity facts into evidence.

Purpose: allow a fact such as `10th lord → 11th` to deterministically mean `career/profession matters connect with gains/income/networks/ambitions`, while dignity remains a separate modifier.

### Phase B — generic Dasha activation
1. Add generic `DashaActivationFact` + extractor.
2. Wire active MD/AD/PD into generic evidence.
3. Gradually replace Career-specific Dasha aspect plumbing with reusable facts.

### Phase C — visible Career synthesis
1. Add a small generic life-area interpretation/synthesis layer using Profile + Facts + metadata.
2. Produce structured deterministic Career output such as:
   - Career Foundation
   - Career Lord
   - Career Direction / house connection
   - Current MD / AD / PD activation
   - Current Outlook
   - Effective Window
3. Inspect actual runtime Career result.
4. Freeze Career V1.

Only after Career V1 is proven should Finance / Relationships / Education / Health / Family / Property be added using the same generic pipeline.

## Architectural target after Career

```text
Finance metadata / Relationship metadata / etc.
        +
Same generic facts
        +
Same interpretation pipeline
        ↓
New life-area synthesis
```

Career is the expensive proof. Additional life areas should mainly become metadata + relevance configuration rather than new calculation engines.

## Exact next step for next session

Start with **Phase A, Step 1: house-significations metadata**.

Do not redo prior cleanup and do not add another fact before defining the stable 12-house meanings needed by the generic house-lord connector.

## Checkpoint label

**Career Prediction V1 — metadata profile proven; HouseLordPlacement, HouseAspect and PlanetAspect facts integrated; next = house significations + dignity, then Dasha activation, then visible Career synthesis.**
