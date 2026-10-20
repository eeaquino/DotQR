using SkiaSharp;
using static DotQR.QRCodeGenerator;

namespace DotQRTests.SkiaSharp;

/// <summary>
/// Semantic assertions that do not depend on any approved baseline, so a renderer regression still
/// fails after the PNG baselines are regenerated for a new SkiaSharp version.
/// </summary>
public class RasterInvariantTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void DotQr_raster_has_expected_dimensions_and_module_colors(int pixelsPerModule)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("invariant", ECCLevel.M);
        using var bitmap = new QRCode(data).GetGraphic(pixelsPerModule);

        var modules = data.ModuleMatrix.Count;
        bitmap.Width.ShouldBe(modules * pixelsPerModule);
        bitmap.Height.ShouldBe(modules * pixelsPerModule);

        // Sample the centre of every module and assert the exact expected colour.
        for (var y = 0; y < modules; y++)
        {
            for (var x = 0; x < modules; x++)
            {
                var expected = data.ModuleMatrix[y][x] ? SKColors.Black : SKColors.White;
                var px = x * pixelsPerModule + pixelsPerModule / 2;
                var py = y * pixelsPerModule + pixelsPerModule / 2;
                bitmap.GetPixel(px, py).ShouldBe(expected, $"module {x},{y} at pixel {px},{py}");
            }
        }
    }

    [Fact]
    public void DotQr_raster_preserves_transparency_of_the_light_color()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("transparent", ECCLevel.M);
        using var bitmap = new QRCode(data).GetGraphic(5, SKColors.Black, SKColors.Transparent, icon: null);

        // The top-left pixel is inside the quiet zone, so it is a light module.
        bitmap.GetPixel(0, 0).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void DotQr_raster_without_quiet_zones_is_eight_modules_smaller()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("size", ECCLevel.M);

        using var withQuiet = new QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, icon: null, drawQuietZones: true);
        using var withoutQuiet = new QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, icon: null, drawQuietZones: false);

        (withQuiet.Width - withoutQuiet.Width).ShouldBe(8 * 4);
    }

    [Fact]
    public void ArtQr_raster_has_expected_dimensions_and_solid_finder_patterns()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("invariant", ECCLevel.M);
        using var bitmap = new ArtQRCode(data).GetGraphic(10, SKColors.Black, SKColors.White, SKColors.White);

        var modules = data.ModuleMatrix.Count;
        bitmap.Width.ShouldBe(modules * 10);

        // The centre of the top-left finder pattern is always drawn as a solid rectangle.
        var centre = 4 * 10 + 3 * 10 + 5;
        bitmap.GetPixel(centre, centre).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Base64_png_matches_the_byte_renderer_output()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("base64", ECCLevel.M);

        using var base64 = new Base64QRCode(data);
        var text = base64.GetGraphic(4);

        using var png = new PngByteQRCode(data);
        Convert.FromBase64String(text).ShouldBe(png.GetGraphic(4));
    }

    [Fact]
    public void Base64_webp_encodes_a_decodable_image()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("base64", ECCLevel.M);

        using var base64 = new Base64QRCode(data);
        var bytes = Convert.FromBase64String(base64.GetGraphic(4, SKColors.Black, SKColors.White, true, Base64QRCode.ImageType.Webp));

        using var decoded = SKBitmap.Decode(bytes);
        decoded.ShouldNotBeNull();
        decoded.Width.ShouldBe(data.ModuleMatrix.Count * 4);
    }

    [Fact]
    public void Svg_logo_extension_encodes_the_icon_as_png()
    {
        using var icon = HelperFunctions.GetIconBitmap();
        var logo = icon.ToSvgLogo();

        logo.GetMediaType().ShouldBe(DotQR.SvgQRCode.SvgLogo.MediaType.PNG);

        var dataUri = logo.GetDataUri();
        var base64 = dataUri.Substring(dataUri.IndexOf("base64,", StringComparison.Ordinal) + 7);
        var embedded = Convert.FromBase64String(base64);

        // ToSvgLogo re-encodes the bitmap, so the payload is a valid PNG with the icon's
        // dimensions rather than the original file's bytes.
        using var decoded = SKBitmap.Decode(embedded);
        decoded.ShouldNotBeNull();
        decoded.Width.ShouldBe(icon.Width);
        decoded.Height.ShouldBe(icon.Height);
        decoded.GetPixel(0, 0).ShouldBe(icon.GetPixel(0, 0));
    }
}
