# Agent Notes

Use this file as the first repo-orientation stop before searching sibling folders. The frontend and backend are siblings under the shared `GIT` folder.

## Shared Layout

- Shared repo/work area: `D:\Learning\Ai Kundali\GIT`
- Frontend/UI project: `D:\Learning\Ai Kundali\GIT\Frontend\kundali-portal-ui`
- Backend workspace: `D:\Learning\Ai Kundali\GIT\Kundali portal`
- Backend solution root: `D:\Learning\Ai Kundali\GIT\Kundali portal\KundaliEngine`
- Backend solution file: `D:\Learning\Ai Kundali\GIT\Kundali portal\KundaliEngine\KundaliEngine.sln`

From the backend workspace (`Kundali portal`), the UI path is `..\Frontend\kundali-portal-ui`.

## Frontend / UI

- Folder: `Frontend\kundali-portal-ui`
- Use this folder for Angular/UI routes, layout, pages, components, styles, and frontend services.
- Do not treat `Kundali portal\KundaliEngine\KundaliEngine` as the UI. That is only a minimal .NET class-library style project.

## Backend

- API project: `Kundali portal\KundaliEngine\KundaliEngine.API`
  - Project file: `KundaliEngine.API.csproj`
  - Entry point: `Program.cs`
  - Main folders: `Controllers\`, `DTOs\`, `Services\`, `Security\`, `Helper_functions\`
  - References `KundaliEngine.Astrology` and `KundaliEngine.Infrastructure`.

## Supporting Backend Libraries

- Domain models / contracts: `Kundali portal\KundaliEngine\KundaliEngine.Domain`
- Astrology calculations: `Kundali portal\KundaliEngine\KundaliEngine.Astrology`
- Infrastructure / persistence support: `Kundali portal\KundaliEngine\KundaliEngine.Infrastructure`
- Knowledge base logic/data support: `Kundali portal\KundaliEngine\KundaliEngine.KnowledgeBase`
- Console runner: `Kundali portal\KundaliEngine\KundaliEngine.Console`
- Minimal base project: `Kundali portal\KundaliEngine\KundaliEngine`

## Common Commands

Run backend commands from `D:\Learning\Ai Kundali\GIT\Kundali portal\KundaliEngine`:

```powershell
dotnet build KundaliEngine.sln
dotnet run --project KundaliEngine.API\KundaliEngine.API.csproj
```

Run frontend commands from `D:\Learning\Ai Kundali\GIT\Frontend\kundali-portal-ui`.

## Working Notes

- Before searching, check this map to avoid confusing sibling folders.
- For UI work, go to `D:\Learning\Ai Kundali\GIT\Frontend\kundali-portal-ui`.
- For backend API work, go to `D:\Learning\Ai Kundali\GIT\Kundali portal\KundaliEngine\KundaliEngine.API`.
- Check project references before moving code across backend projects.

## HoraLife Versioning Reference

- Formal HoraLife product versioning starts at `v0.8.0-alpha.1` as of 2026-08-07.
- Canonical versioning docs:
  - `README.md`
  - `docs\roadmap\HORALIFE_VERSIONING.md`
  - `docs\roadmap\HORALIFE_SPRINT_STATUS.md`
  - `docs\architecture\HORALIFE_ARCHITECTURE.md`
  - `docs\CHANGELOG.md`
- Original imported source bundle from `HoraLife_MD_Pack.zip` is preserved at `docs\reference\HoraLife_MD_Pack\` for future reference.
- Product version decisions belong in `HORALIFE_VERSIONING.md`; active sprint/status belongs in `HORALIFE_SPRINT_STATUS.md`; released user-visible changes belong in `CHANGELOG.md`.
