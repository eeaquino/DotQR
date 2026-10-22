# Blazor Server QR Playground Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development (recommended) or executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an interactive sample covering all DotQR payload generators and renderers using local project references.

**Architecture:** A .NET 10 Interactive Server app hosts a schema-driven payload editor and explicit typed payload factories. A rendering service owns disposable library resources and returns immutable output; a circuit-scoped coordinator debounces updates and prevents stale results. Razor components handle editing, previews, and blob downloads without persistence or separate API infrastructure.

**Tech Stack:** ASP.NET Core Blazor Interactive Server, C#, DotQR.Core, DotQR.ByteRenderers, DotQR.SkiaSharp, ordinary HTML/CSS, a small JavaScript download module, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-22-blazor-playground-design.md`

## Global Constraints

- Create a runnable .NET 10 Blazor Interactive Server sample.
- The application references the three local DotQR projects directly, never their NuGet packages.
- Use explicit catalogue definitions and typed payload factories rather than runtime reflection.
- Keep library APIs unchanged.
- No authentication, persistence, external QR service, or third-party UI framework.
- Debounce text edits by approximately 250 ms; discrete options update immediately.
- Never show a control whose value is ignored.
- Disable downloads after invalid edits so stale output cannot be mistaken for the current settings.
- Do not render uploaded raw SVG or arbitrary user HTML inline.
- Do not claim scanner validation.
- Workspace guidance says not to verify generated files: author tests and document commands, but do not execute build/test/UI verification unless the user changes that instruction. Report checks as unexecuted, never passing. This overrides the usual run-red/run-green steps below.

## Review Focus

- Optional blank values must become null where appropriate, not accidental zeroes or current dates (Task 1).
- Payment-specific ECC/version/ECI must override generic settings visibly, rather than changing the payment format (Task 3).
- Micro QR renderer compatibility must be explicit; incompatible trimming/rendering must fail clearly rather than corrupting output (Task 3).
- Compressed uploads with enormous decoded dimensions must be rejected before decoding a full bitmap (Task 4).
- Invalid edits while an older generation is pending must invalidate both download and preview immediately (Task 5).

## File Structure

All new application code lives under `samples/DotQR.Playground`, namespace `DotQR.Playground`.

- `DotQR.Playground.csproj`, `Program.cs`, `Properties/launchSettings.json`: hosting and three project references.
- `Payloads/PayloadField.cs`, `PayloadValues.cs`, `PayloadDefinition.cs`, `PayloadCatalog.cs`: schema, invariant typed parsing, defaults, factory registry.
- `Payloads/CommunicationPayloads.cs`, `ContactPayloads.cs`, `CryptoPayloads.cs`, `PaymentPayloads.cs`, `SwissPaymentPayload.cs`, `RussianPaymentPayload.cs`, `NetworkPayloads.cs`: explicit field definitions and typed constructors, partitioned by purpose.
- `Generation/EncodingOptions.cs`, `RendererOptions.cs`, `GenerationRequest.cs`, `GenerationResult.cs`, `GenerationLimits.cs`: request snapshots, limits, result and error contracts.
- `Generation/QrGenerationService.cs`, `ByteRendererService.cs`, `SkiaRendererService.cs`, `UploadImageReader.cs`, `LiveGenerationCoordinator.cs`: QR data ownership, rendering, upload validation, debouncing.
- `Components/App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/MainLayout.razor`: Blazor shell.
- `Components/Pages/Playground.razor` and `.razor.css`: orchestration and responsive workspace.
- `Components/Editors/PayloadEditor.razor`, `PayloadFieldEditor.razor`, `EncodingEditor.razor`, `RendererEditor.razor`, `ImageUpload.razor`: focused editors.
- `Components/Preview/QrPreview.razor`: output display and downloads.
- `wwwroot/app.css`, `wwwroot/download.js`: shared styling and bounded stream-to-blob download helper.
- `README.md`: run instructions and limitations.
- `tests/DotQR.Playground.Tests`: test project, with files named after the units tested.

Modify only root `DotQR.slnx` and `README.md` outside these new folders.

## Task 1: Typed payload catalogue and editor values

**Files:** Create sample/test projects and all `Payloads` files listed above; modify `DotQR.slnx`.

**Interfaces:**
- `PayloadField(string Key, string Label, FieldKind Kind, string? Example, bool Required = false, IReadOnlyList<string>? Choices = null, string? Help = null, Func<PayloadValues, bool>? VisibleWhen = null)`; kinds Text, Multiline, Password, Integer, Decimal, Boolean, Date, DateTime, Select.
- `PayloadValues`: mutable per-editor string values with `Text(string)`, `OptionalText(string)`, `Integer(string)`, `OptionalInteger(string)`, `Decimal(string)`, `OptionalDecimal(string)`, `Number(string)`, `OptionalNumber(string)`, `Boolean(string)`, `Date(string)`, `OptionalDate(string)`, `Enum<T>(string)`, `Clone()`; use invariant culture and field-named validation errors.
- `PayloadDefinition(string Id, string Label, string Group, IReadOnlyList<PayloadField> Fields, Func<PayloadValues, PayloadGenerator.Payload?> Create)`; null payload is allowed only for the `text` entry, whose `text` field is the raw input.
- `PayloadCatalog.All`: `IReadOnlyList<PayloadDefinition>`; `CreateExample(string id)` returns a new `PayloadValues` populated from field examples.

- [ ] Create the Web SDK sample targeting net10.0, referencing all three source projects, and an xUnit test project referencing the sample. Use existing repository test dependency versions and CLI package commands. Add both projects to solution folders with `dotnet sln`.
- [ ] Author `PayloadCatalogTests`: assert exactly the 25 concrete payload class names listed in the spec plus `text`; every default typed factory returns a nonempty payload; every schema key is unique within its definition; resetting returns independent values.
- [ ] Author `PayloadValuesTests`: optional blank returns null; invalid numeric input names its field; decimal parsing is invariant under `fr-FR`; false remains false; absent optional date is null.
- [ ] Implement the schema and explicit factories using library source as the authority. Include all public optional fields and non-obsolete variants, not merely required constructor arguments. For Swiss, Russian, and BezahlCode variants, use conditional sections and valid library test fixtures as example data. Cover contact formats, OTP HOTP/TOTP, crypto variants, payment nested values, and Shadowsocks parameters.
- [ ] Author variant tests asserting changed fields alter the expected serialized payload, and invalid payment inputs surface the library's message. Do not synthesize expected payloads using the same factory under test.
- [ ] Record optional future check: `dotnet test tests/DotQR.Playground.Tests --filter "FullyQualifiedName~Payload"` should pass. Do not run under current guidance.
- [ ] Commit task changes with message `feat: add playground payload catalogue`.

## Task 2: Encoding, rendering contracts, and output limits

**Files:** Create generation models and `GenerationLimits.cs`.

**Interfaces:**
- `EncodingOptions`: record with `bool IsMicro`, `int Version` (-1 standard auto; -1 through -4 Micro), `QRCodeGenerator.ECCLevel EccLevel`, `bool ForceUtf8`, `bool Utf8Bom`, `QRCodeGenerator.EciMode EciMode`.
- `RendererKind`: Png, Bitmap, Pdf, Svg, Postscript, Ascii, Skia, Art, Base64.
- `RendererOptions`: immutable record containing `RendererKind Kind`, scale/colors/alpha, quiet zones, explicit width/height sizing, DPI, EPS, SVG sizing/logo mode, ASCII compact/strings/repeat/inversion/line-ending options, Base64 image type, logo size/border/background, art factor/styles, and immutable upload byte snapshots for logo/background/finder images. Name members after their corresponding library parameters.
- `GenerationRequest(PayloadDefinition Definition, PayloadValues Values, EncodingOptions Encoding, RendererOptions Renderer)`; clone Values and uploads at submission.
- `GenerationResult(byte[] Content, string FileName, string MimeType, string? PreviewDataUri, string? TextPreview, string PayloadText, int Version, int ModuleCount, string EffectiveEncoding, string? PreviewNotice)`.
- `GenerationOutcome(GenerationResult? Result, string? Error)`; success and failure are exclusive.
- `GenerationLimits`: const maximum text characters 8192, pixels-per-module 32, output side 2048, uploaded bytes 2 MiB, uploaded side 2048, output bytes 16 MiB. Reject excess with actionable messages; validate dimensions before allocating.

- [ ] Author `GenerationLimitsTests` asserting exact boundaries, negative/zero sizes, NaN/infinity art factor, and output size overflow are rejected; valid limits pass.
- [ ] Implement immutable models, enums, and validation. Require art factor in (0,1] and at least one actual pixel per module after scaling. Validate ASCII repetition/string lengths and bounded SVG/PostScript dimensions, not just raster scale.
- [ ] Record optional check: `dotnet test tests/DotQR.Playground.Tests --filter FullyQualifiedName~GenerationLimits` should pass; do not execute.
- [ ] Commit as `feat: define playground generation options and limits`.

## Task 3: QR encoding and dependency-free renderers

**Files:** Create `QrGenerationService.cs`, `ByteRendererService.cs`; test `QrGenerationServiceTests.cs`, `ByteRendererServiceTests.cs`.

**Interfaces:**
- `QrGenerationService.Generate(GenerationRequest request) -> GenerationOutcome` owns generator/data and delegates rendering.
- `ByteRendererService.Render(QRCodeData data, RendererOptions options) -> RenderedOutput`; define internal `RenderedOutput(byte[] Content, string FileName, string MimeType, string? PreviewDataUri, string? TextPreview, string? PreviewNotice)` in this file, shared with Skia rendering.

- [ ] Author encoding tests for standard auto/fixed versions, raw UTF-8/ECI, valid Micro M1–M4 examples, incompatible Micro ECC/capacity, and raw-text-only Micro. Add Slovenian/Swiss mandated metadata tests against the library's payload-aware generation result.
- [ ] Implement standard generation using payload-aware overloads when required metadata exists, otherwise string overloads with requested settings. Inspect metadata overrides and report effective settings explicitly. Micro uses `GenerateMicroQrCode` and cannot silently fall through to standard QR.
- [ ] Author renderer tests for PNG/BMP/PDF signatures, SVG content and sizing mode, PostScript/EPS headers, exact ASCII regular/compact behavior, alpha/quiet-zone option effects, and proper MIME/extension metadata. Test selected renderer output separately from a labeled fallback preview.
- [ ] Implement all six dependency-free renderers by public overloads. Inspect Micro quiet-zone matrix handling in each renderer; reject incompatible combinations with a clear explanation and disable those combinations in UI. Use encoded image sources for SVG and escaped text for ASCII/PostScript. PDF can use a clearly labeled PNG matrix fallback with actual PDF download.
- [ ] Inspect PDF implementation before exposing jpgQuality; omit it if unused. Produce preview and actual download from the same immutable request. Expected input exceptions become field/settings errors; unexpected failures log no content and return a generic error.
- [ ] Record optional check: `dotnet test tests/DotQR.Playground.Tests --filter "FullyQualifiedName~QrGenerationService|FullyQualifiedName~ByteRenderer"`; do not execute.
- [ ] Commit as `feat: generate QR previews and byte renderer downloads`.

## Task 4: Skia renderers and safe uploads

**Files:** Create `SkiaRendererService.cs`, `UploadImageReader.cs`; extend service dispatch; add corresponding tests.

**Interfaces:**
- `SkiaRendererService.Render(QRCodeData data, RendererOptions options) -> RenderedOutput`.
- `UploadImageReader.ReadAsync(Stream stream, long declaredLength, CancellationToken cancellationToken) -> Task<byte[]>`; accept PNG/JPEG/WebP only, verify actual format and dimensions through SKCodec before full decoding, return original bounded bytes.

- [ ] Author tests for PNG Skia output, Base64 PNG/JPEG/WebP MIME signatures and encoded text, and Art factor/style settings. Add logo, background, and finder-pattern fixture tests; ensure options have observable effects.
- [ ] Author upload tests for unsupported SVG, malformed data, size-limit overrun even with false declared length, header dimensions beyond limit, and truncated raster data. Bound decoded pixel count and image sides before allocating full bitmaps.
- [ ] Implement upload validation and three Skia renderers with deterministic disposal of codecs, bitmaps, SKImage, SKData, and renderers. Raster SVG logos use the library's safe byte-based logo support. Base64 download is the library's encoded text as `.txt`, while its selected image format controls the preview; label that distinction.
- [ ] Wire service dispatch and actual content-size limits. Do not retain SKBitmap or matrix resources in result objects.
- [ ] Record optional check: `dotnet test tests/DotQR.Playground.Tests --filter "FullyQualifiedName~SkiaRenderer|FullyQualifiedName~UploadImage"`; do not execute.
- [ ] Commit as `feat: support Skia artwork and safe image uploads`.

## Task 5: Latest-only live generation coordinator

**Files:** Create `LiveGenerationCoordinator.cs`, `LiveGenerationCoordinatorTests.cs`.

**Interfaces:**
- `LiveGenerationCoordinator(Func<GenerationRequest, CancellationToken, Task<GenerationOutcome>> generate, TimeProvider timeProvider)` implements `IAsyncDisposable`.
- `SubmitAsync(GenerationRequest request, bool immediate = false) -> Task`; `Current` returns `GenerationOutcome?`; `IsBusy` returns bool; `event Action? Changed` announces state transitions. Coordinator is per component/circuit, never singleton.

- [ ] Author tests using injectable time to establish that text submissions coalesce at 250 ms, immediate changes bypass delay, submission clears prior output immediately, a slower earlier result never wins, errors clear output, and disposal cancels pending work without notifications afterward. Use a test-controlled TimeProvider without real sleeps.
- [ ] Implement immutable request snapshots, cancellation, monotonic request sequence checks, and safe disposal. Synchronous encoding already started may finish, but its result cannot publish if superseded. UI event handlers dispatch through InvokeAsync.
- [ ] Record optional check: `dotnet test tests/DotQR.Playground.Tests --filter FullyQualifiedName~LiveGenerationCoordinator`; do not execute.
- [ ] Commit as `feat: debounce QR generation and reject stale results`.

## Task 6: Interactive Server workspace, editors, previews, and docs

**Files:** Create hosting, all Components/wwwroot files, launch settings, sample README; modify repository README.

**Interfaces:**
- Editors accept current values/options and EventCallback change notifications; text changes submit debounced, selects/checks/ranges/uploads submit immediately. Immutable option records are replaced using `with` expressions.
- `QrPreview` consumes `GenerationOutcome?` and `bool IsBusy` and never stores an independent downloadable result.
- `download.js` exports `download(fileName, mimeType, streamReference)`; creates Blob/object URL, triggers download, then revokes URL.

- [ ] Add Program registrations with `AddRazorComponents().AddInteractiveServerComponents()`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`, static assets and antiforgery. Use the standard .NET 10 shell; no external CDN/UI libraries or separate server.
- [ ] Implement schema-driven labeled payload editors, conditional sections, advanced optional field groups, preserved values per payload, and reset-to-example. Show payload and actual effective encoding metadata.
- [ ] Implement encoder and renderer controls with exact capability filtering. Micro forces raw text, restricts supported ECC choices, and disables incompatible renderers/options rather than silently ignoring values. Shared color controls support alpha only where available.
- [ ] Implement raster image upload components for logo/background/finder roles with errors and remove buttons. Enforce size limits on OpenReadStream as well as service validation.
- [ ] Implement responsive desktop columns/mobile stacking, clear headings, keyboard-accessible form controls, live error/status regions, and warnings for unsafe contrast/quiet zones/decorative settings. Render all payload text escaped. Display secrets warning prominently.
- [ ] Implement image/text/fallback preview, Base64 encoded text, metadata and download via DotNetStreamReference/module import. Downloads are disabled when pending or invalid. Clean up module and coordinator on disposal; do not generate sensitive URLs.
- [ ] Document `dotnet run --project samples/DotQR.Playground`, the launch URL from launch settings, .NET 10 SDK requirement, Windows/macOS support and Linux Skia native assets setup, privacy, all renderer differences, sample-only resource limits, and unverified status. Link sample from root README.
- [ ] Write a manual checklist in sample README covering each payload default, all renderer downloads, Micro compatibility, optional payment fields, invalid input, fast edits, uploads, keyboard navigation, and mobile layout. Record optional `dotnet build DotQR.slnx` and `dotnet test tests/DotQR.Playground.Tests`; do not execute under current guidance.
- [ ] Perform a source-only requirements review (not runtime verification): every spec feature maps to code, no ignored visible controls, no changes to existing library APIs, no DotQR PackageReference. Request a fresh whole-change code review per chosen execution method; resolve findings without claiming unrun tests passed.
- [ ] Commit as `feat: add interactive Blazor Server QR playground` and report run instructions plus checks not executed.
