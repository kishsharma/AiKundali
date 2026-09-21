# Current Life Outlook — why it feels generic, and how to make it chart-specific

**Status:** Phase A implemented in the Kundali UI (2026-09-21). Engine claim keys unchanged. Phase B (DTO tokens) and Phase C (Gochar in Prediction Engine) are still future work.

**Scope:** the premium Kundali-page block titled **Your Current Life Outlook** (`#predictionSection`). This is not Current Life Focus (Gochar scoring further down the same page).

**Date:** 2026-09-21

---

## 1. What the user sees

On a generated Kundali, premium users get a section whose English title is `kundali.predictions.title` → **Your Current Life Outlook**.

Layout today:

1. Seven life-area tabs: Career, Finance, Relationships, Education, Family, Property, Health.
2. Each tab shows a **tone chip** only: Favorable / Needs Attention / Mixed / Neutral.
3. The open area lists canned bullets under Support, Needs Attention, Mixed Factors.
4. A Dasha strip shows Mahadasha / Antardasha / Pratyantardasha with planet name and tone, **without a sentence**.
5. “Why this result?” is the only place that already names houses and planets (for example `10th Lord Mercury -> 11th House`).

Free users see a lock teaser, not this content.

---

## 2. How it is produced

Premium path in `KundaliController.Generate`:

```text
Chart + Dasha + houses
  → PredictionContextMapper
  → PredictionEngineService.Generate (TimeScope = Current, Mode = Report)
      × 7 life areas
  → Predictions[] on the Kundali response
```

Engine contract (frozen in `docs/roadmap/PREDICTION_ENGINE_CHECKPOINT_2026-08-07.md`):

```text
facts + metadata → PredictionClaim[] → consolidation → synthesis
OpenAI may later reword; it must not invent facts, tone, or timing
```

Each `PredictionClaim` is a **key + tone + stage + evidenceKeys + ruleIds**. There is **no sentence field**. The UI maps `claim.Key` to Transloco:

```text
kundali.predictions.claim.{key}
```

If the key is missing from i18n, the UI humanizes the key (`career_current_outlook_mixed` → “Career current outlook mixed”).

---

## 3. Why it reads as generic

The engine **does** compute chart-specific evidence. The **headline copy does not use it**.

### 3.1 Claim keys collapse many charts into the same sentence

Career Dasha, for example, only emits three keys:

| Key | English copy |
| --- | --- |
| `career_dasha_support` | Current Dasha supports career movement. |
| `career_dasha_pressure` | Current Dasha asks for career caution. |
| `career_dasha_mixed` | Current Dasha gives mixed career factors. |

The generator already has `period.Planet`, period dates, and aspect evidence (`planet:Saturn`, `dasha:…`). Those never enter the string.

Natal Career is only slightly richer (`career_foundation_*`, `career_10th_lord_*`, dignity keys) and still never names the planet, sign, or house.

Finance / Relationships / Education / Family / Property / Health natal generators are the same house-aspect + house-lord template with different prefixes. Most of those natal keys **have no i18n entries at all**, so the UI shows humanized keys such as “Finance house 2 support” instead of a real sentence.

### 3.2 Consolidated outlook keys are untranslated

Consolidator emits `{area}_current_outlook_{support|pressure|mixed}`. There are **no** matching `kundali.predictions.claim.*` strings. Users often see “Career current outlook mixed” as a bullet — machine-readable, not human.

### 3.3 The UI shows keys, not evidence

`getPredictionClaimLabel()` translates the key only. It ignores:

- `dashaPlanet`, `dashaLevel`, `startDate`, `endDate`
- `evidenceKeys` (`house:10`, `planet:Mercury`, `house-lord:10:Mercury:house:11`)
- `ruleIds`
- synthesis `houseConnections` in the Support / Challenge lists
- person name, Lagna, current MD/AD (available on the same Kundali payload)

House connections **are** chart-specific, but they sit under “Why this result?”, so the first impression is still the canned bullets.

### 3.4 Lists are truncated and duplicated

Support / challenge / mixed each take the first **3** claims by tone, mixing natal + dasha + consolidated. The same tone can appear three times with near-identical wording (“foundation supported”, “career lord support”, “dasha support”).

Dasha influence is a separate panel with planet names, but **no claim sentence**, so the personalized timing and the generic bullets never join.

### 3.5 Timing stack is natal + Dasha only

Prediction Engine currently has **no Gochar/transit claims**. Yogas and Doshas on the Kundali response are not fed into Outlook. Current Life Focus *does* score Gochar vs natal houses, but it is a different product surface and the architecture checkpoint says Prediction Engine must not depend on it.

Two “current life” stories on one page, with different models, increases the sense that Outlook is a canned overlay rather than *this* chart.

### 3.6 Breadth over depth

Seven areas are generated on every premium generate, each with the same Support / Pressure / Mixed skeleton. Nothing ranks “this area is actually live for you now.” Current Life Focus already ranks activation; Outlook does not reuse that ranking.

---

## 4. What is already personal (unused in copy)

These fields are already on the API / page and should drive wording **without inventing astrology**:

| Source | Example | Use in copy |
| --- | --- | --- |
| Claim `evidenceKeys` | `planet:Mercury`, `house:10`, `house-lord:10:Mercury:house:11` | Name planet and houses |
| Claim dasha fields | Saturn MD, Mercury AD, date window | Name the running period |
| Synthesis `houseConnections` | 10th lord in 11th, significations Career → Gains | One “why” sentence in the headline |
| Synthesis `overallTone` + effective window | Mixed, Apr 2024–Jan 2027 | Opening line |
| Kundali header | Name, Lagna, Moon, current Dasha | Address the native, not “a career chart” |
| Yogas / Doshas (later) | Named yoga currently active | Only if rule-backed for that area |

**Do not** use OpenAI to invent extra meaning. Optional LLM later: rephrase a filled template; never add lords, dates, or tones that are not in the payload.

---

## 5. Product contract for “real” Outlook

Each open life area should read as **one short briefing**, then proof.

### 5.1 Headline (required)

2–4 sentences, deterministic templates with slots:

1. **Who / when:** `{Name}` — `{MD planet}` Mahadasha / `{AD planet}` Antardasha, `{from}`–`{to}`.
2. **Area verdict:** Career looks `{tone}` in this window because `{top natal fact}`.
3. **What to watch:** `{top pressure or mixed fact}`, or omit if none.
4. **House link (if present):** `{Nth} lord {planet} in the {Mth} house ties {source theme} to {target theme}`.

Example **today** (generic):

> Current Dasha supports career movement.  
> The career lord gives visible support.

Example **after** (same engine facts):

> Kishore, Saturn Mahadasha / Mercury Antardasha (12 Apr 2024 – 3 Jan 2027) is the active career window.  
> Outlook is mixed: Mercury, 10th lord, in the 11th house of gains supports visibility, while Saturn’s dasha keeps results slow.  
> Work-house pressure needs patience; public status is the theme to protect.

Same keys and tones; different interpolation.

### 5.2 Evidence list (keep, rewrite)

Replace key-only bullets with **one line per distinct rule**, interpolating planet/house/dasha. Cap at 3 support + 3 caution. Deduplicate by `ruleIds` / evidence cluster so foundation + 10th-lord + dignity do not triple-say “career is supported.”

### 5.3 Dasha strip

Keep MD/AD/PD rows, but each row’s subtitle should be the interpolated dasha claim, not a second generic list.

### 5.4 “Why this result?”

Keep house connections. Promote the strongest connection into the headline so the user does not have to open the footnote to feel it is *their* chart.

### 5.5 What not to promise in v1

- Specific job titles, salary, marriage dates, diagnoses.
- Transit sentences until Gochar facts exist in Prediction Engine.
- Cross-wiring Current Life Focus scores into Prediction Engine (architecture freeze). Optional later: *display* Focus ranking as “areas most active now” **above** Outlook without merging engines.

---

## 6. Proposed implementation (after approval)

### Phase A — copy and UI only (fastest honesty win)

**Backend:** none, or a small DTO of parsed tokens if we do not want the UI to regex `evidenceKeys`.

**Frontend (`kundali.component.ts` + i18n):**

- Add Transloco templates with params, e.g.  
  `kundali.predictions.claim.career_dasha_support_named`:  
  `{{level}} of {{planet}} ({{period}}) supports career movement.`
- Parse `evidenceKeys` / existing claim fields into `{planet, house, lord, targetHouse, level, period}`.
- Add missing keys for natal finance/health/etc. and for `{area}_current_outlook_*`.
- Build a `headlineParagraphs[]` from synthesis + top natal + top dasha + first house connection.
- Deduplicate claims before `slice(0, 3)`.
- Show native name in the section intro.

**Languages:** `en.json` and `hi.json` in lockstep.

### Phase B — structured narrative on the claim (cleaner)

Extend `PredictionClaim` / `PredictionClaimResponseDto` with optional:

```text
DisplayTokens: { Planet, Houses[], DashaLevel, DashaPlanet, Sign? }
```

or a single `SummaryKey` + `SummaryParams` dictionary filled in C# so Angular does not parse evidence strings. Keep `Key` stable for rules/tests.

Career already has extra natal rules; lift the same **named** 10th-lord / dignity pattern to other areas instead of copy-paste “house N support.”

### Phase C — more “current” (only after A/B)

- Add Gochar facts inside Prediction Engine (not via Current Life Focus).
- Optionally surface named Yogas/Doshas as extra evidence lines when metadata maps them to a life area.
- Rank which of the seven tabs is “live now” using Dasha lords vs primary houses (engine-owned), so Career is not always first visually when Health is the live story.

---

## 7. Files that would change (later)

| Layer | Path |
| --- | --- |
| UI section | `Frontend/kundali-portal-ui/src/app/modules/kundali/kundali.component.html` (`#predictionSection`) |
| Label logic | `…/kundali.component.ts` (`getPredictionClaimLabel`, `premiumPredictionAreaCards`) |
| Copy | `Frontend/kundali-portal-ui/public/i18n/en.json`, `hi.json` |
| Claim model | `HoraLife.PredictionEngine/Models/PredictionClaim.cs` (Phase B) |
| Generators | `HoraLife.PredictionEngine/Service/{Area}/*ClaimGenerator*.cs` |
| Consolidator | `PredictionClaimConsolidatorService.cs` |
| API map | `KundaliController.MapPredictionResponse` / `PredictionResponseMapper` |
| Related, do not merge | `current-life-focus` component + `docs/GocharAnalysis_LifeArea.md` |

---

## 8. Success criteria

A user comparing two different charts should **not** see the same Outlook sentences.

Checks:

- Headline names at least one planet **and** one house **or** a named Dasha lord from *this* payload.
- Dasha bullets name MD/AD planet and dates.
- Missing i18n no longer leaks `career_current_outlook_mixed` style humanized keys.
- Hindi templates fill the same slots.
- Tone, dates, and lords still match engine claims (no LLM-invented facts).
- Current Life Focus section behavior unchanged.

---

## 9. Recommendation

Do **Phase A** first: interpolate existing evidence into templates and add a 2–4 sentence briefing. That attacks the actual UX bug (generic i18n) without waiting for Gochar-in-engine or OpenAI.

Do **not** replace Outlook with Current Life Focus. Focus is transit activation ranking; Outlook is life-area claim synthesis. They should eventually *agree* on timing language, but they are different jobs.

---

## 10. Approval gate

No code until Kishore confirms:

1. Phase A (UI templates + headline) vs jump to Phase B (DTO tokens).
2. Whether the briefing may use the native’s name from the form.
3. Whether seven equal tabs stay, or we visually lead with the most Dasha-linked area in v1.

After approval, implement only the chosen phase.
