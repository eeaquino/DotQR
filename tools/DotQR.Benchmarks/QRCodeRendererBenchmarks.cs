using BenchmarkDotNet.Attributes;
using DotQR;
using SkiaSharp;
using static DotQR.QRCodeGenerator;

namespace DotQR.Benchmarks;

/// <summary>
/// SkiaSharp raster rendering, exercising the module-resolution plus nearest-neighbour path.
/// </summary>
[MemoryDiagnoser]
public class QRCodeRendererBenchmarks
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
    public void Render_10px()
    {
        using var bitmap = new QRCode(_data).GetGraphic(10);
    }

    [Benchmark]
    public void Render_20px()
    {
        using var bitmap = new QRCode(_data).GetGraphic(20);
    }

    [Benchmark]
    public void ArtRender_10px()
    {
        using var bitmap = new ArtQRCode(_data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.White);
    }
}
