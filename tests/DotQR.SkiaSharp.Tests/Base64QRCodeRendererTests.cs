using SkiaSharp;
namespace DotQRTests.SkiaSharp;

public class Base64QRCodeRendererTests
{
    private readonly QRCodeData _data;

    public Base64QRCodeRendererTests()
    {
        var gen = new QRCodeGenerator();
        _data = gen.CreateQrCode("This is a quick test! 123#?", QRCodeGenerator.ECCLevel.L);
    }

    [Fact]
    public void can_render_base64_qrcode_blackwhite()
    {
        var pngCodeGfx = new PngByteQRCode(_data).GetGraphic(5);
        var base64QRCode = new Base64QRCode(_data).GetGraphic(5);
        base64QRCode.ShouldBe(Convert.ToBase64String(pngCodeGfx));
    }

    [Fact]
    public void can_render_base64_qrcode_noquietzones()
    {
        var pngCodeGfx = new PngByteQRCode(_data).GetGraphic(5, false);
        var base64QRCode = new Base64QRCode(_data).GetGraphic(5, SKColors.Black, SKColors.White, false);
        base64QRCode.ShouldBe(Convert.ToBase64String(pngCodeGfx));
    }

    [Fact]
    public void can_render_base64_qrcode_color()
    {
        var pngCodeGfx = new PngByteQRCode(_data).GetGraphic(5, new byte[] { 255, 0, 0 }, new byte[] { 0, 0, 255 });
        var base64QRCode = new Base64QRCode(_data).GetGraphic(5, SKColors.Red, SKColors.Blue);
        base64QRCode.ShouldBe(Convert.ToBase64String(pngCodeGfx));
    }

    [Fact]
    public void can_render_base64_qrcode_transparent()
    {
        var pngCodeGfx = new PngByteQRCode(_data).GetGraphic(5, new byte[] { 0, 255, 0, 255 }, new byte[] { 255, 255, 255, 0 });
        var base64QRCode = new Base64QRCode(_data).GetGraphic(5, SKColors.Lime, SKColors.Transparent);
        base64QRCode.ShouldBe(Convert.ToBase64String(pngCodeGfx));
    }

    [Fact]
    public void can_render_base64_qrcode_jpeg()
    {
        var base64QRCode = new Base64QRCode(_data).GetGraphic(5, SKColors.Black, SKColors.White, true, Base64QRCode.ImageType.Jpeg);
        var jpegBytes = Convert.FromBase64String(base64QRCode);

        using var decoded = SKBitmap.Decode(jpegBytes);
        decoded.ShouldNotBeNull();
        decoded.Width.ShouldBe(_data.ModuleMatrix.Count * 5);
        decoded.Height.ShouldBe(_data.ModuleMatrix.Count * 5);

        // JPEG is lossy, so assert the module polarity survives rather than exact pixels:
        // a decoder must still see dark modules as dark and light modules as light.
        for (var y = 0; y < _data.ModuleMatrix.Count; y++)
        {
            for (var x = 0; x < _data.ModuleMatrix.Count; x++)
            {
                var expectedDark = _data.ModuleMatrix[y][x];
                var px = x * 5 + 2;
                var py = y * 5 + 2;
                var luminance = decoded.GetPixel(px, py).Red;
                (luminance < 128).ShouldBe(expectedDark, $"module {x},{y} luminance {luminance}");
            }
        }
    }
}
