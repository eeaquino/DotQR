# DotQR

DotQR generates QR codes for .NET 10 and later. It splits QR generation from rendering, so you can
produce the module data once and render it with SkiaSharp, with no graphics library at all, or with
your own renderer.

DotQR is derived from [QRCoder](https://github.com/Shane32/QRCoder), which was created by
[Raffael Herrmann](https://raffaelherrmann.de) and is maintained by
[Shane Krueger](https://github.com/Shane32). The QR encoding, Reed-Solomon error correction, mask
selection and payload generators are QRCoder's work. This project replaces the `System.Drawing`
rendering with SkiaSharp, adds a dependency-free byte renderer package, and targets `net10.0` only.
Please use and support the original project if you need `System.Drawing`, older frameworks, or a
stable long-term API.

## Packages

| Package | Contents | Dependencies |
|---|---|---|
| **DotQR.Core** | QR and Micro QR encoding, error correction, mask selection, 25 payload generators (Wi-Fi, vCard, URL, payment, contact, and more), `RgbaColor`, `Size` | none |
| **DotQR.ByteRenderers** | PNG, BMP, PDF, SVG, PostScript and ASCII renderers | none |
| **DotQR.SkiaSharp** | `QRCode`, `ArtQRCode` and `Base64QRCode` raster renderers, including icon compositing | SkiaSharp |

All three share the `DotQR` namespace, so a single `using DotQR;` covers whichever packages you
reference.

DotQR.Core has no reference to either renderer, and DotQR.ByteRenderers has no reference to
SkiaSharp. If you only generate module data, or only produce vector or byte output, you never pull
in a graphics library. This is enforced by the test project graph, not just by convention.

## Installation

```bash
dotnet add package DotQR.Core
dotnet add package DotQR.ByteRenderers
dotnet add package DotQR.SkiaSharp
```

SkiaSharp ships native assets for Windows and macOS. On Linux you must also add:

```bash
dotnet add package SkiaSharp.NativeAssets.Linux
```

## Quick start

### Render to PNG with no graphics dependency

```csharp
using DotQR;

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode("https://example.com", QRCodeGenerator.ECCLevel.Q);

var png = new PngByteQRCode(data).GetGraphic(pixelsPerModule: 10);
await File.WriteAllBytesAsync("qr.png", png);
```

`DotQR.ByteRenderers` also provides `SvgQRCode`, `PdfByteQRCode`, `BitmapByteQRCode`,
`PostscriptQRCode` and `ASCIIQRCode`, each returning `byte[]` or `string`.

### Render with SkiaSharp

```csharp
using DotQR;
using SkiaSharp;

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode("https://example.com", QRCodeGenerator.ECCLevel.Q);
using var bitmap = new QRCode(data).GetGraphic(10);
```

Passing colors requires naming `icon`, because both `GetGraphic` overloads end in a `bool`:

```csharp
using var colored = new QRCode(data).GetGraphic(10, SKColors.DarkSlateGray, SKColors.White, icon: null);
```

Artistic rendering with round module dots, and base64 output for embedding in HTML or JSON:

```csharp
using var art = new ArtQRCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.White);

string dataUri = "data:image/png;base64," +
    new Base64QRCode(data).GetGraphic(10, SKColors.Black, SKColors.White);
```

### Generate module data only

```csharp
using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode("payload", QRCodeGenerator.ECCLevel.M);

// The module matrix includes the 4-module quiet zone when generated with padding.
var modules = data.ModuleMatrix;
int size = modules.Count;
bool dark = modules[y][x];
```

`GetRawData(QRCodeData.Compression.Uncompressed)` serialises the matrix for caching or transport,
and `new QRCodeData(bytes, compression)` restores it.

## Payload generators

DotQR.Core includes QRCoder's payload generators under `DotQR.PayloadGenerator`, for example:

```csharp
var payload = new PayloadGenerator.WiFi("MyNetwork", "password", PayloadGenerator.WiFi.Authentication.WPA);
using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
```

## Performance

The QRCoder encoding path is kept, with the rendering and checksum hot paths rewritten. Measured
with the included BenchmarkDotNet project on a 500-character payload at ECC level H:

| Operation | QRCoder-derived baseline | DotQR | Change |
|---|---:|---:|---:|
| `QRCode.GetGraphic(10)` | 32.33 ms | 1.454 ms | 22× |
| `PngByteQRCode.GetGraphic(10)` | 973 µs | 491 µs | 2.0× |
| `CreateQrCode` | 1826 µs | 1265 µs | 1.44× |

Raster rendering allocates 545× less. Full before/after data, including the optimisations that were
measured and rejected, is in [docs/benchmarks/RESULTS.md](docs/benchmarks/RESULTS.md). Run it
yourself with `dotnet run -c Release --project tools/DotQR.Benchmarks`.

## Differences from QRCoder

This is a deliberate rewrite of the rendering layer, not a drop-in replacement.

- **`System.Drawing` is gone.** `Bitmap` and `Color` become `SKBitmap` and `SKColor`. If you only
  need bytes, use `DotQR.ByteRenderers` and avoid SkiaSharp entirely.
- **Colors.** `DotQR.Core` uses the graphics-free `RgbaColor` and `Size` types. `RgbaColor.Parse`
  accepts hex strings and CSS Level 4 color names, matching what `ColorTranslator.FromHtml` did.
- **Vector sizes.** `SvgQRCode` and `PostscriptQRCode` take `DotQR.Size` instead of
  `System.Drawing.Size`.
- **`Base64QRCode.ImageType` supports `Png`, `Jpeg` and `Webp`.** SkiaSharp cannot encode GIF, so
  `Gif` was removed and unsupported formats throw rather than returning null.
- **`SvgQRCode.SvgLogo` no longer accepts a `Bitmap`.** Pass PNG bytes, or use the `ToSvgLogo()`
  extension in `DotQR.SkiaSharp`.
- **`net10.0` only.** Multi-targeting was dropped.
- **Assembly names changed.** Update `using QRCoder;` to `using DotQR;`.

## Building and testing

```bash
dotnet build DotQR.slnx
dotnet test  DotQR.slnx
```

Approval tests compare against baselines in each test project. Set `CI=1` to guarantee that a
missing or differing baseline fails instead of being written:

```bash
CI=1 dotnet test DotQR.slnx
```

All three libraries are trim- and AOT-analysis clean (`IsTrimmable`).

## License

MIT, the same license as QRCoder. See [LICENSE.txt](LICENSE.txt), which preserves the original
copyright notice.
