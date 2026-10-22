using DotQR.Playground.Generation;

namespace DotQR.Playground.Tests;

public sealed class GenerationLimitsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(int.MaxValue)]
    public void InvalidScaleRejectedBeforeMultiplication(int scale) =>
        Assert.Throws<ArgumentException>(() => GenerationLimits.Validate(new RendererOptions { PixelsPerModule = scale }, 185));

    [Fact]
    public void OversizedImageRejectedButBoundaryAccepted()
    {
        GenerationLimits.Validate(new RendererOptions { PixelsPerModule = 32 }, 64);
        Assert.Throws<ArgumentException>(() => GenerationLimits.Validate(new RendererOptions { PixelsPerModule = 32 }, 65));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.1)]
    public void InvalidArtFactorRejected(double factor) => Assert.Throws<ArgumentException>(() =>
        GenerationLimits.Validate(new RendererOptions { Kind = RendererKind.Art, PixelSizeFactor = factor }, 29));

    [Fact]
    public void UnbalancedAsciiModuleStringsRejected() => Assert.Throws<ArgumentException>(() =>
        GenerationLimits.Validate(new RendererOptions { Kind = RendererKind.Ascii, DarkColorString = "###", WhiteSpaceString = " " }, 29));
}
