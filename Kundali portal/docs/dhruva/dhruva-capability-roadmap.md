# Dhruva Capability Roadmap

> Status: Approved reference  
> Purpose: Defines Dhruva phases, dependencies, statuses, and exclusions  
> Last reviewed date: 2026-07-30  
> Intended readers: Product owners, developers, reviewers, and future Codex tasks  
> Change rule: Update only when an approved Dhruva roadmap decision changes

This roadmap defines the approved implementation sequence. Parked capabilities must not be implemented unless a later task explicitly activates them.

## 1. UI Shell

- Purpose: Establish Dhruva's visible container, entry point, and basic presentational states.
- Dependencies: Confirmed design placement, shared-component location, translation keys, and theme token usage.
- Permitted responsibilities: Display static shell states, open/close behavior, non-data introductory copy.
- Explicit exclusions: Intent matching, AI, astrology interpretation, backend changes.
- Status: Approved for planned implementation.

## 2. General Portal Guide

- Purpose: Help users understand where they are in the portal and navigate supported areas.
- Dependencies: Route/page context and approved copy keys.
- Permitted responsibilities: Explain page purpose, point to existing routes, describe visible sections.
- Explicit exclusions: Chart interpretation, predictions, remedies, product promotion.
- Status: Approved for planned implementation.

## 3. Kundali Data Normalization

- Purpose: Create a stable frontend reading layer over existing Kundali/report data.
- Dependencies: Confirmed Kundali response shape from existing services and backend results.
- Permitted responsibilities: Normalize already calculated values for Dhruva consumption.
- Explicit exclusions: New calculations, changing API contracts, raw astrology rules in UI components.
- Status: Approved for planned implementation.

## 4. Static Chart Assistant MVP

- Purpose: Answer deterministic chart questions from existing calculated data.
- Dependencies: Normalized Kundali data and page/report context.
- Permitted responsibilities: Explain available values for Ascendant, Moon sign, Sun sign, birth Nakshatra, current Mahadasha, current Antardasha, existing strength results, existing Yoga results, existing Dosha results, and upcoming Dasha periods when already available.
- Explicit exclusions: Open-ended predictions, remedies, product recommendations, AI-generated conclusions, Muhurtha interpretation beyond existing data.
- Status: Approved for planned implementation.

## 5. Contextual Ask Dhruva

- Purpose: Let users ask supported questions within the current page/report context.
- Dependencies: Static assistant MVP, supported-intent list, evidence mapping, and fallback responses.
- Permitted responsibilities: Match approved deterministic questions to available data and show evidence.
- Explicit exclusions: Open-ended free-form prediction, AI, unsupported intents, invented data.
- Status: Approved for planned implementation.

## 6. Multilingual Support

- Purpose: Provide consistent Dhruva responses in English, Hindi, and Kannada.
- Dependencies: Transloco keys, terminology decisions, and reviewed translations.
- Permitted responsibilities: Localized static and deterministic responses using existing localization conventions.
- Explicit exclusions: Runtime machine translation, AI translation, unreviewed fear-based language.
- Status: Approved for planned implementation.

## 7. Learning Companion

- Purpose: Help users study Jyotish content inside learning flows.
- Dependencies: Confirmed learning content contract and approved learning interaction model.
- Permitted responsibilities: Explain visible lesson concepts and guide study flow.
- Explicit exclusions: Course generation, grading changes, new learning backend features.
- Status: Parked for future implementation.

## 8. Evidence-Backed Predictive Interpreter

- Purpose: Explain predictive indications using available evidence when this product scope is activated.
- Dependencies: Approved predictive data contract, evidence model, review rules, and safety language.
- Permitted responsibilities: To be confirmed.
- Explicit exclusions: Guaranteed events, fear-based claims, unsupported predictions, AI-generated conclusions unless explicitly activated.
- Status: Parked for future implementation.

## 9. Muhurtha Guide

- Purpose: Explain Muhurtha results in user-friendly language.
- Dependencies: Confirmed Muhurtha response shape and approved interpretation boundaries.
- Permitted responsibilities: To be confirmed.
- Explicit exclusions: Calculating Muhurtha in the UI, inventing auspicious times, overriding engine results.
- Status: Parked for future implementation.

## 10. HoraStore And Remedy Guidance

- Purpose: Offer restrained, optional guidance around remedies or store items only if a future approved HoraLife engine or product contract supports it.
- Dependencies: Approved remedy policy, product rules, store integration contract, and compliance review.
- Permitted responsibilities: To be confirmed.
- Explicit exclusions: Aggressive promotion, fear-based selling, guaranteed remedy claims, unsupported product recommendations.
- Status: Parked for future implementation.

## 11. Restricted AI Enhancement

- Purpose: Add AI assistance only within explicit product, safety, and evidence constraints.
- Dependencies: Later explicit activation, model policy, prompt contract, telemetry, evidence grounding, and fallback rules.
- Permitted responsibilities: To be confirmed.
- Explicit exclusions: Any AI use before explicit approval, unguided open-ended predictions, invented data.
- Status: Parked for future implementation.
