# CustomQuestion Dhruva Status

## Flow
Active Kundali -> Dhruva question -> CustomQuestion API -> PredictionEngine -> Dhruva response

## Supported questions
- `marriage_possibility`
- `marriage_timing`

## Inputs used
- D1 relationship evidence
- D9 confirmation for marriage support/pressure
- Dasha timing windows for ranked future periods

## Outcome logic
- Major foundation: 7th-house strength, 7th-lord condition, D9 confirmation
- Secondary modifiers: benefic/malefic aspects, natural significators, D9 occupants
- Result: `Supported`, `Mixed`, or `Challenging`
- Reasons/evidence are preserved for explainability

## Timing-window behavior
- Future horizon uses focus metadata for CustomQuestion timing
- Windows are ranked by activation relevance, then strength, then proximity
- Overlapping windows from the same MD/AD activation are deduplicated
- Only the best 5 windows are returned

## Main files
- `HoraLife.PredictionEngine/Service/PredictionQuestionAnalyzerService.cs`
- `HoraLife.PredictionEngine/Service/PredictionTimingAnalyzerService.cs`
- `HoraLife.PredictionEngine.Tests/CustomQuestionPredictionTests.cs`
- `src/app/shared/components/dhruva-assistant/dhruva-assistant.component.ts`
- `src/app/shared/components/dhruva-assistant/dhruva-assistant.component.html`
- `src/app/core/services/prediction-api.service.ts`

## Completed
- Marriage outcome refinement prefers dominant foundation evidence
- Dhruva renders CustomQuestion replies in the existing assistant response area
- Loading, empty timing, error, and duplicate-click guard are handled
- Marriage timing shows returned windows, strengths, and trigger planets

## Known limitations / next steps
- Dhruva still uses a single latest-question/latest-answer chat model rather than full multi-message history
- Reason/evidence labels are humanized keys, not curated natural-language explanations yet
