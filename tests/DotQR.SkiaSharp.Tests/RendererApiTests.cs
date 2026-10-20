using SkiaSharp;

namespace DotQRTests.SkiaSharp;

/// <summary>
/// Confirms the renderer overloads can be called in their natural form.
/// </summary>
/// <remarks>
/// These look trivial, but each one previously failed to compile: two <c>GetGraphic</c> overloads
/// both accepted "pixels, dark, light" and both ended in an optional <c>bool</c>, so callers had to
/// write <c>icon: null</c> to pick one. These tests keep that trap from coming back.
/// </remarks>
public class RendererApiTests
{
    private static QRCodeData CreateData()
    {
        var generator = new QRCodeGenerator();
        return generator.CreateQrCode("api shape", QRCodeGenerator.ECCLevel.M);
    }

    [Fact]
    public void QRCode_GetGraphic_accepts_colors_without_naming_an_optional()
    {
        using var data = CreateData();
        using var bitmap = new DotQR.QRCode(data).GetGraphic(4, SKColors.DarkGreen, SKColors.White);
        bitmap.Width.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void QRCode_GetGraphic_accepts_named_draw_quiet_zones_with_colors()
    {
        using var data = CreateData();
        using var withQuietZones = new DotQR.QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, drawQuietZones: true);
        using var without = new DotQR.QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, drawQuietZones: false);

        (withQuietZones.Width - without.Width).ShouldBe(8 * 4);
    }

    [Fact]
    public void QRCode_GetGraphic_accepts_an_icon_without_naming_draw_quiet_zones()
    {
        using var data = CreateData();
        using var icon = HelperFunctions.GetIconBitmap();
        using var bitmap = new DotQR.QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, icon: icon);
        bitmap.Width.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void QRCode_GetGraphic_accepts_an_icon_and_quiet_zone_choice_together()
    {
        using var data = CreateData();
        using var icon = HelperFunctions.GetIconBitmap();
        using var bitmap = new DotQR.QRCode(data).GetGraphic(
            4,
            SKColors.Black,
            SKColors.White,
            icon: icon,
            iconSizePercent: 20,
            iconBorderWidth: 2,
            drawQuietZones: false);
        bitmap.Width.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Base64QRCode_GetGraphic_accepts_colors_and_an_image_type()
    {
        using var data = CreateData();
        var text = new Base64QRCode(data).GetGraphic(4, SKColors.Black, SKColors.White, drawQuietZones: false, Base64QRCode.ImageType.Png);
        text.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ArtQRCode_GetGraphic_accepts_pixels_without_naming_an_optional()
    {
        using var data = CreateData();
        using var bitmap = new ArtQRCode(data).GetGraphic(4);
        bitmap.Width.ShouldBeGreaterThan(0);
    }
}
