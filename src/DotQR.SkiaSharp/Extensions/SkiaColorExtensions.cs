using SkiaSharp;

namespace DotQR;

/// <summary>
/// Converts between <see cref="RgbaColor"/> and SkiaSharp colors.
/// </summary>
public static class SkiaColorExtensions
{
    /// <summary>Converts an <see cref="RgbaColor"/> to an <see cref="SKColor"/>.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The equivalent SkiaSharp color.</returns>
    public static SKColor ToSKColor(this RgbaColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>Converts an <see cref="SKColor"/> to an <see cref="RgbaColor"/>.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The equivalent <see cref="RgbaColor"/>.</returns>
    public static RgbaColor ToRgbaColor(this SKColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);
}
