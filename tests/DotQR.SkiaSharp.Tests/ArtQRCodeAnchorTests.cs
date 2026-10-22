using SkiaSharp;

namespace DotQRTests.SkiaSharp;

public sealed class ArtQRCodeAnchorTests
{
    // Catches ignored shape selections, wrong anchor positions, and missing inner cutouts.
    [Theory]
    [InlineData(ArtQRCode.FinderPatternShape.Square, true)]
    [InlineData(ArtQRCode.FinderPatternShape.Circle, true)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare, true)]
    [InlineData(ArtQRCode.FinderPatternShape.Square, false)]
    [InlineData(ArtQRCode.FinderPatternShape.Circle, false)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare, false)]
    public void AllAnchorsUseSelectedShape(ArtQRCode.FinderPatternShape shape, bool quietZones)
    {
        using var data = QRCodeGenerator.GenerateQrCode("Anchor shapes", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        using var bitmap = renderer.GetGraphic(20, shape, SKColors.Black, SKColors.White, SKColors.White, drawQuietZones: quietZones);
        var start = quietZones ? 80 : 0;
        var far = bitmap.Width - start - 140;
        (int X, int Y)[] anchors = [(start, start), (far, start), (start, far)];
        foreach (var (x, y) in anchors)
        {
            Assert.Equal(SKColors.Black, bitmap.GetPixel(x + 70, y + 5)); // outer ring
            Assert.Equal(SKColors.White, bitmap.GetPixel(x + 70, y + 30)); // inner gap
            Assert.Equal(SKColors.Black, bitmap.GetPixel(x + 70, y + 70)); // center
            Assert.Equal(shape == ArtQRCode.FinderPatternShape.Square ? SKColors.Black : SKColors.White,
                bitmap.GetPixel(x + 1, y + 1));
            Assert.Equal(shape == ArtQRCode.FinderPatternShape.Square ? SKColors.Black : SKColors.White,
                bitmap.GetPixel(x + 41, y + 41)); // center corner
            Assert.Equal(shape == ArtQRCode.FinderPatternShape.Circle ? SKColors.White : SKColors.Black,
                bitmap.GetPixel(x + 7, y + 25)); // distinguishes rounded square from circle
        }
    }

    [Fact]
    public void ExplicitSquareMatchesExistingDefault()
    {
        using var data = QRCodeGenerator.GenerateQrCode("Default anchors", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        using var original = renderer.GetGraphic(9);
        using var square = renderer.GetGraphic(9, ArtQRCode.FinderPatternShape.Square, SKColors.Black, SKColors.White, SKColors.Transparent);
        Assert.True(original.GetPixelSpan().SequenceEqual(square.GetPixelSpan()));
    }

    [Theory]
    [InlineData(ArtQRCode.FinderPatternShape.Circle)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare)]
    public void CustomImageOverridesShape(ArtQRCode.FinderPatternShape shape)
    {
        using var data = QRCodeGenerator.GenerateQrCode("Custom anchors", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        using var finder = new SKBitmap(7, 7);
        finder.Erase(SKColors.Red);
        using var original = renderer.GetGraphic(10, SKColors.Black, SKColors.White, SKColors.White, finderPatternImage: finder);
        using var shaped = renderer.GetGraphic(10, shape, SKColors.Black, SKColors.White, SKColors.White, finderPatternImage: finder);
        Assert.True(original.GetPixelSpan().SequenceEqual(shaped.GetPixelSpan()));
    }

    [Theory]
    [InlineData(ArtQRCode.FinderPatternShape.Circle)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare)]
    public void MatchingOpaqueColorsLeaveNoTransparentSeams(ArtQRCode.FinderPatternShape shape)
    {
        using var data = QRCodeGenerator.GenerateQrCode("Anchor coverage", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        using var bitmap = renderer.GetGraphic(9, shape, SKColors.Black, SKColors.Black, SKColors.Transparent);
        for (var y = 36; y < 99; y++)
            for (var x = 36; x < 99; x++)
                Assert.Equal(SKColors.Black, bitmap.GetPixel(x, y));
    }

    [Theory]
    [InlineData(ArtQRCode.FinderPatternShape.Circle)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare)]
    public void TranslucentFindersCompositeOnceOntoBackground(ArtQRCode.FinderPatternShape shape)
    {
        using var data = QRCodeGenerator.GenerateQrCode("Alpha coverage", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        var translucent = SKColors.Black.WithAlpha(128);
        using var matching = renderer.GetGraphic(9, shape, translucent, translucent, SKColors.Transparent);
        for (var y = 36; y < 99; y++)
            for (var x = 36; x < 99; x++)
                Assert.InRange((int)matching.GetPixel(x, y).Alpha, 127, 129);

        using var bitmap = renderer.GetGraphic(20, shape, translucent, SKColors.White.WithAlpha(128), SKColors.Red);
        Assert.Equal(new SKColor(127, 0, 0), bitmap.GetPixel(150, 150)); // dark center over red
        Assert.Equal(new SKColor(255, 128, 128), bitmap.GetPixel(150, 110)); // light gap over red
    }

    [Theory]
    [InlineData(ArtQRCode.FinderPatternShape.Circle, true)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare, true)]
    [InlineData(ArtQRCode.FinderPatternShape.Circle, false)]
    [InlineData(ArtQRCode.FinderPatternShape.RoundedSquare, false)]
    public void ShapeDoesNotChangePixelsOutsideFinders(ArtQRCode.FinderPatternShape shape, bool quietZones)
    {
        using var data = QRCodeGenerator.GenerateQrCode("Unchanged data dots", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        using var original = renderer.GetGraphic(9, SKColors.Black, SKColors.White, SKColors.Red, drawQuietZones: quietZones);
        using var shaped = renderer.GetGraphic(9, shape, SKColors.Black, SKColors.White, SKColors.Red, drawQuietZones: quietZones);
        var start = quietZones ? 36 : 0;
        var far = original.Width - start - 63;
        for (var y = 0; y < original.Height; y++)
            for (var x = 0; x < original.Width; x++)
            {
                var inLeft = x >= start && x < start + 63;
                var inTop = y >= start && y < start + 63;
                var inRight = x >= far && x < far + 63;
                var inBottom = y >= far && y < far + 63;
                if (!(inLeft && (inTop || inBottom) || inRight && inTop))
                    Assert.Equal(original.GetPixel(x, y), shaped.GetPixel(x, y));
            }
    }

    [Fact]
    public void UnknownShapeIsRejected()
    {
        using var data = QRCodeGenerator.GenerateQrCode("Invalid shape", QRCodeGenerator.ECCLevel.H);
        using var renderer = new ArtQRCode(data);
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.GetGraphic(10, (ArtQRCode.FinderPatternShape)99,
            SKColors.Black, SKColors.White, SKColors.White));
    }
}
