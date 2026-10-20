using System.Reflection;
using SkiaSharp;

namespace DotQRTests.SkiaSharp.Helpers;

/// <summary>
/// Embedded test assets.
/// </summary>
public static class HelperFunctions
{
    /// <summary>Gets the embedded PNG icon as an <see cref="SKBitmap"/>.</summary>
    public static SKBitmap GetIconBitmap()
    {
        using var stream = OpenResource("DotQRTests.SkiaSharp.assets.noun_software engineer_2909346.png");
        return SKBitmap.Decode(stream) ?? throw new InvalidOperationException("The icon could not be decoded.");
    }

    /// <summary>Gets the embedded PNG icon as a byte array.</summary>
    public static byte[] GetIconBytes()
    {
        using var stream = OpenResource("DotQRTests.SkiaSharp.assets.noun_software engineer_2909346.png");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Gets the embedded SVG logo as a string.</summary>
    public static string GetIconSvg()
    {
        using var stream = OpenResource("DotQRTests.SkiaSharp.assets.noun_Scientist_2909361.svg");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Stream OpenResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
    }
}
