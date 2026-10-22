using DotQR.Playground.Generation;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotQR.Playground.Tests;

public sealed class ByteRendererServiceTests
{
    private readonly QrGenerationService _service = new(NullLogger<QrGenerationService>.Instance);

    [Theory]
    [InlineData(RendererKind.Png, "image/png", "89504E47")]
    [InlineData(RendererKind.Bitmap, "image/bmp", "424D")]
    [InlineData(RendererKind.Pdf, "application/pdf", "25504446")]
    public void BinaryOutputHasExpectedFormat(RendererKind kind, string mime, string prefix)
    {
        var result = _service.Generate(QrGenerationServiceTests.Request() with { Renderer = new() { Kind = kind } }).Result!;
        Assert.Equal(mime, result.MimeType);
        Assert.StartsWith(prefix, Convert.ToHexString(result.Content), StringComparison.Ordinal);
        if (kind == RendererKind.Pdf) Assert.Contains("matrix", result.PreviewNotice!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RendererKind.Svg, "<svg")]
    [InlineData(RendererKind.Postscript, "%!PS-Adobe")]
    public void VectorOutputsContainActualRendererText(RendererKind kind, string marker)
    {
        var result = _service.Generate(QrGenerationServiceTests.Request() with { Renderer = new() { Kind = kind } }).Result!;
        Assert.Contains(marker, Encoding.UTF8.GetString(result.Content), StringComparison.Ordinal);
    }

    [Fact]
    public void AsciiOptionsReachRenderer()
    {
        var request = QrGenerationServiceTests.Request() with { Renderer = new() { Kind = RendererKind.Ascii, DarkColorString = "##", WhiteSpaceString = "..", EndOfLine = "\r\n" } };
        var result = _service.Generate(request).Result!;
        Assert.Contains("##", result.TextPreview!, StringComparison.Ordinal);
        Assert.Contains("\r\n", result.TextPreview!, StringComparison.Ordinal);
        Assert.Equal(result.TextPreview, Encoding.UTF8.GetString(result.Content));
    }

    [Fact]
    public void EpsAndSvgViewBoxSettingsReachRenderer()
    {
        var request = QrGenerationServiceTests.Request();
        var eps = _service.Generate(request with { Renderer = new() { Kind = RendererKind.Postscript, EpsFormat = true } }).Result!;
        Assert.Contains("EPSF", eps.TextPreview!, StringComparison.Ordinal);
        Assert.EndsWith(".eps", eps.FileName, StringComparison.Ordinal);
        var svg = _service.Generate(request with { Renderer = new() { Kind = RendererKind.Svg, SizingMode = SvgQRCode.SizingMode.ViewBoxAttribute } }).Result!;
        Assert.Contains("viewBox", Encoding.UTF8.GetString(svg.Content), StringComparison.Ordinal);
        Assert.DoesNotContain(" width=", Encoding.UTF8.GetString(svg.Content).Split('>')[0], StringComparison.Ordinal);
    }
}
