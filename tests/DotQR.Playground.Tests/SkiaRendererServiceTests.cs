using DotQR.Playground.Generation;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace DotQR.Playground.Tests;

public sealed class SkiaRendererServiceTests
{
    private readonly QrGenerationService _service = new(NullLogger<QrGenerationService>.Instance);

    [Theory]
    [InlineData(RendererKind.Skia)]
    [InlineData(RendererKind.Art)]
    public void SkiaRenderersProducePng(RendererKind kind)
    {
        var result = _service.Generate(QrGenerationServiceTests.Request() with { Renderer = new() { Kind = kind } });
        Assert.Null(result.Error);
        Assert.StartsWith("89504E47", Convert.ToHexString(result.Result!.Content), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Base64QRCode.ImageType.Png, "image/png", "89504E47")]
    [InlineData(Base64QRCode.ImageType.Jpeg, "image/jpeg", "FFD8")]
    [InlineData(Base64QRCode.ImageType.Webp, "image/webp", "52494646")]
    public void Base64UsesSelectedImageFormat(Base64QRCode.ImageType type, string mime, string prefix)
    {
        var result = _service.Generate(QrGenerationServiceTests.Request() with { Renderer = new() { Kind = RendererKind.Base64, ImageType = type } }).Result!;
        Assert.StartsWith($"data:{mime};base64,", result.PreviewDataUri!, StringComparison.Ordinal);
        Assert.StartsWith(prefix, Convert.ToHexString(Convert.FromBase64String(result.TextPreview!)), StringComparison.Ordinal);
        Assert.Equal(result.TextPreview, Encoding.UTF8.GetString(result.Content));
    }

    [Fact]
    public void ArtworkAndLogoOptionsChangeOutput()
    {
        var request = QrGenerationServiceTests.Request() with { Renderer = new() { Kind = RendererKind.Art } };
        var plain = _service.Generate(request).Result!.Content;
        var changed = _service.Generate(request with { Renderer = request.Renderer with { PixelSizeFactor = 1 } }).Result!.Content;
        Assert.False(plain.SequenceEqual(changed));
        request = request with { Renderer = new() { Kind = RendererKind.Skia } };
        plain = _service.Generate(request).Result!.Content;
        changed = _service.Generate(request with { Renderer = request.Renderer with { Logo = ImageFixture.Png(16, 16) } }).Result!.Content;
        Assert.False(plain.SequenceEqual(changed));
    }
}

internal static class ImageFixture
{
    internal static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Red);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
