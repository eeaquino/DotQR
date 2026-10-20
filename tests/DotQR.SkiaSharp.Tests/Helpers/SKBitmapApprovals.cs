using System.ComponentModel;
using SkiaSharp;
using Shouldly;

namespace DotQRTests.SkiaSharp.Helpers;

/// <summary>
/// Compares <see cref="SKBitmap"/> output against approved PNG baselines.
/// </summary>
/// <remarks>
/// PNG bytes pin the SkiaSharp version, so these baselines are a change detector rather than an
/// independent oracle. <see cref="RasterInvariantTests"/> carries the semantic assertions that
/// still hold after a baseline is regenerated.
/// </remarks>
[ShouldlyMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SKBitmapApprovals
{
    /// <summary>Asserts that the bitmap matches the approved baseline.</summary>
    /// <param name="bitmap">The produced bitmap.</param>
    /// <param name="discriminator">Optional discriminator appended to the file name.</param>
    public static void ShouldMatchApproved(this SKBitmap bitmap, string? discriminator = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The bitmap could not be encoded as PNG.");

        // Delegates to the shared byte approval; [ShouldlyMethods] makes the stack walk skip this
        // wrapper so the approved file is named after the calling test.
        data.ToArray().ShouldMatchApproved("png", discriminator);
    }
}
