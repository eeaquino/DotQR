using BenchmarkDotNet.Attributes;
using DotQR;
using static DotQR.QRCodeGenerator;

namespace DotQR.Benchmarks;

/// <summary>
/// End-to-end QR generation, exercising mask selection and scoring.
/// </summary>
[MemoryDiagnoser]
public class QRCodeGeneratorBenchmarks
{
    private QRCodeGenerator _generator = null!;
    private string _payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new QRCodeGenerator();
        _payload = new string('x', 500);
    }

    [GlobalCleanup]
    public void Cleanup() => _generator.Dispose();

    [Benchmark]
    public void CreateQrCode_500chars_ECC_H()
    {
        using var data = _generator.CreateQrCode(_payload, ECCLevel.H);
    }

    [Benchmark]
    public void CreateQrCode_ShortPayload_ECC_L()
    {
        using var data = _generator.CreateQrCode("https://example.com/path", ECCLevel.L);
    }
}
