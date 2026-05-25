Scoring Summary - Planet / House / Kundali

High-level summary

- Layered scoring model: Planet -> House -> Overall Kundali.
- Scores are numeric (0..100) and use a neutral base of 50; deltas and weights adjust from that base.
- All components record human-readable factors for transparency.
- Deltas are expressed relative to the neutral baseline (50) and commonly weighted before aggregation.

Planet score (single-file summary)

- Base: 50 (neutral).
- Dignity: mapped semantic label -> numeric weight.
  - Exalted: +25
  - Own Sign: +18
  - Moolatrikona: +20
  - Friendly Sign: +10
  - Neutral Sign: 0
  - Enemy Sign: -10
  - Debilitated: -25
- Combustion: if IsCombust -> -15.
- House placement types (semantic): Kendra/Trikona +10, Dusthana -10, otherwise 0.
- Calculation:
  - PlanetScore = clamp(50 + DignityWeight + CombustionPenalty + HousePlacement, 0, 100)
- Factors list records each applied adjustment and a short reason.

House score (single-file summary)

- Base: 50 (neutral).
- Components:
  - House lord contribution: WeightedDelta(lordScore, HouseLordWeight=0.50)
    - WeightedDelta uses (lordScore - 50) * weight and rounds.
  - House lord placement relative to the house: own house, trines, kendras, upachaya positions, and dusthana positions modify the score; combustion and retrograde status add small penalties.
  - Occupant contributions: sum of (WeightedDelta(occupantScore, OccupantStrengthWeight=0.20) + semantic classification influence + functional role by ascendant)
    - Classification influence examples: NaturalBenefic +4, NaturalMalefic -4, ShadowPlanet -3, Neutral 0.
  - Aspects received: for each aspecting planet add (WeightedDelta(aspectingPlanetScore, AspectStrengthWeight=0.15, with small potency bonus for special Mars/Jupiter/Saturn drishti) + half semantic influence + half functional role).
  - Karaka support: natural house significators are scored and averaged, then applied as a weighted delta.
  - Affliction/protection balance: benefic and malefic/shadow influences, Jupiter protection, node contact with the house/lord, and adjacent-house enclosure add a capped modifier.
  - Functional lordship: house lord role by ascendant gives support or challenge, including yogakaraka-style kendra/trikona ownership.
  - House category influence: semantic mapping to small integers, e.g. Kendra +6, Trikona +8, Upachaya +4, Dusthana -8, Maraka -5.
- Calculation:
  - HouseScore = clamp(50 + HouseLordContribution + HouseLordPlacement + SumOccupantInfluences + SumAspectInfluences + KarakaSupport + AfflictionProtection + FunctionalLordship + HouseCategoryInfluence, 0, 100)
- Result object includes Name, Score, Factors, DominantThemes (from HouseKnowledge life areas / karakatva).

Kundali / Overall chart score (single-file summary)

- Subscores derived from selected houses:
  - Lagna: weighted mix of lagna lord score, first house score, and ascendant sign theme.
  - Moon: Moon planet score.
  - Sun: Sun planet score for vitality, authority, confidence, and visible self-expression.
  - Core identity: weighted mix of Lagna, Moon, and Sun.
  - Kendra power: average of houses 1, 4, 7, 10.
  - Trikona power: average of houses 1, 5, 9.
  - Dharma: average of houses 1, 5, 9.
  - Artha: average of houses 2, 6, 10.
  - Kama: average of houses 3, 7, 11.
  - Moksha: average of houses 4, 8, 12.
  - Planetary balance: average planetary score, strong/weak planet counts, and dispersion.
  - Current dasha is explicitly excluded from static Kundali scoring and belongs to dasha/transit prediction.
- Overall composition uses static weighted subscores and clamps to 0..100.
- Also produces:
  - DominantStrengths: top houses and planets by score.
  - DominantWeaknesses: bottom houses and planets by score.
  - Factors: aggregated descriptive factors from house and planet computations.

Key implementation notes

- All scores use Math.Clamp to 0..100 to avoid overflow/underflow.
- Many internal helpers use the neutral base (50) pattern: WeightedDelta = (score - 50) * weight.
- Semantic knowledge (planet natures, house bhava types, life areas) comes from the AstrologyKnowledgeService and JSON knowledge base.
- Aspect calculations depend on AspectService outputs (HouseAspects and PlanetAspects collections).
- API generation now passes graha drishti aspects into house and overall scoring.
- Date/time sensitive computations (dasha timelines) use DateTime ranges; ensure DateTime.Kind consistency when comparing.

Usage example (conceptual)

1. Compute planet positions for birth date/time.
2. Compute aspects with AspectService.
3. Compute planet scores: PlanetScoreService.CalculateMany(planetPositions).
4. Compute house scores: HouseScoreService.CalculateAllHouseScores(ascendantSign, planetPositions, aspectResult).
5. Compute mahadasha/antardasha and identify current dasha.
6. Compute overall chart scores via OverallKundaliScoreService (composes house scores and current dasha).

Notes & Recommendations

- Keep Zodiac/Nakshatra/House calculators stateless and testable.
- Normalize DateTime comparisons (ToUniversalTime or DateTimeOffset) when deciding current dasha.
- The model is intentionally explainable: each service returns factors to show why a score changed.
