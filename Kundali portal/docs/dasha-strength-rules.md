# Dasha Strength Calculation Rules

This document describes the current implemented strength calculation used by the Dasha analysis API and UI.

The calculation is implemented in:
- `KundaliEngine.API/Controllers/DashaController.cs`
- `KundaliEngine.Astrology/Services/Astrology/PlanetStrengthService.cs`
- `KundaliEngine.Domain/Rules/PlanetStrengthRules.cs`

## Scope

The Dasha page now displays strength for three active Vimshottari levels:

| Level | Active lord used | API field |
| --- | --- | --- |
| Mahadasha | Current Mahadasha planet | `strength.mahadasha` |
| Antardasha | Current Antardasha planet | `strength.antardasha` |
| Pratyantardasha | Current Pratyantardasha planet | `strength.pratyantardasha` |

The legacy/root fields remain for compatibility:

| Field | Meaning |
| --- | --- |
| `strength.overallStrength` | Same as current Mahadasha strength score |
| `strength.planet` | Current Mahadasha planet |
| `strength.planetStrength` | Current Mahadasha strength label |
| `strength.overallResult` | Current Mahadasha result label |

## Calculation Flow

For each dasha level, the backend calls:

```csharp
BuildDashaLevelStrength(level, planet, planets)
```

The method performs these steps:

1. Find the active dasha lord in the natal planet positions.
2. Determine basic planet strength from sign placement.
3. Convert the strength enum into a numeric score.
4. Apply combustion and retrograde deductions.
5. Clamp the final score between `25` and `96`.
6. Convert the final score into a result label.
7. Return the score, planet, sign, house placement, strength label, and result label.

## Sign-Based Strength Rules

The base planet strength is determined by `PlanetStrengthService.GetStrength(planet, sign)`.

### Nodes

Rahu and Ketu are not scored by exaltation, debilitation, or own sign in the current implementation.

| Planet | Strength enum |
| --- | --- |
| Rahu | `NotApplicable` |
| Ketu | `NotApplicable` |

### Exaltation Signs

| Planet | Exaltation sign |
| --- | --- |
| Sun | Aries |
| Moon | Taurus |
| Mars | Capricorn |
| Mercury | Virgo |
| Jupiter | Cancer |
| Venus | Pisces |
| Saturn | Libra |

### Debilitation Signs

| Planet | Debilitation sign |
| --- | --- |
| Sun | Libra |
| Moon | Scorpio |
| Mars | Cancer |
| Mercury | Pisces |
| Jupiter | Capricorn |
| Venus | Virgo |
| Saturn | Aries |

### Own Signs

| Planet | Own signs |
| --- | --- |
| Sun | Leo |
| Moon | Cancer |
| Mars | Aries, Scorpio |
| Mercury | Gemini, Virgo |
| Jupiter | Sagittarius, Pisces |
| Venus | Taurus, Libra |
| Saturn | Capricorn, Aquarius |

### Fallback

If the planet is not exalted, debilitated, in own sign, or a node, the strength is `Neutral`.

## Score Mapping

After the strength enum is found, `DashaController.StrengthToScore(...)` maps it to a base score.

| Strength enum | Base score |
| --- | ---: |
| `Exalted` | 92 |
| `OwnSign` | 84 |
| `Neutral` | 68 |
| `Debilitated` | 42 |
| `NotApplicable` | 62 |
| Any other value | 60 |

## Modifiers

After the base score is selected, two natal-condition modifiers may apply.

| Condition | Score adjustment |
| --- | ---: |
| Planet is combust | `-10` |
| Planet is retrograde | `-4` |

After modifiers, the score is clamped:

```text
minimum = 25
maximum = 96
```

So the final dasha strength score can never go below `25` or above `96`.

## Result Labels

The numeric score is converted into a qualitative result by `ScoreToResult(...)`.

| Final score | Result label |
| --- | --- |
| `80` and above | Very Favorable |
| `65` to `79` | Favorable |
| `50` to `64` | Mixed |
| Below `50` | Challenging |

## Planetary Association Display

For each active dasha level, the backend also calculates a display-only association summary for the active dasha lord.

| API field | Meaning |
| --- | --- |
| `associationTone` | `Benefic`, `Malefic`, `Mixed`, or `None` |
| `associationSummary` | Compact UI text such as `Benefic: Jupiter Aspect`, `Malefic: Saturn Conjunction`, `Mixed: Jupiter + Saturn`, or `None` |
| `associationDetails` | Tooltip details explaining which planet is conjunct or aspecting the dasha lord |

The association check includes conjunction by same house and these aspect rules:

| Planet | Aspects used |
| --- | --- |
| Sun, Moon, Mercury, Venus | 7th aspect |
| Jupiter | 5th, 7th, 9th aspects |
| Mars | 4th, 7th, 8th aspects |
| Saturn | 3rd, 7th, 10th aspects |
| Rahu, Ketu | 5th, 7th, 9th aspects |

The current natural benefic group is Moon, Mercury, Jupiter, and Venus. Sun, Mars, Saturn, Rahu, and Ketu are treated as natural malefics for this display.

Important: planetary association is currently shown as context in the strength table. It does not yet modify the numeric percentage score.

## Strength Labels

The strength enum is also converted into a display label.

| Strength enum | Display label |
| --- | --- |
| `Exalted` | Very Strong |
| `OwnSign` | Strong |
| `Debilitated` | Weak |
| `NotApplicable` | Karmic |
| Any other value | Moderate |

## Level Details

### Mahadasha Strength

Mahadasha strength is calculated from the natal placement of the current Mahadasha lord.

Example flow:

```text
Current Mahadasha = Mercury
Find Mercury in natal planet positions
Check Mercury sign against exaltation/debilitation/own-sign rules
Map strength to score
Apply combustion/retrograde deductions
Return strength.mahadasha
```

This is also used for the legacy top-level fields:

```text
strength.overallStrength
strength.planet
strength.planetStrength
strength.overallResult
summary.overallScore
summary.overallResult
```

### Antardasha Strength

Antardasha strength is calculated from the natal placement of the current Antardasha lord.

Example flow:

```text
Current Antardasha = Venus
Find Venus in natal planet positions
Apply same sign strength rules
Apply same score mapping and modifiers
Return strength.antardasha
```

At present, the Antardasha score is the standalone strength of the Antardasha lord. It does not yet blend Mahadasha lord strength with Antardasha lord strength.

### Pratyantardasha Strength

Pratyantardasha strength is calculated from the natal placement of the current Pratyantardasha lord.

Example flow:

```text
Current Pratyantardasha = Saturn
Find Saturn in natal planet positions
Apply same sign strength rules
Apply same score mapping and modifiers
Return strength.pratyantardasha
```

At present, the Pratyantardasha score is the standalone strength of the Pratyantardasha lord. It does not yet blend Mahadasha, Antardasha, and Pratyantardasha strengths into a weighted combined score.


## Classical BPHS Basis

The numeric values used by this application are product scoring weights, not direct shloka numbers. The classical basis is qualitative: BPHS instructs that dasha results should be judged from the active period lord's dignity, placement, strength, house position, and associations.

### Dasha Lord Dignity and Placement

BPHS Chapter 47, verses 5-6 state the core rule for Dasha results:

| BPHS condition | Classical result direction | App scoring interpretation |
| --- | --- | --- |
| Dasha lord in Lagna | Favorable | Supportive placement context |
| Dasha lord exalted | Favorable | High base score |
| Dasha lord in own sign | Favorable | Strong base score |
| Dasha lord in a peaceful/friendly sign | Favorable | Neutral/supportive score depending on implemented dignity |
| Dasha lord in 6th, 8th, or 12th | Unfavorable | Future placement modifier candidate |
| Dasha lord debilitated | Unfavorable | Low base score |
| Dasha lord in enemy sign | Unfavorable | Future enemy-sign modifier candidate |

This is why the application considers exaltation, own sign, neutral placement, debilitation, combustion, and retrogression when showing Dasha strength.

### Planet-Specific Dasha Results

BPHS Chapter 47, verses 7-11 apply this logic to Surya Dasha. The text describes better results when the Sun is in own sign, exalted, in a Kendra, in the 11th house, associated with the 9th or 10th lord, and strong in varga.

The important principle for this feature is:

```text
The Dasha lord's strength and placement modify how well that planet can deliver its period results.
```

The current app implements only a small first layer of that principle: sign dignity plus combustion/retrograde modifiers.

### House-Lord Dasha Results

BPHS Chapter 48, verse 1 says that if a house lord is in an auspicious house and exalted etc., its Dasha gives favorable results. If it is debilitated and in an inauspicious house, results become adverse.

This supports the broader scoring direction:

```text
Strong dignity + auspicious placement = better Dasha result
Weak dignity + inauspicious placement = harder Dasha result
```

The current app stores placement text, but does not yet numerically score house auspiciousness. That is listed under future expansion.

### Antardasha Rule Support

BPHS Antardasha chapters apply the same dignity logic to sub-periods. For example, in the Antardasha sections, good results are described when the Antardasha lord is exalted, in own sign, in Kendra, in Trikona, or in the 11th house. Adverse results are described when the Antardasha lord is debilitated or placed in difficult houses/signs.

So for the UI:

```text
Mahadasha strength = current Mahadasha lord's natal strength
Antardasha strength = current Antardasha lord's natal strength
Pratyantardasha strength = current Pratyantardasha lord's natal strength, by extension of the same active-lord principle
```

BPHS explicitly discusses Dasha and Antardasha result judgment. The Pratyantardasha score in this app applies the same active-lord dignity principle at the third Vimshottari level.

### Important Clarification About Numbers

BPHS does not say:

```text
Exalted = 92
Own sign = 84
Neutral = 68
Debilitated = 42
```

Those are application weights chosen to convert BPHS-style qualitative rules into a simple UI percentage.

The shastra gives the direction of judgment:

| Classical rule | App score direction |
| --- | --- |
| Exalted gives stronger/favorable results | Highest score bucket |
| Own sign gives favorable results | Strong score bucket |
| Neutral dignity gives ordinary usable results | Mid-positive score bucket |
| Debilitation gives adverse/weaker results | Low score bucket |
| Combustion weakens delivery | Deduction |
| Retrogression complicates/delays delivery | Small deduction in current implementation |

### Example: Saturn Pratyantardasha

Suppose the active period is:

```text
Mercury Mahadasha / Venus Antardasha / Saturn Pratyantardasha
```

And natal Saturn is in Aries and retrograde.

BPHS principle:

```text
A debilitated active Dasha lord gives weaker or more difficult results.
```

Application scoring:

```text
Saturn in Aries = Debilitated
Debilitated base score = 42
Retrograde deduction = -4
Final Pratyantardasha strength = 38%
```

Here `42` is not a BPHS verse number. It is the app's numeric representation of the BPHS qualitative idea that a debilitated dasha lord is weak/adverse.

### Example: Venus Antardasha

Suppose natal Venus is in Libra.

BPHS principle:

```text
A Dasha or Antardasha lord in its own sign gives favorable results.
```

Application scoring:

```text
Venus in Libra = Own sign
Own-sign base score = 84
No combustion or retrograde deduction
Final Antardasha strength = 84%
```

Here no points are added to make `84`. It is the direct base score for own-sign dignity.
## Current Limitations

The current implementation is intentionally simple and deterministic. It does not yet include these classical or advanced factors in the Dasha strength score:

- Functional benefic/malefic status by Lagna.
- House lordship quality for the active dasha lord.
- House placement auspiciousness by house category.
- Aspects received by the dasha lord.
- Conjunctions with benefics or malefics.
- Shadbala or full baladi/avastha calculation.
- Divisional chart confirmation such as Navamsa or Dashamsa.
- Relationship between Mahadasha lord and Antardasha/Pratyantardasha lord.
- Weighted combined score across Maha, Antar, and Pratyantar.
- Transit support or obstruction.

## Suggested Future Rule Expansion

A richer BPHS-aligned scoring model can keep the current base score and add weighted layers:

| Layer | Suggested purpose |
| --- | --- |
| Natal dignity | Current exaltation/own/debilitation score |
| House placement | Reward trikon/kendra/upachaya based on context; penalize dusthana when relevant |
| Functional role | Adjust for Lagna-based benefic/malefic lordship |
| Combustion/retrograde | Keep current deductions, refine by planet-specific rules |
| Association | Add benefic/malefic conjunction and aspect influence |
| Inter-lord relationship | Score relation between Maha-Antar-Pratyantar lords |
| Divisional support | Confirm strength in D9/D10 depending on prediction category |
| Transit modifier | Apply current transit support as a smaller timing modifier |

Possible combined score model:

```text
Mahadasha score = natal strength of Mahadasha lord
Antardasha score = 60% Antardasha lord + 25% Mahadasha lord + 15% inter-lord relationship
Pratyantardasha score = 55% Pratyantardasha lord + 25% Antardasha lord + 10% Mahadasha lord + 10% inter-lord relationship
```

This future model is not implemented yet. The current production score is the direct active-lord score described above.

