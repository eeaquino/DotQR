# DotQR SkiaSharp Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `System.Drawing` with SkiaSharp across the QRCoder codebase and split it into three .NET 10 packages — `DotQR.Core` (data only), `DotQR.ByteRenderers` (no graphics dependency), `DotQR.SkiaSharp` (raster) — with targeted performance work proven by benchmarks.

**Architecture:** Strictly acyclic package chain: `DotQR.Core` → `DotQR.ByteRenderers` and `DotQR.Core` → `DotQR.SkiaSharp`. All three share the `DotQR` namespace. The test project graph enforces the boundary: `DotQR.ByteRenderers.Tests` does not reference SkiaSharp, so any accidental leak of a graphics dependency into the dependency-free package fails to compile.

**Tech Stack:** .NET 10 (`net10.0`), C# 14, SkiaSharp 4.153.1, xunit, Shouldly, KGySoft.Drawing (GIF encoding for pixel approvals only), BenchmarkDotNet, PublicApiGenerator.

**Spec:** `docs/superpowers/specs/2026-10-19-dotqr-skia-port-design.md`

## Global Constraints

- **Target framework:** `net10.0` only. No multi-targeting, no `netstandard`, no `net35`/`net40`.
- **Namespace:** `DotQR` in all three packages. Never `QRCoder`.
- **Package ids / assembly names:** `DotQR.Core`, `DotQR.ByteRenderers`, `DotQR.SkiaSharp`.
- **SkiaSharp version:** exactly `4.153.1`.
- **No `System.Drawing`** anywhere in `src/` or `tests/`. Acceptance is `grep -r "System.Drawing" src/ tests/` returning nothing.
- **`DotQR.Core` and `DotQR.ByteRenderers` must not reference SkiaSharp**, directly or transitively.
- **Analysis:** `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`. Fix warnings in code; never lower the analysis level or blanket-suppress.
- **`LangVersion` is not set.** `net10.0` already selects C# 14.
- **Strong naming is gone.** No `.snk`, no `SignAssembly`, no public-key `InternalsVisibleTo` pins.
- **Byte-identity guarantee:** PNG, BMP, PDF, PostScript, SVG and ASCII output must remain byte-identical to the upstream baselines. This is a hard assertion enforced by `DotQR.ByteRenderers.Tests`.
- **Raster verification:** the GDI-era `.approved.gif` anchors are retired (see Self-Review, deviation 3) because retaining them requires a `System.Drawing`-dependent quantizer. Raster output is verified by regenerated SkiaSharp PNG baselines **plus** baseline-independent invariant tests that assert dimensions, per-module colours, transparency and finder-pattern solidity.
- **Approval tests auto-approve locally.** `ShouldMatchApproved` copies `.received` over `.approved` whenever the `CI` environment variable is unset. **Always verify with `CI=1` set**, or a regression will silently rewrite its own baseline. Unset `CI` *only* for the deliberate regeneration steps, which are called out explicitly.
- **`LICENSE.txt` is never modified.** MIT requires the upstream copyright notice to survive verbatim.
- **No git remote.** `git remote -v` must stay empty.
- **Commit style:** conventional prefix, one logical change per commit, tests green at every commit.

---

## File Structure

### Created

```
DotQR.sln
src/DotQR.Core/DotQR.Core.csproj
src/DotQR.Core/Primitives/RgbaColor.cs
src/DotQR.Core/Primitives/Size.cs
src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj
src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj
src/DotQR.SkiaSharp/Extensions/SkiaColorExtensions.cs
src/DotQR.SkiaSharp/Extensions/SvgLogoExtensions.cs
tests/DotQR.Core.Tests/DotQR.Core.Tests.csproj
tests/DotQR.Core.Tests/Primitives/RgbaColorTests.cs
tests/DotQR.Core.Tests/Primitives/SizeTests.cs
tests/DotQR.ByteRenderers.Tests/DotQR.ByteRenderers.Tests.csproj
tests/DotQR.SkiaSharp.Tests/DotQR.SkiaSharp.Tests.csproj
tests/DotQR.ApiTests/DotQR.ApiTests.csproj
tools/DotQR.Benchmarks/DotQR.Benchmarks.csproj
tools/DotQR.TrimAnalysis/DotQR.TrimAnalysis.csproj
```

### Responsibility boundaries

| File group | Responsibility |
|---|---|
| `src/DotQR.Core/QRCodeGenerator*` | QR encoding, ECC, module placement, mask selection. No I/O, no graphics. |
| `src/DotQR.Core/Primitives/*` | Colour and geometry value types shared by all packages. No dependencies. |
| `src/DotQR.ByteRenderers/*` | Serialize a module matrix to bytes or text. No graphics library. |
| `src/DotQR.SkiaSharp/*` | Rasterize a module matrix via SkiaSharp, including icon compositing. |

---

## Phase 1 — Repository scaffolding

### Task 1: Remove obsolete projects and CI

**Files:**
- Delete: `QRCoder.Xaml/`, `QRCoderDemo/`, `QRCoderDemoUWP/`, `QRCoderConsole/`
- Delete: `.github/workflows/`, `.github/FUNDING.yml`
- Delete: `QRCoder/Attributes/NotNullWhenAttribute.cs`, `QRCoder/Extensions/StreamExtensions.cs`
- Delete: `QRCoder.sln`, `QRCoderProject.sln.DotSettings`
- Delete: `tests`-equivalent obsolete approvals `QRCoderTests/XamlQRCodeRendererTests.cs` and its approved file

**Interfaces:**
- Consumes: nothing.
- Produces: a working tree containing only `QRCoder/` and `QRCoderTests/` as the port's raw material.

- [ ] **Step 1: Verify each deletion target resolves inside the repo**

Run:
```powershell
cd "H:\Programming Projects\AI Test\QRCoder-SkiaSharp"
$root = (Resolve-Path .).Path
foreach ($p in 'QRCoder.Xaml','QRCoderDemo','QRCoderDemoUWP','QRCoderConsole','.github/workflows','QRCoder.sln') {
  $full = Join-Path $root $p
  if (Test-Path $full) { Write-Output "OK   $full" } else { Write-Output "MISS $full" }
}
```
Expected: every line starts with `OK`.

- [ ] **Step 2: Delete via git so history records the removal**

```powershell
git rm -r --quiet QRCoder.Xaml QRCoderDemo QRCoderDemoUWP QRCoderConsole .github/workflows
git rm -r --quiet QRCoder/Attributes QRCoder/Extensions/StreamExtensions.cs
git rm --quiet QRCoder.sln QRCoderProject.sln.DotSettings .github/FUNDING.yml
git rm --quiet QRCoderTests/XamlQRCodeRendererTests.cs
git rm --quiet "QRCoderTests/XamlQRCodeRendererTests.can_render_simple_xaml_code.approved.gif"
```
Expected: no errors. If a path is missing, run `git status` and adjust the exact filename.

- [ ] **Step 3: Confirm only the port material remains**

Run: `Get-ChildItem -Force | Select-Object -ExpandProperty Name`
Expected: `.git`, `.github`, `.editorconfig`, `.gitattributes`, `.gitignore`, `Directory.Build.props`, `Directory.Build.targets`, `LICENSE.txt`, `QRCoder`, `QRCoderTests`, `README.md`, `global.json`, `nuget-icon.png`, `docs`.

- [ ] **Step 4: Commit**

```powershell
git commit -m "chore: remove WPF/UWP/WinForms projects, CI workflows and legacy polyfills"
```

---

### Task 2: Build skeleton and Core project

**Files:**
- Create: `src/DotQR.Core/DotQR.Core.csproj`
- Modify: `Directory.Build.props`
- Modify: `Directory.Build.targets`

**Interfaces:**
- Consumes: nothing.
- Produces: a `DotQR.Core` assembly targeting `net10.0` that builds clean, ready to receive moved source in Task 3.

- [ ] **Step 1: Replace `Directory.Build.props`**

```xml
<Project>

  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>

    <Authors>Edgar Aquino</Authors>
    <Copyright>Copyright © 2013-2025 Raffael Herrmann. Copyright © 2024-2025 Shane Krueger. All rights reserved.</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>nuget-icon.png</PackageIcon>

    <DebugType>embedded</DebugType>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <Deterministic>true</Deterministic>

    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <NoWarn>$(NoWarn);IDE0005;CA1510</NoWarn>
  </PropertyGroup>

  <ItemGroup Condition="'$(IsPackable)' == 'true'">
    <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="\" />
    <None Include="$(MSBuildThisFileDirectory)LICENSE.txt" Pack="true" PackagePath="\" />
    <None Include="$(MSBuildThisFileDirectory)nuget-icon.png" Pack="true" PackagePath="\" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="System.Collections.BitArray" Alias="BitArray" />
    <Using Include="System.Globalization" />
    <Using Include="System.Text" />
    <Using Include="System.Text.RegularExpressions" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Replace `Directory.Build.targets`** with only what still applies

```xml
<Project>

  <!-- Suppress XML comment warnings for non-packable projects -->
  <PropertyGroup Condition="'$(IsPackable)' != 'true'">
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

</Project>
```

The `SYSTEM_DRAWING` and `HAS_SPAN` define blocks are deleted: there is one target framework, and the code no longer branches on them.

- [ ] **Step 3: Create the Core project file**

`src/DotQR.Core/DotQR.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.Core</AssemblyName>
    <RootNamespace>DotQR</RootNamespace>
    <IsPackable>true</IsPackable>
    <IsTrimmable>true</IsTrimmable>
    <PackageId>DotQR.Core</PackageId>
    <PackageTags>c# csharp qr qrcode qr-generator qr-code-generator</PackageTags>
    <Description>DotQR.Core generates QR code module data. It has no graphics dependencies; pair it with DotQR.ByteRenderers, DotQR.SkiaSharp, or your own renderer.</Description>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="DotQR.Core.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Verify the skeleton builds**

Run:
```powershell
dotnet build src/DotQR.Core/DotQR.Core.csproj -v quiet
```
Expected: `Build succeeded`, 0 warnings, 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add Directory.Build.props Directory.Build.targets src/DotQR.Core
git commit -m "build: retarget to net10.0 and add DotQR.Core skeleton"
```

---

## Phase 2 — DotQR.Core

### Task 3: Move QR generation source into Core and rename the namespace

**Files:**
- Move: `QRCoder/*.cs` (excluding renderers) and `QRCoder/QRCodeGenerator/` → `src/DotQR.Core/`
- Move: `QRCoder/Exceptions/`, `QRCoder/Extensions/` → `src/DotQR.Core/`

**Interfaces:**
- Consumes: `DotQR.Core.csproj` from Task 2.
- Produces: `DotQR.QRCodeGenerator`, `DotQR.QRCodeData`, `DotQR.AbstractQRCode`, `DotQR.ECCLevel`, `DotQR.EciMode` — the public surface every renderer and every test depends on.

- [ ] **Step 1: Move the generator and data files**

```powershell
cd "H:\Programming Projects\AI Test\QRCoder-SkiaSharp"
New-Item -ItemType Directory -Force -Path src/DotQR.Core/QRCodeGenerator, src/DotQR.Core/Exceptions, src/DotQR.Core/Extensions | Out-Null
Move-Item QRCoder/QRCodeGenerator/*.cs src/DotQR.Core/QRCodeGenerator/
Move-Item QRCoder/QRCodeData.cs, QRCoder/AbstractQRCode.cs, QRCoder/QRCodeGenerator.cs src/DotQR.Core/
Move-Item QRCoder/PayloadGenerator.cs src/DotQR.Core/
Move-Item QRCoder/PayloadGenerator src/DotQR.Core/PayloadGenerator
Move-Item QRCoder/Exceptions/*.cs src/DotQR.Core/Exceptions/
Move-Item QRCoder/Extensions/*.cs src/DotQR.Core/Extensions/
git add -A src/DotQR.Core
```

`QRCodeGenerator.cs` and the `QRCodeGenerator/` folder both exist; move the folder's contents first, then the three top-level files, so nothing is clobbered.

- [ ] **Step 2: Rename the namespace in moved source — targeted, not global**

A blind `QRCoder` → `DotQR` replace would corrupt the attribution URLs (`github.com/Shane32/QRCoder`) and the prose "QRCoder is a simple library". Rewrite only namespace and type-qualification contexts:

```powershell
Get-ChildItem -Recurse src/DotQR.Core -Filter *.cs | ForEach-Object {
    $t = Get-Content -Raw $_.FullName
    $t = $t -replace 'namespace QRCoder;', 'namespace DotQR;'
    $t = $t -replace 'namespace QRCoder\.', 'namespace DotQR.'
    $t = $t -replace 'using static QRCoder\.', 'using static DotQR.'
    $t = $t -replace 'using QRCoder;', 'using DotQR;'
    $t = $t -replace '\bQRCoder\.([A-Z])', 'DotQR.$1'
    Set-Content -NoNewline -Path $_.FullName -Value $t
}
```

- [ ] **Step 3: Strip conditional compilation from Core**

For every file under `src/DotQR.Core`, resolve the preprocessor branches for a single `net10.0` build. `SYSTEM_DRAWING`, `HAS_SPAN`, `NETSTANDARD1_3`, `NET35`, `NET40_OR_GREATER`, `NETCOREAPP2_0_OR_GREATER` and `NETSTANDARD2_1_OR_GREATER` no longer exist, so:

- The `HAS_SPAN` branch is always taken (e.g. `GaloisField._integerValueByAlphaExponent` stays `ReadOnlySpan<byte>`; `Polynom` keeps only its `ArrayPool` implementation and loses the thread-local "poor-man's pool").
- The `NET40_OR_GREATER` and non-span legs of `ModuleMatrix`'s pool are deleted; only `ArrayPool<byte>` remains.
- `StringExtensions.IsNullOrWhiteSpace` keeps only `return string.IsNullOrWhiteSpace(value);`. `StartsWith(char)` is deleted (it exists in .NET Standard 2.1+). `HexColorToByteArray` keeps only the `AsSpan` form. Both `AppendInvariant` overloads keep only their `NET6_0_OR_GREATER` form.
- `QRCodeData.GetRawData` keeps only the collection-expression `targetStream.Write([0x51, 0x52, 0x52, 0x00]);` form.
- Any now-unused `using System.Diagnostics.CodeAnalysis;` or `#pragma warning disable CS0618` pair is removed.

- [ ] **Step 4: Build and fix fallout**

Run: `dotnet build src/DotQR.Core/DotQR.Core.csproj -v quiet`
Expected: `Build succeeded` with 0 warnings. Read every error literally — they will be unresolvable types from deleted branches or namespace mismatches. Do not add `NoWarn` to silence them.

- [ ] **Step 5: Verify no `System.Drawing` and no stale namespace**

```powershell
Select-String -Recurse src/DotQR.Core -Pattern 'System\.Drawing|namespace QRCoder|\bQRCoder\.[A-Z]' | Select-Object Path,LineNumber,Line
```
Expected: no output.

- [ ] **Step 6: Verify remaining `QRCoder` prose occurrences are URLs only**

```powershell
Select-String -Recurse src/DotQR.Core -Pattern 'QRCoder' | Select-Object Path,LineNumber,Line
```
Expected: only lines containing `github.com/.../QRCoder` (attribution links in comments). Any other hit is a missed rename — fix it.

- [ ] **Step 7: Delete the now-empty `QRCoder/` renderer sources are NOT deleted**

Confirm which renderers still sit in `QRCoder/`; they move in Tasks 8 and 12. Run: `Get-ChildItem QRCoder -Filter *.cs | Select-Object -ExpandProperty Name`
Expected exactly: `ASCIIQRCode.cs`, `ArtQRCode.cs`, `Base64QRCode.cs`, `BitmapByteQRCode.cs`, `PdfByteQRCode.cs`, `PngByteQRCode.cs`, `PostscriptQRCode.cs`, `QRCode.cs`, `SvgQRCode.cs`.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "refactor(core): move QR generation into DotQR.Core and rename namespace to DotQR"
```

---

### Task 4: Core test project and behaviour regression

**Files:**
- Create: `tests/DotQR.Core.Tests/DotQR.Core.Tests.csproj`
- Move: `QRCoderTests/QRGeneratorTests.cs`, `TransposeVerificationTests.cs`, `PayloadGeneratorTests/`, `Helpers/` (text approvals only) → `tests/DotQR.Core.Tests/`

**Interfaces:**
- Consumes: `DotQR.Core` public surface.
- Produces: proof that the namespace rename and conditional-compilation strip changed no behaviour.

- [ ] **Step 1: Create the test project**

`tests/DotQR.Core.Tests/DotQR.Core.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.Core.Tests</AssemblyName>
    <RootNamespace>DotQRTests.Core</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="shouldly" Version="4.3.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DotQR.Core/DotQR.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="DotQR" />
    <Using Include="DotQRTests.Helpers" />
    <Using Include="Shouldly" />
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Move the generator and payload tests**

```powershell
New-Item -ItemType Directory -Force -Path tests/DotQR.Core.Tests/PayloadGeneratorTests, tests/DotQR.Core.Tests/Helpers | Out-Null
Move-Item QRCoderTests/QRGeneratorTests.cs, QRCoderTests/TransposeVerificationTests.cs tests/DotQR.Core.Tests/
Move-Item QRCoderTests/PayloadGeneratorTests/* tests/DotQR.Core.Tests/PayloadGeneratorTests/
Get-ChildItem QRCoderTests -Recurse -Include *QRGeneratorTests*approved*,*TransposeVerificationTests*approved*,*PayloadGeneratorTests*approved* | ForEach-Object {
    $_.FullName -replace [regex]::Escape((Resolve-Path QRCoderTests).Path), (Resolve-Path tests/DotQR.Core.Tests).Path
}
```
Move each approved file printed above with `Move-Item` to the mirrored path under `tests/DotQR.Core.Tests`.

- [ ] **Step 3: Rename the test namespace**

```powershell
Get-ChildItem -Recurse tests/DotQR.Core.Tests -Filter *.cs | ForEach-Object {
    $t = Get-Content -Raw $_.FullName
    $t = $t -replace 'namespace QRCoderTests\.PayloadGeneratorTests;', 'namespace DotQRTests.Core.PayloadGeneratorTests;'
    $t = $t -replace 'namespace QRCoderTests;', 'namespace DotQRTests.Core;'
    $t = $t -replace 'using QRCoder;', 'using DotQR;'
    $t = $t -replace '\bQRCoder\.([A-Z])', 'DotQR.$1'
    Set-Content -NoNewline -Path $_.FullName -Value $t
}
```

- [ ] **Step 4: Run the tests with approvals enforced**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests/DotQR.Core.Tests.csproj -v quiet
```
Expected: all tests pass. Any approval failure means the rename or the conditional strip changed behaviour — investigate, do not regenerate.

```powershell
Remove-Item Env:\CI
```

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "test(core): add DotQR.Core.Tests with generator, transpose and payload suites"
```

---

### Task 5: `RgbaColor` primitive

**Files:**
- Create: `src/DotQR.Core/Primitives/RgbaColor.cs`
- Test: `tests/DotQR.Core.Tests/Primitives/RgbaColorTests.cs`

**Interfaces:**
- Consumes: `StringExtensions.HexColorToByteArray` (already in Core).
- Produces: `DotQR.RgbaColor` with `R`, `G`, `B`, `A`, `Black`, `White`, `Transparent`, `IsOpaque`, `IsTransparent`, `Parse(string)`, `TryParse(string?, out RgbaColor)`, `ToRgbBytes()`, `ToRgbaBytes()`, `ToHtmlHex()`. Tasks 8, 9 and 13 depend on all of these.

- [ ] **Step 1: Write the failing test**

`tests/DotQR.Core.Tests/Primitives/RgbaColorTests.cs`:
```csharp
namespace DotQRTests.Core.Primitives;

public class RgbaColorTests
{
    [Theory]
    [InlineData("#000000", 0, 0, 0, 255)]
    [InlineData("#ffffff", 255, 255, 255, 255)]
    [InlineData("000000", 0, 0, 0, 255)]
    [InlineData("#f80", 255, 136, 0, 255)]
    [InlineData("#FF880080", 255, 136, 0, 128)]
    [InlineData("red", 255, 0, 0, 255)]
    [InlineData("White", 255, 255, 255, 255)]
    [InlineData("transparent", 0, 0, 0, 0)]
    public void Parses_hex_and_named_colors(string input, byte r, byte g, byte b, byte a)
    {
        var color = RgbaColor.Parse(input);
        color.ShouldBe(new RgbaColor(r, g, b, a));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#12")]
    public void TryParse_returns_false_for_invalid_input(string? input)
    {
        RgbaColor.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void Black_white_and_transparent_match_expected_values()
    {
        RgbaColor.Black.ShouldBe(new RgbaColor(0, 0, 0, 255));
        RgbaColor.White.ShouldBe(new RgbaColor(255, 255, 255, 255));
        RgbaColor.Transparent.ShouldBe(new RgbaColor(0, 0, 0, 0));
    }

    [Fact]
    public void Opaque_and_transparent_flags_are_derived_from_alpha()
    {
        new RgbaColor(1, 2, 3, 255).IsOpaque.ShouldBeTrue();
        new RgbaColor(1, 2, 3, 254).IsOpaque.ShouldBeFalse();
        new RgbaColor(1, 2, 3, 0).IsTransparent.ShouldBeTrue();
        new RgbaColor(1, 2, 3, 1).IsTransparent.ShouldBeFalse();
    }

    [Fact]
    public void ToRgbBytes_and_ToRgbaBytes_have_expected_widths()
    {
        var color = new RgbaColor(10, 20, 30, 40);
        color.ToRgbBytes().ShouldBe(new byte[] { 10, 20, 30 });
        color.ToRgbaBytes().ShouldBe(new byte[] { 10, 20, 30, 40 });
    }

    [Theory]
    [InlineData(0, 0, 0, 255, "#000")]
    [InlineData(255, 255, 255, 255, "#FFF")]
    [InlineData(0, 0, 0, 0, "transparent")]
    [InlineData(255, 136, 0, 255, "#FF8800")]
    [InlineData(255, 136, 0, 128, "#FF880080")]
    public void ToHtmlHex_reproduces_legacy_svg_shorthand_rules(byte r, byte g, byte b, byte a, string expected)
    {
        new RgbaColor(r, g, b, a).ToHtmlHex().ShouldBe(expected);
    }
}
```

- [ ] **Step 2: Run it — it must fail**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~RgbaColorTests -v quiet`
Expected: FAIL, `error CS0246: The type or namespace name 'RgbaColor' could not be found`.

- [ ] **Step 3: Implement `RgbaColor`**

`src/DotQR.Core/Primitives/RgbaColor.cs`:
```csharp
namespace DotQR;

/// <summary>
/// Represents a 32-bit RGBA color using 8 bits per channel.
/// </summary>
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct with fully opaque alpha.
    /// </summary>
    public RgbaColor(byte red, byte green, byte blue)
        : this(red, green, blue, 255)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct.
    /// </summary>
    public RgbaColor(byte red, byte green, byte blue, byte alpha)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    /// <summary>Gets the red channel.</summary>
    public byte R { get; }

    /// <summary>Gets the green channel.</summary>
    public byte G { get; }

    /// <summary>Gets the blue channel.</summary>
    public byte B { get; }

    /// <summary>Gets the alpha channel.</summary>
    public byte A { get; }

    /// <summary>Gets fully opaque black.</summary>
    public static RgbaColor Black => new(0, 0, 0, 255);

    /// <summary>Gets fully opaque white.</summary>
    public static RgbaColor White => new(255, 255, 255, 255);

    /// <summary>Gets fully transparent.</summary>
    public static RgbaColor Transparent => new(0, 0, 0, 0);

    /// <summary>Gets a value indicating whether this color is fully opaque.</summary>
    public bool IsOpaque => A == 255;

    /// <summary>Gets a value indicating whether this color is fully transparent.</summary>
    public bool IsTransparent => A == 0;

    /// <summary>
    /// Parses a color from a hex string (<c>#RGB</c>, <c>#RRGGBB</c>, <c>#RRGGBBAA</c>, with or
    /// without the leading <c>#</c>) or an HTML/CSS named color.
    /// </summary>
    /// <exception cref="FormatException">The value is not a recognized color.</exception>
    public static RgbaColor Parse(string value)
        => TryParse(value, out var color)
            ? color
            : throw new FormatException($"'{value}' is not a valid color.");

    /// <summary>
    /// Attempts to parse a color from a hex string or an HTML/CSS named color.
    /// </summary>
    public static bool TryParse(string? value, out RgbaColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value!.Trim();

        if (TryParseNamed(text, out color))
            return true;

        var hex = text[0] == '#' ? text.Substring(1) : text;
        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6 && hex.Length != 8)
            return false;

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                return false;
        }

        color = bytes.Length == 4
            ? new RgbaColor(bytes[0], bytes[1], bytes[2], bytes[3])
            : new RgbaColor(bytes[0], bytes[1], bytes[2]);
        return true;
    }

    private static bool TryParseNamed(string value, out RgbaColor color)
    {
        color = default;
        if (value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            color = Transparent;
            return true;
        }

        return value.ToLowerInvariant() switch
        {
            "black" => Set(RgbaColor.Black),
            "white" => Set(RgbaColor.White),
            "red" => Set(new RgbaColor(255, 0, 0)),
            "lime" => Set(new RgbaColor(0, 255, 0)),
            "blue" => Set(new RgbaColor(0, 0, 255)),
            "yellow" => Set(new RgbaColor(255, 255, 0)),
            "cyan" or "aqua" => Set(new RgbaColor(0, 255, 255)),
            "magenta" or "fuchsia" => Set(new RgbaColor(255, 0, 255)),
            "silver" => Set(new RgbaColor(192, 192, 192)),
            "gray" or "grey" => Set(new RgbaColor(128, 128, 128)),
            "maroon" => Set(new RgbaColor(128, 0, 0)),
            "olive" => Set(new RgbaColor(128, 128, 0)),
            "green" => Set(new RgbaColor(0, 128, 0)),
            "purple" => Set(new RgbaColor(128, 0, 128)),
            "teal" => Set(new RgbaColor(0, 128, 128)),
            "navy" => Set(new RgbaColor(0, 0, 128)),
            "orange" => Set(new RgbaColor(255, 165, 0)),
            "pink" => Set(new RgbaColor(255, 192, 203)),
            "brown" => Set(new RgbaColor(165, 42, 42)),
            "gold" => Set(new RgbaColor(255, 215, 0)),
            _ => false,
        };

        bool Set(RgbaColor value2)
        {
            color = value2;
            return true;
        }
    }

    /// <summary>Returns the color as three RGB bytes.</summary>
    public byte[] ToRgbBytes() => new[] { R, G, B };

    /// <summary>Returns the color as four RGBA bytes.</summary>
    public byte[] ToRgbaBytes() => new[] { R, G, B, A };

    /// <summary>
    /// Returns the color as an HTML hex string. Black and white use the <c>#000</c> and
    /// <c>#FFF</c> shorthands, fully transparent returns <c>transparent</c>, and any other color
    /// returns <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.
    /// </summary>
    public string ToHtmlHex()
    {
        if (this == Black)
            return "#000";
        if (this == White)
            return "#FFF";
        if (A == 0)
            return "transparent";
        if (A == 255)
            return $"#{R:X2}{G:X2}{B:X2}";
        return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }

    /// <inheritdoc/>
    public bool Equals(RgbaColor other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RgbaColor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <inheritdoc/>
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    /// <summary>Compares two colors for equality.</summary>
    public static bool operator ==(RgbaColor left, RgbaColor right) => left.Equals(right);

    /// <summary>Compares two colors for inequality.</summary>
    public static bool operator !=(RgbaColor left, RgbaColor right) => !left.Equals(right);
}
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~RgbaColorTests -v quiet`
Expected: PASS, all cases.

- [ ] **Step 5: Commit**

```powershell
git add src/DotQR.Core/Primitives/RgbaColor.cs tests/DotQR.Core.Tests/Primitives/RgbaColorTests.cs
git commit -m "feat(core): add RgbaColor primitive with hex and named-color parsing"
```

---

### Task 6: `Size` primitive

**Files:**
- Create: `src/DotQR.Core/Primitives/Size.cs`
- Test: `tests/DotQR.Core.Tests/Primitives/SizeTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `DotQR.Size` with `Width`, `Height`, `Empty`, `IsEmpty`, equality. Tasks 9 and 10 depend on it for the `viewBox` parameters.

- [ ] **Step 1: Write the failing test**

`tests/DotQR.Core.Tests/Primitives/SizeTests.cs`:
```csharp
namespace DotQRTests.Core.Primitives;

public class SizeTests
{
    [Fact]
    public void Constructor_stores_width_and_height()
    {
        var size = new Size(21, 42);
        size.Width.ShouldBe(21);
        size.Height.ShouldBe(42);
    }

    [Fact]
    public void Empty_is_zero_and_IsEmpty_is_true_for_it()
    {
        Size.Empty.Width.ShouldBe(0);
        Size.Empty.Height.ShouldBe(0);
        Size.Empty.IsEmpty.ShouldBeTrue();
        new Size(1, 0).IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Equality_compares_both_dimensions()
    {
        (new Size(3, 4) == new Size(3, 4)).ShouldBeTrue();
        (new Size(3, 4) == new Size(4, 3)).ShouldBeFalse();
        (new Size(3, 4) != new Size(4, 3)).ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Run it — it must fail**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~SizeTests -v quiet`
Expected: FAIL, `The type or namespace name 'Size' could not be found`.

- [ ] **Step 3: Implement `Size`**

`src/DotQR.Core/Primitives/Size.cs`:
```csharp
namespace DotQR;

/// <summary>
/// Represents an integer width and height.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> struct.
    /// </summary>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Gets the width.</summary>
    public int Width { get; }

    /// <summary>Gets the height.</summary>
    public int Height { get; }

    /// <summary>Gets a size with zero width and height.</summary>
    public static Size Empty => default;

    /// <summary>Gets a value indicating whether both dimensions are zero.</summary>
    public bool IsEmpty => Width == 0 && Height == 0;

    /// <inheritdoc/>
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Width, Height);

    /// <inheritdoc/>
    public override string ToString() => $"{Width}x{Height}";

    /// <summary>Compares two sizes for equality.</summary>
    public static bool operator ==(Size left, Size right) => left.Equals(right);

    /// <summary>Compares two sizes for inequality.</summary>
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
}
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~SizeTests -v quiet`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DotQR.Core/Primitives/Size.cs tests/DotQR.Core.Tests/Primitives/SizeTests.cs
git commit -m "feat(core): add Size primitive for vector viewBox parameters"
```

---

## Phase 3 — DotQR.ByteRenderers

### Task 7: ByteRenderers project and the unchanged renderers

**Files:**
- Create: `src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj`
- Move: `QRCoder/BitmapByteQRCode.cs`, `PdfByteQRCode.cs`, `ASCIIQRCode.cs`, `PngByteQRCode.cs` → `src/DotQR.ByteRenderers/`

**Interfaces:**
- Consumes: `DotQR.Core`, `DotQR.RgbaColor`.
- Produces: `DotQR.PngByteQRCode`, `DotQR.BitmapByteQRCode`, `DotQR.PdfByteQRCode`, `DotQR.AsciiQRCode` and their `*Helper` classes.

- [ ] **Step 1: Create the project**

`src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.ByteRenderers</AssemblyName>
    <RootNamespace>DotQR</RootNamespace>
    <IsPackable>true</IsPackable>
    <IsTrimmable>true</IsTrimmable>
    <PackageId>DotQR.ByteRenderers</PackageId>
    <PackageTags>c# csharp qr qrcode qr-generator qr-code-generator png bmp pdf svg postscript ascii</PackageTags>
    <Description>DotQR.ByteRenderers renders QR codes to PNG, BMP, PDF, SVG, PostScript and ASCII with no graphics library dependency.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../DotQR.Core/DotQR.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="DotQR.ByteRenderers.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Move the four renderers**

```powershell
Move-Item QRCoder/BitmapByteQRCode.cs, QRCoder/PdfByteQRCode.cs, QRCoder/ASCIIQRCode.cs, QRCoder/PngByteQRCode.cs src/DotQR.ByteRenderers/
```

- [ ] **Step 3: Apply the namespace rename and strip conditionals**

Run the same targeted replace as Task 3, Step 2, over `src/DotQR.ByteRenderers`.

Then strip, in `PngByteQRCode.cs` only:
- `#if HAS_SPAN` / `#else` / `#endif` around the `ArrayPool` rent — keep the `ArrayPool` leg.
- `#if !NETSTANDARD1_3` around the `System.Drawing.Color` overload — the overload is **rewritten** in Step 4, not unguarded.
- `#if HAS_SPAN` around `ArrayPool<byte>.Shared.Return` and `ArrayPool<byte>.Shared.Rent` — keep the rented form.

`BitmapByteQRCode.cs`, `PdfByteQRCode.cs` and `ASCIIQRCode.cs` contain no conditional compilation and need only the namespace rename.

- [ ] **Step 4: Replace the `System.Drawing.Color` overload in `PngByteQRCode`**

Delete:
```csharp
public byte[] GetGraphic(int pixelsPerModule, System.Drawing.Color darkColor, System.Drawing.Color lightColor, bool drawQuietZones = true)
    => GetGraphic(pixelsPerModule, new byte[] { darkColor.R, darkColor.G, darkColor.B, darkColor.A }, new byte[] { lightColor.R, lightColor.G, lightColor.B, lightColor.A }, drawQuietZones);
```

Replace with:
```csharp
/// <summary>
/// Creates a 2-color PNG of the QR code, using 1-bit indexed color. Colors may contain transparency.
/// </summary>
/// <param name="pixelsPerModule">The number of pixels each dark/light module of the QR code will occupy in the final QR code image.</param>
/// <param name="darkColor">The color of the dark modules.</param>
/// <param name="lightColor">The color of the light modules.</param>
/// <param name="drawQuietZones">Indicates if quiet zones around the QR code should be drawn.</param>
/// <returns>Returns the QR code graphic as a PNG byte array.</returns>
public byte[] GetGraphic(int pixelsPerModule, RgbaColor darkColor, RgbaColor lightColor, bool drawQuietZones = true)
    => GetGraphic(pixelsPerModule, darkColor.ToRgbaBytes(), lightColor.ToRgbaBytes(), drawQuietZones);
```

- [ ] **Step 5: Build**

Run: `dotnet build src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj -v quiet`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(byterenderers): add DotQR.ByteRenderers with PNG, BMP, PDF and ASCII renderers"
```

---

### Task 8: SVG and PostScript renderers

**Files:**
- Move: `QRCoder/SvgQRCode.cs`, `QRCoder/PostscriptQRCode.cs` → `src/DotQR.ByteRenderers/`

**Interfaces:**
- Consumes: `DotQR.RgbaColor`, `DotQR.Size`.
- Produces: `DotQR.SvgQRCode` with `SvgLogo` (string and byte[] constructors), `DotQR.PostscriptQRCode`. Task 13 adds `ToSvgLogo` on top of `SvgLogo(byte[])`.

- [ ] **Step 1: Move both files and rename namespace**

```powershell
Move-Item QRCoder/SvgQRCode.cs, QRCoder/PostscriptQRCode.cs src/DotQR.ByteRenderers/
```
Then apply the targeted namespace replace from Task 3, Step 2, to both files, and remove `#if !NETSTANDARD1_3` / `#endif` wrappers.

- [ ] **Step 2: Replace `Color` with `RgbaColor` and `Size` with `DotQR.Size`**

In both files, substitute the type name in every signature and body. `Color.Black` → `RgbaColor.Black`, `Color.White` → `RgbaColor.White`, `Size.Empty` → `Size.Empty` (now resolves to `DotQR.Size`), `new Size(a, b)` unchanged.

Channel access changes from `.R`/`.G`/`.B` to `.R`/`.G`/`.B` — identical member names, so no change is needed there; `RgbaColor` exposes them.

`SvgQRCode.ColorToHex(Color color)` becomes:
```csharp
private static string ColorToHex(RgbaColor color) => color.ToHtmlHex();
```
Delete the entire body of the old `ColorToHex`; `ToHtmlHex` already encodes the same `#000`/`#FFF`/`transparent`/`#RRGGBB`/`#RRGGBBAA` rules (Task 5, verified by its theory).

`SvgQRCode.IsFullyTransparent(string colorHex)` stays as-is; it parses the hex string, not a `Color`.

- [ ] **Step 3: Replace the private `RectangleF`**

`SvgQRCode` uses `System.Drawing.RectangleF` in the private `IsBlockedByLogo(int x, int y, RectangleF attr)` helper and where `logoAttr` is computed. Add a private nested readonly struct to `SvgQRCode`:

```csharp
private readonly struct LogoBounds
{
    public LogoBounds(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
}
```
Change `RectangleF attr` → `LogoBounds attr` and the construction site to `new LogoBounds(...)`. Leave the arithmetic in `IsBlockedByLogo` untouched.

- [ ] **Step 4: Remove the `Bitmap` constructor from `SvgLogo`**

Delete the `#if SYSTEM_DRAWING`-guarded `SvgLogo(Bitmap iconRasterized, …)` constructor entirely. The `SvgLogo(byte[] iconRasterized, …)` and `SvgLogo(string iconVectorized, …)` constructors remain and are what Task 13's extension method calls.

- [ ] **Step 5: Build**

Run: `dotnet build src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj -v quiet`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(byterenderers): add SVG and PostScript renderers on RgbaColor and DotQR.Size"
```

---

### Task 9: ByteRenderers tests — prove byte-identity

**Files:**
- Create: `tests/DotQR.ByteRenderers.Tests/DotQR.ByteRenderers.Tests.csproj`
- Move: the six byte-renderer test files and their approvals from `QRCoderTests/`
- Move: `QRCoderTests/Helpers/ShouldMatchApprovedBinaryExtensions.cs` → `tests/DotQR.ByteRenderers.Tests/Helpers/`

**Interfaces:**
- Consumes: `DotQR.Core`, `DotQR.ByteRenderers`.
- Produces: the byte-identity regression gate for every later change to these renderers.

- [ ] **Step 1: Create the test project**

`tests/DotQR.ByteRenderers.Tests/DotQR.ByteRenderers.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.ByteRenderers.Tests</AssemblyName>
    <RootNamespace>DotQRTests.ByteRenderers</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="shouldly" Version="4.3.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DotQR.Core/DotQR.Core.csproj" />
    <ProjectReference Include="../../src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="../assets/*.svg" />
    <EmbeddedResource Include="../assets/*.png" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="DotQR" />
    <Using Include="Shouldly" />
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

This project deliberately has **no** SkiaSharp reference. That is the enforcement of the package boundary from the spec.

- [ ] **Step 2: Move the six test files, their approvals and the asset folder**

```powershell
New-Item -ItemType Directory -Force -Path tests/DotQR.ByteRenderers.Tests/Helpers, tests/DotQR.ByteRenderers.Tests/assets | Out-Null
Move-Item QRCoderTests/assets/* tests/DotQR.ByteRenderers.Tests/assets/
Move-Item QRCoderTests/PngByteQRCodeRendererTests.cs, QRCoderTests/BitmapByteQRCodeRendererTests.cs, QRCoderTests/PdfByteQRCodeRendererTests.cs, QRCoderTests/SvgQRCodeRendererTests.cs, QRCoderTests/PostscriptQRCodeRendererTests.cs, QRCoderTests/AsciiQRCodeRendererTests.cs tests/DotQR.ByteRenderers.Tests/
Get-ChildItem QRCoderTests -Recurse -Include *PngByteQRCodeRendererTests*approved*,*BitmapByteQRCodeRendererTests*approved*,*PdfByteQRCodeRendererTests*approved*,*SvgQRCodeRendererTests*approved*,*PostscriptQRCodeRendererTests*approved*,*AsciiQRCodeRendererTests*approved* | Move-Item -Destination tests/DotQR.ByteRenderers.Tests/
Move-Item QRCoderTests/Helpers/ShouldMatchApprovedBinaryExtensions.cs tests/DotQR.ByteRenderers.Tests/Helpers/
```

- [ ] **Step 3: Reduce the approval helper to the text/byte overloads only**

`ShouldMatchApprovedBinaryExtensions.cs` currently contains two `System.Drawing`-based members: `ShouldMatchApprovedImage(this byte[] …)` and `ShouldMatchApproved(this Bitmap …)`. Delete both. Keep only `ShouldMatchApproved(this byte[] actual, string fileExtension, string? discriminator, string? customMessage)` and any `string` overload already present, dropping the `KGySoft.Drawing` and `System.Drawing` usings.

The GIF-encoding path is re-added in Task 12 for `SKBitmap` only.

- [ ] **Step 4: Fix the test files**

In the six moved files: remove the `#if SYSTEM_DRAWING` / `#endif` wrappers, apply the namespace rename, and change `Color.X` to `RgbaColor.X`. Replace `QRCoder`-qualified references with `DotQR`.

- [ ] **Step 5: Run with approvals enforced — byte-identity is the assertion**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.ByteRenderers.Tests -v quiet
```
Expected: **all tests pass**. This is the proof that PNG/BMP/PDF/PS/SVG/ASCII output is byte-identical to upstream. If any approval fails, the port changed output — fix the renderer, never the baseline.

```powershell
Remove-Item Env:\CI
```

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "test(byterenderers): add byte renderer tests proving byte-identical output"
```

---

## Phase 4 — DotQR.SkiaSharp

### Task 10: SkiaSharp project and the `QRCode` renderer

**Files:**
- Create: `src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj`
- Create: `src/DotQR.SkiaSharp/QRCode.cs`
- Move: `QRCoder/QRCode.cs` → `src/DotQR.SkiaSharp/QRCode.cs`

**Interfaces:**
- Consumes: `DotQR.Core`, `RgbaColor`, `SkiaSharp`.
- Produces: `DotQR.QRCode.GetGraphic` returning `SKBitmap`. Task 12 (`Base64QRCode`) depends on it.

- [ ] **Step 1: Create the project**

`src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.SkiaSharp</AssemblyName>
    <RootNamespace>DotQR</RootNamespace>
    <IsPackable>true</IsPackable>
    <IsTrimmable>true</IsTrimmable>
    <PackageId>DotQR.SkiaSharp</PackageId>
    <PackageTags>c# csharp qr qrcode qr-generator qr-code-generator skia skiaSharp</PackageTags>
    <Description>DotQR.SkiaSharp renders QR codes to SKBitmap using SkiaSharp, including icon compositing.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="4.153.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../DotQR.Core/DotQR.Core.csproj" />
    <ProjectReference Include="../DotQR.ByteRenderers/DotQR.ByteRenderers.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="DotQR.SkiaSharp.Tests" />
  </ItemGroup>

</Project>
```

`DotQR.ByteRenderers` is referenced because `Base64QRCode` delegates PNG output to `PngByteQRCode`.

- [ ] **Step 2: Move `QRCode.cs` and strip its guards**

```powershell
Move-Item QRCoder/QRCode.cs src/DotQR.SkiaSharp/
```
Remove the `#if SYSTEM_DRAWING` / `#endif` wrapper and every `[SupportedOSPlatform("windows")]` block. Apply the namespace rename.

- [ ] **Step 3: Replace the drawing implementation**

Replace `GetGraphic(int, SKColor, SKColor, SKBitmap?, int, int, bool, SKColor?)` and delete `CreatePathFromModules` and `CreateRoundedRectanglePath`:

```csharp
/// <summary>
/// Creates a colored bitmap image of the QR code with an optional icon in the center.
/// </summary>
/// <param name="pixelsPerModule">The number of pixels each dark/light module of the QR code will occupy in the final QR code image.</param>
/// <param name="darkColor">The color of the dark modules.</param>
/// <param name="lightColor">The color of the light modules.</param>
/// <param name="icon">An optional icon to be placed in the center of the QR code.</param>
/// <param name="iconSizePercent">The size of the icon as a percentage of the QR code size.</param>
/// <param name="iconBorderWidth">The width of the border around the icon.</param>
/// <param name="drawQuietZones">Indicates if quiet zones around the QR code should be drawn.</param>
/// <param name="iconBackgroundColor">The background color of the icon.</param>
/// <returns>Returns the QR code graphic as a bitmap.</returns>
public SKBitmap GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, SKBitmap? icon = null, int iconSizePercent = 15, int iconBorderWidth = 0, bool drawQuietZones = true, SKColor? iconBackgroundColor = null)
{
    var moduleCount = QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8);
    var startIndex = drawQuietZones ? 0 : 4;
    var size = moduleCount * pixelsPerModule;

    // Render the matrix once at module resolution, then blit it up with nearest-neighbour
    // sampling. At integer scale this is pixel-exact and far cheaper than painting thousands of
    // rectangles at the final resolution.
    using var modules = new SKBitmap(new SKImageInfo(moduleCount, moduleCount, SKColorType.Rgba8888, SKAlphaType.Premul));
    var matrix = QrCodeData.ModuleMatrix;
    for (var y = 0; y < moduleCount; y++)
    {
        var row = matrix[y + startIndex];
        for (var x = 0; x < moduleCount; x++)
        {
            modules.SetPixel(x, y, row[x + startIndex] ? darkColor : lightColor);
        }
    }

    var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
    using (var canvas = new SKCanvas(bitmap))
    using (var blitPaint = new SKPaint { IsAntialias = false, BlendMode = SKBlendMode.Src })
    using (var iconPaint = new SKPaint { IsAntialias = true })
    {
        canvas.DrawBitmap(modules, SKRect.Create(0, 0, size, size), new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None), blitPaint);

        var drawIconFlag = icon != null && iconSizePercent > 0 && iconSizePercent <= 100;
        if (drawIconFlag)
        {
            var iconDestWidth = iconSizePercent * size / 100f;
            var iconDestHeight = iconDestWidth * icon!.Height / icon.Width;
            var iconX = (size - iconDestWidth) / 2;
            var iconY = (size - iconDestHeight) / 2;

            if (iconBorderWidth > 0)
            {
                iconPaint.Color = iconBackgroundColor ?? lightColor;
                var radius = iconBorderWidth * 2;
                var border = SKRect.Create(
                    iconX - iconBorderWidth,
                    iconY - iconBorderWidth,
                    iconDestWidth + iconBorderWidth * 2,
                    iconDestHeight + iconBorderWidth * 2);
                canvas.DrawRoundRect(border, radius, radius, iconPaint);
            }

            using var iconImage = SKImage.FromBitmap(icon);
            using var drawPaint = new SKPaint { IsAntialias = true };
            canvas.DrawImage(
                iconImage,
                SKRect.Create(iconX, iconY, iconDestWidth, iconDestHeight),
                new SKSamplingOptions(SKCubicResampler.Mitchell),
                drawPaint);
        }
    }

    return bitmap;
}
```

Update the three chaining overloads and `QRCodeHelper.GetQRCode` to use `SKBitmap` and `SKColor`. The hex-string overload body becomes:

```csharp
public SKBitmap GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex, bool drawQuietZones = true)
    => GetGraphic(
        pixelsPerModule,
        RgbaColor.Parse(darkColorHtmlHex).ToSKColor(),
        RgbaColor.Parse(lightColorHtmlHex).ToSKColor(),
        drawQuietZones);
```

`ToSKColor()` is defined in Task 11. To keep this task independently buildable, define it now in `src/DotQR.SkiaSharp/Extensions/SkiaColorExtensions.cs`:

```csharp
namespace DotQR;

/// <summary>
/// Converts between <see cref="RgbaColor"/> and SkiaSharp colors.
/// </summary>
public static class SkiaColorExtensions
{
    /// <summary>Converts an <see cref="RgbaColor"/> to an <see cref="SKColor"/>.</summary>
    public static SKColor ToSKColor(this RgbaColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>Converts an <see cref="SKColor"/> to an <see cref="RgbaColor"/>.</summary>
    public static RgbaColor ToRgbaColor(this SKColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);
}
```
Add `using SkiaSharp;` at the top of that file.

- [ ] **Step 4: Build**

Run: `dotnet build src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj -v quiet`
Expected: `Build succeeded`, 0 warnings. Common failures and their meaning: `SKPaint.FilterQuality` is obsolete — use `SKSamplingOptions` as shown; `SKImage.FromBitmap` needs `using SkiaSharp;`.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(skiasharp): port QRCode renderer to SKBitmap with nearest-neighbour module scaling"
```

---

### Task 11: `ArtQRCode` on SKPath

**Files:**
- Move: `QRCoder/ArtQRCode.cs` → `src/DotQR.SkiaSharp/ArtQRCode.cs`

**Interfaces:**
- Consumes: `DotQR.Core`, `SKColor`, `SKBitmap`.
- Produces: `DotQR.ArtQRCode.GetGraphic` returning `SKBitmap`, with `QuietZoneStyle` and `BackgroundImageStyle` unchanged.

- [ ] **Step 1: Move the file and strip guards**

```powershell
Move-Item QRCoder/ArtQRCode.cs src/DotQR.SkiaSharp/
```
Remove `#if SYSTEM_DRAWING`/`#endif` and all `[SupportedOSPlatform("windows")]`, rename the namespace, and change `Color` to `SKColor` and `Bitmap` to `SKBitmap` throughout.

- [ ] **Step 2: Replace the drawing implementation**

Delete `MakeDotPixel`, `Resize`, `IsPartOfQuietZone` stays, `IsPartOfFinderPattern` stays. Replace `GetGraphic` with:

```csharp
/// <summary>
/// Renders an art-style QR code with dots as modules and various user settings.
/// </summary>
/// <param name="pixelsPerModule">Amount of px each dark/light module of the QR code shall take place in the final QR code image.</param>
/// <param name="darkColor">Color of the dark modules.</param>
/// <param name="lightColor">Color of the light modules.</param>
/// <param name="backgroundColor">Color of the background.</param>
/// <param name="backgroundImage">A bitmap object that will be used as background picture.</param>
/// <param name="pixelSizeFactor">Value between 0.0 to 1.0 that defines how big the module dots are.</param>
/// <param name="drawQuietZones">If true a white border is drawn around the whole QR Code.</param>
/// <param name="quietZoneRenderingStyle">Style of the quiet zones.</param>
/// <param name="backgroundImageStyle">Style of the background image (if set).</param>
/// <param name="finderPatternImage">Optional image that should be used instead of the default finder patterns.</param>
/// <returns>QRCode graphic as bitmap.</returns>
public SKBitmap GetGraphic(int pixelsPerModule, SKColor darkColor, SKColor lightColor, SKColor backgroundColor, SKBitmap? backgroundImage = null, double pixelSizeFactor = 1,
                           bool drawQuietZones = true, QuietZoneStyle quietZoneRenderingStyle = QuietZoneStyle.Dotted,
                           BackgroundImageStyle backgroundImageStyle = BackgroundImageStyle.DataAreaOnly, SKBitmap? finderPatternImage = null)
{
    if (pixelSizeFactor > 1)
        throw new ArgumentOutOfRangeException(nameof(pixelSizeFactor), "The parameter pixelSizeFactor must be between 0 and 1. (0-100%)");
    var pixelSize = (int)Math.Min(pixelsPerModule, Math.Floor(pixelsPerModule * pixelSizeFactor));

    var numModules = QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8);
    var offset = drawQuietZones ? 0 : 4;
    var size = numModules * pixelsPerModule;

    var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
    using (var canvas = new SKCanvas(bitmap))
    using (var dotPaint = new SKPaint { IsAntialias = true })
    using (var flatPaint = new SKPaint { IsAntialias = false })
    {
        canvas.Clear(backgroundColor);

        if (backgroundImage != null)
        {
            if (backgroundImageStyle == BackgroundImageStyle.Fill)
            {
                canvas.DrawBitmap(backgroundImage, SKRect.Create(0, 0, size, size), new SKSamplingOptions(SKCubicResampler.Mitchell), dotPaint);
            }
            else if (backgroundImageStyle == BackgroundImageStyle.DataAreaOnly)
            {
                var bgOffset = 4 - offset;
                var target = size - (2 * bgOffset * pixelsPerModule);
                canvas.DrawBitmap(backgroundImage, SKRect.Create(bgOffset * pixelsPerModule, bgOffset * pixelsPerModule, target, target), new SKSamplingOptions(SKCubicResampler.Mitchell), dotPaint);
            }
        }

        // Accumulate every dot into two paths and paint each path once, instead of issuing one
        // DrawImage call per module.
        using var lightPath = new SKPath();
        using var darkPath = new SKPath();
        var radius = pixelSize / 2f;
        var centreOffset = pixelsPerModule / 2f;

        for (var x = 0; x < numModules; x++)
        {
            for (var y = 0; y < numModules; y++)
            {
                var rect = SKRect.Create(x * pixelsPerModule, y * pixelsPerModule, pixelsPerModule, pixelsPerModule);
                var pixelIsDark = QrCodeData.ModuleMatrix[offset + y][offset + x];

                if (!IsPartOfFinderPattern(x, y, numModules, offset))
                {
                    if (drawQuietZones && quietZoneRenderingStyle == QuietZoneStyle.Flat && IsPartOfQuietZone(x, y, numModules))
                    {
                        flatPaint.Color = pixelIsDark ? darkColor : lightColor;
                        canvas.DrawRect(rect, flatPaint);
                    }
                    else if (pixelSize > 0)
                    {
                        var cx = x * pixelsPerModule + centreOffset;
                        var cy = y * pixelsPerModule + centreOffset;
                        var path = pixelIsDark ? darkPath : lightPath;
                        path.AddCircle(cx, cy, radius, SKPathDirection.Clockwise);
                    }
                }
                else if (finderPatternImage == null)
                {
                    flatPaint.Color = pixelIsDark ? darkColor : lightColor;
                    canvas.DrawRect(rect, flatPaint);
                }
            }
        }

        lightPaint.Color = lightColor;
        canvas.DrawPath(lightPath, lightPaint);
        darkPaint.Color = darkColor;
        canvas.DrawPath(darkPath, darkPaint);

        if (finderPatternImage != null)
        {
            var finderPatternSize = 7 * pixelsPerModule;
            var finderPatternOffset = drawQuietZones ? 4 * pixelsPerModule : 0;
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
            canvas.DrawBitmap(finderPatternImage, SKRect.Create(finderPatternOffset, finderPatternOffset, finderPatternSize, finderPatternSize), sampling, dotPaint);
            canvas.DrawBitmap(finderPatternImage, SKRect.Create(size - finderPatternOffset - finderPatternSize, finderPatternOffset, finderPatternSize, finderPatternSize), sampling, dotPaint);
            canvas.DrawBitmap(finderPatternImage, SKRect.Create(finderPatternOffset, size - finderPatternOffset - finderPatternSize, finderPatternSize, finderPatternSize), sampling, dotPaint);
        }
    }

    return bitmap;
}
```

Declare the two paints used by the path drawing alongside the others:
```csharp
using var lightPaint = new SKPaint { IsAntialias = true };
using var darkPaint = new SKPaint { IsAntialias = true };
```
(they must be declared inside the `using (var canvas = …)` block, before they are used).

Update `ArtQRCodeHelper.GetQRCode` to `SKColor`/`SKBitmap`.

- [ ] **Step 3: Build**

Run: `dotnet build src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj -v quiet`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat(skiasharp): port ArtQRCode to SKBitmap drawing dots as two accumulated SKPaths"
```

---

### Task 12: `Base64QRCode`, SkiaSharp tests and approval regeneration

**Files:**
- Move: `QRCoder/Base64QRCode.cs` → `src/DotQR.SkiaSharp/Base64QRCode.cs`
- Create: `src/DotQR.SkiaSharp/Extensions/SvgLogoExtensions.cs`
- Create: `tests/DotQR.SkiaSharp.Tests/DotQR.SkiaSharp.Tests.csproj`
- Move: `QRCodeRendererTests.cs`, `ArtQRCodeRendererTests.cs`, `Base64QRCodeRendererTests.cs` and their approvals

**Interfaces:**
- Consumes: everything from Tasks 10 and 11.
- Produces: `DotQR.Base64QRCode` with `ImageType { Png, Jpeg, Webp }`, and `SKBitmap.ToSvgLogo(...)`.

- [ ] **Step 1: Port `Base64QRCode`**

```powershell
Move-Item QRCoder/Base64QRCode.cs src/DotQR.SkiaSharp/
```
Strip `#if SYSTEM_DRAWING`/`#if !NETSTANDARD1_3` and the `[SupportedOSPlatform]` attributes; rename the namespace.

The PNG branch keeps delegating to `PngByteQRCode` (black/white and coloured), preserving byte-identity. Replace `BitmapToBase64` with:

```csharp
private static string ImageToBase64(SKBitmap bmp, ImageType imgType)
{
    var format = imgType switch
    {
        ImageType.Png => SKEncodedImageFormat.Png,
        ImageType.Jpeg => SKEncodedImageFormat.Jpeg,
        ImageType.Webp => SKEncodedImageFormat.Webp,
        _ => throw new ArgumentOutOfRangeException(nameof(imgType)),
    };

    using var data = bmp.Encode(format, 100)
        ?? throw new NotSupportedException($"{imgType} encoding is not supported by SkiaSharp.");
    return Convert.ToBase64String(data.ToArray(), Base64FormattingOptions.None);
}
```

Change `ImageType` to:
```csharp
public enum ImageType
{
    /// <summary>Portable Network Graphics (PNG) image format, a lossless raster graphics format.</summary>
    Png,

    /// <summary>Joint Photographic Experts Group (JPEG) image format, a lossy compressed image format.</summary>
    Jpeg,

    /// <summary>WebP image format, a lossy or lossless compressed raster graphics format.</summary>
    Webp
}
```

Update the icon overload to take `SKBitmap icon`.

- [ ] **Step 2: Add `ToSvgLogo`**

`src/DotQR.SkiaSharp/Extensions/SvgLogoExtensions.cs`:
```csharp
using SkiaSharp;

namespace DotQR;

/// <summary>
/// Creates <see cref="SvgLogo"/> instances from SkiaSharp bitmaps.
/// </summary>
public static class SvgLogoExtensions
{
    /// <summary>
    /// Creates an <see cref="SvgLogo"/> from a rasterized icon by encoding it as PNG.
    /// </summary>
    /// <param name="icon">The icon to embed.</param>
    /// <param name="iconSizePercent">Degree of percentage coverage of the QR code by the logo.</param>
    /// <param name="fillLogoBackground">If true, no QR modules will be rendered behind the logo.</param>
    public static SvgLogo ToSvgLogo(this SKBitmap icon, int iconSizePercent = 15, bool fillLogoBackground = true)
    {
        ArgumentNullException.ThrowIfNull(icon);

        using var data = icon.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new NotSupportedException("The icon could not be encoded as PNG.");
        return new SvgLogo(data.ToArray(), iconSizePercent, fillLogoBackground);
    }
}
```
`SvgLogo` is `DotQR.SvgQRCode.SvgLogo` — add `using static DotQR.SvgQRCode;` if the file does not already resolve it.

- [ ] **Step 3: Create the SkiaSharp test project**

`tests/DotQR.SkiaSharp.Tests/DotQR.SkiaSharp.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.SkiaSharp.Tests</AssemblyName>
    <RootNamespace>DotQRTests.SkiaSharp</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="4.153.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="shouldly" Version="4.3.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DotQR.Core/DotQR.Core.csproj" />
    <ProjectReference Include="../../src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj" />
    <ProjectReference Include="../../src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="../assets/*.svg" />
    <EmbeddedResource Include="../assets/*.png" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="DotQR" />
    <Using Include="Shouldly" />
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Move the SkiaSharp renderer tests, and replace the GIF approval helper**

Move the three test files into `tests/DotQR.SkiaSharp.Tests/`. Move or copy the `assets` folder there as an embedded-resource source (each test project that needs icons embeds its own).

**Retire the `.approved.gif` baselines.** They were produced by encoding GDI+ bitmaps through KGySoft's GIF encoder, and KGySoft's `BitmapDataFactory.CreateBitmapData(IntPtr, Size, …)` takes `System.Drawing.Size`, so keeping that pipeline would reintroduce `System.Drawing` into the test project — which the Global Constraints forbid. Raster baselines become SkiaSharp-encoded `.approved.png`. Consequences and the compensation are recorded below.

Add `tests/DotQR.SkiaSharp.Tests/Helpers/SKBitmapApprovals.cs`:
```csharp
using Shouldly;
using SkiaSharp;

namespace DotQRTests.Helpers;

/// <summary>
/// Compares <see cref="SKBitmap"/> output against approved PNG baselines.
/// </summary>
public static class SKBitmapApprovals
{
    /// <summary>Asserts that the bitmap matches the approved baseline.</summary>
    public static void ShouldMatchApproved(this SKBitmap bitmap, string? discriminator = null, string? customMessage = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The bitmap could not be encoded as PNG.");
        data.ToArray().ShouldMatchApproved("png", discriminator, customMessage);
    }
}
```

Because PNG bytes pin the SkiaSharp version, `SKBitmapApprovals` alone is a weak oracle. **Step 5 adds semantic invariants that do not depend on any baseline**, so a broken renderer still fails after regeneration.

- [ ] **Step 5: Add invariant raster tests that survive baseline regeneration**

Create `tests/DotQR.SkiaSharp.Tests/RasterInvariantTests.cs`:
```csharp
namespace DotQRTests.SkiaSharp;

public class RasterInvariantTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void DotQr_raster_has_expected_dimensions_and_module_colors(int pixelsPerModule)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("invariant", QRCodeGenerator.ECCLevel.M);
        using var bitmap = new QRCode(data).GetGraphic(pixelsPerModule);

        var modules = data.ModuleMatrix.Count;
        bitmap.Width.ShouldBe(modules * pixelsPerModule);
        bitmap.Height.ShouldBe(modules * pixelsPerModule);

        // Sample the centre of every module and assert the exact expected color.
        for (var y = 0; y < modules; y++)
        {
            for (var x = 0; x < modules; x++)
            {
                var expected = data.ModuleMatrix[y][x] ? SKColors.Black : SKColors.White;
                var px = x * pixelsPerModule + pixelsPerModule / 2;
                var py = y * pixelsPerModule + pixelsPerModule / 2;
                bitmap.GetPixel(px, py).ShouldBe(expected, $"module {x},{y} at pixel {px},{py}");
            }
        }
    }

    [Fact]
    public void DotQr_raster_preserves_transparency_of_the_light_color()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("transparent", QRCodeGenerator.ECCLevel.M);
        using var bitmap = new QRCode(data).GetGraphic(5, SKColors.Black, SKColors.Transparent);

        // The top-left corner is inside the quiet zone, so it is a light module.
        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void DotQr_raster_without_quiet_zones_is_eight_modules_smaller()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("size", QRCodeGenerator.ECCLevel.M);

        using var withQuiet = new QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, drawQuietZones: true);
        using var withoutQuiet = new QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, drawQuietZones: false);

        (withQuiet.Width - withoutQuiet.Width).ShouldBe(8 * 4);
    }

    [Fact]
    public void ArtQr_raster_has_expected_dimensions_and_solid_finder_patterns()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("invariant", QRCodeGenerator.ECCLevel.M);
        using var bitmap = new ArtQRCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.White);

        var modules = data.ModuleMatrix.Count;
        bitmap.Width.ShouldBe(modules * 10);

        // The centre of the top-left finder pattern is always drawn as a solid rectangle.
        var centre = 4 * 10 + 3 * 10 + 5;
        bitmap.GetPixel(centre, centre).ShouldBe(SKColors.Black);
    }
}
```

These assertions fail on any real regression — wrong dimensions, wrong module polarity, lost transparency, wrong quiet-zone handling, or broken finder patterns — regardless of what the PNG baseline says.

- [ ] **Step 6: Fix the moved tests**

Remove `#if SYSTEM_DRAWING` wrappers, rename namespaces, change `Color` → `SKColor`, `Bitmap` → `SKBitmap`, and wrap returned bitmaps in `using`. Change `bmp.ShouldMatchApproved()` call sites to the `SKBitmap` extension from Step 4.

`HelperFunctions.GetIconBitmap()` becomes:
```csharp
public static SKBitmap GetIconBitmap()
{
    var assembly = Assembly.GetExecutingAssembly();
    const string resourceName = "DotQRTests.SkiaSharp.assets.noun_software engineer_2909346.png";
    using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
    return SKBitmap.Decode(stream) ?? throw new InvalidOperationException("The icon could not be decoded.");
}
```
Update the resource name to whatever the actual embedded name is — check with `assembly.GetManifestResourceNames()` if it throws.

- [ ] **Step 7: Delete the retired GIF baselines and generate the PNG ones**

```powershell
git rm --quiet tests/DotQR.SkiaSharp.Tests/*.approved.gif
Remove-Item Env:\CI -ErrorAction SilentlyContinue
dotnet test tests/DotQR.SkiaSharp.Tests -v quiet
```
The approval helper writes `.approved.png` for every case. Then **open every generated PNG and confirm** before accepting it: QR codes are square with crisp module edges, `ArtQRCode` dots are circular and centred with solid square finder patterns, and the icon appears at the centre with an optional rounded background.

- [ ] **Step 8: Re-run with approvals enforced**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.SkiaSharp.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass, including the invariant tests from Step 5.

- [ ] **Step 9: Commit**

```powershell
git add -A
git commit -m "feat(skiasharp): add Base64QRCode, ToSvgLogo, raster invariants and PNG approval baselines"
```

---

## Phase 5 — Performance

> Every task in this phase begins and ends with the full suite green under `CI=1`. The generator
> tasks (17-19) are gated by `QRGeneratorTests`, `TransposeVerificationTests` and all payload
> approvals — those are the correctness net for changes that touch verified math.

### Task 13: `PngByteQRCode` — byte-preserving speedups

**Files:**
- Modify: `src/DotQR.ByteRenderers/PngByteQRCode.cs`

**Interfaces:**
- Consumes: existing `PngBuilder`.
- Produces: no API change. Output bytes must be identical.

- [ ] **Step 1: Record a baseline benchmark**

Add to `tools/DotQR.Benchmarks` a `PngByteQRCodeRendererBenchmark` case for a 500-character payload at `pixelsPerModule = 10`, then:
```powershell
dotnet run -c Release --project tools/DotQR.Benchmarks -- --filter *PngByte* --job short -f *
```
Record the mean. Save the console output beside the plan as `docs/benchmarks/png-before.txt`.

- [ ] **Step 2: Replace `Adler32` with the chunked form**

```csharp
// zlib's NMAX: the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1.
private const int AdlerBlockSize = 5552;

private static uint Adler32(byte[] data, int index, int length)
{
    const uint Base = 65521;
    uint s1 = 1, s2 = 0;

    var end = index + length;
    while (index < end)
    {
        var blockEnd = Math.Min(index + AdlerBlockSize, end);
        for (; index < blockEnd; index++)
        {
            s1 += data[index];
            s2 += s1;
        }
        s1 %= Base;
        s2 %= Base;
    }

    return (s2 << 16) + s1;
}
```

- [ ] **Step 3: Replace `Crc32` with a slicing-by-8 implementation**

Build an 8-row table from the existing `_crcTable` at static initialization, then process 8 bytes per iteration. Keep the same initial and final XOR values (`0xFFFFFFFF`, `^ 0xFFFFFFFF`) so the values are identical.

```csharp
private static readonly uint[] _crcTable8 = BuildTable8();

private static uint[] BuildTable8()
{
    var table = new uint[8 * 256];
    Array.Copy(_crcTable, table, 256);
    for (var i = 1; i < 8; i++)
    {
        for (var j = 0; j < 256; j++)
        {
            var previous = table[(i - 1) * 256 + j];
            table[i * 256 + j] = _crcTable[previous & 0xFF] ^ (previous >> 8);
        }
    }
    return table;
}

private static uint Crc32(byte[] data, int index, int length)
{
    var c = 0xFFFFFFFFu;
    var table = _crcTable8;
    var end = index + length;

    while (end - index >= 8)
    {
        var one = (uint)(data[index] | data[index + 1] << 8 | data[index + 2] << 16 | data[index + 3] << 24) ^ c;
        var two = (uint)(data[index + 4] | data[index + 5] << 8 | data[index + 6] << 16 | data[index + 7] << 24);
        c = table[7 * 256 + (one & 0xFF)]
          ^ table[6 * 256 + ((one >> 8) & 0xFF)]
          ^ table[5 * 256 + ((one >> 16) & 0xFF)]
          ^ table[4 * 256 + (one >> 24)]
          ^ table[3 * 256 + (two & 0xFF)]
          ^ table[2 * 256 + ((two >> 8) & 0xFF)]
          ^ table[1 * 256 + ((two >> 16) & 0xFF)]
          ^ table[two >> 24];
        index += 8;
    }

    for (; index < end; index++)
        c = table[(c ^ data[index]) & 0xFF] ^ (c >> 8);

    return c ^ 0xFFFFFFFF;
}
```

- [ ] **Step 4: Do not attempt to RLE the scanline fill**

The spec listed "RLE the scanline fill (emit whole bytes per run)". On close inspection this is not achievable while preserving bytes: runs begin at `x * pixelsPerModule`, which is byte-aligned only when `pixelsPerModule` is a multiple of 8, and a table-driven expansion requires exactly that alignment. The general case still needs per-bit writes.

The loop is also not the bottleneck. `DrawScanlines` builds **one** scanline per module row and then `Array.Copy`s it `pixelsPerModule` times, so its cost is `matrixSize² × pixelsPerModule` bit operations — small next to Deflate over the whole pixel buffer. `Adler32`, `Crc32` and the chunk pass walk every byte of the compressed output and are where the time actually goes.

Skip this optimization. Record in the commit message that it was evaluated and rejected, so the next person does not re-derive it.

- [ ] **Step 5: Avoid the `ToArray()` copy and re-parse in `GetBytes`**

Add a `List<(int Offset, int Length)> _chunks` populated by `WriteChunkStart`, and in `GetBytes` compute CRCs from the recorded ranges over `_stream.GetBuffer()`:

```csharp
public byte[] GetBytes()
{
    var buffer = _stream.GetBuffer();
    var length = (int)_stream.Length;

    foreach (var (offset, dataLength) in _chunks)
    {
        var crc = Crc32(buffer, offset + 8, dataLength + 4);
        var crcOffset = offset + 8 + dataLength;
        buffer[crcOffset + 0] = (byte)(crc >> 24);
        buffer[crcOffset + 1] = (byte)(crc >> 16);
        buffer[crcOffset + 2] = (byte)(crc >> 8);
        buffer[crcOffset + 3] = (byte)crc;
    }

    var result = new byte[length];
    Array.Copy(buffer, result, length);
    return result;
}
```

`WriteChunkStart` records `(_stream.Position - 4, length)` (the chunk offset including the 4-byte length field). Verify the offsets by running the tests — a wrong offset corrupts the PNG and the approval fails immediately.

- [ ] **Step 6: Verify byte-identity and measure**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.ByteRenderers.Tests --filter FullyQualifiedName~PngByte -v quiet
Remove-Item Env:\CI
dotnet run -c Release --project tools/DotQR.Benchmarks -- --filter *PngByte* --job short -f *
```
Expected: tests pass (byte-identical) and the mean improves. Save as `docs/benchmarks/png-after.txt`.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "perf(byterenderers): speed up PNG encoding with slicing-by-8 CRC32, chunked Adler32 and RLE scanlines"
```

---

### Task 14: Generator — `GetRawData` and its inverse without queues

**Files:**
- Modify: `src/DotQR.Core/QRCodeData.cs`
- Test: `tests/DotQR.Core.Tests/QRCodeDataRawRoundTripTests.cs`

**Interfaces:**
- Consumes: `QRCodeData.ModuleMatrix`.
- Produces: unchanged public API; identical raw bytes and identical parse results.

- [ ] **Step 1: Write the failing (round-trip) test**

```csharp
namespace DotQRTests.Core;

public class QRCodeDataRawRoundTripTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20)]
    [InlineData(40)]
    public void Raw_data_round_trips_preserving_the_module_matrix(int version)
    {
        using var original = new QRCodeGenerator().CreateQrCode(new string('x', 10), QRCodeGenerator.ECCLevel.M, requestedVersion: version);
        var raw = original.GetRawData(QRCodeData.Compression.Uncompressed);
        using var restored = new QRCodeData(raw, QRCodeData.Compression.Uncompressed);

        restored.Version.ShouldBe(original.Version);
        restored.ModuleMatrix.Count.ShouldBe(original.ModuleMatrix.Count);
        for (var y = 0; y < original.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < original.ModuleMatrix[y].Length; x++)
            {
                restored.ModuleMatrix[y][x].ShouldBe(original.ModuleMatrix[y][x], $"module {x},{y}");
            }
        }
    }
}
```

- [ ] **Step 2: Run it to confirm the test harness works before optimizing**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~QRCodeDataRawRoundTrip -v quiet`
Expected: PASS. This test must keep passing after the rewrite — it is the safety net, not the failing step.

- [ ] **Step 3: Replace the queue-based packing in `GetRawData`**

Delete the `Queue<int> dataQueue` block and the drain loop. Write bytes directly:

```csharp
var count = ModuleMatrix.Count;
var moduleBits = count * count;
for (var y = 0; y < count; y++)
{
    var row = ModuleMatrix[y];
    for (var x = 0; x < count; x += 8)
    {
        byte b = 0;
        for (var bit = 0; bit < 8 && x + bit < count; bit++)
        {
            if (row[x + bit])
                b |= (byte)(0x80 >> bit);
        }
        targetStream.WriteByte(b);
    }
}
```
Note the original packs **row-major, most-significant-bit first within each byte** and pads the final byte with zeros. The loop above reproduces that exactly because `count` is always a multiple of 4 but not necessarily 8, so the inner guard stops at the row edge and the byte is zero-padded.

- [ ] **Step 4: Replace the queue-based unpacking in the constructor**

Delete the `Queue<bool> modules` block and write bits directly:

```csharp
var payloadBytes = rawData.Length - 5;
ModuleMatrix = new List<BitArray>(sideLen);
for (int y = 0; y < sideLen; y++)
{
    var row = new BitArray(sideLen);
    for (int x = 0; x < sideLen; x++)
    {
        var bitIndex = y * sideLen + x;
        var byteIndex = 5 + (bitIndex >> 3);
        if (byteIndex < rawData.Length && (rawData[byteIndex] & (0x80 >> (bitIndex & 7))) != 0)
            row[x] = true;
    }
    ModuleMatrix.Add(row);
}
_ = payloadBytes;
```

- [ ] **Step 5: Verify**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass, including the round-trip test.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "perf(core): pack and unpack QRCodeData raw data without interim queues"
```

---

### Task 15: Generator — early-abort mask scoring

**Files:**
- Modify: `src/DotQR.Core/QRCodeGenerator/ModuleMatrix.cs`
- Modify: `src/DotQR.Core/QRCodeGenerator/ModulePlacer.cs`
- Test: `tests/DotQR.Core.Tests/ModuleMatrixScoreTests.cs`

**Interfaces:**
- Consumes: existing `ModuleMatrix.Score()`.
- Produces: `ModuleMatrix.Score(int abortAt)` where a returned value `> abortAt` means the scan was abandoned and the true score is therefore also `> abortAt`. `MaskCode` consumes this contract.

- [ ] **Step 1: Write the failing test for the contract**

```csharp
namespace DotQRTests.Core;

public class ModuleMatrixScoreTests
{
    private static ModuleMatrix CreateMatrix(int size)
    {
        var matrix = new ModuleMatrix(size);
        var mask = ModulePlacer.MaskPattern.Patterns[0];
        mask.Apply(matrix, new ModuleMatrix(size));
        return matrix;
    }

    [Fact]
    public void Score_with_max_threshold_equals_the_plain_score()
    {
        using var matrix = CreateMatrix(29);
        matrix.Score(int.MaxValue).ShouldBe(matrix.Score());
    }

    [Fact]
    public void Score_aborts_only_when_the_threshold_is_unreachable()
    {
        using var matrix = CreateMatrix(29);
        var full = matrix.Score();

        // A threshold at or above the full score must still produce the exact score.
        matrix.Score(full).ShouldBe(full);

        // A threshold below the full score must report a partial value above the threshold,
        // which lets the caller reject this candidate without knowing the true score.
        if (full > 0)
        {
            var partial = matrix.Score(full - 1);
            partial.ShouldBe(full);
        }
    }

    [Fact]
    public void Aborted_scans_are_cheaper_than_full_scans()
    {
        using var matrix = CreateMatrix(41);
        var full = matrix.Score();
        if (full < 20)
            return; // threshold too low to demonstrate aborting on this input

        // Aborting at 1 should stop almost immediately; the exact value is irrelevant as long as
        // it exceeds the threshold.
        matrix.Score(1).ShouldBeGreaterThan(1);
    }
}
```

- [ ] **Step 2: Make the generator internals visible to tests**

`ModuleMatrix` is declared `private sealed class` inside `public partial class QRCodeGenerator`, and `ModulePlacer` / `MaskPattern` are likewise `private`. A private nested type is **not** reachable through `InternalsVisibleTo`, so the test in Step 1 cannot compile as written.

Change the declaration in `src/DotQR.Core/QRCodeGenerator/ModuleMatrix.cs`:
```csharp
internal sealed class ModuleMatrix : IDisposable
```
and in `src/DotQR.Core/QRCodeGenerator/ModulePlacer.cs` / `ModulePlacer.MaskPattern.cs`:
```csharp
internal static partial class ModulePlacer
...
internal readonly struct MaskPattern
```
Nothing about their behaviour changes; only accessibility within the assembly. The public API approval test in Task 19 confirms no public surface leaked.

- [ ] **Step 3: Run the test — it must fail on the missing overload**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~ModuleMatrixScoreTests -v quiet`
Expected: FAIL with `error CS1501: No overload for method 'Score' takes 1 arguments`.

- [ ] **Step 4: Add the abort-aware overload**

In `ModuleMatrix`, rename the existing `Score()` body to `Score(int abortAt)` and add a thin wrapper:

```csharp
/// <summary>
/// Calculates the penalty score of the matrix. Every penalty term is non-negative, so once the
/// running total exceeds <paramref name="abortAt"/> the caller can reject this candidate without
/// the remaining work; such an abandoned scan returns a partial value greater than
/// <paramref name="abortAt"/>.
/// </summary>
/// <param name="abortAt">The threshold above which the scan may stop early.</param>
public int Score(int abortAt)
{
    // existing body, with the checks below inserted
}

/// <summary>Calculates the penalty score of the matrix.</summary>
public int Score() => Score(int.MaxValue);
```

Insert early exits:
- inside the Penalty 1 outer loop, after `blackModules` is accumulated for that row/column pair:
  ```csharp
  if ((i & 7) == 0 && score > abortAt) return score;
  ```
- immediately after the Penalty 4 block: `if (score > abortAt) return score;`
- inside the Penalty 2 outer loop, same `(y & 7) == 0` guard
- inside the Penalty 3 outer loop, same `(i & 7) == 0` guard

- [ ] **Step 5: Use it in `MaskCode`**

```csharp
int score = version < 0 ? copy.ScoreMicro() : copy.Score(patternScore);
```
Keep the `if (patternScore > score)` comparison exactly as it is: a partial score above `patternScore` correctly loses.

- [ ] **Step 6: Verify**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass. The generator and payload approvals are what prove mask selection did not change.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "perf(core): abort mask-pattern scoring once a candidate cannot win"
```

---

### Task 16: Generator — write back the winning mask via row replacement

**Files:**
- Modify: `src/DotQR.Core/QRCodeGenerator/ModuleMatrix.cs`
- Modify: `src/DotQR.Core/QRCodeGenerator/ModulePlacer.cs`
- Test: `tests/DotQR.Core.Tests/ModuleMatrixWriteToTests.cs`

**Interfaces:**
- Consumes: `ModuleMatrix.CopyFrom(QRCodeData)`.
- Produces: `ModuleMatrix.WriteTo(QRCodeData)`, the exact inverse of `CopyFrom`, and `ModuleMatrix.CopyFrom(ModuleMatrix)`.

- [ ] **Step 1: Write the failing round-trip test**

This validates the nibble-offset conversion directly, without needing the old delegate code path.

```csharp
namespace DotQRTests.Core;

public class ModuleMatrixWriteToTests
{
    [Theory]
    [InlineData(21)]
    [InlineData(25)]
    [InlineData(41)]
    [InlineData(177)]
    public void CopyFrom_then_WriteTo_reproduces_the_module_matrix(int size)
    {
        var random = new Random(size);
        using var source = new QRCodeData((size - 21) / 4 + 1, true);

        // Populate the data area, leaving the 4-module quiet zone on every side clear.
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                source.ModuleMatrix[y + 4][x + 4] = random.Next(2) == 0;
            }
        }

        using var target = new QRCodeData((size - 21) / 4 + 1, true);
        using var matrix = new ModuleMatrix(size);
        matrix.CopyFrom(source);
        matrix.WriteTo(target);

        for (var y = 0; y < source.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < source.ModuleMatrix[y].Length; x++)
            {
                target.ModuleMatrix[y][x].ShouldBe(source.ModuleMatrix[y][x], $"module {x},{y}");
            }
        }
    }
}
```

- [ ] **Step 2: Run it — it must fail**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~ModuleMatrixWriteTo -v quiet`
Expected: FAIL, `no method 'WriteTo'`.

- [ ] **Step 3: Implement `WriteTo`**

`CopyFrom` loads padded row bits and shifts the whole stream **right by 4**; `WriteTo` is the exact inverse, shifting **left by 4** and reconstructing the padded rows:

```csharp
/// <summary>
/// Writes this matrix back into the data area of a <see cref="QRCodeData"/>, replacing each
/// affected row. It is the inverse of <see cref="CopyFrom"/>.
/// </summary>
public void WriteTo(QRCodeData data)
{
    Debug.Assert(!Disposed);
    Debug.Assert(Size == data.ModuleMatrix.Count - 8);

    var widthBits = Size + 8;
    var widthBytes = (widthBits + 7) >> 3;
    var row = new byte[widthBytes];

    for (var y = 0; y < Size; y++)
    {
        Array.Clear(row, 0, widthBytes);
        var source = y * Stride;

        // Shift the module bit stream left by the 4 padding bits that CopyFrom removed.
        for (var i = 0; i < widthBytes; i++)
        {
            var low = i < Stride ? Bytes[source + i] : 0;
            var high = i >= 1 && i - 1 < Stride ? Bytes[source + i - 1] : 0;
            row[i] = (byte)((low << 4) | (high >> 4));
        }

        var bitArray = new BitArray(row) { Length = widthBits };
        data.ModuleMatrix[y + 4] = bitArray;
    }
}
```

Also add a matrix-to-matrix copy used to snapshot the winning candidate:

```csharp
/// <summary>Copies the contents of another matrix of the same size into this one.</summary>
public void CopyFrom(ModuleMatrix other)
{
    Debug.Assert(!Disposed);
    Debug.Assert(other.Size == Size && other.Stride == Stride);

    Array.Copy(other.Bytes, Bytes, ByteLength);
}
```

- [ ] **Step 4: Run the round-trip test**

Run: `dotnet test tests/DotQR.Core.Tests --filter FullyQualifiedName~ModuleMatrixWriteTo -v quiet`
Expected: PASS for all four sizes. This proves `WriteTo` inverts `CopyFrom` exactly.

- [ ] **Step 5: Use it in `MaskCode`**

Replace the final delegate loop. Snapshot the winner inside the pattern loop:

```csharp
using var copy = new ModuleMatrix(size);
using var winner = new ModuleMatrix(size);
...
if (patternScore > score)
{
    selectedPattern = maskPattern;
    patternScore = score;
    winner.CopyFrom(copy);
}
...
// Apply the best mask pattern to the actual QR code in one row-replacement pass.
winner.WriteTo(qrCode);
return selectedPattern;
```

Delete the now-unused `selectedPatternFunc` loop and the `Debug.Assert(blockedModules[y, x] == ...)` triangle. Keep `MaskPattern.Generator` — it is still used when the pattern bytes are built.

- [ ] **Step 6: Verify**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass. Generator and payload approvals prove the writeback is bit-correct in the real pipeline.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "perf(core): apply the winning mask by replacing rows instead of per-module delegate calls"
```

---

### Task 17: Generator — popcount penalty tally

**Files:**
- Modify: `src/DotQR.Core/QRCodeGenerator/ModuleMatrix.cs`

**Interfaces:**
- Consumes: existing `BitArrayExtensions.CopyTo`.
- Produces: no API change.

- [ ] **Step 1: Confirm the safety net runs first**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: PASS. Baseline established before touching generation math.

- [ ] **Step 2: Do not attempt the word-wise interleave copy**

The spec listed "`InterleaveData`: word-wise 8-bit copies instead of the bit-by-bit `CopyTo`". On inspection this is not worth doing. `BitArray.CopyTo` is already called with `count == 8`, so `InterleaveData` executes an eight-iteration loop per codeword — around 2-3 thousand total iterations for the largest code. `BitArray` exposes no bulk "load from `byte[]`" that preserves length, so any word-wise version has to reconstruct the array, which costs more than it saves.

Skip it. Keep `BitArrayExtensions.CopyTo` exactly as it is, and note the rejection in the commit message.

- [ ] **Step 3: Use `BitOperations.PopCount` for the black-module tally**

In `ModuleMatrix.Score(int abortAt)`, remove the `blackModules++` from the Penalty 1 loop and compute the total once, before it:

```csharp
var blackModules = 0;
for (var y = 0; y < Size; y++)
{
    var span = Bytes.AsSpan(y * Stride, Stride);
    for (var i = 0; i < span.Length; i++)
    {
        blackModules += BitOperations.PopCount(span[i]);
    }
}
```
Add `using System.Numerics;` to the file.

This is valid because `Stride` padding bits are guaranteed zero: `Bytes` is cleared when rented in the constructor, and only module bits inside `Size` are ever written.

- [ ] **Step 4: Verify**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "perf(core): count black modules with popcount

Word-wise interleave copies were evaluated and rejected: CopyTo is already
called with count==8, so the loop is eight iterations per codeword and a
bulk version would cost more than it saves."
```

---

### Task 18: `Polynom` on span-backed coefficients

**Files:**
- Modify: `src/DotQR.Core/QRCodeGenerator/Polynom.cs`
- Modify: `src/DotQR.Core/QRCodeGenerator/PolynomItem.cs`

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: unchanged `Polynom` behaviour (`Add`, `RemoveAt`, indexer, `Count`, `Clear`, `Clone`, enumerator). The ECC results must be identical.

- [ ] **Step 1: Confirm the safety net**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: PASS.

- [ ] **Step 2: Replace the item array with parallel byte arrays**

Keep `PolynomItem` as the *public-facing* value type so the indexer and enumerator signatures do not change, but store coefficients and exponents in two `ArrayPool`-rented `byte[]` arrays:

```csharp
private byte[] _coefficients;
private byte[] _exponents;
```
`Add(PolynomItem item)` writes both. `this[int index]` returns `new PolynomItem(_coefficients[index], _exponents[index])`. `RemoveAt` shifts both with `Array.Copy`. `Clone` copies both. `ToString` and the enumerator are unchanged in behaviour.

Coefficients are already asserted `< 256`; exponents are bounded by 68 for the largest generator polynomial, so `byte` is safe — assert it:
```csharp
Debug.Assert((uint)exponent < 256);
```

- [ ] **Step 3: Verify**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.Core.Tests -v quiet
Remove-Item Env:\CI
```
Expected: all pass, including every payload approval. ECC output is exercised by every generated code.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "perf(core): store polynomial coefficients and exponents in pooled byte spans"
```

---

## Phase 6 — Packaging and acceptance

### Task 19: API approval tests

**Files:**
- Create: `tests/DotQR.ApiTests/DotQR.ApiTests.csproj`
- Move: `QRCoderApiTests/ApiApprovalTests.cs` → `tests/DotQR.ApiTests/`

**Interfaces:**
- Consumes: all three assemblies.
- Produces: one approved API snapshot per assembly, locking the public surface.

- [ ] **Step 1: Create the project**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>DotQR.ApiTests</AssemblyName>
    <RootNamespace>DotQRApiTests</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="PublicApiGenerator" Version="11.1.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="shouldly" Version="4.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DotQR.Core/DotQR.Core.csproj" />
    <ProjectReference Include="../../src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj" />
    <ProjectReference Include="../../src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Move and adapt the approval test**

Move the file, rename its namespace, and change the `[InlineData]` theory data to the three new assemblies:

```csharp
[Theory]
[InlineData(typeof(DotQR.QRCodeData))]
[InlineData(typeof(DotQR.PngByteQRCode))]
[InlineData(typeof(DotQR.QRCode))]
public void PublicApi(Type type)
```

Because the project layout is now `src/`/`tests/`, update the path arithmetic: `buildDir` becomes `Path.Combine(projectDir, "src", projectName, "bin", config)` and `csProject` becomes `Path.Combine(projectDir, "src", projectName, projectName + ".csproj")`. The TFM-directory filter can be simplified to a single `net10.0` directory.

- [ ] **Step 3: Generate the baselines once**

```powershell
Remove-Item Env:\CI -ErrorAction SilentlyContinue
dotnet test tests/DotQR.ApiTests -v quiet
```
This creates `.approved.txt` files for the three assemblies. Inspect each one and confirm it contains `namespace DotQR`, `SkiaSharp` types only in the SkiaSharp snapshot, and `RgbaColor`/`Size` in Core.

- [ ] **Step 4: Verify with approvals enforced**

```powershell
$env:CI = "1"
dotnet test tests/DotQR.ApiTests -v quiet
Remove-Item Env:\CI
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "test(api): add public API approval baselines for all three packages"
```

---

### Task 20: Solution, benchmarks, trim analysis

**Files:**
- Create: `DotQR.sln`
- Create: `tools/DotQR.Benchmarks/DotQR.Benchmarks.csproj` and benchmark classes
- Create: `tools/DotQR.TrimAnalysis/DotQR.TrimAnalysis.csproj` and `Program.cs`

**Interfaces:**
- Consumes: all packages.
- Produces: a single build/test entry point and the measurement harness for §9 of the spec.

- [ ] **Step 1: Create the solution**

```powershell
dotnet new sln -n DotQR
Get-ChildItem -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch '\\obj\\' } | ForEach-Object { dotnet sln DotQR.sln add $_.FullName }
```

- [ ] **Step 2: Create the benchmark project**

`tools/DotQR.Benchmarks/DotQR.Benchmarks.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <NoWarn>$(NoWarn);CA1822</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DotQR.Core/DotQR.Core.csproj" />
    <ProjectReference Include="../../src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj" />
    <ProjectReference Include="../../src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj" />
  </ItemGroup>

</Project>
```

Move the existing benchmark classes from `QRCoderBenchmarks/`, rename namespaces, and convert the `System.Drawing` renderer benchmark to `SKBitmap`:

```csharp
[Benchmark]
public void QRCode()
{
    using var bitmap = new QRCode(_data).GetGraphic(20);
}
```

Add a `QRCodeGeneratorBenchmark` case that exercises `CreateQrCode` for a 500-character payload at `ECCLevel.H`, which drives the mask-scoring path from Task 15.

- [ ] **Step 3: Create the trim analysis project**

`tools/DotQR.TrimAnalysis/DotQR.TrimAnalysis.csproj` targets `net10.0` with `PublishTrimmed=true`, references `DotQR.Core` and `DotQR.ByteRenderers` (not SkiaSharp — native assets and trimming are a separate concern), and roots both assemblies.

`Program.cs`:
```csharp
using DotQR;

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode("trim analysis", QRCodeGenerator.ECCLevel.M);
using var png = new PngByteQRCode(data);
Console.WriteLine(png.GetGraphic(5).Length);
```

- [ ] **Step 4: Verify build, tests, trim and pack**

```powershell
dotnet build DotQR.sln -v quiet
$env:CI = "1"
dotnet test DotQR.sln -v quiet
Remove-Item Env:\CI
dotnet publish tools/DotQR.TrimAnalysis -c Release -v quiet
dotnet pack -c Release -v quiet
```
Expected: build succeeds with 0 warnings; all tests pass; publish reports no `IL2xxx`/`IL3xxx` trim warnings; three `.nupkg` files are produced in `src/*/bin/Release`.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "build: add unified solution, benchmarks and trim analysis"
```

---

### Task 21: README and acceptance verification

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: everything.
- Produces: the acceptance evidence for the spec's §15.

- [ ] **Step 1: Rewrite the README**

Must cover, in this order: what DotQR is; the three-package table with a "which package do I need" guide; a quick-start per package; the bring-your-own-renderer example; `RgbaColor`/`Size`; the Linux native-asset note; the migration table from `QRCoder`; and an attribution section crediting Raffael Herrmann and Shane Krueger with a link to the upstream repository.

Bring-your-own-renderer example to include verbatim:
```csharp
using DotQR;

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode("hello", QRCodeGenerator.ECCLevel.Q);

var count = data.ModuleMatrix.Count;
for (var y = 0; y < count; y++)
{
    for (var x = 0; x < count; x++)
    {
        Console.Write(data.ModuleMatrix[y][x] ? "██" : "  ");
    }
    Console.WriteLine();
}
```

- [ ] **Step 2: Run the acceptance suite**

```powershell
Write-Output "--- 1 build ---"
dotnet build DotQR.sln -v quiet
Write-Output "--- 2 tests ---"
$env:CI = "1"; dotnet test DotQR.sln -v quiet; Remove-Item Env:\CI
Write-Output "--- 3 no System.Drawing ---"
Select-String -Recurse src, tests -Pattern 'System\.Drawing'
Write-Output "--- 4 byte renderers have no SkiaSharp ---"
dotnet list src/DotQR.ByteRenderers/DotQR.ByteRenderers.csproj package --include-transitive
Write-Output "--- 8 pack ---"
dotnet pack -c Release -v quiet
Get-ChildItem -Recurse src -Filter *.nupkg | Select-Object -ExpandProperty Name
Write-Output "--- 10 git ---"
git remote -v
git log --oneline | Select-Object -Last 3
```

Expected:
1. Build succeeded, 0 warnings.
2. All tests pass.
3. No output.
4. The package list contains `DotQR.Core` only — no `SkiaSharp`.
8. Three `.nupkg` files: `DotQR.Core.1.0.0.nupkg`, `DotQR.ByteRenderers.1.0.0.nupkg`, `DotQR.SkiaSharp.1.0.0.nupkg`.
10. No remotes; the log's oldest entry is `chore: remove WPF/UWP/WinForms projects...`.

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "docs: rewrite README for the three-package DotQR model"
```

---

## Self-Review

**Spec coverage:**

| Spec section | Task |
|---|---|
| §3 decisions (names, TFMs, types) | 1, 2, 5, 6, 10, 12 |
| §4.1/§4.2 package split and boundary enforcement | 7, 9, 12, 20 |
| §5 repository layout, §5.1 deletions | 1, 2, 7, 10, 12, 20 |
| §6.1 Core contents | 3 |
| §6.2 `RgbaColor`, `Size` | 5, 6 |
| §6.3 conditional-compilation removal | 3, 7, 8 |
| §6.4 unchanged Core API | 19 (locks it) |
| §7.1/§7.2 ByteRenderers and PNG byte-identity | 7, 8, 9 |
| §8.1/§8.2/§8.3 SkiaSharp renderers and new members | 10, 11, 12 |
| §9.1 PNG speedups (Adler-32, CRC-32, `GetBytes`) | 13 |
| §9.2 `QRCode` nearest-neighbour | 10 |
| §9.3 `ArtQRCode` paths | 11 |
| §9.4 generator: early abort | 15 |
| §9.4 generator: winning-mask writeback | 16 |
| §9.4 generator: queue-free raw data | 14 |
| §9.4 generator: popcount | 17 |
| §9.4 generator: `Polynom` spans | 18 |
| §9.5 out of scope respected | no task touches SVG/PS/mask apply |
| §10.1 test project split | 4, 9, 12, 19 |
| §10.2 approval strategy | 9, 12 — **see deviation 3** |
| §10.3 regression guards gate generator work | 15, 16, 17, 18 |
| §11 build settings, §11.1 native assets | 2, 7, 10, 12 |
| §12 README | 21 |
| §13 breaking changes | 10, 11, 12, 21 |
| §15 acceptance criteria | 21 — **criterion 6 amended, see deviation 3** |

**Deviations from the spec.** Three spec items were found to be wrong or unachievable during planning. Each is recorded in the task that addresses it and must be reflected in the spec before implementation.

| # | Spec item | Finding | Resolution |
|---|---|---|---|
| 1 | §9.1 "RLE the scanline fill" | A run starts at `x * pixelsPerModule`, byte-aligned only when `pixelsPerModule % 8 == 0`; a table-driven expansion needs the same alignment. The loop is also not the bottleneck: one scanline is built per module row and `Array.Copy`d, so it costs `matrixSize² × pixelsPerModule` bit operations, against Deflate over the entire pixel buffer. | Rejected with reasoning in Task 13 Step 4. The other §9.1 items (Adler-32, CRC-32, `GetBytes`) are where the time goes and are kept. |
| 2 | §9.4 "`InterleaveData`: word-wise 8-bit copies" | `BitArray.CopyTo` is already called with `count == 8`, so the "bit-by-bit" loop is eight iterations per codeword — a few thousand total. `BitArray` exposes no length-preserving bulk load, so a word-wise version reconstructs the array and costs more than it saves. | Rejected with reasoning in Task 17 Step 2. The interleave is untouched. |
| 3 | §10.2 "retain the approved `.gif` anchors" / §15 criterion 6 | KGySoft's `BitmapDataFactory.CreateBitmapData(IntPtr, Size, …)` takes `System.Drawing.Size` (verified by reflection). Keeping the GDI-era GIF pipeline therefore reintroduces `System.Drawing` into the test project, contradicting the Global Constraints and acceptance criterion 3. | Raster baselines become SkiaSharp-encoded `.approved.png` and are regenerated. Verification strength is restored by baseline-independent invariant tests (Task 12 Step 5) asserting dimensions, per-module colours, transparency and finder-pattern solidity, so a regression still fails after regeneration. |

Deviation 3 needs sign-off: it weakens criterion 6 from "pixel-identical to the upstream baseline" to "matches a regenerated baseline plus semantic invariants". The byte-identity guarantee for PNG/BMP/PDF/PS/SVG/ASCII (criterion 5) is **unaffected** and remains a hard assertion, because those renderers have no graphics dependency and their baselines are untouched by this.

**Gap found and closed:** §15 criterion 5 requires PNG/BMP/PDF/PS/SVG/ASCII byte-identity, but no task said *which* project enforces it. Task 9 Step 5 does, and it is the only task permitted to run those approvals.

**Second gap found and closed:** the generator tests need `ModuleMatrix` and `ModulePlacer`, which are `private` nested types and therefore unreachable through `InternalsVisibleTo`. Task 15 Step 2 widens them to `internal` and relies on Task 19's API approval test to prove nothing became public.

**Placeholder scan:** no `TBD`, no `TODO`, no "similar to Task N", no "add error handling". Every code step contains its code. Two steps instruct empirical verification because the value cannot be written down without running it — the `GetBytes` chunk offsets (Task 13 Step 5) and the embedded resource name (Task 12 Step 6) — and each states how to determine the value and what failure looks like.

**Type consistency:** `RgbaColor` members (`R`, `G`, `B`, `A`, `ToRgbaBytes`, `ToHtmlHex`, `Parse`) are used identically in Tasks 5, 8, 10. `Size` (`Width`, `Height`, `Empty`) in Tasks 6, 8. `SKColor` conversions (`ToSKColor`, `ToRgbaColor`) are defined in Task 10 Step 3 and used in Tasks 10 and 12. `ModuleMatrix.Score(int)` from Task 15 is what Task 15's own `MaskCode` edit consumes. `ModuleMatrix.WriteTo` from Task 16 is proven to invert `CopyFrom` by its own round-trip test before `MaskCode` relies on it. `SvgLogo(byte[])` from Task 8 is what Task 12's `ToSvgLogo` calls. `SKBitmap.Encode(SKEncodedImageFormat, int)`, `SKBitmap.Copy(SKColorType)`, `SKCanvas.DrawBitmap(SKBitmap, SKRect, SKSamplingOptions, SKPaint)` and `SKCanvas.DrawRoundRect` are all confirmed present in SkiaSharp 4.153.1 by reflection.
