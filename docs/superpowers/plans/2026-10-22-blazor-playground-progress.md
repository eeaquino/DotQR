# SDD ledger — plan: docs/superpowers/plans/2026-10-22-blazor-playground.md

Base: 2046ad5. Branch: feature/blazor-playground. Native execution.

Ruling: Work in the current checkout on a new feature branch — user explicitly selected this instead of a worktree — implementation is visible here, not isolated from other local edits.
Ruling: Author tests but do not run builds, tests, Slopwatch, or generated-file verification — workspace guidance and approved plan explicitly prohibit verification — compile/runtime defects may remain unobserved.
Ruling: Use this persisted progress document and native PowerShell/git commands instead of Bash workspace scripts — Windows harness and no need for temporary review packages — extra progress document is the only additional documentation outside the sample/test folders.

Pre-flight interfaces:
- Tasks 1 → 3/6: PayloadDefinition/PayloadValues consumed by service/editor; compatible.
- Tasks 2 → 3/4/5/6: immutable request/results/options consumed by rendering/coordinator/UI; compatible.
- Tasks 3 → 4: internal RenderedOutput shared inside sample assembly; compatible.
- Tasks 3/4 → 5: synchronous Generate adapted to cancellation-aware Task delegate; cancellation suppresses publication, not interrupted native rendering.
- Tasks 5 → 6: Changed dispatched through InvokeAsync; coordinator is component-owned, not singleton.

Task 2: implementation authored (option snapshots, capability predicates, resource limits, boundary tests); no build/test verification run.
Task 3: encoding and all six byte renderers authored with format/metadata/error tests; no execution. Skia dispatch is implemented in Task 4.
Task 4: Skia renderers, bounded codec-first upload validation, canonical PNG conversion, and tests authored; no execution.
Ruling: Normalize uploaded raster images to PNG after bounded successful decoding — SvgLogo byte overload always declares PNG — normalized files over 2 MiB are rejected even if original compressed bytes fit.
Ruling: Disable ArtQRCode for Micro QR — its standard three-finder-pattern assumptions are unsuitable — Micro artwork is unavailable, all other renderers retain matrix-based Micro support.
Ruling: Omit PDF jpgQuality — source explicitly documents it as unused — no functional configuration is lost.
Ruling: Renderers own QRCodeData; capture metadata first and clone only for the PDF fallback preview — disposing a preview renderer must not invalidate the selected renderer's input — one small matrix copy for PDF.

Task 1: implementation authored (catalogue, parsers, tests, project references); test execution intentionally omitted. Commit recorded in git.
Ruling: Catalogue follows the explicit spec list (24 concrete payload classes plus plain text), not the README/plan's count of 25 concrete generators — source has 24 — no type omitted.
Ruling: Do not expose Swiss requested payment date or ultimate creditor controls — source intentionally never serializes them in Swiss v2 — reserved constructor parameters cannot be experimented with through the UI.
Ruling: Preserve Russian legacy character sets with ToBytes()/binary QR generation — documented library contract — generic version/ECI/UTF-8 settings will be disabled for this payload.

