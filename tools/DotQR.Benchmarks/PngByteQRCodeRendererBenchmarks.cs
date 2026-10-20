using BenchmarkDotNet.Attributes;
using DotQR;
using static DotQR.QRCodeGenerator;

namespace DotQR.Benchmarks;

/// <summary>
/// PNG encoding, exercising the hand-written deflate/CRC/Adler path.
/// </summary>
[MemoryDiagnoser]
public class PngByteQRCodeRendererBenchmarks
{
    private QRCodeGenerator _generator = null!;
    private QRCodeData _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new QRCodeGenerator();
        _data = _generator.CreateQrCode(new string('x', 500), ECCLevel.H);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _data.Dispose();
        _generator.Dispose();
    }

    [Benchmark(Baseline = true)]
    public byte[] BlackWhite() => new PngByteQRCode(_data).GetGraphic(10);

    [Benchmark]
    public byte[] Colored() => new PngByteQRCode(_data).GetGraphic(10, new byte[] { 255, 0, 0 }, new byte[] { 0, 0, 255 });
}
