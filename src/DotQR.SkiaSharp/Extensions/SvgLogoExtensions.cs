using SkiaSharp;
using static DotQR.SvgQRCode;

namespace DotQR;

/// <summary>
/// Creates <see cref="SvgLogo"/> instances from SkiaSharp bitmaps.
/// </summary>
public static class SvgLogoExtensions
{
    /// <summary>
    /// Creates an <see cref="SvgLogo"/> from a rasterized icon by encoding it as PNG.
    /// </summary>
    /// <param name="icon">The icon to embed.</param>
    /// <param name="iconSizePercent">Degree of percentage coverage of the QR code by the logo.</param>
    /// <param name="fillLogoBackground">If true, no QR modules will be rendered behind the logo.</param>
    /// <returns>The logo, ready to pass to <see cref="SvgQRCode.GetGraphic(int, RgbaColor, RgbaColor, bool, SizingMode, SvgLogo?)"/>.</returns>
    public static SvgLogo ToSvgLogo(this SKBitmap icon, int iconSizePercent = 15, bool fillLogoBackground = true)
    {
        ArgumentNullException.ThrowIfNull(icon);

        using var data = icon.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new NotSupportedException("The icon could not be encoded as PNG.");
        return new SvgLogo(data.ToArray(), iconSizePercent, fillLogoBackground);
    }
}
