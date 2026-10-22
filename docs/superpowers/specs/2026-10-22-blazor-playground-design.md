# DotQR Blazor Server playground

## Intent and success criteria

Create a runnable .NET 10 Blazor Interactive Server sample for exploring every QR payload and renderer supported by this repository. The user approved server hosting and the proposed comprehensive playground scope. The application references the three local DotQR projects directly, never their NuGet packages. Third-party dependencies already used by those projects remain transitive dependencies.

Success means users can select a payload, edit its fields, change encoding and rendering settings, see an updated preview or actionable validation error, and download the selected output without manually regenerating it.

## Project and hosting

Add `samples/DotQR.Playground` to the solution's samples folder. Use the ASP.NET Core Web SDK and Interactive Server components, with no authentication, persistence, external QR service, or third-party UI framework. Reference DotQR.Core, DotQR.ByteRenderers, and DotQR.SkiaSharp by ProjectReference. Keep library APIs unchanged. Document the run command and the Linux native SkiaSharp dependency in the sample README and link the sample from the repository README.

Server hosting keeps native graphics processing on the server and avoids WebAssembly native-library limitations. This is a local-development sample, not a public multi-tenant service. Payload contents are held in circuit memory and are not intentionally logged or persisted. Explain that secrets such as Wi-Fi passwords and OTP seeds are processed by the server.

## Payload catalogue

Provide plain text and every concrete public payload generator: BezahlCode, BitcoinAddress, BitcoinCashAddress, BitcoinLikeCryptoCurrencyAddress, Bookmark, CalendarEvent, ContactData, Geolocation, Girocode, LitecoinAddress, MMS, Mail, MoneroTransaction, OneTimePassword, PhoneNumber, RussiaPaymentOrder, ShadowSocksConfig, SkypeCall, SMS, SlovenianUpnQr, SwissQrCode, Url, WhatsAppMessage, and WiFi.

Use explicit catalogue definitions and typed payload factories rather than runtime reflection. Group entries by purpose and supply usable example values. Field definitions drive reusable text, multiline, password, numeric, boolean, date/time, and enum editors. Cover public payload configuration, including optional parameters, relevant constructor variants, OTP properties, Russian payment optional fields, and nested Swiss payment contacts/reference details. Conditional fields expose variant-specific options without overwhelming simpler forms. Preserve independent field values when switching payloads. Provide reset-to-example behavior.

Construct actual library payloads rather than reimplementing their serialization. Respect payload-mandated version, ECC, and ECI settings through the payload-aware generation API. Display effective settings and explain any override of user settings. Raw text is a separate explicit mode, not an escape hatch replacing structured payload support.

## Encoding and live updates

Standard QR offers automatic or explicit versions 1–40, supported error-correction levels, UTF-8 options, and ECI. Micro QR exposes M1–M4 and the library's applicable settings using GenerateMicroQrCode, not the standard entry point. Use Micro QR with raw text; explain its limited capacity and encoding/ECC combinations. Disable known unsupported combinations and surface remaining library validation errors.

Debounce text edits by approximately 250 ms; discrete options update immediately. A change builds payload, generates disposable QR data, renders output, and atomically replaces the preview/download state. Cancel or supersede pending updates so stale results cannot replace newer ones. Bound text length, output dimensions, pixels per module, and upload size to prevent accidental excessive allocations. Do not reuse disposed matrices between renders.

## Renderers and controls

Expose all nine renderer classes: PngByteQRCode, BitmapByteQRCode, PdfByteQRCode, SvgQRCode, PostscriptQRCode, ASCIIQRCode, QRCode, ArtQRCode, and Base64QRCode.

Show shared settings only where supported and renderer-specific controls where applicable:

- PNG byte: scale, colors including alpha, quiet zones.
- BMP byte: scale and colors; explain the renderer's quiet-zone behavior rather than offering a nonfunctional toggle.
- PDF: scale, colors, DPI, and the API's quality argument only if it actually affects output.
- SVG: scale or explicit dimensions, colors, quiet zones, sizing mode, and logo configuration.
- PostScript/EPS: scale or explicit dimensions, colors, quiet zones, and EPS selection.
- ASCII: regular or compact mode, module strings/repetition where applicable, quiet zones, inversion for compact output, and line endings.
- Skia QRCode: scale, colors, quiet zones, and optional logo with size, border, and background settings.
- ArtQRCode: scale, colors/background, pixel-size factor, quiet-zone style, background-image style, and applicable finder-pattern/background-image options.
- Base64QRCode: PNG/JPEG/WebP, scale, colors, quiet zones, optional logo and corresponding placement settings.

Review renderer source during implementation to map each setting to a functioning public overload; never show a control whose value is ignored. Accept bounded raster image uploads for logos/backgrounds and use library SVG logo support with safely encoded raster bytes. Do not render uploaded raw SVG or arbitrary user HTML inline.

## User interface and downloads

Use a responsive workspace with payload selection and fields, rendering/encoding configuration, and a preview panel. Show encoded payload text, effective QR version/module dimensions, selected format, output size, and validation feedback. Use normal labeled HTML controls and accessible error/status regions. Warn that decorative settings, insufficient quiet zones, and low color contrast can make codes unscannable; do not claim scanner validation.

Preview image formats as encoded image sources; show ASCII and PostScript in escaped text views. PDF offers a browser preview or download, with a matrix image fallback when embedding is unavailable. A fallback preview must be labeled as a matrix preview rather than the selected renderer's exact output. SVG is displayed through an image source, not raw HTML markup. Base64 output exposes both its image preview and encoded text.

Downloads contain the actual selected renderer output with correct file extension and MIME type. Use a small JavaScript stream/blob download helper when appropriate; do not place sensitive payloads in download URLs or create unbounded server-side caches. Disable downloads after invalid edits so stale output cannot be mistaken for the current settings.

## Code organization

Separate payload catalogue/factories, editor state and field definitions, generation/rendering service, output result metadata, reusable field editors, renderer options, and the main playground page. Keep large payment payload definitions in focused files. Services return output and metadata without depending on Razor UI types. Dispose QRCodeGenerator, QRCodeData, renderers, decoded bitmaps, and encoded image buffers according to their ownership contracts.

## Error handling

Represent expected invalid input, incompatible settings, and capacity failures as user-facing errors without killing the circuit. Log unexpected failures without payload content and show a generic error message. Clear stale preview/download state on failure. Enforce upload and rendering limits before allocation where possible.

## Test and delivery scope

Plan focused coverage for catalogue completeness, default payload creation, optional/variant field mapping, Micro QR constraints, renderer output types and options, invalid inputs, output limits, and stale-result handling. Include sample run instructions and manual interaction guidance. The user's workspace guidance requests avoiding verification of generated files; any unexecuted checks must be reported honestly rather than claimed as passing.

No changes to existing library behavior, dependency packaging, authentication, stored history, deployment infrastructure, or external service integration are included.
