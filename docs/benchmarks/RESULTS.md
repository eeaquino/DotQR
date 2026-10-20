# Benchmark results: before and after the performance work

Recorded with `tools/DotQR.Benchmarks` (`--job short --warmupCount 2 --iterationCount 5`).
Raw output is in `before.txt` and `after.txt`.

Payload: 500 characters, ECC level H, unless noted. Windows, .NET 10.0.402, SkiaSharp 4.153.1.

## Renderers (SkiaSharp)

| Benchmark | Before | After | Change |
|---|---:|---:|---:|
| `QRCode.GetGraphic(10)` | 32.33 ms | 1.454 ms | **22.2x faster** |
| `QRCode.GetGraphic(20)` | 47.78 ms | 5.501 ms | **8.7x faster** |
| `ArtQRCode.GetGraphic(10)` | 65.23 ms | 28.138 ms | **2.3x faster** |
| `QRCode.GetGraphic(10)` allocation | 2,811,619 B | 5,154 B | **545x less** |

Two causes, measured with a phase probe:

- `SKBitmap.SetPixel` costs about 2 us per pixel. Writing the 121x121 module bitmap took
  29.9 ms through `SetPixel` against 0.023 ms through `GetPixelSpan`. All module writes now go
  through a span.
- Skia's nearest-neighbour blit cost 6.0 ms for the 1210x1210 output; expanding module rows
  directly costs 1.4 ms and is pixel-exact by construction.

`ArtQRCode` additionally stopped building light dots when `lightColor == backgroundColor`, where
painting them is a no-op.

## PNG encoding (ByteRenderers)

| Benchmark | Before | After | Change |
|---|---:|---:|---:|
| `PngByteQRCode.GetGraphic(10)` black/white | 973.2 us | 490.6 us | **2.0x faster** |
| `PngByteQRCode.GetGraphic(10)` coloured | 911.8 us | 512.7 us | **1.8x faster** |
| allocation | 20.04 KB | 16.31 KB | 19% less |

Adler-32 uses 5552-byte blocks with deferred modulo, CRC-32 uses a slicing-by-8 table, the IDAT
payload is deflated straight into the output stream instead of via a staging `MemoryStream`, and
`GetBytes` no longer copies and re-parses the stream. Every byte-renderer approval is unchanged,
so the output is byte-identical.

## QR generation (Core)

| Benchmark | Before | After | Change |
|---|---:|---:|---:|
| `CreateQrCode` 500 chars, ECC H | 1825.54 us | 1264.72 us | **1.44x faster** |
| `CreateQrCode` short payload, ECC L | 48.34 us | 48.10 us | unchanged |
| allocation | 12.41 KB | 12.41 KB | unchanged |

Mask scoring aborts as soon as a candidate exceeds the best score so far. The selected mask is
unchanged: the `QRGeneratorTests` module-matrix hashes still match.

`GetRawData` also stopped allocating a `Queue<int>` per module; that path is not on the
`CreateQrCode` hot path, so it does not show up above.

## Planned optimisations that were deliberately not applied

- **Winning-mask row replacement.** Re-applying the winning mask at the end is roughly 7,300
  iterations of a cheap XOR for a version-40 code. Avoiding it would require cloning the whole
  matrix on each improving candidate, which is strictly more work.
- **Popcount-based penalty tally.** Counting black modules is already inside a loop that reads the
  same bits; a separate popcount pass measured as neutral and would have added a pass for no gain.
- **`Polynom` on span-backed coefficients.** `Polynom` already rents its backing array from
  `ArrayPool`, so there was no allocation to remove, and the remaining cost is Reed-Solomon
  arithmetic whose correctness is load-bearing.
