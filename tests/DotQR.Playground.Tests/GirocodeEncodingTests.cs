using DotQR.Playground.Generation;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotQR.Playground.Tests;

public sealed class GirocodeEncodingTests
{
    [Theory]
    [InlineData("UTF_8", QRCodeGenerator.EciMode.Utf8, "1")]
    [InlineData("ISO_8859_1", QRCodeGenerator.EciMode.Iso8859_1, "2")]
    [InlineData("ISO_8859_2", QRCodeGenerator.EciMode.Iso8859_2, "3")]
    public void SupportedEciUsesSelectedCharsetAndRequestedVersion(string charset, QRCodeGenerator.EciMode eci, string header)
    {
        var request = QrGenerationServiceTests.Request("Girocode") with { Encoding = new() { Version = 8, EccLevel = QRCodeGenerator.ECCLevel.H } };
        request.Values["encoding"] = charset;
        request.Values["name"] = "Müller";
        var service = new QrGenerationService(NullLogger<QrGenerationService>.Instance);
        var result = service.Generate(request).Result!;
        var literal = $"BCD\n001\n{header}\nSCT\nCOBADEFFXXX\nMüller\nDE89370400440532013000\nEUR12.50\n\n\nDemo invoice\n";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(literal, QRCodeGenerator.ECCLevel.M, eciMode: eci, requestedVersion: 8);
        using var png = new PngByteQRCode(data);
        Assert.Equal(8, result.Version);
        Assert.Equal(literal, result.PayloadText);
        Assert.Equal(png.GetGraphic(8, request.Renderer.Dark, request.Renderer.Light), result.Content);
    }

    [Fact]
    public void LegacyCharsetUsesExactPaymentBytes()
    {
        var request = QrGenerationServiceTests.Request("Girocode");
        request.Values["encoding"] = "ISO_8859_7";
        request.Values["name"] = "Αλέξανδρος";
        var service = new QrGenerationService(NullLogger<QrGenerationService>.Instance);
        var result = service.Generate(request).Result!;
        var literal = "BCD\n001\n6\nSCT\nCOBADEFFXXX\nΑλέξανδρος\nDE89370400440532013000\nEUR12.50\n\n\nDemo invoice\n";
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Encoding.GetEncoding("iso-8859-7").GetBytes(literal), QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        Assert.Equal(literal, result.PayloadText);
        Assert.Equal(png.GetGraphic(8, request.Renderer.Dark, request.Renderer.Light), result.Content);
    }
}
