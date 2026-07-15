# Muhurtha Engine Specification

## 1. Purpose

This document defines the functional and technical design for implementing a configurable Muhurtha engine in the Kundali Portal.

The engine must identify suitable time windows for specific activities by combining:

1. Generic Muhurtha filters
2. Panchanga suitability
3. Personal horoscope compatibility
4. Muhurtha chart strength
5. Event-specific rules
6. Regional and tradition-based configuration

The implementation must reuse existing Kundali, Panchanga, ephemeris, location, timezone, house, planet, Dasha, transit, Nakshatra, Tithi, Yoga, Karana, and divisional-chart services wherever available.

Do not modify existing Kundali calculation behaviour unless explicitly required.

---

## 2. Primary Goals

The Muhurtha engine should:

- Accept an event type, date range, location, and optional horoscope reference
- Generate candidate date and time windows
- Apply generic inauspicious-time filters
- Evaluate Panchanga suitability
- Evaluate personal suitability using the native horoscope
- Generate and assess the Muhurtha chart
- Reject invalid windows
- Score and rank valid windows
- Explain strengths, warnings, and rejection reasons
- Support configurable regional and event-specific behaviour
- Allow expert overrides with audit information

---

## 3. Muhurtha Evaluation Layers

### 3.1 Generic Time Rules

These rules are independent of the native horoscope and are calculated using local sunrise, sunset, weekday, Panchanga, and astronomical events.

Initial rules:

- Rahu Kalam
- Yamaganda
- Gulika Kalam
- Durmuhurtha
- Varjyam
- Bhadra / Vishti Karana
- Panchaka
- Gandanta
- Eclipse period
- Sutak period
- Sankranti restriction
- Choghadiya
- Abhijit Muhurtha

### 3.2 Panchanga Rules

Evaluate the five Panchanga elements:

- Tithi
- Vara
- Nakshatra
- Yoga
- Karana

Suitability must depend on the selected event type.

Examples:

- A Tithi suitable for property purchase may not be suitable for marriage
- A Nakshatra suitable for travel may not be suitable for Gruha Pravesha
- Vishti Karana may be blocked for auspicious ceremonies but handled differently for specific activities

### 3.3 Personal Horoscope Rules

When a horoscope is provided, evaluate:

- Tara Bala
- Chandra Bala
- Current Mahadasha
- Current Antardasha
- Optional Pratyantardasha
- Dasha-lord relationship with event houses
- Dasha-lord natal strength
- Major transits
- Transit Moon
- Event-house activation
- Natal contraindications relevant to the activity

### 3.4 Muhurtha Chart Rules

For each candidate time window, calculate the Muhurtha chart and evaluate:

- Muhurtha Lagna
- Lagna lord strength
- Lagna affliction
- Moon strength
- Moon placement
- Event-related houses
- Event-related house lords
- Relevant natural significators
- Benefic and malefic influence
- Sixth, eighth, and twelfth-house involvement
- Rahu/Ketu influence
- Combustion
- Retrogression where applicable
- Gandanta
- Navamsha confirmation
- Vargottama support where applicable

---

## 4. Supported Event Types

Initial event types:

- Marriage
- Engagement
- Gruha Pravesha
- Bhoomi Puja
- Property Purchase
- Business Opening
- Job Joining
- Vehicle Purchase
- Travel
- Education Start
- Medical Procedure
- Naming Ceremony
- Annaprashana
- Upanayana
- General Auspicious Work

The design must allow new event types to be added without modifying the rule engine core.

---

## 5. Event-Specific House Mapping

Initial house mappings:

| Event Type | Important Houses |
|---|---|
| Marriage | 2, 7, 11 |
| Engagement | 2, 7, 11 |
| Gruha Pravesha | 4, 8, 11 |
| Bhoomi Puja | 4, 8, 11 |
| Property Purchase | 4, 11 |
| Business Opening | 2, 7, 10, 11 |
| Job Joining | 6, 10, 11 |
| Vehicle Purchase | 4, 11 |
| Travel | 3, 9, 12 |
| Education Start | 2, 4, 5, 9 |
| Medical Procedure | 1, 6, 8 |
| Naming Ceremony | 1, 2, 5 |
| Annaprashana | 1, 2, 5 |
| Upanayana | 2, 5, 9 |
| General Auspicious Work | 1, 5, 9, 11 |

These mappings must remain configurable.

---

## 6. Rule Severity Model

Every rule result must use one of the following severity levels:

```text
HardBlock
StrongAvoid
SoftAvoid
Neutral
Positive
```

### HardBlock

Reject the candidate slot.

Examples:

- Eclipse period
- Configured Sutak period
- Severe Gandanta
- Event-prohibited Bhadra / Vishti
- Severe Naidhana Tara
- Ashtama Chandra when configured as blocking
- Event-specific destructive combination

### StrongAvoid

Normally reject or heavily penalize.

Examples:

- Rahu Kalam for an auspicious beginning
- Durmuhurtha
- Varjyam where followed
- Vipat or Pratyari Tara
- Severely afflicted event lord

### SoftAvoid

Allow but reduce the score and display a warning.

Examples:

- Yamaganda
- Gulika
- Weak Choghadiya
- Panchaka for selected events
- Mild transit obstruction

### Positive

Increase the score.

Examples:

- Good Choghadiya
- Abhijit Muhurtha
- Sampat, Kshema, Sadhana, Mitra, or Parama Mitra Tara
- Good Chandra Bala
- Strong Lagna
- Strong event house
- Benefic Navamsha confirmation

---

## 7. Rule Configuration

Rule behaviour must be configurable by:

```text
Rule + Event Type + Regional Profile
```

Each configured rule should support:

- Rule key
- Display name
- Sanskrit name
- Group
- Rule type
- Enabled flag
- Severity
- Score impact
- Hard-reject flag
- Applicable event types
- Excluded event types
- Regional applicability
- Calculation method
- User-facing message
- Recommendation
- Expert-override permission

Do not permanently hard-code every severity inside rule classes.

---

## 8. Regional Profiles

Initial regional profiles:

- General
- North India
- South India
- Custom

Examples:

- Varjyam may be enabled by default for South India
- Panchaka rules may vary by region
- Bhadra interpretation may vary by activity and tradition
- Sankranti restriction windows may be configurable
- Choghadiya may be shown as a secondary indicator

The rule engine must remain deterministic for a selected regional profile.

---

## 9. Generic Rule Priority

Use this priority order:

```text
Hard astronomical and Panchanga defects
    >
Event-specific Panchanga suitability
    >
Personal horoscope compatibility
    >
Muhurtha Lagna and chart strength
    >
Choghadiya and secondary refinements
```

Choghadiya must not be the primary deciding factor.

It should refine an already acceptable Muhurtha rather than override major defects.

---

## 10. Tara Bala

Calculate the inclusive Nakshatra count from the native's Janma Nakshatra to the Muhurtha Moon Nakshatra.

```text
TaraPosition = InclusiveNakshatraCount % 9

If remainder is 0, use 9.
```

Mapping:

| Position | Tara | Default Result |
|---:|---|---|
| 1 | Janma | Soft Avoid |
| 2 | Sampat | Positive |
| 3 | Vipat | Strong Avoid |
| 4 | Kshema | Positive |
| 5 | Pratyari | Strong Avoid |
| 6 | Sadhana | Positive |
| 7 | Naidhana | Hard Block |
| 8 | Mitra | Positive |
| 9 | Parama Mitra | Strong Positive |

The severity must remain configurable by event type and tradition.

---

## 11. Chandra Bala

Count inclusively from the natal Moon sign to the Muhurtha Moon sign.

Default favourable positions:

```text
1, 3, 6, 7, 10, 11
```

Default difficult positions:

```text
4, 8, 12
```

The eighth position may be configured as a hard block for important activities.

---

## 12. Muhurtha Candidate Generation

The engine should:

1. Accept a requested date range and daily time range
2. Calculate sunrise and sunset for each date and location
3. Generate candidate time slots
4. Split slots whenever any relevant factor changes

A candidate boundary may occur when:

- Tithi changes
- Nakshatra changes
- Yoga changes
- Karana changes
- Lagna changes
- Navamsha changes
- Rahu Kalam begins or ends
- Yamaganda begins or ends
- Gulika begins or ends
- Durmuhurtha begins or ends
- Varjyam begins or ends
- Bhadra begins or ends
- Choghadiya changes
- Abhijit Muhurtha begins or ends
- Eclipse or Sutak begins or ends
- Gandanta begins or ends

Do not evaluate only one fixed interval per hour. Use astronomical and Panchanga transition boundaries where possible.

---

## 13. Recommended Evaluation Pipeline

```text
Request Validation
    ↓
Resolve Event Configuration
    ↓
Resolve Regional Profile
    ↓
Generate Panchanga and Astronomical Boundaries
    ↓
Generate Candidate Time Slots
    ↓
Apply Generic Time Rules
    ↓
Apply Panchanga Rules
    ↓
Reject Hard-Blocked Slots
    ↓
Apply Personal Horoscope Rules
    ↓
Generate Muhurtha Chart
    ↓
Evaluate Lagna, Moon, Event Houses, and Significators
    ↓
Evaluate Navamsha
    ↓
Normalize Score
    ↓
Rank Candidate Slots
    ↓
Merge Adjacent Equivalent Slots
    ↓
Return Recommended Windows with Explanations
```

---

## 14. Scoring

Initial group weights:

| Group | Weight |
|---|---:|
| Generic Time Rules | 15 |
| Panchanga Rules | 25 |
| Personal Horoscope Rules | 30 |
| Muhurtha Chart Rules | 30 |

Total:

```text
100
```

Suggested verdicts:

| Score | Verdict |
|---:|---|
| 85–100 | Excellent Muhurtha |
| 70–84 | Highly Favourable |
| 55–69 | Acceptable with Minor Cautions |
| 40–54 | Weak Muhurtha |
| 0–39 | Not Recommended |

A hard-block result must override the numeric score.

Scoring should remain configurable and normalized to prevent unlimited cumulative positive or negative values.

---

## 15. Core Domain Models

Suggested models:

```text
MuhurthaRequest
MuhurthaLocation
MuhurthaCandidateSlot
MuhurthaRuleDefinition
MuhurthaRuleConfiguration
MuhurthaRuleContext
MuhurthaRuleResult
MuhurthaScoreBreakdown
MuhurthaRecommendation
MuhurthaResponse
MuhurthaRegionalProfile
MuhurthaEventType
MuhurthaEventConfiguration
MuhurthaExpertOverride
```

Suggested enums:

```text
MuhurthaEventType
MuhurthaRuleGroup
MuhurthaRuleSeverity
MuhurthaRuleStatus
MuhurthaVerdict
MuhurthaRegionalProfileType
MuhurthaCandidateStatus
```

---

## 16. Rule Result Contract

Every rule should return a common result structure.

```json
{
  "ruleKey": "rahu_kalam",
  "status": "Failed",
  "severity": "StrongAvoid",
  "scoreImpact": -12,
  "hardRejected": false,
  "startTime": "2026-08-30T16:30:00+05:30",
  "endTime": "2026-08-30T18:00:00+05:30",
  "summary": "The selected beginning time overlaps Rahu Kalam.",
  "recommendation": "Begin the activity outside Rahu Kalam.",
  "metadata": {
    "source": "GenericTimeRule"
  }
}
```

---

## 17. Final Response Contract

Example:

```json
{
  "eventType": "GruhaPravesha",
  "regionalProfile": "SouthIndia",
  "location": {
    "name": "Bengaluru, Karnataka, India",
    "latitude": 12.9716,
    "longitude": 77.5946,
    "timezone": "Asia/Kolkata"
  },
  "requestedWindow": {
    "start": "2026-08-30T06:00:00+05:30",
    "end": "2026-08-30T14:00:00+05:30"
  },
  "recommendedSlots": [
    {
      "start": "2026-08-30T09:21:00+05:30",
      "end": "2026-08-30T10:18:00+05:30",
      "score": 88,
      "verdict": "Excellent",
      "hardRejected": false,
      "strengths": [
        "Favourable Nakshatra",
        "Good Tara Bala",
        "Good Chandra Bala",
        "Strong Muhurtha Lagna",
        "Benefic influence on the fourth house"
      ],
      "warnings": [
        "Average Choghadiya"
      ],
      "ruleResults": []
    }
  ]
}
```

---

## 18. Configuration Module

Do not build a large admin UI initially.

Phase 1 should use:

- Seeded database configuration or JSON configuration
- Rule Master
- Event Rule Configuration
- Regional Profile Configuration
- Scoring Configuration

Initial suggested tables:

```text
MuhurthaEventTypes
MuhurthaRules
MuhurthaEventRuleConfigs
MuhurthaRegionalProfiles
MuhurthaRegionalRuleConfigs
MuhurthaScoringConfigs
MuhurthaRuleOverrides
```

Normal users should only see simplified filters.

Examples:

- Avoid Rahu Kalam
- Avoid Bhadra
- Prefer Good Choghadiya
- Check Personal Horoscope
- Check Muhurtha Lagna

Advanced severity and scoring configuration should be restricted to administrators or expert astrologers.

---

## 19. Suggested Interfaces

```csharp
public interface IMuhurthaService
{
    Task<MuhurthaResponse> FindMuhurthaAsync(
        MuhurthaRequest request,
        CancellationToken cancellationToken = default);
}

public interface IMuhurthaCandidateGenerator
{
    Task<IReadOnlyList<MuhurthaCandidateSlot>> GenerateAsync(
        MuhurthaRuleContext context,
        CancellationToken cancellationToken = default);
}

public interface IMuhurthaRule
{
    string Key { get; }

    Task<MuhurthaRuleResult> EvaluateAsync(
        MuhurthaRuleContext context,
        MuhurthaCandidateSlot candidate,
        CancellationToken cancellationToken = default);
}

public interface IMuhurthaRuleConfigurationProvider
{
    Task<IReadOnlyList<MuhurthaRuleConfiguration>> GetRulesAsync(
        MuhurthaEventType eventType,
        string regionalProfile,
        CancellationToken cancellationToken = default);
}

public interface IMuhurthaScoringService
{
    MuhurthaScoreBreakdown Calculate(
        IReadOnlyList<MuhurthaRuleResult> ruleResults);
}

public interface IMuhurthaRecommendationService
{
    IReadOnlyList<MuhurthaRecommendation> RankAndMerge(
        IReadOnlyList<MuhurthaCandidateSlot> candidates);
}
```

Adapt names and namespaces to the existing solution conventions.

---

## 20. Implementation Phases

### Phase 1 — Architecture and Configuration

Implement:

- Domain models
- Enums
- Common rule contract
- Rule configuration provider
- Seed configuration
- Event types
- Regional profiles
- Scoring skeleton
- Unit-test foundation

Do not implement every astrology rule in this phase.

### Phase 2 — Generic Time Rules

Implement:

- Rahu Kalam
- Yamaganda
- Gulika Kalam
- Durmuhurtha
- Abhijit Muhurtha
- Choghadiya
- Varjyam where source data is available

### Phase 3 — Panchanga Rules

Implement:

- Tithi suitability
- Nakshatra suitability
- Yoga suitability
- Karana suitability
- Bhadra / Vishti
- Panchaka
- Gandanta
- Sankranti
- Eclipse and Sutak

### Phase 4 — Personal Horoscope Rules

Implement:

- Tara Bala
- Chandra Bala
- Mahadasha and Antardasha support
- Transit support
- Event-house activation

### Phase 5 — Muhurtha Chart

Implement:

- Muhurtha Lagna generation
- Lagna strength
- Moon strength
- Event-house strength
- Significator strength
- Navamsha confirmation

### Phase 6 — Ranking and API

Implement:

- Candidate scoring
- Hard-block handling
- Slot ranking
- Adjacent-slot merging
- Explanation generation
- API endpoint
- Validation
- Logging
- Caching where useful

### Phase 7 — Angular UI

Implement:

- Event selector
- Location selector
- Date range
- Optional horoscope selector
- Regional profile
- User filters
- Recommended slot cards
- Rule-strength display
- Warning display
- Rejected-slot explanation
- Expert override screen, later

---

## 21. Codex Implementation Instructions

Before making changes:

1. Inspect the existing solution structure
2. Identify reusable services
3. Identify current Panchanga capabilities
4. Identify sunrise and sunset calculations
5. Identify current ephemeris provider
6. Identify existing Dasha and transit services
7. Identify current house and divisional-chart calculators
8. Identify existing metadata and configuration conventions
9. Report missing calculations
10. Propose the exact projects, folders, files, and interfaces to add

Implementation constraints:

- Do not change existing Kundali calculation results
- Reuse existing services
- Follow existing Clean Architecture boundaries
- Keep rule evaluation deterministic
- Keep configuration separate from calculation logic
- Add unit tests for every implemented rule
- Store date-times with timezone awareness
- Use location-specific sunrise and sunset
- Avoid hard-coded display text where localization is already supported
- Do not introduce SQLite unless explicitly approved
- Do not add a large configuration UI in the first phase
- Do not implement all phases in one change
- Document assumptions and unsupported calculations

---

## 22. First Codex Task

Use the following prompt after this file is added to the repository:

```text
Read /docs/muhurtha-engine-spec.md and inspect the existing Kundali Portal solution.

Implement only Phase 1: Muhurtha architecture and configuration foundation.

Before coding, report:

1. Existing services that can be reused
2. Missing capabilities
3. Proposed projects, folders, and files
4. Proposed domain models and interfaces
5. Configuration storage approach
6. Unit-test approach
7. Any assumptions

Then implement:

- Muhurtha domain models and enums
- Common IMuhurthaRule contract
- Muhurtha rule result model
- Event type configuration
- Regional profile configuration
- Seed-based rule configuration
- Scoring configuration skeleton
- Dependency injection registration
- Unit-test project or test coverage following the current solution structure

Do not implement the actual Rahu Kalam, Panchanga, Tara Bala, Chandra Bala, Dasha, transit, Lagna, or Navamsha calculations yet.

Do not modify existing Kundali calculation behaviour.
```

---

## 23. AGENTS.md Instruction

Add the following section to the repository's root `AGENTS.md`.

```md
## Muhurtha Engine

Before modifying or implementing Muhurtha functionality, read:

- `/docs/muhurtha-engine-spec.md`

Implementation rules:

- Follow the phased plan in the specification
- Reuse existing Kundali, Panchanga, ephemeris, Dasha, transit, location, and chart services
- Do not change existing Kundali calculation behaviour
- Keep Muhurtha rule configuration separate from rule calculation
- Add tests for each implemented Muhurtha rule
- Do not implement multiple phases unless the task explicitly requests them
```

---

## 24. Initial Acceptance Criteria

Phase 1 is complete when:

- Muhurtha models compile
- Rule interfaces are registered through dependency injection
- Event types are configurable
- Regional profiles are configurable
- Rule severity and scoring are configurable
- Seed configuration loads successfully
- No existing Kundali tests fail
- New unit tests pass
- No astrology calculation is duplicated unnecessarily
- No existing Kundali API behaviour changes
