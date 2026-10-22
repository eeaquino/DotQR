# DotQR Blazor Server playground

A .NET 10 Interactive Server sample with live forms for every concrete payload generator (24, plus plain text), standard/Micro QR encoding, and all nine renderers. It references the three projects under `src` directly. No DotQR NuGet packages or external QR services are used.

## Run

From the repository root, with the .NET SDK selected by `global.json` installed:

```sh
dotnet run --project samples/DotQR.Playground
```

Open **http://localhost:5080**. The development launch profile binds to localhost and does not require a development HTTPS certificate. To use another local port:

```sh
dotnet run --project samples/DotQR.Playground --no-launch-profile --environment Development --urls http://localhost:5081
```

Do not expose this development sample to the internet. Production hosting needs HTTPS, appropriate request/concurrency limits, access control, and deployment hardening beyond this sample.

### Native graphics

PNG/BMP/PDF/SVG/PostScript/ASCII renderers need no native graphics library. Skia, Art, Base64, and image uploads use the local SkiaSharp project and its transitive dependencies; Windows and macOS native assets come with SkiaSharp.

On Linux, add native assets matching the SkiaSharp version referenced by `src/DotQR.SkiaSharp/DotQR.SkiaSharp.csproj` (currently `4.153.1`) to this sample:

```sh
dotnet add samples/DotQR.Playground package SkiaSharp.NativeAssets.Linux --version 4.153.1
```

Follow your distribution's requirements for the native package. A missing native dependency produces a generic generation/upload error without logging payload contents. This Linux package is a third-party dependency, not a DotQR package reference.

## Explore

1. Choose a payload: text/URL/bookmark/location, communications, contact/calendar, cryptocurrencies, payments, Wi-Fi, OTP, or Shadowsocks.
2. Edit fields. Selects/checks apply immediately; text edits settle for 250 ms before generation. Optional blank fields remain absent. Switching payloads preserves that circuit's values; **Reset example** restores sample data.
3. Choose a renderer and its applicable controls. Image uploads accept PNG/JPEG/WebP, validate encoded and decoded size, and normalize to PNG. SVG uploads are not accepted.
4. Change QR encoding, inspect actual encoded text/effective settings, and download the selected output. Invalid/pending edits clear the preview and disable downloads.

Micro QR has its own plain-text editor, initially `12345`; switching back restores your previous standard payload and its values. M1 is numeric and detection-only. M2 supports numeric/alphanumeric. M3–M4 have limited byte capacity. H is unsupported and Q is available only for M4. Art rendering is disabled for Micro because it assumes standard three-corner finder patterns. Other matrix renderers retain the library's four-module padding; no claim of scanner compatibility is made.

### Renderer details

| Renderer | Output / options |
| --- | --- |
| PngByteQRCode | PNG; scale, colors/alpha, quiet zones |
| BitmapByteQRCode | BMP; scale, opaque RGB; matrix quiet zone always included |
| PdfByteQRCode | PDF; scale, opaque RGB, DPI; PNG **matrix fallback preview**, actual PDF download; obsolete unused JPEG-quality argument omitted |
| SvgQRCode | SVG image preview/download; colors/alpha, quiet zones, fixed size or responsive viewBox, raster logo/coverage/background clearing |
| PostscriptQRCode | Escaped PS/EPS text preview and actual download; scale or dimensions, opaque colors, quiet zones |
| AsciiQRCode | Regular module strings/repetition or compact blocks/inversion; quiet zones, LF/CRLF |
| QRCode (Skia) | PNG; scale, colors/alpha, quiet zones, optional logo/coverage/border/background |
| ArtQRCode | PNG; dot factor, colors/background, quiet-zone style, optional background and finder-pattern images |
| Base64QRCode | Base64 `.txt` download plus image and encoded-text preview; PNG/JPEG/WebP, colors, quiet zones, optional logo/coverage/border |

SVG viewBox-only mode is responsive and intentionally hides fixed sizing: the library always uses module coordinates in its viewBox. PS/EPS explicit dimensions are a bounding box, not arbitrary module stretching. Long text previews are truncated at 12,000 characters; downloads always contain the entire output.

Payment payloads can mandate ECC, ECI, or version. Swiss and Slovenian UPN lock generic encoding controls and show effective metadata. Girocode enforces ECC M and its selected payload charset: UTF-8 / ISO-8859-1 / ISO-8859-2 use the corresponding ECI and support requested QR versions; other character sets use exact binary bytes with automatic version, declaring the charset in the payment header. Generic ECI/UTF-8/BOM controls do not apply to Girocode. Russian orders use `ToBytes()` with the chosen character set; only ECC remains configurable because the binary API selects version automatically. Swiss v2 does not serialize the reserved ultimate-creditor or payment-date constructor arguments, so they have no controls. A Swiss QR bill additionally requires a prescribed Swiss-cross logo and payment-slip layout: this is a payload playground, not a certified QR-bill producer.

Crypto addresses and payment examples are illustrative, not instructions to send money. This sample never submits a payment or initiates any encoded action.

## Privacy and limits

This is **server-side Blazor**. Payloads, Wi-Fi passwords, OTP seeds, and images travel to and are processed by the server. They live in the circuit's memory and are not intentionally persisted or logged by the sample. Downloads use streamed blobs, not sensitive URLs. Use test credentials; inspect development/hosting logging policies separately.

Per request: text up to 8192 characters (actual QR capacity is lower), scale 1–32, output sides up to 2048 pixels/points, output at most 16 MiB. Uploads are at most 2 MiB both before and after PNG normalization and at most 2048 × 2048 decoded pixels. Limits prevent accidental large allocations; this is not a hardened public image-processing service.

Low contrast, transparency, decorative dots, logos, missing quiet zones, and different scanner capabilities can all prevent scanning. A rendered preview does **not** establish scannability.

## Tests and manual checklist

All four test projects use **xunit.v3 4.0.2** executable runners. The solution builds with zero warnings/errors, and all 783 tests pass (68 playground tests). Browser interactions were not automated; Slopwatch was not installed.

```sh
dotnet build DotQR.slnx
dotnet test tests/DotQR.Playground.Tests
```

In environments that restrict VSTest parent-process monitoring or compiler-server named pipes, build single-node without shared compilation and use the standalone xUnit v3 runner:

```sh
dotnet build DotQR.slnx -m:1 -p:UseSharedCompilation=false
dotnet tests/DotQR.Playground.Tests/bin/Debug/net10.0/DotQR.Playground.Tests.dll -noLogo -noColor
```

The repository's dependency audit checks every resolved direct/transitive package version against NuGet deprecation metadata. It requires Node 18+ and an existing restore, without npm dependencies:

```sh
node tools/audit-deprecations.mjs
```

The recorded report is `docs/nuget-deprecation-audit.json`. BenchmarkDotNet was updated to 0.15.8 to remove deprecated transitive packages.

Manual checks:

- Select every payload's default; edit optional/variant fields and inspect serialization.
- Exercise contact formats, OTP TOTP/HOTP, payment address/reference/recurrence fields, Russian optional fields/character sets, and proxy plugin/parameter modes.
- Download all nine renderer outputs and open them in appropriate viewers; check MIME, extension, and preview labeling.
- Exercise every exposed renderer option, SVG fixed/viewBox sizing, PS/EPS, ASCII regular/compact, PDF DPI, Base64 image formats, and transparent colors.
- Try M1–M4 with valid content/ECC and too-large/incompatible content; switch back and check standard payload preservation.
- Enter invalid values and rapidly alternate edits; no old result should regain the download button.
- Upload valid, oversized, truncated, unsupported, and high-dimension images; clear upload errors and replace/remove each image role.
- Use keyboard-only controls and narrow/mobile layout. Confirm labels, focus indicators, status announcements, and readable errors.
- Disconnect/reconnect the circuit and retry downloads without disclosing payload details in logs.
