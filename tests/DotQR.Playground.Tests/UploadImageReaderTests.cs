using DotQR.Playground.Generation;

namespace DotQR.Playground.Tests;

public sealed class UploadImageReaderTests
{
    [Fact]
    public async Task RejectsSvgAndMalformedData()
    {
        using var svg = new MemoryStream(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'></svg>"));
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(svg, svg.Length));
    }

    [Fact]
    public async Task EnforcesActualSizeEvenWhenDeclaredLengthIsFalse()
    {
        using var stream = new MemoryStream(new byte[GenerationLimits.MaxUploadBytes + 1]);
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, 1));
    }

    [Fact]
    public async Task RejectsOversizedDecodedDimensions()
    {
        using var stream = new MemoryStream(ImageFixture.Png(2049, 1));
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, stream.Length));
    }

    [Fact]
    public async Task AcceptsSmallRasterAndReturnsCanonicalPng()
    {
        using var stream = new MemoryStream(ImageFixture.Png(16, 16));
        var result = await UploadImageReader.ReadAsync(stream, stream.Length);
        Assert.StartsWith("89504E47", Convert.ToHexString(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsTruncatedRaster()
    {
        var png = ImageFixture.Png(16, 16);
        using var stream = new MemoryStream(png[..24]);
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, stream.Length));
    }
}
