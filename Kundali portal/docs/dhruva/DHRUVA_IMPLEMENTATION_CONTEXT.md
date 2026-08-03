# Dhruva Implementation Context

> Status: Approved reference  
> Purpose: Defines Dhruva's technical integration context and repository rules  
> Last reviewed date: 2026-07-27  
> Intended readers: Developers, reviewers, and future Codex tasks  
> Change rule: Update only when verified repository structure or an approved Dhruva architecture decision changes

This is the technical entry point for future Dhruva work. It describes confirmed repository facts and implementation guardrails. It does not authorize application code changes by itself.

## Confirmed Frontend Architecture

- Frontend project: `Frontend/kundali-portal-ui`.
- Angular application using standalone components and routes in `src/app/app.routes.ts`.
- Main app folders include `components`, `config`, `core`, `features`, `layout`, `modules`, `services`, `shared`, and `styles`.
- Shared UI conventions are present under `src/app/shared/components`, including `brand-logo`, `cosmic-loader`, `astrology-icon.component.ts`, `loading-spinner.component.ts`, `page-header.component.ts`, and `section-card.component.ts`.
- Feature and page areas are organized under `src/app/modules` and `src/app/features`.

## Recommended Dhruva Folder Location

To be confirmed before implementation.

Current conventions suggest:

- Reusable Dhruva UI shell or small presentational pieces should live under `src/app/shared/components/dhruva` if used across pages.
- Page-specific Dhruva behavior should stay near the owning page or module.
- Data orchestration should use existing services or a new narrowly scoped service only after the data contract is confirmed.

## Localization And Theme Conventions

- Localization uses `@jsverse/transloco`.
- Translation files are in `public/i18n/en.json`, `public/i18n/hi.json`, and `public/i18n/kn.json`.
- Existing templates use `TranslocoPipe` and translation keys such as `{{ 'kundali.chart' | transloco }}`.
- Runtime themes are controlled by `ThemeService` with modes `dark`, `light`, and `hora`.
- Theme state is applied through `document.body.dataset['theme']`.
- Theme values are CSS custom properties in `src/styles.scss`, such as `--theme-bg`, `--theme-text`, `--theme-accent`, `--theme-line`, and radius variables.

## Testing Conventions

- `package.json` defines `npm run test` as `ng test`.
- `angular.json` uses `@angular/build:unit-test`.
- Test files use `.spec.ts` with Vitest-style `describe` and `it`.
- Existing examples include `src/app/app.spec.ts`, component specs, and HoraVerse service specs.

## Existing Kundali Data References

- `src/app/modules/kundali/kundali.service.ts` calls backend endpoints for generating, saving, loading, deleting Kundalis, report email, and PDF generation.
- `src/app/core/services/kundali-api.service.ts` defines `GenerateKundaliRequest` and basic Kundali API operations.
- `src/app/core/services/dasha-api.service.ts` exposes Dasha and Mahadasha API calls, including current Dasha, predictions, Mahadasha periods, and enhanced Mahadasha analysis.
- Other confirmed astrology-related API services include compatibility, panchang, muhurtha, and dasha services under `src/app/core/services`.

## Implementation Rules

- Reuse existing Angular, service, shared-component, localization, and theme patterns.
- Do not duplicate astrology calculations in the frontend.
- Do not place raw Kundali business logic inside UI components.
- Do not use AI unless a later task explicitly requests it.
- Do not refactor unrelated code while implementing Dhruva.
- Visible text must use translation keys.
- Dhruva must explain existing calculated data; HoraLife engines remain the source of facts.
- Missing data must be handled explicitly and never invented.
- Do not recommend remedies unless a future approved HoraLife engine or product contract provides that capability.

## How future Codex prompts should use these documents

Every future Dhruva prompt should:

1. Read this implementation context first.
2. Read only the additional Dhruva document relevant to the task.
3. Follow the current prompt when it is narrower than the roadmap.
4. Avoid implementing future or parked phases.
5. Stop after the requested task.

## Related Dhruva Documents

- [Product Contract](./dhruva-product-contract.md)
- [Response Guidelines](./dhruva-response-guidelines.md)
- [Capability Roadmap](./dhruva-capability-roadmap.md)
