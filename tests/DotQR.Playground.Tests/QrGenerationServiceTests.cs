using DotQR.Playground.Generation;
using DotQR.Playground.Payloads;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotQR.Playground.Tests;

public sealed class QrGenerationServiceTests
{
    private readonly QrGenerationService _service = new(NullLogger<QrGenerationService>.Instance);
    internal static GenerationRequest Request(string id = "text") => new(PayloadCatalog.All.Single(p => p.Id == id), PayloadCatalog.CreateExample(id), new(), new());

    [Fact]
    public void ExplicitStandardVersionIsRespected()
    {
        var request = Request() with { Encoding = new() { Version = 5 } };
        Assert.Equal(5, _service.Generate(request).Result!.Version);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-3)]
    [InlineData(-4)]
    public void MicroUsesItsDedicatedVersion(int version)
    {
        var request = Request() with { Encoding = new() { IsMicro = true, Version = version, EccLevel = QRCodeGenerator.ECCLevel.Default } };
        request.Values["text"] = "12345";
        var result = _service.Generate(request);
        Assert.Null(result.Error);
        Assert.Equal(version, result.Result!.Version);
    }

    [Fact]
    public void MandatedPaymentMetadataOverridesGenericSettings()
    {
        var request = Request("SlovenianUpnQr") with { Encoding = new() { Version = 1, EccLevel = QRCodeGenerator.ECCLevel.H } };
        var result = _service.Generate(request);
        Assert.Null(result.Error);
        Assert.Equal(15, result.Result!.Version);
        Assert.Contains("ECC M", result.Result.EffectiveEncoding, StringComparison.Ordinal);
    }

    [Fact]
    public void SwissUsesRequiredEccAndUtf8()
    {
        var result = _service.Generate(Request("SwissQrCode"));
        Assert.Null(result.Error);
        Assert.Contains("Utf8", result.Result!.EffectiveEncoding, StringComparison.Ordinal);
    }

    [Fact]
    public void MicroRejectsUnsupportedEccAndArt()
    {
        var request = Request() with { Encoding = new() { IsMicro = true, Version = -1, EccLevel = QRCodeGenerator.ECCLevel.H } };
        Assert.NotNull(_service.Generate(request).Error);
        request = request with { Encoding = request.Encoding with { EccLevel = QRCodeGenerator.ECCLevel.Default }, Renderer = new() { Kind = RendererKind.Art } };
        Assert.NotNull(_service.Generate(request).Error);
    }

    [Fact]
    public void InvalidPaymentReturnsActionableErrorNotOutput()
    {
        var request = Request("Girocode");
        request.Values["iban"] = "bad";
        var result = _service.Generate(request);
        Assert.Null(result.Result);
        Assert.Contains("IBAN", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
