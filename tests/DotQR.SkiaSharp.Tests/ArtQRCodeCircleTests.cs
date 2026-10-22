using SkiaSharp;

namespace DotQRTests.SkiaSharp;

public sealed class ArtQRCodeCircleTests
{
    [Theory]
    [InlineData(8, 1)]
    [InlineData(9, 1)]
    [InlineData(20, 1)]
    [InlineData(8, 0.8)]
    [InlineData(9, 0.8)]
    [InlineData(20, 0.93)]
    public void DataModulesRenderAsIndependentCircles(int pixelsPerModule, double factor)
    {
        using var data = new QRCodeData(1, true);
        (int X, int Y)[] dots = [(14, 14), (15, 14), (14, 15), (15, 15), (18, 17)];
        foreach (var (x, y) in dots)
            data.ModuleMatrix[y][x] = true;
        using var renderer = new ArtQRCode(data);
        using var actual = renderer.GetGraphic(pixelsPerModule, SKColors.Black, SKColors.White, SKColors.White, pixelSizeFactor: factor);
        using var expected = new SKBitmap(new SKImageInfo(actual.Width, actual.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(expected))
        using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
        {
            canvas.Clear(SKColors.White);
            foreach (var (x, y) in dots)
                canvas.DrawCircle((x + 0.5f) * pixelsPerModule, (y + 0.5f) * pixelsPerModule, (float)(pixelsPerModule * factor / 2), paint);
        }
        Assert.True(expected.GetPixelSpan().SequenceEqual(actual.GetPixelSpan()),
            $"Art modules must match independent Skia circles at scale {pixelsPerModule}, factor {factor}.");
    }
}
