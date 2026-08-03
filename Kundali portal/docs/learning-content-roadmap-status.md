# Learning Content Roadmap Status

> Status: Active working reference
> Last updated: 2026-08-03
> Scope: Backend KnowledgeBase learning content plus frontend learning asset/UI wiring
> Source folders:
> - Backend: `KundaliEngine/KundaliEngine.KnowledgeBase/LearningContent`
> - Frontend assets: `../Frontend/kundali-portal-ui/src/assets/learning`

## Current Course Structure

| Order | Level code | Status | Sessions | UI availability | Notes |
| --- | --- | --- | ---: | --- | --- |
| 1 | `level-1` | `content-in-progress` | 15 | Active | Main Level 1 course. Session IDs in `level-1.json` are expected to match each individual session JSON `id`. |
| 2 | `bonus-bridge` | `content-in-progress` | 1 | Active | Bridge session between Level 1 and Level 2, added from `horalife-bonus-bridge-complete-session.zip`. |
| 3 | `level-2` | `planned` | 0 | Disabled / future | Placeholder manifest only. Actual Level 2 content files are not present yet. |

## Completed Learning Content Work

- Verified Level 1 manifest/session identity contract: each `level-1.json` session `id` must match the corresponding session JSON `id` returned by the backend.
- Fixed Level 1 prerequisite session IDs so prerequisites point to real session IDs in the same manifest.
- Updated Level 1 sessions 9-15 from planned/unavailable state to active preview/content-in-progress so the UI can open them.
- Updated backend session folder resolution to support both `Session09` and `session-09` folder naming patterns.
- Moved backend session diagrams for Level 1 sessions 9-15 into the frontend learning assets structure.
- Normalized learning asset paths in session JSON to `assets/learning/...`.
- Added active Bonus Bridge level manifest and session JSON files in English, Hindi, and Kannada.
- Added Bonus Bridge diagrams to frontend assets under `assets/learning/bonus-bridge`.
- Added planned Level 2 placeholder manifest so UI/course order is ready before Level 2 content exists.
- Updated frontend learning home to show available levels in order: Level 1, Bonus Bridge, Level 2.
- Updated frontend session loading so it no longer assumes all sessions belong to `level-1`.

## Backend Contract Notes

- Level manifest files live in `KundaliEngine/KundaliEngine.KnowledgeBase/LearningContent/Levels`.
- Session JSON files live under `KundaliEngine/KundaliEngine.KnowledgeBase/LearningContent/Sessions`.
- `LearningContentService` maps manifest session `id` to `LearningSessionSummary.Id`.
- `GET /api/learning/sessions/{sessionGuid}` locates the session through manifests, loads the session JSON, and validates the session JSON `id` matches the requested manifest/session ID.
- Prerequisite IDs should always reference real session IDs from already available earlier course steps.

## Asset Rules

- Session diagram paths in JSON should use frontend public asset paths beginning with `assets/learning/`.
- Level 1 session assets should follow `assets/learning/Level-1/session-XX/...`.
- Bonus Bridge assets should follow `assets/learning/bonus-bridge/...`.
- Frontend files must exist under `../Frontend/kundali-portal-ui/src/assets/learning` with the same structure referenced by JSON.

## Remaining / Planned

- Level 2 content files still need to be created or supplied before Level 2 can become active.
- Completion-based unlock from Bonus Bridge to Level 2 is planned conceptually, but no user-progress gate has been implemented yet.
- Level 1 sessions 2-8 still reference some diagram assets that were not found in backend or frontend during the previous asset pass.
- Purple dark-theme alignment is parked for later and is not part of this learning-content status.

## Verification Snapshot

- Backend API temp-output build passed after the learning changes.
- Frontend Angular build passed after the learning changes.
- Existing warnings remained: nullable warnings in backend and Angular/Sass warnings in frontend.
