# HoraLife Vastu Consultant - Lean Handover Pack

## Start here
1. Read `HoraLife_Vastu_Consultant_Lean_Handover_v0.1.docx`.
2. Replace the placeholder sample assets with the existing HoraLife Vastu floor plan, metadata/manifest and layered-analysis reference image.
3. Build the first vertical slice only: upload -> confirm north/rooms -> run a small approved rule set -> show evidence layers -> generate report.

## Frozen boundaries
- Separate frontend, backend, repository, deployment and release lifecycle.
- Dedicated PostgreSQL schema: `vastu`.
- Same PostgreSQL infrastructure may be used initially.
- Standalone product first; HoraLife integration later through APIs.
- Do not start with AI consultation, payments, mobile app, full 3D modelling, or thousands of rules.

## Files
- `manifest-example.json`: starter structure reflecting the frozen metadata approach.
- `rule-result-contract-example.json`: rule output/evidence contract; it is not a production Vastu rule.
- `layered-analysis-reference.png`: conceptual UI reference.
- `Resource_Handover_Checklist.md`: items HoraLife must provide before development starts.
