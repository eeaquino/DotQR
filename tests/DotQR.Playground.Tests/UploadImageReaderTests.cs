using DotQR.Playground.Generation;

namespace DotQR.Playground.Tests;

public sealed class UploadImageReaderTests
{
    [Fact]
    public async Task RejectsSvgAndMalformedDataAsync()
    {
        using var svg = new MemoryStream(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'></svg>"));
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(svg, svg.Length, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnforcesActualSizeEvenWhenDeclaredLengthIsFalseAsync()
    {
        using var stream = new MemoryStream(new byte[GenerationLimits.MAX_UPLOAD_BYTES + 1]);
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsOversizedDecodedDimensionsAsync()
    {
        using var stream = new MemoryStream(ImageFixture.Png(2049, 1));
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, stream.Length, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcceptsSmallRasterAndReturnsCanonicalPngAsync()
    {
        using var stream = new MemoryStream(ImageFixture.Png(16, 16));
        var result = await UploadImageReader.ReadAsync(stream, stream.Length, TestContext.Current.CancellationToken);
        Assert.StartsWith("89504E47", Convert.ToHexString(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsTruncatedRasterAsync()
    {
        var png = ImageFixture.Png(16, 16);
        using var stream = new MemoryStream(png[..24]);
        await Assert.ThrowsAsync<ArgumentException>(() => UploadImageReader.ReadAsync(stream, stream.Length, TestContext.Current.CancellationToken));
    }
}
