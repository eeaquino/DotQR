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
Task 5: latest-only immutable-state coordinator and deterministic timer/order/disposal tests authored; no execution.
Task 6: Blazor shell, schema-driven forms, conditional renderer/encoding controls, uploads, responsive CSS, preview/download interop, and documentation authored. Whole-change source review performed by fresh gpt-6-luna reviewer; important findings addressed below. Browser interactions remain manual.
Ruling: SVG viewBox-only mode hides scale and explicit dimensions — source always uses module coordinates and omits physical width/height — users size that responsive output in its container.
Ruling: Add coordinator Reject(error) for locally invalid renderer inputs and upload-in-progress — they must invalidate in-flight results even before a renderable typed request exists — one extra coordinator API and regression test.
Ruling: Use a separate Micro text editor state initialized to 12345 — preserves the standard plain-text payload across family switches — Micro and standard plain text are edited independently.
Ruling: Use loopback HTTP on port 5080 in Development; production enables HTTPS redirection/HSTS — removes development certificate setup for a local-only sample — do not expose the development profile publicly.
Ruling: Supersede pending edits through atomic request identity rather than cancelling every timer — the spec explicitly allows superseding; disposal cancels lifetime work — old timers live at most 250 ms, old native renders may finish but never publish.
Ruling: Normalize uploaded raster images to PNG after bounded successful decoding — SvgLogo byte overload always declares PNG — normalized files over 2 MiB are rejected even if original compressed bytes fit.
Ruling: Disable ArtQRCode for Micro QR — its standard three-finder-pattern assumptions are unsuitable — Micro artwork is unavailable, all other renderers retain matrix-based Micro support.
Ruling: Omit PDF jpgQuality — source explicitly documents it as unused — no functional configuration is lost.
Ruling: Renderers own QRCodeData; capture metadata first and clone only for the PDF fallback preview — disposing a preview renderer must not invalidate the selected renderer's input — one small matrix copy for PDF.

Task 1: implementation authored (catalogue, parsers, tests, project references); test execution intentionally omitted. Commit recorded in git.
Ruling: Catalogue follows the explicit spec list (24 concrete payload classes plus plain text), not the README/plan's count of 25 concrete generators — source has 24 — no type omitted.
Ruling: Do not expose Swiss requested payment date or ultimate creditor controls — source intentionally never serializes them in Swiss v2 — reserved constructor parameters cannot be experimented with through the UI.
Ruling: Preserve Russian legacy character sets with ToBytes()/binary QR generation — documented library contract — generic version/ECI/UTF-8 settings will be disabled for this payload.

## Final correction and expanded scope

Ruling: The user's later explicit request to build supersedes the earlier no-verification guidance — builds and test execution now required — compiler/analyzer failures were exposed and corrected rather than suppressed.
Ruling: Rename planned Integer/Decimal identifiers to WholeNumber/FractionalNumber and keep UPPER_CASE limit constants internal with test friend access — repository CA1720/CA1707/IDE1006 rules conflict with the planned public names — sample-only interfaces differ from the plan; library APIs unchanged.
Ruling: Use single-node builds with UseSharedCompilation=false — sandbox compiler-server named pipes time out — slightly slower clean compilation, no compiler/analyzer checks disabled.
Ruling: Migrate all four repository test projects to xunit.v3 4.0.2 and update BenchmarkDotNet to 0.15.8 — user explicitly expanded scope to all tests and no deprecated libraries — existing test/benchmark project files now changed beyond original sample-only scope.
Ruling: Use standalone xUnit v3 executable runners — VSTest parent-process monitoring returned Windows access denied even after approved retry — same test suites run directly without the restricted VSTest wrapper.
Ruling: Audit NuGet registration/catalog metadata with a dependency-free Node script — .NET's online deprecation query failed TLS authentication — requires Node 18+ and a prior restore; every response checked and every resolved package version recorded.
Ruling: Girocode maps UTF-8/ISO-8859-1/ISO-8859-2 to exact ECI and preserves requested QR version; remaining charsets use binary encoding with auto version — the public library has no arbitrary-charset fixed-version binary overload — fixed version is unavailable only for those legacy sets, accurately disabled in UI.

Final: addressed important ContactData no-op controls — conditional nickname/address-type visibility plus four format regression cases.
Final: addressed important Girocode charset mismatch — explicit ECI/binary policy, non-ASCII rendering comparisons, and field-specific UI locks.
Final: addressed important PDF side limit — DPI-adjusted page-size validation and boundary regression case.
Final: corrected compilation failures — AsciiQRCode spelling, repository naming/formatting rules, generated logging delegate, async test suffixes, xUnit v3 cancellation tokens; zero rule suppressions added.
Final: runtime test failures exposed missing CodePages provider initialization and the library's too-short IBAN exception — service registers CodePages before factory/encoding, and sample input validation reports an actionable IBAN error.
Final: minor (deferred): matrix dimensions display the padded library matrix, not just data modules.
Final: minor (deferred): download helper follows Microsoft's immediate object-URL revocation example; a delayed revocation could improve older-browser behavior.
Final: Ruling: Reviewer did not assess generated correctness/runtime/browser behavior under the original no-verification instruction — builds and all tests now run after the user's override; browser behavior remains manual — no browser/scanner compatibility claim.
Final: Ruling: Reviewer set aside additional Micro incompatibilities after inspecting padded matrix renderers — retain all matrix renderers and exclude Art — external scanner support remains unproven.
Final: Ruling: Reviewer found no extra actionable invalidation/upload/disposal issue — retain design, covered by deterministic and upload tests — browser/circuit-disconnection behavior still needs manual testing.

Task 1: complete; Task 2: complete; Task 3: complete; Task 4: complete; Task 5: complete; Task 6: complete. Verification: solution build + executable runs of all four xUnit v3 suites, with CI=1 so approval files cannot silently change. Core 613, ByteRenderers 55, SkiaSharp 47, Playground 68; total 783 passed, no failures/skips. Dependency audit after BenchmarkDotNet update: 58 unique resolved package versions, zero deprecated. Slopwatch not installed; no Slopwatch-clean claim. Final rebuild after dependency update succeeded: zero warnings/errors; all 783 tests passed again. Git whitespace check passed.

