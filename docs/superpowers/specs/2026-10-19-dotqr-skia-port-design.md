# DotQR: SkiaSharp port and three-package split

**Date:** 2026-10-19
**Status:** Approved for implementation planning
**Source repository:** https://github.com/Shane32/QRCoder.git (clone only; no ongoing link)

## 1. Problem

The upstream QRCoder library renders QR codes through `System.Drawing.Common`. That dependency
brings three problems for modern .NET:

1. `System.Drawing.Common` is Windows-only on .NET 6+ — every raster renderer is annotated
   `[SupportedOSPlatform("windows")]`, so the library is not cross-platform despite the
   `netstandard` targets.
2. The build carries seven target frameworks (`net35` through `net6.0`) and a large amount of
   `#if SYSTEM_DRAWING` / `#if HAS_SPAN` / `#if !NETSTANDARD1_3` / `#if NET35` conditional
   compilation that obscures the code.
3. QR *data generation* and QR *rendering* are one assembly, so a consumer who wants to write
   their own renderer must still take the whole image stack.

## 2. Goal

Produce a .NET 10+ library named **DotQR** that:

- renders through **SkiaSharp 4.153.1** instead of `System.Drawing` (or, for six renderers, uses
  no graphics library at all);
- is split into **three packages** so consumers can import only what they need, including
  "data only" for people writing their own renderer;
- keeps QR encoding, error-correction and payload-generation behaviour **bit-for-bit identical**
  to upstream;
- applies targeted performance work to both renderers and generator, proven by benchmarks;
- carries no git linkage to the upstream repository.

Nothing about the QR algorithm itself changes. This is a rendering and packaging port.

## 3. Decisions

| Topic | Decision |
|---|---|
| Package ids | `DotQR.Core`, `DotQR.ByteRenderers`, `DotQR.SkiaSharp` |
| Assembly names | Same as package ids |
| C# namespace | `DotQR` in all three packages |
| Target framework | `net10.0` only |
| SkiaSharp version | `4.153.1` (natively targets `net10.0`) |
| Raster return type | `SKBitmap` |
| SkiaSharp color type | `SKColor` |
| Dependency-free color type | Core-owned `RgbaColor` struct |
| Size type | Core-owned `Size` struct |
| `Base64QRCode` | SkiaSharp package (JPEG/WebP need an encoder) |
| `ImageType` | `{ Png, Jpeg, Webp }` — `Gif` removed |
| Strong naming | Dropped |
| Git history | `.git` deleted; fresh repository |
| Licensing | `LICENSE.txt` retained verbatim (MIT) |

## 4. Package architecture

The dependency graph is a strict chain, and it is the single most important constraint in this
design:

```
DotQR.Core          (no dependencies)
      |
      +--> DotQR.ByteRenderers    (no dependency beyond Core)
      |
      +--> DotQR.SkiaSharp        (Core + SkiaSharp)
```

**Rule:** `DotQR.Core` and `DotQR.ByteRenderers` must not reference `SkiaSharp`,
`System.Drawing.Common`, or any other graphics package, directly or transitively.

### 4.1 Why this split

Of the nine renderers, only three need a graphics library:

| Renderer | Needs a graphics library? |
|---|---|
| `QRCode` (`SKBitmap` output, icon compositing) | Yes — SkiaSharp |
| `ArtQRCode` (`SKBitmap` output, dotted modules, image compositing) | Yes — SkiaSharp |
| `Base64QRCode` (JPEG/WebP encoding) | Yes — SkiaSharp |
| `PngByteQRCode` (own PNG encoder) | No |
| `BitmapByteQRCode` (raw BMP writer) | No |
| `PdfByteQRCode` (raw PDF writer) | No |
| `SvgQRCode` (string writer) | No |
| `PostscriptQRCode` (string writer) | No |
| `AsciiQRCode` (string writer) | No |

Isolating the six dependency-free renderers into their own package means a consumer can produce
PNG, BMP, PDF, SVG, PostScript and ASCII output with **zero native dependencies** — worthwhile
for serverless, containerized and AOT-published scenarios — while `DotQR.SkiaSharp` is the only
package that ships native assets.

### 4.2 Boundary enforcement

The test project graph *is* the enforcement mechanism:

- `DotQR.Core.Tests` references `DotQR.Core` only.
- `DotQR.ByteRenderers.Tests` references `DotQR.Core` + `DotQR.ByteRenderers`, never SkiaSharp.
- `DotQR.SkiaSharp.Tests` references all three.

Any accidental SkiaSharp dependency introduced into the ByteRenderers package becomes a compile
error in `DotQR.ByteRenderers.Tests`.

## 5. Repository layout

```
DotQR.sln
Directory.Build.props
Directory.Build.targets
.editorconfig
.gitattributes
.gitignore
global.json
LICENSE.txt
README.md
nuget-icon.png
docs/superpowers/specs/2026-10-19-dotqr-skia-port-design.md

src/DotQR.Core/
src/DotQR.ByteRenderers/
src/DotQR.SkiaSharp/

tests/DotQR.Core.Tests/
tests/DotQR.ByteRenderers.Tests/
tests/DotQR.SkiaSharp.Tests/
tests/DotQR.ApiTests/

tools/DotQR.Benchmarks/
tools/DotQR.TrimAnalysis/
```

### 5.1 Deleted from the clone

| Path | Reason |
|---|---|
| `.git/` | Fresh history; no link to upstream |
| `QRCoder.Xaml/` | WPF; `XamlQRCode` is Windows-only and outside a cross-platform scope |
| `QRCoderDemo/` | WinForms demo |
| `QRCoderDemoUWP/` | UWP demo |
| `QRCoderConsole/` | CLI that only exercises removed projects |
| `.github/workflows/` | Reference dead target frameworks and the upstream publish pipeline |
| `.github/FUNDING.yml` | Upstream funding metadata |
| `QRCoder/Attributes/NotNullWhenAttribute.cs` | Polyfill; `NotNullWhen` exists in net10.0 |
| `QRCoder/Extensions/StreamExtensions.cs` | `#if NET35` polyfill; no body compiles for net10.0 |

`.github/ISSUE_TEMPLATE.md`, `.github/PULL_REQUEST_TEMPLATE.md` and `.github/docu/*` are retained
but reworded to refer to DotQR where they name the upstream project.

## 6. `DotQR.Core`

### 6.1 Contents

| Source (from clone) | Destination |
|---|---|
| `QRCoder/QRCodeGenerator.cs` | `src/DotQR.Core/QRCodeGenerator.cs` |
| `QRCoder/QRCodeGenerator/*.cs` (23 files) | `src/DotQR.Core/QRCodeGenerator/` |
| `QRCoder/QRCodeData.cs` | `src/DotQR.Core/QRCodeData.cs` |
| `QRCoder/AbstractQRCode.cs` | `src/DotQR.Core/AbstractQRCode.cs` |
| `QRCoder/PayloadGenerator.cs` | `src/DotQR.Core/PayloadGenerator.cs` |
| `QRCoder/PayloadGenerator/*.cs` (25 files) | `src/DotQR.Core/PayloadGenerator/` |
| `QRCoder/Exceptions/DataTooLongException.cs` | `src/DotQR.Core/Exceptions/` |
| `QRCoder/Extensions/BitArrayExtensions.cs` | `src/DotQR.Core/Extensions/` |
| `QRCoder/Extensions/StringExtensions.cs` | `src/DotQR.Core/Extensions/` |
| `QRCoder/Extensions/StringValueAttribute.cs` | `src/DotQR.Core/Extensions/` |

### 6.2 New public types

Two new public types are introduced. They exist so that `DotQR.ByteRenderers` can express colour
and geometry without a graphics dependency.

#### `DotQR.RgbaColor`

```csharp
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    public RgbaColor(byte red, byte green, byte blue);
    public RgbaColor(byte red, byte green, byte blue, byte alpha);

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public static RgbaColor Black { get; }        // 0,0,0,255
    public static RgbaColor White { get; }        // 255,255,255,255
    public static RgbaColor Transparent { get; }  // 0,0,0,0

    public bool IsOpaque { get; }                 // A == 255
    public bool IsTransparent { get; }            // A == 0

    public static RgbaColor Parse(string value);
    public static bool TryParse(string? value, out RgbaColor color);

    public byte[] ToRgbBytes();      // 3 bytes
    public byte[] ToRgbaBytes();     // 4 bytes
    public string ToHtmlHex();       // "#RGB" for black/white, "#RRGGBB", "#RRGGBBAA", or "transparent"
    public override string ToString();
}
```

`Parse`/`TryParse` accept, case-insensitively:

- `#RGB` (each digit duplicated: `#f80` → `#ff8800`)
- `#RRGGBB`
- `#RRGGBBAA`
- a leading `#` is optional for the hex forms
- HTML/CSS named colours (`black`, `white`, `red`, `transparent`, …) — the full CSS Level 4
  named-colour table plus `transparent`

This is required because `SKColor.Parse("red")` throws `ArgumentException`; the legacy
`ColorTranslator.FromHtml` accepted named colours and existing call sites depend on it. Parsing
reuses the existing `StringExtensions.HexColorToByteArray` for the hex forms.

`ToHtmlHex` reproduces the exact current `SvgQRCode.ColorToHex` output rules (`#000` for black,
`#FFF` for white, `transparent` for A==0, `#RRGGBB` when opaque, `#RRGGBBAA` otherwise) so that
SVG output stays byte-identical.

#### `DotQR.Size`

A faithful replacement for the `System.Drawing.Size` that `SvgQRCode` and `PostscriptQRCode`
expose in their public API today:

```csharp
public readonly struct Size : IEquatable<Size>
{
    public Size(int width, int height);
    public int Width { get; }
    public int Height { get; }
    public static Size Empty { get; }   // 0x0
    public bool IsEmpty { get; }
    public override string ToString();
}
```

### 6.3 Removed conditional compilation

All of the following disappear from `DotQR.Core` because there is exactly one target framework:

- `#if SYSTEM_DRAWING` / `#endif`
- `#if HAS_SPAN` / `#else` / `#endif` — always take the span branch
- `#if !NETSTANDARD1_3` / `#endif`
- `#if NET35`, `#if NET40_OR_GREATER`, `#if NETCOREAPP2_0_OR_GREATER`,
  `#if NETSTANDARD2_1_OR_GREATER`, `#if NET6_0_OR_GREATER` reachable-from-Core branches
- `#pragma warning disable CS0618` wrappers that exist only because of the above
- the `NET40_OR_GREATER` / `NETSTANDARD1_3` legs of the `ModuleMatrix` and `Polynom`
  array-pool implementations — only the `ArrayPool<T>` implementation is retained

### 6.4 Unchanged public API

`QRCodeGenerator`, `QRCodeData`, `PayloadGenerator` and every payload generator keep their exact
public signatures. `QRCodeData.ModuleMatrix` stays `List<BitArray>`. This is deliberate: it is
the surface that third-party renderers consume, which is the entire point of splitting Core out.

## 7. `DotQR.ByteRenderers`

### 7.1 Contents and changes

| Renderer | Change |
|---|---|
| `PngByteQRCode` | One convenience overload changes from `System.Drawing.Color` to `RgbaColor`. Existing `byte[]` RGBA overload unchanged. |
| `BitmapByteQRCode` | **No change** — already hex-string and `byte[]` based. |
| `PdfByteQRCode` | **No change** — already hex-string based. |
| `AsciiQRCode` | **No change** — already `string` based. |
| `SvgQRCode` | `Color` → `RgbaColor`; `Size` → `DotQR.Size`; `SvgLogo(Bitmap)` constructor removed; private `RectangleF` replaced by a private internal struct. |
| `PostscriptQRCode` | `Color` → `RgbaColor`; `Size` → `DotQR.Size`. |

`SvgQRCode.SvgLogo` keeps its `string` (vector) and `byte[]` (raster PNG) constructors. The
`SKBitmap` constructor cannot exist here because it would introduce a SkiaSharp dependency.

### 7.2 PNG output must not change

`PngByteQRCode` keeps producing 1-bit greyscale (default black/white) or 1-bit indexed PNGs with
the same palette/chunk layout as today. The performance work in §9.1 is constrained to be
byte-preserving. `SvgQRCode` and `PostscriptQRCode` likewise keep byte-identical output.

## 8. `DotQR.SkiaSharp`

### 8.1 Contents

| Renderer | Change |
|---|---|
| `QRCode` | `Bitmap` → `SKBitmap`; `Color` → `SKColor`; `Bitmap? icon` → `SKBitmap? icon`; `GraphicsPath` painting replaced by a module-resolution bitmap scaled with nearest-neighbour sampling; `CreateRoundedRectanglePath` → `SKRoundRect`/`SKPath`. |
| `ArtQRCode` | `Bitmap` → `SKBitmap` for output, background image and finder-pattern image; `Color` → `SKColor`; dotted modules drawn as two accumulated `SKPath`s. |
| `Base64QRCode` | PNG path unchanged (delegates to `PngByteQRCode`, so base64 of the identical PNG). JPEG/WebP paths encode via `SKBitmap.Encode`. `ImageType` becomes `{ Png, Jpeg, Webp }`. |

### 8.2 New members

```csharp
// SvgLogoExtensions.cs
public static class SvgLogoExtensions
{
    public static SvgLogo ToSvgLogo(
        this SKBitmap icon,
        int iconSizePercent = 15,
        bool fillLogoBackground = true);
}

// SkiaColorExtensions.cs
public static class SkiaColorExtensions
{
    public static SKColor ToSKColor(this RgbaColor color);
    public static RgbaColor ToRgbaColor(this SKColor color);
}
```

`ToSvgLogo` encodes the `SKBitmap` to PNG (`SKEncodedImageFormat.Png`, quality 100) and forwards
to `new SvgLogo(byte[], …)` in `DotQR.ByteRenderers`, preserving today's behaviour.

### 8.3 Colour semantics

`SKBitmap` output is `SKColorType.Rgba8888` with `SKAlphaType.Premul`, matching the
`Format32bppArgb` intent of the original. Background fill uses `SKCanvas.Clear(SKColor)`, which
writes pixels directly and is equivalent to the original `CompositingMode.SourceCopy` + fill.
Icon compositing uses the default source-over blend, matching the original.

## 9. Performance work

All performance work is measured with `tools/DotQR.Benchmarks` (BenchmarkDotNet). Every
optimization is a separate commit with the full test suite green before and after.

### 9.1 `PngByteQRCode` — byte-preserving

Nothing here may change the produced PNG bytes.

| Change | Rationale |
|---|---|
| RLE the scanline fill: emit whole `0xFF`/`0x00` byte spans per module run instead of one read-modify-write and one modulo per pixel | Current inner loop touches a byte per pixel for every dark module |
| zlib-style Adler-32: 5552-byte blocks with deferred modulo | Current implementation does `%` twice per byte |
| Slicing-by-8 CRC-32 table | Current implementation is byte-at-a-time over the whole file |
| Record each chunk's offset and length as it is written, so `GetBytes` computes CRCs over the recorded ranges instead of re-parsing the output stream | Removes a full parse pass |
| Replace `_stream.ToArray()` with `_stream.GetBuffer()` plus the known length | Removes a full copy of the PNG |

**Constraints that must not change**, or the PNG bytes move and the approvals break:

- `DeflateStream` stays at its default `CompressionLevel.Optimal` — do not "optimize" it to
  `Fastest`.
- The manual zlib header (`0x78`, `0x9C`) and hand-written Adler-32 trailer stay. The code emits
  raw deflate with its own zlib framing; switching to `ZLibStream` would double-wrap it.
- Palette, `tRNS` emission rules and chunk ordering stay exactly as they are.

### 9.2 `QRCode` — pixel-exact

Render the matrix once at module resolution into an `SKBitmap` (`modules × modules`), then blit
it to the final size with `SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None)`.

This replaces hundreds of `FillPath` rectangles painted at full resolution with a single small
source bitmap plus one scaling blit. Nearest-neighbour sampling at integer scale is
**pixel-exact** — verified on SkiaSharp 4.153.1 with a checkerboard 4×4 → 40×40 test, zero
mismatches. This is what allows the existing image approvals to remain valid.

The icon overlay still draws onto a full-size canvas after the blit.

### 9.3 `ArtQRCode` — anti-aliasing may change

Replace `MakeDotPixel` (which allocated and painted a `pixelSize × pixelSize` bitmap per render,
then cropped a second bitmap) and the `numModules²` `DrawImage` calls with two accumulated
`SKPath`s — one for dark dots, one for light dots — each drawn once.

Geometry is derived from the existing behaviour: `pixelSize = min(pim, floor(pim × factor))`,
margin-centred, which reduces exactly to a circle of diameter `pixelSize` centred in each module.
Finder patterns remain solid rectangles; `QuietZoneStyle.Flat` quiet zones remain rectangles.

Because Skia's anti-aliased circle rasterization differs from GDI+'s, the affected image
approvals are regenerated and reviewed file by file.

### 9.4 Generator

Upstream is already well optimized here — flat byte bitsets (`ModuleMatrix`), `ArrayPool` rent
and return, span-based nibble shifting, precomputed mask patterns, precomputed generator
polynomials and GF(256) tables. The following are the remaining wins.

| Change | Rationale | Correctness argument |
|---|---|---|
| **Early-abort mask scoring** in `ModulePlacer.MaskCode` | Scoring runs 8 full `O(size²)` passes; this dominates generation time | Every penalty term is non-negative, so a partial sum can only grow. Aborting a candidate once its running total exceeds the incumbent best cannot change which mask wins or the final score. |
| Apply the winning mask by replacing row `BitArray`s via `new BitArray(byte[])` instead of `size²` `Func<int,int,bool>` delegate calls | The final application loop costs one delegate invoke plus two `BitArray` index operations per module | The mask pattern bytes are already computed by `MaskPattern`; the same bits are written, only the write path changes. |
| `QRCodeData.GetRawData()` and `QRCodeData(byte[], Compression)`: direct bit packing instead of `Queue<int>` / `Queue<bool>` | One queue operation per module, plus a large queue allocation | Same bit order, same output bytes. `QRGeneratorTests` round-trip tests cover this. |
| `InterleaveData`: word-wise 8-bit copies instead of the bit-by-bit `BitArrayExtensions.CopyTo` | Called once per interleaved codeword | Identical bit sequence. |
| `Score()`: `BitOperations.PopCount` for the black-module tally; fuse penalty-2 and penalty-3 passes | Three separate full scans over the matrix | Pure refactor of the same arithmetic. |
| `Polynom`: `Span<byte>` coefficients instead of `PolynomItem[]` (`int Coefficient` + `int Exponent`) | Halves the working-set size of the ECC polynomial and removes struct field unpacking | Coefficients are asserted `< 256` already; exponents are index-derived. |

### 9.5 Explicitly out of scope

Already optimal, left alone: GF(256) tables, `MaskPattern.Apply` (byte-wise already),
`SvgQRCode` module emission (already RLE), `PostscriptQRCode` (already pre-sized; its output
format requires one token per module), `BitmapByteQRCode`, `PdfByteQRCode`, `AsciiQRCode`.

## 10. Testing

### 10.1 Project split

| Project | References | Covers |
|---|---|---|
| `tests/DotQR.Core.Tests` | `DotQR.Core` | `QRGeneratorTests`, `TransposeVerificationTests`, all `PayloadGeneratorTests` |
| `tests/DotQR.ByteRenderers.Tests` | `DotQR.Core`, `DotQR.ByteRenderers` | `PngByteQRCodeRendererTests`, `BitmapByteQRCodeRendererTests`, `PdfByteQRCodeRendererTests`, `SvgQRCodeRendererTests`, `PostscriptQRCodeRendererTests`, `AsciiQRCodeRendererTests` |
| `tests/DotQR.SkiaSharp.Tests` | all three | `QRCodeRendererTests`, `ArtQRCodeRendererTests`, `Base64QRCodeRendererTests` |
| `tests/DotQR.ApiTests` | all three | Public API approval snapshots, one per assembly |

`XamlQRCodeRendererTests` and the `TEST_XAML` conditional compilation are deleted, as are the
`#if SYSTEM_DRAWING` guards around the nine affected test files.

### 10.2 Approval strategy

The existing approved `.gif` files were produced by encoding GDI+ bitmaps through KGySoft's GIF
encoder, precisely to avoid platform-specific compression differences. This strategy is retained
but the pixel source changes: `ShouldMatchApproved(this SKBitmap …)` reads the `SKBitmap`'s RGBA
buffer and hands it to KGySoft via `BitmapDataFactory.CreateBitmapData`, then encodes to GIF
exactly as before.

There are 32 approved `.gif` baselines today; the 1 belonging to `XamlQRCodeRendererTests` is
deleted with that project, leaving 31 anchors.

Consequence: the approved `.gif` anchors remain directly comparable, so any pixel difference
introduced by the SkiaSharp port shows up as a failed approval rather than being masked by
regenerating everything.

| Category | Expectation |
|---|---|
| PNG (7), BMP (3), PDF, PS, SVG (3), ASCII | **Byte-identical.** Any diff is a bug. |
| `QRCode` raster (9) | **Pixel-identical** by the nearest-neighbour argument in §9.2. A diff indicates a scaling or colour-semantics error. |
| `ArtQRCode` raster (5) | Regenerated. Anti-aliasing differs by design. Each regenerated file is reviewed before approval. |
| `Base64QRCode` JPEG/WebP | Regenerated; new baselines. |
| `Base64QRCode` PNG (1) | Byte-identical (delegates to `PngByteQRCode`). |
| `TransposeVerificationTests` ASCII (3) | Byte-identical. |
| Public API snapshots | Regenerated for the rename and the `RgbaColor`/`Size` additions. |

### 10.3 Round-trip and regression guards

`QRGeneratorTests`, `TransposeVerificationTests` and every `PayloadGeneratorTests` file are
unchanged in content. They are the safety net for the §9.4 generator work, which is the highest
risk area because it touches verified correctness code.

## 11. Build and packaging

`Directory.Build.props`:

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<AnalysisLevel>latest-recommended</AnalysisLevel>
<IsPackable>false</IsPackable>   <!-- overridden per package -->
```

`LangVersion` is deliberately not set: targeting `net10.0` already selects the SDK's matching
language version (C# 14). `preview` would enable unstable language features in a published
library.

`AnalysisLevel` uses `latest-recommended` rather than a pinned `10-recommended`, because the
pinned form is not a documented value. Any new analyzer warning surfacing under
`TreatWarningsAsErrors` is fixed in code; the analysis level is not lowered to make the build
pass.

Per package:

| Property | `Core` | `ByteRenderers` | `SkiaSharp` |
|---|---|---|---|
| `TargetFramework` | `net10.0` | `net10.0` | `net10.0` |
| `IsPackable` | `true` | `true` | `true` |
| `IsTrimmable` | `true` | `true` | `true` |
| `PackageId` | `DotQR.Core` | `DotQR.ByteRenderers` | `DotQR.SkiaSharp` |
| `Description` | QR data generation | Dependency-free renderers | SkiaSharp renderers |
| `PackageReference` | — | `DotQR.Core` | `DotQR.Core`, `SkiaSharp 4.153.1` |

Removed properties: `SignAssembly`, `DelaySign`, `AssemblyOriginatorKeyFile`, `ComVisible`,
`Guid`, `RepositoryType`, `PublishRepositoryUrl`, and the `InternalsVisibleTo` items that pinned
the upstream public key.

`InternalsVisibleTo` is re-declared unsigned for `DotQR.Core.Tests` and
`DotQR.ByteRenderers.Tests` against `DotQR.Core`, and for `DotQR.SkiaSharp.Tests` against
`DotQR.SkiaSharp`.

### 11.1 Native assets

The `SkiaSharp` package supplies Win32 and macOS native assets automatically. Linux consumers
must add `SkiaSharp.NativeAssets.Linux` (or `SkiaSharp.NativeAssets.Linux.NoDependencies`) — this
is standard SkiaSharp practice and is documented in `README.md` rather than being forced on
consumers. Test projects add the Linux asset package so CI works.

## 12. README

Rewritten to cover:

- the three-package model and a decision table for which to install;
- a "bring your own renderer" example that uses only `DotQR.Core` and reads
  `QRCodeData.ModuleMatrix`;
- the `RgbaColor` and `Size` primitives;
- the Linux native-asset note;
- migration notes from `QRCoder` (§13);
- an attribution section crediting Raffael Herrmann and Shane Krueger and linking the upstream
  repository, since the Copyright line and MIT licence text must be preserved.

## 13. Breaking changes for migrating consumers

1. Namespace `QRCoder` → `DotQR`; package id `QRCoder` → one of the three `DotQR.*` packages.
2. `Bitmap` → `SKBitmap`.
3. `Color` → `RgbaColor` in `DotQR.ByteRenderers`; `Color` → `SKColor` in `DotQR.SkiaSharp`.
4. `Size` → `DotQR.Size`.
5. `Base64QRCode.ImageType.Gif` removed; `Webp` added. Skia cannot encode GIF —
   `SKBitmap.Encode(SKEncodedImageFormat.Gif, …)` returns `null` rather than throwing (verified).
6. `XamlQRCode` and the `QRCoder.Xaml` package no longer exist.
7. `SvgLogo(Bitmap)` replaced by the `ToSvgLogo(SKBitmap)` extension in `DotQR.SkiaSharp`.
8. Strong-named assembly identity is gone.

## 14. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| `QRCode` nearest-neighbour scaling is not pixel-exact for some `pixelsPerModule` | Low — verified for a representative case | The 9 existing `QRCode` raster approvals act as the check across all real cases |
| `ArtQRCode` anti-aliasing changes more than expected | Medium | Approvals regenerated with per-file review; output compared visually |
| Generator optimizations introduce subtle bit-order errors | Medium — highest-risk area | Covered by generator, transpose and payload suites; early-abort scoring has a formal non-negativity argument; each change is an isolated commit |
| SkiaSharp 4.x is a new major release | Medium | API surface fully probed and exercised on 4.153.1; `3.119.1` is a fallback (no `net10.0` asset, but the `net8.0` asset works) |
| Linux/macOS-arm consumers hit missing native assets | Medium | Documented in README; present in test projects |
| Renaming to `DotQR` breaks copy-pasted upstream samples | Certain | README migration table (§13) |

## 15. Acceptance criteria

1. `dotnet build DotQR.sln` succeeds with zero warnings under `TreatWarningsAsErrors`.
2. `dotnet test DotQR.sln` passes across all four test projects.
3. No reference to `System.Drawing` anywhere in the solution, verified by
   `grep -r "System.Drawing" src/ tests/` returning nothing.
4. `DotQR.ByteRenderers` has no transitive dependency on SkiaSharp; verified by
   `dotnet list src/DotQR.ByteRenderers package --include-transitive`.
5. PNG/BMP/PDF/PS/SVG/ASCII approvals are byte-identical to the upstream baselines.
6. `QRCode` raster approvals are pixel-identical to the upstream baselines.
7. `tools/DotQR.Benchmarks` records a before/after comparison for each §9 item.
8. `dotnet pack` produces three `.nupkg` files, each with `LICENSE.txt`, `README.md` and
   `nuget-icon.png`.
9. `tools/DotQR.TrimAnalysis` publishes with `PublishTrimmed` and no trim warnings.
10. `git remote -v` is empty and `git log` contains no upstream commits.
